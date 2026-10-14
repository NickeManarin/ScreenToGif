using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class BoardRecordingSessionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-board-session-{Guid.NewGuid():N}");

    private sealed class ManualClock : IPlaybackClock
    {
        public long ElapsedMilliseconds { get; set; }
        public bool Stopped { get; private set; }
        public int Restarts { get; private set; }

        public void Restart()
        {
            Restarts++;
            Stopped = false;
            ElapsedMilliseconds = 0;
        }

        public void Stop() => Stopped = true;
    }

    private sealed class FileSink : IBoardFrameSink
    {
        public int? FailAtWrite { get; set; }
        public int Writes { get; private set; }

        public void Write(string path)
        {
            if (FailAtWrite == Writes)
                throw new IOException("The frame could not be written.");

            File.WriteAllText(path, $"frame-{Writes}");
            Writes++;
        }
    }

    private sealed record Harness(BoardRecordingSession Session, ManualClock Clock, FileSink Sink, Func<string?> WorkspacePath);

    private Harness Create(
        int fps = 10,
        int width = 800,
        int height = 400,
        Func<BoardRecording>? openRecording = null)
    {
        var clock = new ManualClock();
        var sink = new FileSink();
        string? workspacePath = null;
        var plan = new BoardRecordingPlan(fps, width, height);

        var session = new BoardRecordingSession(clock, sink, () => plan, openRecording ?? (() =>
        {
            var workspace = EditorWorkspace.Create(Path.Combine(_root, Guid.NewGuid().ToString("N")));
            workspacePath = workspace.RootPath;
            return new BoardRecording(workspace);
        }));

        return new Harness(session, clock, sink, () => workspacePath);
    }

    // ---- Timing ----------------------------------------------------------------------------

    [Theory]
    [InlineData(10, 100)]
    [InlineData(15, 66)]
    [InlineData(60, 16)]
    [InlineData(0, 1000)]
    [InlineData(240, 16)]
    public void TheIntervalIsTheWindowsBoardsOneSecondOverTheClampedFrameRate(int fps, int expectedMs)
    {
        var (session, _, _, _) = Create(fps: fps);

        session.Start();

        Assert.Equal(expectedMs, session.IntervalMs);
    }

    [Fact]
    public void ANewSessionIsStoppedEmptyAndRecordsOnPress()
    {
        var (session, _, _, workspace) = Create();

        Assert.Equal(BoardRecordingStage.Stopped, session.Stage);
        Assert.False(session.HasFrames);
        Assert.True(session.AutoRecord);
        Assert.Null(workspace());
    }

    [Fact]
    public void TickingWhileStoppedIsRefused()
    {
        var (session, _, sink, _) = Create();

        Assert.Throws<InvalidOperationException>(() => session.Tick());
        Assert.Equal(0, sink.Writes);
    }

    [Fact]
    public void TheFirstFrameCarriesTheScheduledIntervalAndLaterFramesCarryTheMeasuredGap()
    {
        var (session, clock, _, _) = Create(fps: 10);
        session.Start();

        var first = session.Tick();
        clock.ElapsedMilliseconds = 104;
        var second = session.Tick();
        clock.ElapsedMilliseconds = 315;
        var third = session.Tick();

        Assert.Equal([100, 104, 211], new[] { first.DelayMs, second.DelayMs, third.DelayMs });
        Assert.Equal(["000000.png", "000001.png", "000002.png"], new[] { first, second, third }.Select(frame => Path.GetFileName(frame.Path)));
    }

    [Fact]
    public void AMeasuredGapIsNeverZeroSoFramesAlwaysHaveAVisibleDuration()
    {
        var (session, clock, _, _) = Create();
        session.Start();
        session.Tick();
        clock.ElapsedMilliseconds = 0;

        Assert.Equal(1, session.Tick().DelayMs);
    }

    [Fact]
    public void ResumingRestartsTheGapMeasurementSoAPauseIsNotBilledToTheNextFrame()
    {
        var (session, clock, _, _) = Create(fps: 10);
        session.Start();
        session.Tick();
        clock.ElapsedMilliseconds = 100;
        session.Tick();
        session.Pause();

        clock.ElapsedMilliseconds = 9_000;
        session.Start();

        Assert.Equal(100, session.Tick().DelayMs);
        Assert.Equal(3, session.FrameCount);
        Assert.Equal(1, clock.Restarts);
    }

    [Fact]
    public void ThreeSecondsAtTenFramesPerSecondYieldsAboutThirtyEvenlySpacedFrames()
    {
        var (session, clock, _, _) = Create(fps: 10);
        session.Start();

        for (var elapsed = 0; elapsed <= 3_000; elapsed += session.IntervalMs)
        {
            clock.ElapsedMilliseconds = elapsed;
            session.Tick();
        }

        using var project = RequireProject(session);
        Assert.InRange(project.Frames.Count, 25, 35);
        Assert.All(project.Frames.Skip(1), frame => Assert.InRange(frame.DelayMs, 70, 130));
    }

    [Fact]
    public void TheFrameRateIsFixedWhenTheRecordingStartsNotWhenItResumes()
    {
        var fps = 10;
        var clock = new ManualClock();
        var session = new BoardRecordingSession(
            clock, new FileSink(), () => new BoardRecordingPlan(fps, 800, 400), OpenWorkspaceRecording);
        session.Start();
        session.Pause();

        fps = 30;
        session.Start();

        Assert.Equal(100, session.IntervalMs);
    }

    // ---- Pointer gestures ------------------------------------------------------------------

    [Fact]
    public void WithAutoRecordOnPressingStartsReleasingPausesAndPressingAgainResumes()
    {
        var (session, _, _, _) = Create();

        Assert.True(session.PointerPressed());
        Assert.Equal(BoardRecordingStage.Recording, session.Stage);
        session.Tick();

        Assert.True(session.PointerReleased());
        Assert.Equal(BoardRecordingStage.Paused, session.Stage);

        Assert.True(session.PointerPressed());
        Assert.Equal(BoardRecordingStage.Recording, session.Stage);
        Assert.Equal(1, session.FrameCount);
    }

    [Fact]
    public void WithAutoRecordOffPressingDrawsWithoutRecording()
    {
        var (session, _, _, workspace) = Create();
        session.SetAutoRecord(false);

        Assert.False(session.PointerPressed());

        Assert.Equal(BoardRecordingStage.Stopped, session.Stage);
        Assert.Null(workspace());
    }

    [Fact]
    public void ARecordingStartedByHandKeepsGoingBetweenStrokes()
    {
        var (session, _, _, _) = Create();
        session.SetAutoRecord(false);
        session.Start();

        Assert.False(session.PointerPressed());
        Assert.False(session.PointerReleased());

        Assert.Equal(BoardRecordingStage.Recording, session.Stage);
    }

    [Fact]
    public void ReleasingWithoutHavingPressedDoesNothing()
    {
        var (session, _, _, _) = Create();

        Assert.False(session.PointerReleased());
        Assert.Equal(BoardRecordingStage.Stopped, session.Stage);
    }

    // ---- Ctrl inversion --------------------------------------------------------------------

    [Fact]
    public void HoldingCtrlInvertsAutoRecordAndReleasingItRevertsIt()
    {
        var (session, _, _, _) = Create();

        Assert.True(session.CtrlPressed());
        Assert.False(session.AutoRecord);
        Assert.False(session.PointerPressed());

        Assert.True(session.CtrlReleased());
        Assert.True(session.AutoRecord);
    }

    [Fact]
    public void CtrlHeldWithAutoRecordOffLetsOneStrokeRecord()
    {
        var (session, _, _, _) = Create();
        session.SetAutoRecord(false);

        session.CtrlPressed();
        Assert.True(session.PointerPressed());
        Assert.True(session.PointerReleased());
        session.CtrlReleased();

        Assert.False(session.AutoRecord);
        Assert.Equal(BoardRecordingStage.Paused, session.Stage);
    }

    [Fact]
    public void KeyRepeatWhileCtrlIsHeldInvertsOnlyOnce()
    {
        var (session, _, _, _) = Create();

        session.CtrlPressed();
        Assert.False(session.CtrlPressed());
        Assert.False(session.CtrlPressed());

        Assert.False(session.AutoRecord);
    }

    [Fact]
    public void LosingFocusWhileCtrlIsHeldRevertsTheInversion()
    {
        var (session, _, _, _) = Create();
        session.CtrlPressed();

        Assert.True(session.Deactivated());
        Assert.True(session.AutoRecord);

        // Ctrl coming up later, in another window, must not invert it back.
        Assert.False(session.CtrlReleased());
        Assert.True(session.AutoRecord);
    }

    [Fact]
    public void LosingFocusWithoutCtrlHeldChangesNothing()
    {
        var (session, _, _, _) = Create();

        Assert.False(session.Deactivated());
        Assert.True(session.AutoRecord);
    }

    // ---- Stop and hand-over ----------------------------------------------------------------

    [Fact]
    public void StopWithFramesEndsTheSessionAndStopsTheClock()
    {
        var (session, clock, _, _) = Create();
        session.Start();
        session.Tick();

        Assert.True(session.Stop());

        Assert.Equal(BoardRecordingStage.Stopped, session.Stage);
        Assert.True(clock.Stopped);
        Assert.True(session.HasFrames);
    }

    [Fact]
    public void StopWithNothingRecordedDoesNothing()
    {
        var (session, _, _, _) = Create();
        session.Start();

        Assert.False(session.Stop());

        Assert.Equal(BoardRecordingStage.Recording, session.Stage);
    }

    [Fact]
    public void TakingTheProjectHandsOverTheFramesAndTheirWorkspaceExactlyOnce()
    {
        var (session, _, _, workspace) = Create(fps: 10);
        session.Start();
        session.Tick();
        session.Tick();

        using var project = RequireProject(session);

        Assert.Equal(workspace(), project.Workspace.RootPath);
        Assert.Equal(2, project.Frames.Count);
        Assert.All(project.Frames, frame => Assert.True(File.Exists(frame.FilePath)));
        Assert.Equal(100, project.Frames[0].DelayMs);
        Assert.Null(session.TakeProject());
        Assert.True(Directory.Exists(workspace()));
    }

    [Fact]
    public void TakingTheProjectWithNothingRecordedDeletesTheWorkspaceTheSessionOpened()
    {
        var (session, _, _, workspace) = Create();
        session.Start();

        Assert.Null(session.TakeProject());

        Assert.False(Directory.Exists(workspace()));
        Assert.Equal(BoardRecordingStage.Stopped, session.Stage);
    }

    [Fact]
    public void TakingTheProjectFromASessionThatNeverRecordedReturnsNothing() =>
        Assert.Null(Create().Session.TakeProject());

    // ---- Discard ---------------------------------------------------------------------------

    [Fact]
    public async Task DecliningDiscardLeavesTheFramesTheStateAndTheWorkspaceAsTheyWere()
    {
        var (session, _, _, workspace) = Create();
        session.PointerPressed();
        session.Tick();
        session.PointerReleased();

        Assert.False(await session.DiscardAsync(() => Task.FromResult(false)));

        Assert.Equal(1, session.FrameCount);
        Assert.Equal(BoardRecordingStage.Paused, session.Stage);
        Assert.True(Directory.Exists(workspace()));
    }

    [Fact]
    public async Task AcceptingDiscardDeletesTheWorkspaceAndReturnsToIdle()
    {
        var (session, clock, _, workspace) = Create();
        session.Start();
        session.Tick();

        Assert.True(await session.DiscardAsync(() => Task.FromResult(true)));

        Assert.False(session.HasFrames);
        Assert.Equal(BoardRecordingStage.Stopped, session.Stage);
        Assert.True(clock.Stopped);
        Assert.False(Directory.Exists(workspace()));
        Assert.Null(session.TakeProject());
    }

    [Fact]
    public async Task DiscardWithNothingRecordedNeverAsks()
    {
        var (session, _, _, _) = Create();
        var asked = false;

        Assert.False(await session.DiscardAsync(() =>
        {
            asked = true;
            return Task.FromResult(true);
        }));

        Assert.False(asked);
    }

    [Fact]
    public async Task ARecordingStartedAfterADiscardUsesAFreshWorkspace()
    {
        var (session, _, _, workspace) = Create(fps: 10);
        session.Start();
        session.Tick();
        var first = workspace();
        await session.DiscardAsync(() => Task.FromResult(true));

        session.Start();

        Assert.NotEqual(first, workspace());
        Assert.Equal(100, session.Tick().DelayMs);
        Assert.Equal(1, session.FrameCount);
    }

    // ---- Failures --------------------------------------------------------------------------

    [Fact]
    public void AFrameThatCannotBeWrittenPausesAndKeepsTheFramesAlreadyTaken()
    {
        var (session, _, sink, _) = Create();
        session.Start();
        session.Tick();
        sink.FailAtWrite = 1;

        Assert.Throws<IOException>(() => session.Tick());

        Assert.Equal(BoardRecordingStage.Paused, session.Stage);
        Assert.Equal(1, session.FrameCount);
    }

    [Fact]
    public void AWorkspaceThatCannotBeCreatedLeavesTheSessionStoppedAndSurfacesTheFailure()
    {
        var (session, _, _, _) = Create(openRecording: () => throw new IOException("No space left on device."));

        Assert.Throws<IOException>(() => session.PointerPressed());

        Assert.Equal(BoardRecordingStage.Stopped, session.Stage);
        Assert.False(session.HasFrames);
    }

    // ---- The editor's frame limit ----------------------------------------------------------

    [Fact]
    public void TheFrameLimitComesFromTheFrameSizeFixedAtStart()
    {
        var (session, _, _, _) = Create(width: 1920, height: 1080);

        session.Start();

        Assert.Equal(EditorResourceLimits.MaximumFramesAt(1920, 1080), session.FrameLimit);
    }

    [Fact]
    public void ReachingTheFrameLimitPausesAndARecordingThereCannotResume()
    {
        // At the largest frame the editor accepts, the pixel budget allows only a handful of frames.
        var (session, _, sink, _) = Create(width: 8192, height: 8192);
        session.Start();

        while (session.Stage == BoardRecordingStage.Recording)
            session.Tick();

        Assert.True(session.IsAtFrameLimit);
        Assert.Equal(session.FrameLimit, session.FrameCount);
        Assert.Equal(session.FrameLimit, sink.Writes);
        Assert.False(session.Start());
        Assert.False(session.PointerPressed());
        Assert.True(session.Stop());
    }

    private static LoadedProject RequireProject(BoardRecordingSession session) =>
        session.TakeProject() ?? throw new Xunit.Sdk.XunitException("The session handed over no project.");

    private BoardRecording OpenWorkspaceRecording()
    {
        var workspace = EditorWorkspace.Create(Path.Combine(_root, Guid.NewGuid().ToString("N")));
        return new BoardRecording(workspace);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
