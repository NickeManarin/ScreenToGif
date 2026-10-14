using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class TransitionEditingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-transition-tests-{Guid.NewGuid():N}");
    private readonly FfmpegTool _ffmpeg = new();
    private readonly EditorWorkspace _workspace;

    public TransitionEditingTests() => _workspace = EditorWorkspace.Create(_root);

    [Fact]
    public async Task Fade_and_every_slide_direction_generate_editable_timed_frames()
    {
        var first = new EditorFrame(await CreateColorAsync("red", "first.png"), 90);
        var second = new EditorFrame(await CreateColorAsync("blue", "second.png"), 110);
        var service = new FrameTransitionService(_ffmpeg);
        var firstPixel = PixelDigestAt(first.FilePath, 0, 0);
        var secondPixel = PixelDigestAt(second.FilePath, 0, 0);

        foreach (var direction in new SlideDirection?[] { null, SlideDirection.Left, SlideDirection.Right, SlideDirection.Up, SlideDirection.Down })
        {
            var generated = await service.GenerateAsync(first, second, _workspace, new TransitionRequest(3, 101, direction));

            Assert.Equal(3, generated.Count);
            Assert.Equal(101, generated.Sum(frame => frame.DelayMs));
            Assert.All(generated, frame => Assert.True(File.Exists(frame.FilePath)));
            Assert.All(generated, frame => Assert.Equal((8, 6), GetSize(frame.FilePath)));
            Assert.Equal(3, generated.Select(frame => PixelDigest(frame.FilePath)).Distinct().Count());
            var lumas = generated.Select(frame => AverageLuma(frame.FilePath)).ToArray();
            Assert.True(lumas[0] > lumas[1] && lumas[1] > lumas[2], $"Expected monotonic red-to-blue progression for {direction?.ToString() ?? "Fade"}.");

            var middle = generated[1].FilePath;
            var corners = new[]
            {
                PixelDigestAt(middle, 0, 0), PixelDigestAt(middle, 7, 0),
                PixelDigestAt(middle, 0, 5), PixelDigestAt(middle, 7, 5)
            };
            var expected = direction switch
            {
                null => Enumerable.Repeat(corners[0], 4).ToArray(),
                SlideDirection.Left => [firstPixel, secondPixel, firstPixel, secondPixel],
                SlideDirection.Right => [secondPixel, firstPixel, secondPixel, firstPixel],
                SlideDirection.Up => [firstPixel, firstPixel, secondPixel, secondPixel],
                SlideDirection.Down => [secondPixel, secondPixel, firstPixel, firstPixel],
                _ => throw new ArgumentOutOfRangeException()
            };
            Assert.Equal(expected, corners);
            if (direction is null)
            {
                Assert.NotEqual(firstPixel, corners[0]);
                Assert.NotEqual(secondPixel, corners[0]);
            }

            var timeline = new List<EditorFrame> { first, second };
            var before = EditorSnapshot.Capture(timeline, [0]);
            FrameSequenceOperations.InsertAfter(timeline, 0, generated);
            var generatedSelection = Enumerable.Range(1, generated.Count).ToArray();
            var after = EditorSnapshot.Capture(timeline, generatedSelection);
            Assert.Equal(
                new[] { first.FilePath }.Concat(generated.Select(frame => frame.FilePath)).Append(second.FilePath),
                timeline.Select(frame => frame.FilePath));
            Assert.Equal([90, 34, 34, 33, 110], timeline.Select(frame => frame.DelayMs));

            var history = new FrameEditHistory();
            history.SetBaseline(before);
            history.Record(direction is null ? "Insert fade" : $"Insert slide {direction}", before, after);
            Assert.True(history.TryUndo(out var undone, out _));
            Assert.Equal(before, undone);
            Assert.True(history.TryRedo(out var redone, out _));
            Assert.Equal(after, redone);
        }
    }

    [Fact]
    public async Task Smooth_loop_inserts_after_the_scope_as_one_reversible_edit()
    {
        var first = new EditorFrame(await CreateColorAsync("red", "loop-first.png"), 70);
        var middle = new EditorFrame(await CreateColorAsync("green", "loop-middle.png"), 80);
        var last = new EditorFrame(await CreateColorAsync("blue", "loop-last.png"), 90);
        var timeline = new List<EditorFrame> { first, middle, last };
        var before = EditorSnapshot.Capture(timeline, [0, 1, 2]);
        var generated = await new FrameTransitionService(_ffmpeg).GenerateAsync(
            last, first, _workspace, new TransitionRequest(2, 81));

        FrameSequenceOperations.InsertAfter(timeline, 2, generated);
        var after = EditorSnapshot.Capture(timeline, [3, 4]);

        Assert.Equal(
            new[] { first.FilePath, middle.FilePath, last.FilePath }
                .Concat(generated.Select(frame => frame.FilePath)),
            timeline.Select(frame => frame.FilePath));
        Assert.Equal([70, 80, 90, 41, 40], timeline.Select(frame => frame.DelayMs));
        var history = new FrameEditHistory();
        history.SetBaseline(before);
        history.Record("Smooth loop", before, after);
        Assert.True(history.TryUndo(out var undone, out _));
        Assert.Equal(before, undone);
        Assert.True(history.TryRedo(out var redone, out _));
        Assert.Equal(after, redone);
    }

    [Fact]
    public async Task Incompatible_frames_and_invalid_duration_fail_before_timeline_state_changes()
    {
        var first = new EditorFrame(await CreateColorAsync("red", "first.png", "8x6"), 90);
        var second = new EditorFrame(await CreateColorAsync("blue", "second.png", "6x8"), 110);
        var service = new FrameTransitionService(_ffmpeg);

        var mismatch = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(first, second, _workspace, new TransitionRequest(2, 100)));
        Assert.Contains("matching dimensions", mismatch.Message);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GenerateAsync(first, first, _workspace, new TransitionRequest(4, 3)));
        Assert.Equal(90, first.DelayMs);
        Assert.Equal(110, second.DelayMs);
    }

    [Fact]
    public async Task Partial_generation_failure_removes_all_staged_outputs()
    {
        var tool = new TransitionFailingFfmpeg();
        var service = new FrameTransitionService(tool);
        var first = new EditorFrame("first.png", 100);
        var second = new EditorFrame("second.png", 100);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(first, second, _workspace, new TransitionRequest(3, 90)));

        var transitionRoot = Path.Combine(_root, "transitions");
        Assert.False(Directory.Exists(transitionRoot) && Directory.EnumerateFiles(transitionRoot, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Cancellation_after_one_generated_frame_removes_the_transition_batch()
    {
        using var cancellation = new CancellationTokenSource();
        var service = new FrameTransitionService(new TransitionCancelingFfmpeg(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateAsync(
            new EditorFrame("first.png", 100),
            new EditorFrame("second.png", 100),
            _workspace,
            new TransitionRequest(3, 90),
            cancellation.Token));

        var transitionRoot = Path.Combine(_root, "transitions");
        Assert.False(Directory.Exists(transitionRoot) && Directory.EnumerateFiles(transitionRoot, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Aggregate_pixel_budget_fails_before_creating_a_transition_batch()
    {
        var tool = new TransitionSizingFfmpeg("8192x8192\n");
        var service = new FrameTransitionService(tool);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(
            new EditorFrame("first.png", 100),
            new EditorFrame("second.png", 100),
            _workspace,
            new TransitionRequest(16, 160)));

        Assert.Contains("decoded-pixel limit", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, tool.GenerationCalls);
        var transitionRoot = Path.Combine(_root, "transitions");
        Assert.False(Directory.Exists(transitionRoot));
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    private async Task<string> CreateColorAsync(string color, string name, string size = "8x6")
    {
        var path = Path.Combine(_root, name);
        await _ffmpeg.RunFfmpegCheckedAsync(
            ["-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", $"color={color}:size={size}", "-frames:v", "1", path]);
        return path;
    }

    private (int Width, int Height) GetSize(string path)
    {
        var result = _ffmpeg.RunFfprobeCheckedAsync(
            ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "csv=s=x:p=0", path]).GetAwaiter().GetResult();
        var parts = result.StandardOutput.Trim().Split('x').Select(int.Parse).ToArray();
        return (parts[0], parts[1]);
    }

    private string PixelDigest(string path)
    {
        var result = _ffmpeg.RunFfmpegAsync(["-hide_banner", "-loglevel", "error", "-i", path, "-f", "framemd5", "-"]).GetAwaiter().GetResult();
        Assert.Equal(0, result.ExitCode);
        return result.StandardOutput.Split('\n').Last(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
    }

    private string PixelDigestAt(string path, int x, int y)
    {
        var result = _ffmpeg.RunFfmpegAsync(
            ["-hide_banner", "-loglevel", "error", "-i", path, "-vf", $"format=rgba,crop=1:1:{x}:{y}", "-f", "framemd5", "-"]).GetAwaiter().GetResult();
        Assert.Equal(0, result.ExitCode);
        return result.StandardOutput.Split('\n').Last(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
    }

    private double AverageLuma(string path)
    {
        var result = _ffmpeg.RunFfmpegAsync(
            ["-hide_banner", "-loglevel", "error", "-i", path, "-vf", "signalstats,metadata=print:file=-", "-frames:v", "1", "-f", "null", "-"]).GetAwaiter().GetResult();
        Assert.Equal(0, result.ExitCode);
        var line = result.StandardOutput.Split('\n').Single(value => value.StartsWith("lavfi.signalstats.YAVG=", StringComparison.Ordinal));
        return double.Parse(line[(line.IndexOf('=') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class TransitionFailingFfmpeg : IFfmpegTool
    {
        private int _generationCalls;

        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessResult(0, "8x6\n", string.Empty));

        public Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            var output = arguments.Last();
            if (_generationCalls++ == 0)
            {
                File.WriteAllText(output, "staged");
                return Task.CompletedTask;
            }

            throw new InvalidOperationException("simulated transition failure");
        }
    }

    private sealed class TransitionCancelingFfmpeg(CancellationTokenSource cancellation) : IFfmpegTool
    {
        private int _generationCalls;

        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessResult(0, "8x6\n", string.Empty));

        public async Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(arguments.Last(), "staged", CancellationToken.None);
            if (_generationCalls++ == 0)
                return;
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private sealed class TransitionSizingFfmpeg(string dimensions) : IFfmpegTool
    {
        public int GenerationCalls { get; private set; }

        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessResult(0, dimensions, string.Empty));

        public Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            GenerationCalls++;
            throw new InvalidOperationException("Generation should not start after a failed preflight.");
        }
    }
}
