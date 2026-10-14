using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public enum SlideDirection
{
    Left,
    Right,
    Up,
    Down
}

public sealed record TransitionRequest(int FrameCount, int DurationMs, SlideDirection? SlideDirection = null);

public sealed class FrameTransitionService(IFfmpegTool ffmpeg)
{
    public async Task<IReadOnlyList<EditorFrame>> GenerateAsync(
        EditorFrame first,
        EditorFrame second,
        EditorWorkspace workspace,
        TransitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FrameCount is < 1 or > 120)
            throw new ArgumentOutOfRangeException(nameof(request), "Transition frame count must be between 1 and 120.");
        if (request.DurationMs < request.FrameCount)
            throw new ArgumentOutOfRangeException(nameof(request), "Transition duration must provide at least 1 ms per generated frame.");

        var firstSize = await GetSizeAsync(first.FilePath, cancellationToken);
        var secondSize = await GetSizeAsync(second.FilePath, cancellationToken);
        if (firstSize != secondSize)
            throw new InvalidOperationException("Transition frames must have matching dimensions. Resize or crop them first.");
        if (firstSize.Width is < 1 or > EditorResourceLimits.MaximumMediaDimension ||
            firstSize.Height is < 1 or > EditorResourceLimits.MaximumMediaDimension)
            throw new InvalidOperationException(
                $"Transition frame dimensions must be between 1 and {EditorResourceLimits.MaximumMediaDimension} pixels.");
        if ((long)firstSize.Width * firstSize.Height * request.FrameCount > EditorResourceLimits.MaximumImportedPixels)
            throw new InvalidOperationException(
                "The generated transition exceeds the editor's decoded-pixel limit; reduce its dimensions or frame count.");

        var operationPath = workspace.CreateBatch(EditorArtifactKind.Transitions);
        var frames = new List<EditorFrame>(request.FrameCount);
        var baseDelay = request.DurationMs / request.FrameCount;
        var remainder = request.DurationMs % request.FrameCount;

        try
        {
            for (var index = 0; index < request.FrameCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var progress = (index + 1d) / (request.FrameCount + 1d);
                var output = Path.Combine(operationPath, $"{index:000000}.png");
                var transition = request.SlideDirection switch
                {
                    SlideDirection.Left => "slideleft",
                    SlideDirection.Right => "slideright",
                    SlideDirection.Up => "slideup",
                    SlideDirection.Down => "slidedown",
                    _ => "fade"
                };
                var start = progress.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);
                var end = (progress + 0.02).ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);

                await ffmpeg.RunFfmpegCheckedAsync(
                [
                    "-y", "-hide_banner", "-loglevel", "error",
                    "-loop", "1", "-framerate", "50", "-t", "2", "-i", first.FilePath,
                    "-loop", "1", "-framerate", "50", "-t", "2", "-i", second.FilePath,
                    "-filter_complex", $"[0:v][1:v]xfade=transition={transition}:duration=1:offset=0,trim=start={start}:end={end},setpts=PTS-STARTPTS",
                    "-frames:v", "1", output
                ], cancellationToken);

                if (!File.Exists(output))
                    throw new IOException("FFmpeg did not produce the transition frame.");

                frames.Add(new EditorFrame(output, baseDelay + (index < remainder ? 1 : 0)));
            }

            return frames;
        }
        catch
        {
            foreach (var frame in frames)
                frame.Dispose();
            TryDelete(operationPath);
            throw;
        }
    }

    private async Task<(int Width, int Height)> GetSizeAsync(string path, CancellationToken cancellationToken)
    {
        var result = await ffmpeg.RunFfprobeCheckedAsync(
        [
            "-v", "error", "-select_streams", "v:0",
            "-show_entries", "stream=width,height", "-of", "csv=s=x:p=0", path
        ], cancellationToken);
        var parts = result.StandardOutput.Trim().Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
            throw new InvalidDataException("Could not read frame dimensions.");
        return (width, height);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
