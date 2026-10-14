using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Drives the recording session from a clock the test advances by hand, so the frame delays the editor
/// receives are checked as arithmetic rather than as timing.
/// </summary>
public sealed class WebcamRecordingSessionTests
{
    private const int TenFps = 10;

    /// <summary>A cap no test in this file reaches, so the frame limit stays out of the way.</summary>
    private const int Unbounded = int.MaxValue;

    [Fact]
    public void FrameDelaysMeasureTheGapBetweenCapturesAndTheLastFrameKeepsTheNominalInterval()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        Assert.Equal(100, session.IntervalMs);

        session.RegisterCapture("000000.png");
        clock.Advance(100);
        session.RegisterCapture("000001.png");

        var frames = session.Stop();

        Assert.Equal(["000000.png", "000001.png"], frames.Select(frame => frame.FilePath));
        Assert.Equal([100, 100], frames.Select(frame => frame.DelayMs));
    }

    [Fact]
    public void APauseIsExcludedFromTheMeasuredDelays()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        session.RegisterCapture("000000.png");
        clock.Advance(100);
        session.RegisterCapture("000001.png");

        clock.Advance(30);
        session.Pause();
        Assert.Equal(WebcamRecordingStage.Paused, session.Stage);
        Assert.Equal(130, session.ActiveMilliseconds);

        clock.Advance(5_000);
        session.Resume();
        Assert.True(session.IsCaptureDue);
        session.RegisterCapture("000002.png");

        var frames = session.Stop();

        Assert.Equal([100, 30, 100], frames.Select(frame => frame.DelayMs));
    }

    [Fact]
    public void ATickAFullIntervalLateRestartsTheScheduleInsteadOfComingDueAgainAtOnce()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        session.RegisterCapture("000000.png");

        clock.Advance(200);
        Assert.True(session.IsCaptureDue);
        session.RegisterCapture("000001.png");

        Assert.False(session.IsCaptureDue);

        clock.Advance(99);
        Assert.False(session.IsCaptureDue);
        clock.Advance(1);
        Assert.True(session.IsCaptureDue);
    }

    [Fact]
    public void ADroppedCaptureIsNotRegisteredAndTheNextKeptFrameAbsorbsItsInterval()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        session.RegisterCapture("000000.png");

        clock.Advance(100);
        session.RegisterDroppedCapture();
        Assert.Equal(1, session.FrameCount);
        Assert.False(session.IsCaptureDue);

        clock.Advance(100);
        Assert.True(session.IsCaptureDue);
        session.RegisterCapture("000001.png");

        var frames = session.Stop();

        Assert.Equal(["000000.png", "000001.png"], frames.Select(frame => frame.FilePath));
        Assert.Equal([200, 100], frames.Select(frame => frame.DelayMs));
    }

    [Fact]
    public void DiscardHandsBackEveryWrittenFrameAndReturnsTheSessionToItsIdleState()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        session.RegisterCapture("000000.png");
        clock.Advance(100);
        session.RegisterCapture("000001.png");

        var abandoned = session.Discard();

        Assert.Equal(["000000.png", "000001.png"], abandoned);
        Assert.Equal(WebcamRecordingStage.Stopped, session.Stage);
        Assert.False(session.HasFrames);
        Assert.Equal(0, session.FrameCount);
        Assert.Equal(0, session.ActiveMilliseconds);
        Assert.False(session.IsCaptureDue);
    }

    [Fact]
    public void ASessionCanBeStartedAgainAfterDiscardWithoutCarryingTheOldFramesOrClock()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        session.RegisterCapture("000000.png");
        clock.Advance(500);
        session.Discard();

        session.Start(TenFps, Unbounded);
        session.RegisterCapture("000000.png");
        clock.Advance(100);
        session.RegisterCapture("000001.png");

        Assert.Equal([100, 100], session.Stop().Select(frame => frame.DelayMs));
    }

    [Fact]
    public void CapturesAreRejectedUnlessTheSessionIsRecording()
    {
        var session = new WebcamRecordingSession(new ManualClock());

        Assert.Throws<InvalidOperationException>(() => session.RegisterCapture("000000.png"));
        Assert.Throws<InvalidOperationException>(session.RegisterDroppedCapture);
        Assert.Throws<InvalidOperationException>(() => session.Stop());

        session.Start(TenFps, Unbounded);
        session.Pause();

        Assert.Throws<InvalidOperationException>(() => session.RegisterCapture("000000.png"));
        Assert.Throws<InvalidOperationException>(() => session.Start(TenFps, Unbounded));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void AFrameRateOutsideTheSupportedRangeIsRejected(int fps) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WebcamRecordingSession.IntervalFor(fps));

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(15, 67)]
    [InlineData(30, 33)]
    [InlineData(60, 17)]
    public void TheIntervalIsTheRoundedPeriodOfTheFrameRate(int fps, int expected) =>
        Assert.Equal(expected, WebcamRecordingSession.IntervalFor(fps));

    [Fact]
    public void FrameFilesAreNamedInOrderAndADroppedCaptureLeavesNoGap()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        Assert.Equal("/batch/000000.png", session.NextFramePath("/batch"));
        session.RegisterCapture(session.NextFramePath("/batch"));

        clock.Advance(100);
        Assert.Equal("/batch/000001.png", session.NextFramePath("/batch"));
        session.RegisterDroppedCapture();
        Assert.Equal("/batch/000001.png", session.NextFramePath("/batch"));

        clock.Advance(100);
        session.RegisterCapture(session.NextFramePath("/batch"));

        Assert.Equal(
            ["/batch/000000.png", "/batch/000001.png"],
            session.Stop().Select(frame => frame.FilePath));
    }

    [Fact]
    public void ARecordingReportsItsLimitOnlyOnceItHoldsThatManyFrames()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, maximumFrames: 2);
        Assert.Equal(2, session.MaximumFrames);
        Assert.False(session.IsAtFrameLimit);

        session.RegisterCapture("000000.png");
        Assert.False(session.IsAtFrameLimit);

        clock.Advance(100);
        session.RegisterCapture("000001.png");
        Assert.True(session.IsAtFrameLimit);
    }

    [Fact]
    public void ADroppedCaptureDoesNotCountTowardsTheFrameLimit()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, maximumFrames: 1);
        clock.Advance(100);
        session.RegisterDroppedCapture();

        Assert.False(session.IsAtFrameLimit);
    }

    [Fact]
    public void ARecordingAllowedNoFramesIsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebcamRecordingSession(new ManualClock()).Start(TenFps, 0));

    [Fact]
    public void RestartingAfterDiscardNamesFramesFromTheBeginningAgain()
    {
        var clock = new ManualClock();
        var session = new WebcamRecordingSession(clock);

        session.Start(TenFps, Unbounded);
        session.RegisterCapture(session.NextFramePath("/batch"));
        session.Discard();

        session.Start(TenFps, Unbounded);

        Assert.Equal("/batch/000000.png", session.NextFramePath("/batch"));
    }

    [Fact]
    public void ALargeCaptureSizeIsBoundedByTheEditorsPixelBudgetRatherThanItsFrameCount()
    {
        var format = new CameraCaptureFormat("mjpeg", IsCompressed: true, 1280, 720);

        var maximum = WebcamRecordingSession.MaximumFramesFor(format);

        Assert.Equal((int)(EditorResourceLimits.MaximumImportedPixels / format.Area), maximum);
        Assert.True(maximum < EditorResourceLimits.MaximumProjectFrames);
        Assert.True((long)maximum * format.Area <= EditorResourceLimits.MaximumImportedPixels);
    }

    [Fact]
    public void ASmallCaptureSizeIsBoundedByTheEditorsFrameCountInstead() =>
        Assert.Equal(
            EditorResourceLimits.MaximumProjectFrames,
            WebcamRecordingSession.MaximumFramesFor(new CameraCaptureFormat("yuyv422", IsCompressed: false, 320, 240)));

    [Fact]
    public void EvenAnOversizedCaptureAllowsOneFrame() =>
        Assert.Equal(1, WebcamRecordingSession.MaximumFramesFor(new CameraCaptureFormat("mjpeg", true, 40_000, 40_000)));

    private sealed class ManualClock : IPlaybackClock
    {
        private long _running;
        private bool _stopped = true;

        public long ElapsedMilliseconds => _running;

        public void Restart()
        {
            _running = 0;
            _stopped = false;
        }

        public void Stop() => _stopped = true;

        public void Advance(long milliseconds)
        {
            if (!_stopped)
                _running += milliseconds;
        }
    }
}
