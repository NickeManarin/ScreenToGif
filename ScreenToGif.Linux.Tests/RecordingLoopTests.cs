using Avalonia;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Drives a real <see cref="RecordingSession"/> through the thread and queue the recorder window
/// actually uses, with a real <see cref="PngFrameWriter"/> writing to a temporary workspace. What
/// the session tests cannot reach from one thread is the ordering: two commands posted before the
/// first one's answer comes back run against each other, which is the shape a double click has.
/// </summary>
public sealed class RecordingLoopTests : IDisposable
{
    private static readonly PixelRect Region = new(0, 0, 8, 6);

    private readonly EditorWorkspace _workspace = EditorWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public async Task A_second_stop_arriving_before_the_first_answers_does_not_delete_the_recording()
    {
        var batch = _workspace.CreateBatch(EditorArtifactKind.Recordings);
        await using var loop = NewLoop(batch, out _);

        loop.Post(session => session.Record());
        await WaitForFramesAsync(batch, 3);

        // Stop stays on screen and F8 stays live while the first stop drains the writer, so both
        // commands can be in the queue at once. The recording thread runs them back to back.
        var first = loop.StopAsync();
        var second = loop.StopAsync();

        var frames = await first;
        Assert.Empty(await second);

        Assert.NotEmpty(frames);
        Assert.All(frames, frame => Assert.True(File.Exists(frame.FilePath),
            $"The second stop deleted {frame.FilePath}, which the first one had already handed over."));
        Assert.True(Directory.Exists(batch));
    }

    [Fact]
    public async Task A_discard_arriving_after_a_stop_leaves_the_handed_over_frames_alone()
    {
        var batch = _workspace.CreateBatch(EditorArtifactKind.Recordings);
        await using var loop = NewLoop(batch, out _);

        loop.Post(session => session.Record());
        await WaitForFramesAsync(batch, 2);

        var frames = await loop.StopAsync();
        Assert.NotEmpty(frames);

        loop.Post(session => session.Discard());
        await WaitForQuietAsync(loop);

        Assert.All(frames, frame => Assert.True(File.Exists(frame.FilePath),
            $"A discard after the hand-off deleted {frame.FilePath}."));
    }

    [Fact]
    public async Task A_failing_source_reaches_the_host_as_one_plain_message_rather_than_an_exception()
    {
        var batch = _workspace.CreateBatch(EditorArtifactKind.Recordings);
        var failures = new List<string>();
        await using var loop = NewLoop(batch, out var source, error => failures.Add(error.Message));

        source.FailWith = "The screen region could not be read.";
        loop.Post(session => session.Record());

        await WaitUntilAsync(() => failures.Count > 0, "the failure to reach the host");
        await WaitForQuietAsync(loop);

        Assert.Equal("The screen region could not be read.", failures[0]);
        Assert.Single(failures);
    }

    private RecordingLoop NewLoop(string batch, out FakeScreenSource source,
        Action<RecordingErrorEventArgs>? failed = null)
    {
        source = new FakeScreenSource(new PixelSize(Region.Width, Region.Height));
        var session = new RecordingSession(
            source,
            new PngFrameWriter(batch),
            new StopwatchRecordingClock(),
            new RecordingSettings { FramesPerSecond = 60 },
            () => Region);

        return new RecordingLoop(session, _ => { }, failed ?? (_ => { }));
    }

    private static Task WaitForFramesAsync(string batch, int count) =>
        WaitUntilAsync(
            () => Directory.Exists(batch) && Directory.GetFiles(batch, "*.png").Length >= count,
            $"{count} frames to be encoded");

    /// <summary>Lets the recording thread drain whatever is queued before the test looks again.</summary>
    private static async Task WaitForQuietAsync(RecordingLoop loop)
    {
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loop.Post(_ => drained.SetResult());
        await drained.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(20);
        }

        Assert.True(false, $"Timed out waiting for {what}.");
    }
}
