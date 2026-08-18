using ScreenToGif.Linux.Models;
using System.Globalization;

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

    private readonly FfmpegTool _ffmpeg;

    public MediaImporter(FfmpegTool ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public async Task<IReadOnlyList<EditorFrame>> ImportAsync(IEnumerable<string> sourcePaths, string workspacePath, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(workspacePath);
        var importWorkspace = Path.Combine(workspacePath, $"import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(importWorkspace);

        var result = new List<EditorFrame>();
        var sourceIndex = 0;

        try
        {
            foreach (var sourcePath in sourcePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException("Media file was not found.", sourcePath);

                var extension = Path.GetExtension(sourcePath);

                if (StillImageExtensions.Contains(extension))
                    await ImportStillImageAsync(sourcePath, importWorkspace, sourceIndex, result, cancellationToken);
                else if (VideoExtensions.Contains(extension))
                    await ImportVideoAsync(sourcePath, importWorkspace, sourceIndex, result, cancellationToken);
                else
                    throw new NotSupportedException($"The Linux editor does not yet support '{extension}' media.");

                sourceIndex++;
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

    private async Task ImportVideoAsync(string sourcePath, string workspacePath, int sourceIndex, ICollection<EditorFrame> frames, CancellationToken cancellationToken)
    {
        var prefix = $"source-{sourceIndex:000}-frame-";
        var outputPattern = Path.Combine(workspacePath, prefix + "%06d.png");
        var delay = await GetFrameDelayAsync(sourcePath, cancellationToken);

        await _ffmpeg.RunFfmpegCheckedAsync(
        [
            "-y", "-hide_banner", "-loglevel", "error",
            "-i", sourcePath,
            "-map", "0:v:0",
            "-fps_mode", "passthrough",
            "-start_number", "1",
            outputPattern
        ], cancellationToken);

        var extracted = Directory.GetFiles(workspacePath, prefix + "*.png").OrderBy(path => path, StringComparer.Ordinal).ToArray();

        if (extracted.Length == 0)
            throw new InvalidOperationException($"FFmpeg did not produce frames for '{sourcePath}'.");

        foreach (var path in extracted)
            frames.Add(new EditorFrame(path, delay));
    }

    private async Task<int> GetFrameDelayAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var result = await _ffmpeg.RunFfprobeAsync(
        [
            "-v", "error",
            "-select_streams", "v:0",
            "-show_entries", "stream=avg_frame_rate,r_frame_rate",
            "-of", "default=noprint_wrappers=1:nokey=1",
            sourcePath
        ], cancellationToken);

        var rate = result.StandardOutput
            .Split([ '\r', '\n' ], StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseRate)
            .FirstOrDefault(value => value > 0);

        return rate > 0 ? Math.Max(1, (int)Math.Round(1000d / rate)) : 100;
    }

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
