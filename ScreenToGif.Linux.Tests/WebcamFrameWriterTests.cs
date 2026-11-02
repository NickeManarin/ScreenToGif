using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Covers the bounded PNG writer that takes frames off the capture thread: what reaches disk, what is
/// dropped when encoding falls behind, and how a write that cannot succeed is reported back.
/// </summary>
public sealed class WebcamFrameWriterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"stg-writer-{Guid.NewGuid():N}");

    public WebcamFrameWriterTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task EveryEnqueuedFrameReachesDiskAsAReadablePng()
    {
        await using var writer = new WebcamFrameWriter(workerCount: 1);
        var paths = Enumerable.Range(0, 5).Select(index => Path.Combine(_root, $"{index:000000}.png")).ToArray();

        foreach (var path in paths)
            Assert.True(writer.TryEnqueue(path, Frame(4, 3)));

        await writer.CompleteAsync();

        Assert.Equal(0, writer.DroppedFrames);
        foreach (var path in paths)
        {
            Assert.True(File.Exists(path));
            var content = await File.ReadAllBytesAsync(path);
            Assert.Equal<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], content[..8]);
        }
    }

    [Fact]
    public async Task TheFrameHandedToTheWriterIsCopiedSoTheCallerMayReuseItsBuffer()
    {
        await using var writer = new WebcamFrameWriter(workerCount: 1);
        var path = Path.Combine(_root, "reused.png");
        var frame = Frame(2, 2);

        Assert.True(writer.TryEnqueue(path, frame));
        Array.Fill(frame.Bgra, (byte)0);

        await writer.CompleteAsync();

        var expected = Path.Combine(_root, "expected.png");
        await using (var stream = File.Create(expected))
            RgbaPngEncoder.WriteBgra(stream, 2, 2, Frame(2, 2).Bgra);

        Assert.Equal(await File.ReadAllBytesAsync(expected), await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task AFullQueueDropsTheFrameRatherThanBlockingTheCaptureThread()
    {
        await using var writer = new WebcamFrameWriter(capacity: 1, workerCount: 1);
        var accepted = 0;
        var dropped = 0;

        const int offered = 100;
        for (var index = 0; index < offered; index++)
        {
            if (writer.TryEnqueue(Path.Combine(_root, $"{index:000000}.png"), Frame(32, 32)))
                accepted++;
            else
                dropped++;
        }

        Assert.True(dropped > 0, $"A capacity-1 queue fed {offered} frames should have dropped at least one.");
        Assert.Equal(dropped, writer.DroppedFrames);
        Assert.Equal(offered, accepted + dropped);

        await writer.CompleteAsync();
    }

    [Fact]
    public async Task AWriteThatCannotSucceedIsReportedAndNamesTheUnderlyingFailure()
    {
        await using var writer = new WebcamFrameWriter(workerCount: 1);
        var unwritable = Path.Combine(_root, "missing-directory", "000000.png");

        Assert.True(writer.TryEnqueue(unwritable, Frame(2, 2)));

        var error = await Assert.ThrowsAsync<IOException>(writer.CompleteAsync);

        Assert.NotNull(error.InnerException);
        Assert.NotNull(writer.FirstFailure);
        Assert.False(File.Exists(unwritable));
    }

    [Fact]
    public async Task AFailureBecomesVisibleBeforeCompletionSoRecordingCanStopOnIt()
    {
        await using var writer = new WebcamFrameWriter(workerCount: 1);

        Assert.True(writer.TryEnqueue(Path.Combine(_root, "no-such-directory", "000000.png"), Frame(2, 2)));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (writer.FirstFailure is null && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.NotNull(writer.FirstFailure);
    }

    [Fact]
    public async Task EnqueuingAfterCompletionIsRefusedRatherThanLosingTheFrameSilently()
    {
        await using var writer = new WebcamFrameWriter(workerCount: 1);
        await writer.CompleteAsync();

        Assert.False(writer.TryEnqueue(Path.Combine(_root, "late.png"), Frame(2, 2)));
        Assert.False(File.Exists(Path.Combine(_root, "late.png")));
    }

    private static CameraFrame Frame(int width, int height)
    {
        var bgra = new byte[width * height * 4];
        for (var index = 0; index < bgra.Length; index++)
            bgra[index] = (byte)(index * 7 % 251);
        return new CameraFrame(width, height, bgra);
    }
}
