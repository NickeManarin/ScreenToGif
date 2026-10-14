using ScreenToGif.Linux.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ScreenToGif.Linux.Services;

public sealed class FfmpegExporter
{
    private readonly IFfmpegTool _ffmpeg;

    public FfmpegExporter(IFfmpegTool ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public async Task ExportAsync(IEnumerable<EditorFrame> sourceFrames, string outputPath, CancellationToken cancellationToken = default)
    {
        var frames = sourceFrames.ToArray();

        if (frames.Length == 0)
            throw new InvalidOperationException("There are no frames to export.");

        foreach (var frame in frames)
        {
            if (!File.Exists(frame.FilePath))
                throw new FileNotFoundException("A frame file is missing.", frame.FilePath);
        }

        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        if (extension == ".gif" && frames.Any(frame => frame.DelayMs < 10))
            throw new InvalidOperationException("GIF frame delays must be at least 10 ms. Increase the delay or export APNG, MP4, or WebM.");

        var listPath = Path.Combine(Path.GetTempPath(), $"screentogif-linux-{Guid.NewGuid():N}.txt");
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;
        var stagedOutputPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileNameWithoutExtension(outputPath)}.{Guid.NewGuid():N}.tmp{Path.GetExtension(outputPath)}");
        var prepared = await PrepareFramesAsync(frames, extension, cancellationToken);

        try
        {
            var isVideo = extension is ".mp4" or ".webm";
            await File.WriteAllTextAsync(listPath, BuildConcatList(prepared.Frames, isVideo), cancellationToken);

            var arguments = new List<string>
            {
                "-y", "-hide_banner", "-loglevel", "error",
                "-f", "concat",
                "-safe", "0",
                "-i", listPath,
                "-fps_mode", "vfr",
                "-an"
            };

            AddCodecArguments(arguments, stagedOutputPath, prepared.Frames[^1].DelayMs);
            arguments.Add(stagedOutputPath);

            await _ffmpeg.RunFfmpegCheckedAsync(arguments, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(stagedOutputPath, outputPath, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(listPath);
            }
            catch (IOException)
            {
                // The export result is more important than cleanup of this temporary list.
            }

            try
            {
                File.Delete(stagedOutputPath);
            }
            catch (IOException)
            {
                // Cleanup is best-effort; the GUID path cannot collide with a later export.
            }

            TryDeleteDirectory(prepared.DirectoryPath);
        }
    }

    private async Task<PreparedExportFrames> PrepareFramesAsync(
        IReadOnlyList<EditorFrame> frames,
        string extension,
        CancellationToken cancellationToken)
    {
        var media = new FrameMediaInfo[frames.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, frames.Count),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
            async (index, token) => media[index] = await ReadMediaInfoAsync(frames[index].FilePath, token));

        var canvasWidth = media.Max(info => info.Width);
        var canvasHeight = media.Max(info => info.Height);
        if (extension is ".mp4" or ".webm")
        {
            canvasWidth += canvasWidth % 2;
            canvasHeight += canvasHeight % 2;
        }

        var first = media[0];
        var needsNormalization = media.Any(info =>
            info.Width != canvasWidth || info.Height != canvasHeight ||
            !string.Equals(info.PixelFormat, first.PixelFormat, StringComparison.Ordinal));
        if (!needsNormalization)
            return new PreparedExportFrames(frames, null);

        var directory = Path.Combine(Path.GetTempPath(), $"screentogif-linux-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var normalized = new EditorFrame[frames.Count];
        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, frames.Count),
                new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
                async (index, token) =>
                {
                    var output = Path.Combine(directory, $"{index:000000}.png");
                    await _ffmpeg.RunFfmpegCheckedAsync(
                    [
                        "-y", "-hide_banner", "-loglevel", "error", "-i", frames[index].FilePath,
                        "-vf", $"pad={canvasWidth}:{canvasHeight}:(ow-iw)/2:(oh-ih)/2:color=black,format=rgba",
                        "-frames:v", "1", output
                    ], token);
                    if (!File.Exists(output))
                        throw new IOException("FFmpeg did not produce a normalized export frame.");
                    normalized[index] = new EditorFrame(output, frames[index].DelayMs);
                });
            return new PreparedExportFrames(normalized, directory);
        }
        catch
        {
            TryDeleteDirectory(directory);
            throw;
        }
    }

    private async Task<FrameMediaInfo> ReadMediaInfoAsync(string path, CancellationToken cancellationToken)
    {
        var result = await _ffmpeg.RunFfprobeCheckedAsync(
        [
            "-v", "error", "-select_streams", "v:0",
            "-show_entries", "stream=width,height,pix_fmt", "-of", "json", path
        ], cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            if (!document.RootElement.TryGetProperty("streams", out var streams) ||
                streams.ValueKind != JsonValueKind.Array || streams.GetArrayLength() == 0)
                throw new InvalidDataException($"Could not read export frame metadata for '{Path.GetFileName(path)}'.");
            var stream = streams[0];
            string? pixelFormat = null;
            if (stream.ValueKind == JsonValueKind.Object &&
                stream.TryGetProperty("pix_fmt", out var pixelFormatValue) &&
                pixelFormatValue.ValueKind == JsonValueKind.String)
                pixelFormat = pixelFormatValue.GetString();
            if (stream.ValueKind != JsonValueKind.Object ||
                !stream.TryGetProperty("width", out var widthValue) || !widthValue.TryGetInt32(out var width) || width <= 0 ||
                !stream.TryGetProperty("height", out var heightValue) || !heightValue.TryGetInt32(out var height) || height <= 0 ||
                string.IsNullOrWhiteSpace(pixelFormat))
                throw new InvalidDataException($"Could not read export frame metadata for '{Path.GetFileName(path)}'.");
            return new FrameMediaInfo(
                width,
                height,
                pixelFormat);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Could not read export frame metadata for '{Path.GetFileName(path)}'.", ex);
        }
    }

    /// <summary>
    /// Lists the frames for FFmpeg's concat demuxer. The demuxer ignores the last entry's duration,
    /// and a repeated final entry is encoded as a frame of its own. An animated image must hold
    /// exactly the project's frames, so it gets one entry per frame and its last delay is set
    /// through the muxer instead. A video has no per-frame delay to set, so it keeps the repeated
    /// entry: the extra frame is a copy of the last one and is what makes the last frame last.
    /// </summary>
    private static string BuildConcatList(IReadOnlyList<EditorFrame> frames, bool repeatLastFrame)
    {
        var builder = new StringBuilder();

        foreach (var frame in frames)
        {
            builder.Append("file ").AppendLine(QuoteConcatPath(frame.FilePath));
            builder.Append("duration ").AppendLine((Math.Max(1, frame.DelayMs) / 1000d).ToString("0.######", CultureInfo.InvariantCulture));
        }

        if (repeatLastFrame)
            builder.Append("file ").AppendLine(QuoteConcatPath(frames[^1].FilePath));

        return builder.ToString();
    }

    private static string QuoteConcatPath(string path) =>
        $"'{Path.GetFullPath(path).Replace("'", "'\\''", StringComparison.Ordinal)}'";

    private const int MaximumGifFinalDelayCentiseconds = 65535;
    private const double MaximumApngFinalDelaySeconds = 65535;

    private static void AddCodecArguments(ICollection<string> arguments, string outputPath, int lastDelayMs)
    {
        switch (Path.GetExtension(outputPath).ToLowerInvariant())
        {
            case ".gif":
                arguments.Add("-loop");
                arguments.Add("0");
                // The muxers refuse a final delay outside their range rather than clamping it, as
                // they do for every other frame, so it is clamped here to the same limits.
                arguments.Add("-final_delay");
                arguments.Add(Math.Clamp((int)Math.Round(lastDelayMs / 10d), 1, MaximumGifFinalDelayCentiseconds)
                    .ToString(CultureInfo.InvariantCulture));
                break;
            case ".mp4":
                arguments.Add("-c:v");
                arguments.Add("libx264");
                arguments.Add("-pix_fmt");
                arguments.Add("yuv420p");
                arguments.Add("-movflags");
                arguments.Add("+faststart");
                break;
            case ".webm":
                arguments.Add("-c:v");
                arguments.Add("libvpx-vp9");
                arguments.Add("-pix_fmt");
                arguments.Add("yuv420p");
                break;
            case ".apng":
                arguments.Add("-plays");
                arguments.Add("0");
                arguments.Add("-final_delay");
                arguments.Add(Math.Min(Math.Max(1, lastDelayMs) / 1000d, MaximumApngFinalDelaySeconds)
                    .ToString("0.######", CultureInfo.InvariantCulture));
                break;
            default:
                throw new NotSupportedException("The Linux editor currently exports GIF, APNG, MP4, and WebM.");
        }
    }

    private static void TryDeleteDirectory(string? path)
    {
        if (path is null)
            return;
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed record FrameMediaInfo(int Width, int Height, string PixelFormat);
    private sealed record PreparedExportFrames(IReadOnlyList<EditorFrame> Frames, string? DirectoryPath);
}
