using ScreenToGif.Linux.Models;
using System.Globalization;
using System.Text.Json;

namespace ScreenToGif.Linux.Services;

public sealed class MediaImporter
{
    private static readonly HashSet<string> StillImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".jpeg", ".jpg", ".png", ".webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".apng", ".avi", ".avif", ".gif", ".mkv", ".mov", ".mp4", ".webm", ".wmv"
    };

    private readonly IFfmpegTool _ffmpeg;

    public MediaImporter(IFfmpegTool ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public Task<IReadOnlyList<EditorFrame>> ImportAsync(IEnumerable<string> sourcePaths, EditorWorkspace workspace, CancellationToken cancellationToken = default) =>
        ImportAsync(sourcePaths, workspace, existingFrameCount: 0, cancellationToken);

    public async Task<IReadOnlyList<EditorFrame>> ImportAsync(
        IEnumerable<string> sourcePaths,
        EditorWorkspace workspace,
        int existingFrameCount,
        CancellationToken cancellationToken = default)
    {
        var paths = sourcePaths.ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        if (paths.Length == 0)
            return [];
        EditorResourceLimits.EnsureCanInsertFrames(existingFrameCount, 1, "Importing media");

        var plans = new List<ImportPlan>(paths.Length);
        var plannedFrameCount = 0;
        var reservedPixels = 0L;
        foreach (var sourcePath in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Media file was not found.", sourcePath);

            var extension = Path.GetExtension(sourcePath);
            var isStillImage = StillImageExtensions.Contains(extension);
            if (!isStillImage && !VideoExtensions.Contains(extension))
                throw new NotSupportedException($"The Linux editor does not yet support '{extension}' media.");

            var media = await GetMediaInfoAsync(sourcePath, countFrames: !isStillImage, cancellationToken);
            ReserveImportBudget(media, existingFrameCount, plannedFrameCount, ref reservedPixels);
            plannedFrameCount += media.FrameCount;
            plans.Add(new ImportPlan(sourcePath, isStillImage, media));
        }

        var importWorkspace = workspace.CreateBatch(EditorArtifactKind.Imports);
        var result = new List<EditorFrame>(plannedFrameCount);
        var generatedPixels = 0L;
        var reservedArchiveBytes = 0L;

        try
        {
            for (var sourceIndex = 0; sourceIndex < plans.Count; sourceIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var firstNewFrame = result.Count;
                var plan = plans[sourceIndex];

                if (plan.IsStillImage)
                    await ImportStillImageAsync(plan.Path, importWorkspace, sourceIndex, result, cancellationToken);
                else
                    await ImportVideoAsync(plan.Path, importWorkspace, sourceIndex, result, existingFrameCount, plan.Media.FallbackDelayMs, cancellationToken);

                var newFrames = result.Skip(firstNewFrame);
                ReserveGeneratedPixelBudget(newFrames, ref generatedPixels);
                ReserveArchiveBytes(newFrames, ref reservedArchiveBytes);
            }

            return result;
        }
        catch
        {
            foreach (var frame in result)
                frame.Dispose();

            TryDeleteDirectory(importWorkspace);
            throw;
        }
    }

    private async Task ImportStillImageAsync(string sourcePath, string workspacePath, int sourceIndex, ICollection<EditorFrame> frames, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(workspacePath, $"source-{sourceIndex:000}-frame-000001.png");

        await _ffmpeg.RunFfmpegCheckedAsync(
        [
            "-y", "-hide_banner", "-loglevel", "error",
            "-i", sourcePath,
            "-frames:v", "1",
            "-vf", "format=rgba",
            outputPath
        ], cancellationToken);

        frames.Add(new EditorFrame(outputPath, 100));
    }

    private async Task ImportVideoAsync(
        string sourcePath,
        string workspacePath,
        int sourceIndex,
        ICollection<EditorFrame> frames,
        int existingFrameCount,
        int fallbackDelayMs,
        CancellationToken cancellationToken)
    {
        var prefix = $"source-{sourceIndex:000}-frame-";
        var outputPattern = Path.Combine(workspacePath, prefix + "%06d.png");
        var timings = await GetFrameTimingsAsync(sourcePath, cancellationToken);

        await _ffmpeg.RunFfmpegCheckedAsync(
        [
            "-y", "-hide_banner", "-loglevel", "error",
            "-i", sourcePath,
            "-map", "0:v:0",
            "-fps_mode", "passthrough",
            "-start_number", "1",
            "-frames:v", (EditorResourceLimits.MaximumProjectFrames + 1).ToString(CultureInfo.InvariantCulture),
            outputPattern
        ], cancellationToken);

        var extracted = Directory.GetFiles(workspacePath, prefix + "*.png").OrderBy(path => path, StringComparer.Ordinal).ToArray();

        if (extracted.Length == 0)
            throw new InvalidOperationException($"FFmpeg did not produce frames for '{sourcePath}'.");
        EditorResourceLimits.EnsureCanInsertFrames(existingFrameCount + frames.Count, extracted.Length, "Importing media");

        var delays = Enumerable.Range(0, extracted.Length)
            .Select(index => GetFrameDelay(timings, index))
            .ToArray();

        for (var index = 0; index < extracted.Length; index++)
        {
            var delay = delays[index] > 0 ? delays[index] : fallbackDelayMs;
            frames.Add(new EditorFrame(extracted[index], delay));
        }
    }

    private async Task<MediaInfo> GetMediaInfoAsync(string sourcePath, bool countFrames, CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "-v", "error", "-select_streams", "v:0"
        };
        if (countFrames)
            arguments.Add("-count_frames");
        arguments.AddRange(
        [
            "-show_entries",
            countFrames
                ? "stream=width,height,nb_read_frames,nb_frames,avg_frame_rate,r_frame_rate:format=duration"
                : "stream=width,height",
            "-of", "json", sourcePath
        ]);

        var result = await _ffmpeg.RunFfprobeAsync(arguments, cancellationToken);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Unable to inspect '{Path.GetFileName(sourcePath)}' before import.");
        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            if (!document.RootElement.TryGetProperty("streams", out var streams) ||
                streams.ValueKind != JsonValueKind.Array ||
                streams.GetArrayLength() == 0)
                throw new InvalidOperationException($"'{Path.GetFileName(sourcePath)}' does not contain a readable video stream.");
            var stream = streams[0];
            var width = ReadPositiveInt(stream, "width");
            var height = ReadPositiveInt(stream, "height");
            var frameCount = countFrames ? ReadFrameCount(stream, document.RootElement) : 1;
            var rate = ReadRate(stream);
            return new MediaInfo(width, height, frameCount,
                rate > 0 ? Math.Max(1, (int)Math.Round(1000d / rate)) : 100);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"FFprobe returned invalid media metadata for '{Path.GetFileName(sourcePath)}'.", ex);
        }
    }

    private static void ReserveImportBudget(MediaInfo media, int existingFrameCount, int importedFrameCount, ref long reservedPixels)
    {
        if (media.Width is < 1 or > EditorResourceLimits.MaximumMediaDimension ||
            media.Height is < 1 or > EditorResourceLimits.MaximumMediaDimension)
            throw new InvalidOperationException($"Media dimensions must be between 1 and {EditorResourceLimits.MaximumMediaDimension} pixels.");
        if (media.FrameCount < 1)
            throw new InvalidOperationException("Media metadata must report at least one frame.");
        EditorResourceLimits.EnsureCanInsertFrames(existingFrameCount + importedFrameCount, media.FrameCount, "Importing media");
        var sourcePixels = (long)media.Width * media.Height * media.FrameCount;
        if (sourcePixels > EditorResourceLimits.MaximumImportedPixels - reservedPixels)
            throw new InvalidOperationException("The import is too large for the editor; reduce its dimensions, duration, or frame rate.");
        reservedPixels += sourcePixels;
    }

    private static void ReserveArchiveBytes(IEnumerable<EditorFrame> frames, ref long reservedBytes)
    {
        foreach (var frame in frames)
        {
            var length = new FileInfo(frame.FilePath).Length;
            if (length > EditorResourceLimits.MaximumProjectFrameBytes ||
                length > EditorResourceLimits.MaximumProjectArchiveBytes -
                EditorResourceLimits.MaximumProjectManifestBytes - reservedBytes)
                throw new InvalidOperationException("The imported frames are too large for a reopenable project; reduce their dimensions, duration, or frame rate.");
            reservedBytes += length;
        }
    }

    private static void ReserveGeneratedPixelBudget(IEnumerable<EditorFrame> frames, ref long reservedPixels)
    {
        foreach (var frame in frames)
        {
            var size = EditorProjectBudget.ReadPngDimensions(frame.FilePath);
            var pixels = (long)size.Width * size.Height;
            if (pixels > EditorResourceLimits.MaximumImportedPixels - reservedPixels)
                throw new InvalidOperationException("The generated frames exceed the editor's decoded-pixel limit; reduce their dimensions, duration, or frame rate.");
            reservedPixels += pixels;
        }
    }

    private static int ReadPositiveInt(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) && number > 0)
            return number;
        throw new InvalidOperationException($"Media metadata is missing a valid {name} value.");
    }

    private static int ReadFrameCount(JsonElement stream, JsonElement root)
    {
        if (TryPositiveInt(stream, "nb_read_frames", out var count) ||
            TryPositiveInt(stream, "nb_frames", out count))
            return count;

        var rate = ReadRate(stream);
        if (rate > 0 && root.TryGetProperty("format", out var format) &&
            TryJsonDouble(format, "duration", out var duration) && duration > 0)
        {
            var estimate = Math.Ceiling(duration * rate);
            return estimate >= int.MaxValue ? int.MaxValue : (int)estimate;
        }

        throw new InvalidOperationException("Media frame count could not be determined safely before import.");
    }

    private static bool TryPositiveInt(JsonElement element, string name, out int number)
    {
        number = 0;
        if (!element.TryGetProperty(name, out var value))
            return false;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out number) && number > 0,
            JsonValueKind.String => int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number > 0,
            _ => false
        };
    }

    private static double ReadRate(JsonElement stream)
    {
        foreach (var name in new[] { "avg_frame_rate", "r_frame_rate" })
        {
            if (stream.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var rate = ParseRate(value.GetString() ?? string.Empty);
                if (rate > 0 && double.IsFinite(rate))
                    return rate;
            }
        }

        return 0;
    }

    private static bool TryJsonDouble(JsonElement element, string name, out double number)
    {
        number = 0;
        return element.TryGetProperty(name, out var value) &&
               value.ValueKind == JsonValueKind.String &&
               double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
               double.IsFinite(number);
    }

    private async Task<IReadOnlyList<FrameTiming>> GetFrameTimingsAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var result = await _ffmpeg.RunFfprobeAsync(
        [
            "-v", "error", "-select_streams", "v:0",
            "-show_entries", "frame=best_effort_timestamp_time,pts_time,duration_time,pkt_duration_time",
            "-of", "json", sourcePath
        ], cancellationToken);
        if (result.ExitCode != 0)
            return [];
        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            if (!document.RootElement.TryGetProperty("frames", out var frames) ||
                frames.ValueKind != JsonValueKind.Array)
                return [];
            return frames.EnumerateArray().Select(frame =>
            {
                var timestamp = TryTime(frame, "best_effort_timestamp_time", out var bestEffortTimestamp)
                    ? bestEffortTimestamp
                    : TryTime(frame, "pts_time", out var presentationTimestamp) ? presentationTimestamp : (double?)null;
                if (TryDuration(frame, "duration_time", out var duration) ||
                    TryDuration(frame, "pkt_duration_time", out duration))
                    return new FrameTiming(timestamp, Math.Max(1, (int)Math.Round(duration * 1000d)));
                return new FrameTiming(timestamp, 0);
            }).ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int GetFrameDelay(IReadOnlyList<FrameTiming> timings, int index)
    {
        if (index >= timings.Count)
            return 0;
        if (index + 1 < timings.Count &&
            timings[index].TimestampSeconds is { } current &&
            timings[index + 1].TimestampSeconds is { } next &&
            next > current)
            return Math.Max(1, (int)Math.Round((next - current) * 1000d));
        return timings[index].DurationMs;
    }

    private static bool TryTime(JsonElement frame, string name, out double time)
    {
        time = 0;
        return frame.ValueKind == JsonValueKind.Object &&
               frame.TryGetProperty(name, out var value) &&
               value.ValueKind == JsonValueKind.String &&
               double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out time) &&
               double.IsFinite(time);
    }

    private static bool TryDuration(JsonElement frame, string name, out double duration)
    {
        duration = 0;
        return frame.ValueKind == JsonValueKind.Object &&
               frame.TryGetProperty(name, out var value) &&
               value.ValueKind == JsonValueKind.String &&
               double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out duration) &&
               double.IsFinite(duration) &&
               duration > 0;
    }

    private sealed record FrameTiming(double? TimestampSeconds, int DurationMs);
    private sealed record MediaInfo(int Width, int Height, int FrameCount, int FallbackDelayMs);
    private sealed record ImportPlan(string Path, bool IsStillImage, MediaInfo Media);

    private static double ParseRate(string value)
    {
        var parts = value.Trim().Split('/');

        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) &&
            denominator != 0)
            return numerator / denominator;

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) ? rate : 0;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Temporary import workspaces are best-effort cleanup only.
        }
        catch (UnauthorizedAccessException)
        {
            // Temporary import workspaces are best-effort cleanup only.
        }
    }
}
