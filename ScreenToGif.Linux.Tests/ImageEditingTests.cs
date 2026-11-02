using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class ImageEditingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-image-tests-{Guid.NewGuid():N}");
    private readonly FfmpegTool _ffmpeg = new();
    private readonly EditorWorkspace _workspace;

    public ImageEditingTests() => _workspace = EditorWorkspace.Create(_root);

    [Fact]
    public async Task Resize_crop_flip_and_rotate_transform_real_non_square_pngs()
    {
        var source = await CreatePatternAsync();
        var frame = new EditorFrame(source, 123);
        var service = new FrameTransformService(_ffmpeg);

        var resized = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.Resize, 12, 8));
        Assert.Equal((12, 8), await GetSizeAsync(resized[0].FilePath));

        var cropped = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.Crop, 3, 2, 1, 1));
        Assert.Equal((3, 2), await GetSizeAsync(cropped[0].FilePath));
        Assert.Equal(await PixelDigestAtAsync(source, 1, 1), await PixelDigestAtAsync(cropped[0].FilePath, 0, 0));

        var horizontal = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.FlipHorizontal));
        var vertical = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.FlipVertical));
        Assert.Equal(await PixelDigestAtAsync(source, 5, 0), await PixelDigestAtAsync(horizontal[0].FilePath, 0, 0));
        Assert.Equal(await PixelDigestAtAsync(source, 0, 3), await PixelDigestAtAsync(vertical[0].FilePath, 0, 0));

        var clockwise = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.RotateClockwise));
        var counterClockwise = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.RotateCounterClockwise));
        Assert.Equal((4, 6), await GetSizeAsync(clockwise[0].FilePath));
        Assert.Equal((4, 6), await GetSizeAsync(counterClockwise[0].FilePath));
        Assert.Equal(await PixelDigestAtAsync(source, 0, 0), await PixelDigestAtAsync(clockwise[0].FilePath, 3, 0));
        Assert.Equal(await PixelDigestAtAsync(source, 0, 0), await PixelDigestAtAsync(counterClockwise[0].FilePath, 0, 5));

        var black = await CreateColorAsync("black", "black.png");
        var gray = await CreateColorAsync("#555555", "gray.png");
        var bordered = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.Border, 3));
        Assert.Equal((12, 10), await GetSizeAsync(bordered[0].FilePath));
        Assert.Equal(await PixelDigestAtAsync(black, 0, 0), await PixelDigestAtAsync(bordered[0].FilePath, 0, 0));
        Assert.Equal(await PixelDigestAtAsync(source, 0, 0), await PixelDigestAtAsync(bordered[0].FilePath, 3, 3));
        var shadowed = await service.TransformAsync([frame], _workspace, new FrameTransformRequest(FrameTransformKind.Shadow));
        Assert.Equal((22, 20), await GetSizeAsync(shadowed[0].FilePath));
        Assert.Equal(await PixelDigestAtAsync(source, 0, 0), await PixelDigestAtAsync(shadowed[0].FilePath, 0, 0));
        Assert.Equal(await PixelDigestAtAsync(gray, 0, 0), await PixelDigestAtAsync(shadowed[0].FilePath, 21, 19));
        Assert.Equal(123, frame.DelayMs);
    }

    [Fact]
    public async Task Cancellation_after_staging_output_removes_parallel_transform_batch()
    {
        using var cancellation = new CancellationTokenSource();
        var source = await CreatePatternAsync();
        var frames = Enumerable.Range(0, 6).Select(_ => new EditorFrame(source, 100)).ToArray();
        var service = new FrameTransformService(new CancelingFfmpeg(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.TransformAsync(frames, _workspace, new FrameTransformRequest(FrameTransformKind.FlipHorizontal), cancellation.Token));

        var edits = Path.Combine(_root, "edits");
        Assert.False(Directory.Exists(edits) && Directory.EnumerateFiles(edits, "*", SearchOption.AllDirectories).Any());
        Assert.All(frames, frame => Assert.Equal(source, frame.FilePath));
    }

    [Fact]
    public async Task Invalid_parameters_and_partial_failure_leave_inputs_and_staging_clean()
    {
        var source = await CreatePatternAsync();
        var frames = new[] { new EditorFrame(source, 100), new EditorFrame(source, 200) };
        var service = new FrameTransformService(new FailingFfmpeg());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.TransformAsync(frames, _workspace, new FrameTransformRequest(FrameTransformKind.Crop, 0, 2)));
        var oversized = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.TransformAsync(frames, _workspace,
                new FrameTransformRequest(FrameTransformKind.Resize, FrameTransformService.MaximumDimension, FrameTransformService.MaximumDimension)));
        Assert.Contains("pixels total", oversized.Message);
        var border = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.TransformAsync(frames, _workspace,
                new FrameTransformRequest(FrameTransformKind.Border, FrameTransformService.MaximumDimension)));
        Assert.Contains("Border width", border.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransformAsync(frames, _workspace, new FrameTransformRequest(FrameTransformKind.FlipHorizontal)));

        Assert.True(File.Exists(source));
        Assert.False(Directory.Exists(Path.Combine(_root, "edits")) &&
                     Directory.EnumerateFiles(Path.Combine(_root, "edits"), "*", SearchOption.AllDirectories).Any());
        Assert.Equal([source, source], frames.Select(frame => frame.FilePath));
    }

    [Fact]
    public void History_restores_replacements_selection_and_redo_without_deleting_files()
    {
        var first = Path.Combine(_root, "first.png");
        var transformed = Path.Combine(_root, "transformed.png");
        File.WriteAllText(first, "original");
        File.WriteAllText(transformed, "replacement");
        var history = new FrameEditHistory();
        var before = new EditorSnapshot([new FrameState(first, 80)], [0]);
        var after = new EditorSnapshot([new FrameState(transformed, 80)], [0]);

        history.SetBaseline(before);
        history.Record("Transform", before, after);

        Assert.True(history.TryUndo(out var undone, out _));
        Assert.Equal(before, undone);
        Assert.True(history.TryRedo(out var redone, out _));
        Assert.Equal(after, redone);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(transformed));
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    private async Task<string> CreatePatternAsync()
    {
        var path = Path.Combine(_root, "pattern.png");
        await _ffmpeg.RunFfmpegCheckedAsync(
        [
            "-y", "-hide_banner", "-loglevel", "error",
            "-f", "lavfi", "-i", "testsrc2=size=6x4:rate=1",
            "-frames:v", "1", path
        ]);
        return path;
    }

    private async Task<string> CreateColorAsync(string color, string name)
    {
        var path = Path.Combine(_root, name);
        await _ffmpeg.RunFfmpegCheckedAsync(
            ["-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", $"color={color}:size=2x2", "-frames:v", "1", path]);
        return path;
    }

    private async Task<(int Width, int Height)> GetSizeAsync(string path)
    {
        var result = await _ffmpeg.RunFfprobeCheckedAsync(
        [
            "-v", "error", "-select_streams", "v:0",
            "-show_entries", "stream=width,height", "-of", "csv=s=x:p=0", path
        ]);
        var values = result.StandardOutput.Trim().Split('x').Select(int.Parse).ToArray();
        return (values[0], values[1]);
    }

    private async Task<string> PixelDigestAsync(string path)
    {
        var result = await _ffmpeg.RunFfmpegAsync(
        [
            "-hide_banner", "-loglevel", "error", "-i", path,
            "-f", "framemd5", "-"
        ]);
        Assert.Equal(0, result.ExitCode);
        return result.StandardOutput.Split('\n').Last(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
    }

    private async Task<string> PixelDigestAtAsync(string path, int x, int y)
    {
        var result = await _ffmpeg.RunFfmpegAsync(
        [
            "-hide_banner", "-loglevel", "error", "-i", path,
            "-vf", $"format=rgba,crop=1:1:{x}:{y}", "-f", "framemd5", "-"
        ]);
        Assert.Equal(0, result.ExitCode);
        return result.StandardOutput.Split('\n').Last(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
    }

    private sealed class FailingFfmpeg : IFfmpegTool
    {
        private int _calls;

        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            var output = arguments.Last();
            if (_calls++ == 0)
            {
                File.WriteAllText(output, "staged");
                return Task.CompletedTask;
            }

            throw new InvalidOperationException("simulated FFmpeg failure");
        }
    }

    private sealed class CancelingFfmpeg(CancellationTokenSource cancellation) : IFfmpegTool
    {
        private int _calls;

        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            var output = arguments.Last();
            if (Interlocked.Increment(ref _calls) == 1)
            {
                await File.WriteAllTextAsync(output, "staged", CancellationToken.None);
                cancellation.Cancel();
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
