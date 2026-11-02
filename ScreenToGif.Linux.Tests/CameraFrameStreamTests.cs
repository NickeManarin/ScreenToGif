using ScreenToGif.Linux.Services;
using System.Diagnostics;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Drives the camera frame stream against FFmpeg's synthetic <c>lavfi</c> source, so the reader,
/// the frame framing, and the child-process lifetime are covered without a capture device.
/// </summary>
public sealed class CameraFrameStreamTests
{
    private const int Width = 64;
    private const int Height = 48;
    private const int Rate = 10;

    [Fact]
    public async Task TestSourceFramesAreExactlySizedDistinctAndStopWithTheStream()
    {
        var recorded = new object();
        var collected = new List<byte[]>();
        var declaredSizes = new List<(int Width, int Height)>();
        var arrived = new SemaphoreSlim(0);

        await using var stream = CameraFrameStream.CreateForTestSource(Width, Height, Rate);
        stream.FrameArrived += (_, frame) =>
        {
            lock (recorded)
            {
                if (collected.Count < Rate * 2)
                {
                    declaredSizes.Add((frame.Width, frame.Height));
                    collected.Add(frame.Bgra.ToArray());
                }
            }

            arrived.Release();
        };

        stream.Start();
        var processId = RequireProcessId(stream);

        for (var index = 0; index < Rate; index++)
            Assert.True(await arrived.WaitAsync(TimeSpan.FromSeconds(20)), $"Frame {index + 1} of {Rate} did not arrive.");

        byte[][] frames;
        (int Width, int Height)[] sizes;
        lock (recorded)
        {
            frames = [.. collected];
            sizes = [.. declaredSizes];
        }

        Assert.True(frames.Length >= Rate, $"Expected at least {Rate} frames but captured {frames.Length}.");
        Assert.All(frames, frame => Assert.Equal(Width * Height * 4, frame.Length));
        Assert.All(sizes, size => Assert.Equal((Width, Height), size));
        Assert.Equal(Width * Height * 4, stream.FrameByteLength);

        var distinctPairs = frames.Zip(frames.Skip(1)).Count(pair => !pair.First.AsSpan().SequenceEqual(pair.Second));
        Assert.Equal(frames.Length - 1, distinctPairs);

        await stream.DisposeAsync();

        Assert.True(stream.ChildHasExited);
        Assert.False(IsProcessAlive(processId), $"FFmpeg process {processId} was still alive after disposal.");
    }

    [Fact]
    public async Task AStreamThatCannotStartReportsTheFfmpegErrorInsteadOfThrowingOnTheReader()
    {
        var failure = new TaskCompletionSource<CameraStreamFailure>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var stream = CameraFrameStream.CreateForArguments(
            ["-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "no_such_filter_source", "-f", "rawvideo", "-pix_fmt", "bgra", "-"],
            Width,
            Height,
            "test source");
        stream.Failed += (_, reason) => failure.TrySetResult(reason);

        stream.Start();

        var completed = await Task.WhenAny(failure.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Same(failure.Task, completed);
        var reason = await failure.Task;
        Assert.NotEqual(0, reason.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(reason.StandardErrorTail));
    }

    [Fact]
    public async Task DisposingBeforeAnyFrameStillStopsTheChild()
    {
        var stream = CameraFrameStream.CreateForTestSource(Width, Height, Rate);
        stream.Start();
        var processId = RequireProcessId(stream);

        await stream.DisposeAsync();

        Assert.True(stream.ChildHasExited);
        Assert.False(IsProcessAlive(processId));
    }

    [Fact]
    public async Task TheReportedErrorDropsFfmpegsAllocatorPrefixesAndRepeatedLines()
    {
        var failure = new TaskCompletionSource<CameraStreamFailure>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var stream = CameraFrameStream.CreateForArguments(
            ["-hide_banner", "-loglevel", "error", "-i", "/definitely/not/a/file.mp4",
             "-f", "rawvideo", "-pix_fmt", "bgra", "-"],
            Width,
            Height,
            "/definitely/not/a/file.mp4");
        stream.Failed += (_, reason) => failure.TrySetResult(reason);

        stream.Start();

        var completed = await Task.WhenAny(failure.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Same(failure.Task, completed);
        var tail = (await failure.Task).StandardErrorTail;

        Assert.False(string.IsNullOrWhiteSpace(tail));
        Assert.DoesNotContain(" @ 0x", tail, StringComparison.Ordinal);
        var lines = tail.Split(Environment.NewLine);
        Assert.Equal(lines.Length, lines.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task AMissingFfmpegIsReportedAsSuchByEveryPathThatLaunchesIt()
    {
        const string variable = "SCREENTOGIF_FFMPEG";
        var previous = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, "/definitely/not/an/ffmpeg");
        try
        {
            var listing = await Assert.ThrowsAsync<FfmpegUnavailableException>(
                () => new FfmpegTool().RunFfmpegAsync(["-hide_banner", "-version"]));
            Assert.Contains("Install FFmpeg", listing.Message, StringComparison.Ordinal);

            await using var stream = CameraFrameStream.CreateForTestSource(Width, Height, Rate);
            var launch = Assert.Throws<FfmpegUnavailableException>(stream.Start);
            Assert.Contains("Install FFmpeg", launch.Message, StringComparison.Ordinal);

            Assert.Contains(
                "FFmpeg could not be started",
                WebcamStatus.FromMissingFfmpeg("/dev/video0", launch.Message).Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    [Fact]
    public void CameraArgumentsCarryTheChosenFormatSizeAndRate()
    {
        var format = new CameraCaptureFormat("mjpeg", IsCompressed: true, 1280, 720);

        var arguments = CameraFrameStream.CameraArguments("/dev/video0", format, 30);

        Assert.Contains("-nostdin", arguments);
        Assert.Contains("v4l2", arguments);
        Assert.Equal("mjpeg", ArgumentAfter(arguments, "-input_format"));
        Assert.Equal("1280x720", ArgumentAfter(arguments, "-video_size"));
        Assert.Equal("30", ArgumentAfter(arguments, "-framerate"));
        Assert.Equal("/dev/video0", ArgumentAfter(arguments, "-i"));
        Assert.Equal("bgra", ArgumentAfter(arguments, "-pix_fmt"));
        Assert.Equal("rawvideo", ArgumentAfter(arguments, "-f", lastOccurrence: true));
        Assert.Equal("-", arguments[^1]);
    }

    private static string ArgumentAfter(IReadOnlyList<string> arguments, string flag, bool lastOccurrence = false)
    {
        for (var index = lastOccurrence ? arguments.Count - 2 : 0;
             lastOccurrence ? index >= 0 : index < arguments.Count - 1;
             index += lastOccurrence ? -1 : 1)
        {
            if (arguments[index] == flag)
                return arguments[index + 1];
        }

        throw new Xunit.Sdk.XunitException($"'{flag}' is not present in the argument list.");
    }

    private static int RequireProcessId(CameraFrameStream stream) =>
        stream.ProcessId is { } processId
            ? processId
            : throw new Xunit.Sdk.XunitException("The stream did not report a child process id after Start.");

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
