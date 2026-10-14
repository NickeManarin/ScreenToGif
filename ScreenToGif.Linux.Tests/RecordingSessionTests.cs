using Avalonia;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Drives every stage and every capture-frequency rule of <see cref="RecordingSession"/> through a
/// fake clock and a fake screen source, so the contract is pinned without an X server.
/// </summary>
public sealed class RecordingSessionTests
{
    private static readonly PixelRect Region = new(10, 20, 64, 48);

    [Fact]
    public void Record_moves_a_stopped_session_straight_to_recording_when_pre_start_is_off()
    {
        var harness = new Harness(new RecordingSettings { PreStartEnabled = false });

        Assert.Equal(RecordingStage.Stopped, harness.Session.Stage);
        harness.Session.Record();

        Assert.Equal(RecordingStage.Recording, harness.Session.Stage);
        Assert.Equal(0, harness.Session.PreStartRemainingSeconds);

        // The first capture is due immediately.
        harness.Session.Tick();
        Assert.Equal(1, harness.Session.FrameCount);
    }

    [Fact]
    public void Pre_start_counts_down_one_second_per_clock_second_before_capturing()
    {
        var harness = new Harness(new RecordingSettings { PreStartEnabled = true, PreStartSeconds = 3 });

        harness.Session.Record();
        Assert.Equal(RecordingStage.PreStarting, harness.Session.Stage);
        Assert.Equal(3, harness.Session.PreStartRemainingSeconds);

        harness.Clock.Advance(1000);
        harness.Session.Tick();
        Assert.Equal(RecordingStage.PreStarting, harness.Session.Stage);
        Assert.Equal(2, harness.Session.PreStartRemainingSeconds);

        harness.Clock.Advance(1000);
        harness.Session.Tick();
        Assert.Equal(1, harness.Session.PreStartRemainingSeconds);
        Assert.Equal(0, harness.Session.FrameCount);

        harness.Clock.Advance(1000);
        harness.Session.Tick();
        Assert.Equal(RecordingStage.Recording, harness.Session.Stage);
        Assert.Equal(0, harness.Session.PreStartRemainingSeconds);
        Assert.Equal(1, harness.Session.FrameCount);
    }

    [Fact]
    public void Pause_then_resume_continues_the_same_frame_sequence()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });

        harness.Session.Record();
        harness.Session.Tick();
        harness.Clock.Advance(100);
        harness.Session.Tick();
        Assert.Equal(2, harness.Session.FrameCount);

        harness.Session.Pause();
        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);

        // Nothing is captured while paused, however far the clock runs.
        harness.Clock.Advance(5000);
        Assert.Equal(Timeout.Infinite, harness.Session.Tick());
        Assert.Equal(2, harness.Session.FrameCount);

        harness.Session.Record();
        Assert.Equal(RecordingStage.Recording, harness.Session.Stage);
        harness.Clock.Advance(100);
        harness.Session.Tick();

        Assert.Equal(3, harness.Session.FrameCount);
        Assert.Equal(3, harness.Writer.Paths.Count);
    }

    [Fact]
    public async Task Stop_from_recording_or_paused_yields_the_frames_and_ends_the_session()
    {
        foreach (var pauseFirst in new[] { false, true })
        {
            var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
            harness.Session.Record();
            harness.Session.Tick();
            harness.Clock.Advance(100);
            harness.Session.Tick();

            if (pauseFirst)
                harness.Session.Pause();

            var frames = await harness.Session.StopAsync();

            Assert.Equal(2, frames.Count);
            Assert.Equal(RecordingStage.Stopped, harness.Session.Stage);
            Assert.Equal(0, harness.Session.FrameCount);
            Assert.Equal(0, harness.Writer.DiscardCount);
        }
    }

    [Fact]
    public async Task Stop_with_no_frames_returns_to_stopped_without_yielding()
    {
        var harness = new Harness(new RecordingSettings());
        harness.Session.Record();

        var frames = await harness.Session.StopAsync();

        Assert.Empty(frames);
        Assert.Equal(RecordingStage.Stopped, harness.Session.Stage);
        Assert.Equal(1, harness.Writer.DiscardCount);
    }

    [Fact]
    public void A_recording_paused_during_the_countdown_still_captures_the_size_it_started_with()
    {
        var harness = new Harness(new RecordingSettings { PreStartEnabled = true, PreStartSeconds = 3 });

        harness.Session.Record();
        Assert.Equal(RecordingStage.PreStarting, harness.Session.Stage);

        // Pausing mid-countdown and resuming reaches capture without passing the start path again,
        // so a size taken only there would be the default and the region would be empty.
        harness.Clock.Advance(1000);
        harness.Session.Tick();
        harness.Session.Pause();
        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);

        // The frame is resized while paused, which the window forbids; the session must ignore it
        // either way, because every frame of one recording has to be the same size.
        harness.Region = new PixelRect(Region.X, Region.Y, Region.Width * 2, Region.Height * 2);

        harness.Session.Record();
        Assert.Equal(RecordingStage.Recording, harness.Session.Stage);
        harness.Session.Tick();

        Assert.Equal(1, harness.Session.FrameCount);
        Assert.Equal(new PixelSize(Region.Width, Region.Height), Assert.Single(harness.Source.Requests).Size);
    }

    [Fact]
    public async Task Stop_during_the_countdown_returns_to_stopped_without_yielding()
    {
        var harness = new Harness(new RecordingSettings { PreStartEnabled = true, PreStartSeconds = 3 });
        harness.Session.Record();
        Assert.Equal(RecordingStage.PreStarting, harness.Session.Stage);

        var frames = await harness.Session.StopAsync();

        Assert.Empty(frames);
        Assert.Equal(RecordingStage.Stopped, harness.Session.Stage);
        Assert.Equal(0, harness.Session.PreStartRemainingSeconds);
        Assert.Equal(1, harness.Writer.DiscardCount);
    }

    [Fact]
    public void Manual_mode_captures_exactly_one_frame_per_snap()
    {
        var harness = new Harness(new RecordingSettings { Mode = RecorderCaptureMode.Manual });

        harness.Session.Snap();
        Assert.Equal(1, harness.Session.FrameCount);

        // Ticking never adds a frame in manual mode, however far the clock runs.
        harness.Clock.Advance(10_000);
        Assert.Equal(Timeout.Infinite, harness.Session.Tick());
        Assert.Equal(1, harness.Session.FrameCount);

        harness.Session.Snap();
        Assert.Equal(2, harness.Session.FrameCount);
        Assert.Equal(2, harness.Source.CaptureCount);
    }

    [Fact]
    public void Manual_mode_exposes_stop_and_discard_only_once_a_frame_exists()
    {
        var harness = new Harness(new RecordingSettings { Mode = RecorderCaptureMode.Manual });

        Assert.True(harness.Session.CanSnap);
        Assert.False(harness.Session.CanRecord);
        Assert.False(harness.Session.CanPause);
        Assert.False(harness.Session.CanStop);
        Assert.False(harness.Session.CanDiscard);

        harness.Session.Snap();

        Assert.True(harness.Session.CanStop);
        Assert.True(harness.Session.CanDiscard);
        Assert.True(harness.Session.CanSnap);
        Assert.False(harness.Session.CanPause);
    }

    [Fact]
    public void Availability_follows_the_stage_in_paced_modes()
    {
        var harness = new Harness(new RecordingSettings { PreStartEnabled = true, PreStartSeconds = 2 });

        AssertAvailability(harness.Session, record: true, snap: false, pause: false, stop: false, discard: false,
            region: true, frequency: true);

        harness.Session.Record();
        AssertAvailability(harness.Session, record: false, snap: false, pause: true, stop: true, discard: false,
            region: false, frequency: false);

        harness.Clock.Advance(2000);
        harness.Session.Tick();
        AssertAvailability(harness.Session, record: false, snap: false, pause: true, stop: true, discard: true,
            region: false, frequency: false);

        harness.Session.Pause();
        AssertAvailability(harness.Session, record: true, snap: false, pause: false, stop: true, discard: true,
            region: false, frequency: true);
    }

    [Fact]
    public void Discard_drops_the_batch_and_returns_to_stopped()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
        harness.Session.Record();
        harness.Session.Tick();
        harness.Clock.Advance(100);
        harness.Session.Tick();
        Assert.Equal(2, harness.Session.FrameCount);

        harness.Session.Discard();

        Assert.Equal(RecordingStage.Stopped, harness.Session.Stage);
        Assert.Equal(0, harness.Session.FrameCount);
        Assert.Equal(1, harness.Writer.DiscardCount);
        Assert.Empty(harness.Writer.Paths);
    }

    [Fact]
    public void A_recording_keeps_the_size_it_started_at_and_still_follows_the_frame_that_moves()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });

        harness.Session.Record();
        harness.Session.Tick();

        // Whatever resizes the frame — a drag on its edge, a window manager, a restored placement —
        // every frame of one recording must be one size, or the project will not open.
        harness.Region = new PixelRect(10, 20, 200, 150);
        harness.Clock.Advance(100);
        harness.Session.Tick();

        // Moving is allowed, and what is recorded moves with the frame.
        harness.Region = new PixelRect(300, 400, 200, 150);
        harness.Clock.Advance(100);
        harness.Session.Tick();

        Assert.Equal(3, harness.Session.FrameCount);
        Assert.All(harness.Source.Requests, request => Assert.Equal(Region.Size, request.Size));
        Assert.Equal([new PixelPoint(10, 20), new PixelPoint(10, 20), new PixelPoint(300, 400)],
            harness.Source.Requests.Select(request => request.Position));

        // The next recording is free to take the size the frame is now.
        harness.Session.StopAsync().GetAwaiter().GetResult();
        harness.Session.Record();
        harness.Session.Tick();
        Assert.Equal(new PixelSize(200, 150), harness.Source.Requests[^1].Size);
    }

    [Fact]
    public async Task A_failing_source_faults_the_session_once_and_keeps_the_frames_already_written()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
        var errors = new List<string>();
        harness.Session.Error += (_, args) => errors.Add(args.Message);

        harness.Session.Record();
        harness.Session.Tick();
        harness.Clock.Advance(100);
        harness.Session.Tick();
        Assert.Equal(2, harness.Session.FrameCount);

        harness.Source.FailWith = "The X server refused to read the screen region.";
        harness.Clock.Advance(100);
        harness.Session.Tick();

        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);
        Assert.Equal(2, harness.Session.FrameCount);
        Assert.Equal(["The X server refused to read the screen region."], errors);

        // Ticking on does not flood the user: a fault pauses, and a paused session captures nothing.
        harness.Clock.Advance(100);
        harness.Session.Tick();
        Assert.Single(errors);

        // Resuming is the user asking to try again, so a failure that is still there says so again
        // rather than leaving the bar showing a healthy recording that captures nothing.
        harness.Session.Record();
        harness.Clock.Advance(100);
        harness.Session.Tick();
        Assert.Equal(2, errors.Count);
        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);
        Assert.Equal(2, harness.Session.FrameCount);

        var frames = await harness.Session.StopAsync();
        Assert.Equal(2, frames.Count);
    }

    [Fact]
    public async Task Per_second_with_a_free_frame_rate_stamps_the_measured_gap_to_the_next_capture()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10, FixedFrameRate = false });
        harness.Session.Record();
        harness.Session.Tick();

        foreach (var gap in new[] { 137, 141, 209 })
        {
            harness.Clock.Advance(gap);
            harness.Session.Tick();
        }

        var frames = await harness.Session.StopAsync();

        Assert.Equal(4, frames.Count);
        Assert.Equal([137, 141, 209], frames.Take(3).Select(frame => frame.DelayMs));

        // The last frame has no next capture to measure against, so it keeps the nominal interval.
        Assert.Equal(100, frames[^1].DelayMs);
    }

    [Fact]
    public async Task A_capture_slower_than_its_interval_is_kept_and_carries_the_real_gap()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 20, FixedFrameRate = false });
        harness.Session.Record();
        harness.Session.Tick();

        // 320 ms is over six times the 50 ms interval; the frame is late, not dropped or multiplied.
        harness.Clock.Advance(320);
        harness.Session.Tick();
        Assert.Equal(2, harness.Session.FrameCount);

        harness.Clock.Advance(50);
        harness.Session.Tick();

        var frames = await harness.Session.StopAsync();

        Assert.Equal(3, frames.Count);
        Assert.Equal(320, frames[0].DelayMs);
        Assert.Equal(50, frames[1].DelayMs);
    }

    [Fact]
    public async Task A_fixed_frame_rate_stamps_the_nominal_delay_whatever_the_real_gap_was()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 25, FixedFrameRate = true });
        harness.Session.Record();
        harness.Session.Tick();
        harness.Clock.Advance(137);
        harness.Session.Tick();
        harness.Clock.Advance(60);
        harness.Session.Tick();

        var frames = await harness.Session.StopAsync();

        Assert.Equal(3, frames.Count);
        Assert.All(frames, frame => Assert.Equal(1000 / 25, frame.DelayMs));
    }

    [Theory]
    [InlineData(RecorderCaptureMode.PerMinute, 15, 60_000 / 15)]
    [InlineData(RecorderCaptureMode.PerMinute, 4, 60_000 / 4)]
    [InlineData(RecorderCaptureMode.PerHour, 15, 3_600_000 / 15)]
    [InlineData(RecorderCaptureMode.PerHour, 60, 3_600_000 / 60)]
    public async Task Slow_modes_pace_by_their_own_interval_and_stamp_the_fixed_playback_delay(
        RecorderCaptureMode mode, int framesPerSecond, int expectedIntervalMs)
    {
        var settings = new RecordingSettings { Mode = mode, FramesPerSecond = framesPerSecond };
        Assert.Equal(expectedIntervalMs, settings.CaptureIntervalMilliseconds);

        var harness = new Harness(settings);
        harness.Session.Record();
        harness.Session.Tick();
        Assert.Equal(1, harness.Session.FrameCount);

        // One interval short of due: nothing is captured.
        harness.Clock.Advance(expectedIntervalMs - 1);
        harness.Session.Tick();
        Assert.Equal(1, harness.Session.FrameCount);

        harness.Clock.Advance(1);
        harness.Session.Tick();
        Assert.Equal(2, harness.Session.FrameCount);

        var frames = await harness.Session.StopAsync();
        Assert.All(frames, frame => Assert.Equal(RecordingSettings.SlowModePlaybackDelayMs, frame.DelayMs));
    }

    [Fact]
    public async Task Manual_frames_carry_the_manual_playback_delay()
    {
        var harness = new Harness(new RecordingSettings
        {
            Mode = RecorderCaptureMode.Manual,
            ManualPlaybackDelayMs = 750
        });

        harness.Session.Snap();
        harness.Clock.Advance(4321);
        harness.Session.Snap();

        var frames = await harness.Session.StopAsync();

        Assert.Equal(2, frames.Count);
        Assert.All(frames, frame => Assert.Equal(750, frame.DelayMs));
    }

    [Fact]
    public void The_cursor_setting_reaches_the_screen_source_on_every_capture()
    {
        foreach (var showCursor in new[] { true, false })
        {
            var harness = new Harness(new RecordingSettings { ShowCursor = showCursor });
            harness.Session.Record();
            harness.Session.Tick();

            Assert.Equal(showCursor, harness.Source.LastRequestedCursor);
            Assert.Equal(Region, Assert.Single(harness.Source.Requests));
        }
    }

    [Fact]
    public async Task An_abandoned_session_deletes_its_own_frames_while_a_handed_off_one_does_not()
    {
        var abandoned = new Harness(new RecordingSettings());
        abandoned.Session.Record();
        abandoned.Session.Tick();
        await abandoned.Session.DisposeAsync();
        Assert.Equal(1, abandoned.Writer.DiscardCount);
        Assert.True(abandoned.Source.IsDisposed);

        var handedOff = new Harness(new RecordingSettings());
        handedOff.Session.Record();
        handedOff.Session.Tick();
        Assert.Single(await handedOff.Session.StopAsync());
        await handedOff.Session.DisposeAsync();

        Assert.Equal(0, handedOff.Writer.DiscardCount);
        Assert.True(handedOff.Writer.IsDisposed);
    }

    [Fact]
    public async Task A_write_that_failed_after_the_last_capture_is_reported_when_the_recording_stops()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
        var errors = new List<string>();
        harness.Session.Error += (_, args) => errors.Add(args.Message);

        harness.Session.Record();
        harness.Session.Tick();
        harness.Clock.Advance(100);
        harness.Session.Tick();

        // The writer fails after the last capture, so nothing polls it again before Stop. Without
        // the report in StopAsync the user would get a short recording and no explanation.
        harness.Writer.Failure = new ScreenCaptureException("The disk holding the recording is full.");

        var frames = await harness.Session.StopAsync();

        Assert.Equal(2, frames.Count);
        Assert.Equal("The disk holding the recording is full.", Assert.Single(errors));
    }

    [Fact]
    public async Task A_recording_pauses_at_the_project_frame_limit_and_keeps_every_frame()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
        var errors = new List<string>();
        harness.Session.Error += (_, args) => errors.Add(args.Message);

        harness.Session.Record();
        for (var i = 0; i < EditorResourceLimits.MaximumProjectFrames; i++)
        {
            harness.Session.Tick();
            harness.Clock.Advance(100);
        }

        Assert.Equal(EditorResourceLimits.MaximumProjectFrames, harness.Session.FrameCount);
        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);
        Assert.Contains($"{EditorResourceLimits.MaximumProjectFrames:N0}", Assert.Single(errors));

        // Paused means paused: the clock running on adds nothing.
        harness.Clock.Advance(5000);
        Assert.Equal(Timeout.Infinite, harness.Session.Tick());
        Assert.Equal(EditorResourceLimits.MaximumProjectFrames, harness.Session.FrameCount);

        var frames = await harness.Session.StopAsync();
        Assert.Equal(EditorResourceLimits.MaximumProjectFrames, frames.Count);
    }

    [Fact]
    public async Task A_large_region_pauses_where_the_editor_can_still_save_it_and_resuming_adds_nothing()
    {
        // 4000x4000 is 16 million pixels a frame, so the one-billion-pixel limit arrives at 62 frames,
        // long before the 10,000-frame limit.
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
        harness.Region = new PixelRect(0, 0, 4000, 4000);
        var errors = new List<string>();
        harness.Session.Error += (_, args) => errors.Add(args.Message);
        var bound = EditorResourceLimits.MaximumFramesAt(4000, 4000);
        Assert.Equal(62, bound);

        harness.Session.Record();
        for (var i = 0; i < bound + 5; i++)
        {
            harness.Session.Tick();
            harness.Clock.Advance(100);
        }

        Assert.Equal(bound, harness.Session.FrameCount);
        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);
        Assert.Contains($"{bound:N0} frames", Assert.Single(errors));

        // Resuming is allowed, because Paused is where every failure leaves the user, but the next
        // capture pauses again instead of taking the frame that would make the project unsavable.
        harness.Session.Record();
        harness.Clock.Advance(100);
        harness.Session.Tick();

        Assert.Equal(bound, harness.Session.FrameCount);
        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);

        var frames = await harness.Session.StopAsync();
        Assert.Equal(bound, frames.Count);
    }

    [Fact]
    public async Task A_recording_pauses_when_the_encoder_falls_far_enough_behind()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
        var errors = new List<string>();
        harness.Session.Error += (_, args) => errors.Add(args.Message);

        harness.Session.Record();
        harness.Session.Tick();
        Assert.Equal(RecordingStage.Recording, harness.Session.Stage);

        // Encoding runs off the capture loop, so a backlog is the only sign the encoder cannot keep
        // up. Left unbounded it ends in the process being killed, taking the recording with it.
        harness.Writer.PendingBytes = 1024L * 1024 * 1024;
        harness.Clock.Advance(100);
        harness.Session.Tick();

        Assert.Equal(RecordingStage.Paused, harness.Session.Stage);
        Assert.Equal(2, harness.Session.FrameCount);
        Assert.Contains("faster than they can be saved", Assert.Single(errors));

        var frames = await harness.Session.StopAsync();
        Assert.Equal(2, frames.Count);
    }

    [Fact]
    public async Task A_pause_is_not_charged_to_the_frame_that_was_on_screen_when_it_started()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10, FixedFrameRate = false });

        harness.Session.Record();
        harness.Session.Tick();
        harness.Clock.Advance(100);
        harness.Session.Tick();

        harness.Session.Pause();
        harness.Clock.Advance(5000);
        harness.Session.Record();

        harness.Clock.Advance(137);
        harness.Session.Tick();

        var frames = await harness.Session.StopAsync();

        // The five paused seconds belong to no frame. The 137 is deliberately not the nominal 100,
        // so this also fails against an implementation that stopped measuring the real gap.
        Assert.Equal([100, 137, 100], frames.Select(frame => frame.DelayMs));
    }

    [Fact]
    public async Task A_second_stop_cannot_take_away_what_the_first_one_handed_over()
    {
        var harness = new Harness(new RecordingSettings { FramesPerSecond = 10 });
        harness.Session.Record();
        harness.Session.Tick();
        harness.Clock.Advance(100);
        harness.Session.Tick();

        var frames = await harness.Session.StopAsync();
        Assert.Equal(2, frames.Count);

        // Stop stays reachable while the first one drains, so a second one arrives at a session
        // whose frame list is already empty. Discarding there would delete the files just returned.
        Assert.Empty(await harness.Session.StopAsync());
        harness.Session.Discard();

        Assert.Equal(0, harness.Writer.DiscardCount);
    }

    [Fact]
    public void A_manual_recording_can_snap_again_after_a_capture_fails()
    {
        var harness = new Harness(new RecordingSettings { Mode = RecorderCaptureMode.Manual });
        var errors = new List<string>();
        harness.Session.Error += (_, args) => errors.Add(args.Message);

        harness.Session.Snap();
        Assert.Equal(1, harness.Session.FrameCount);

        harness.Source.FailWith = "The screen region could not be read.";
        harness.Session.Snap();

        Assert.Equal(1, harness.Session.FrameCount);
        Assert.Equal("The screen region could not be read.", Assert.Single(errors));

        // Manual has no Resume, so the next Snap is the retry. A session parked in Paused could
        // never take another frame, because Snap is unavailable there and Record is not Manual's.
        Assert.True(harness.Session.CanSnap);

        // A retry that fails again is answered again, rather than being swallowed by the latch
        // that keeps a paced recording from flooding the user.
        harness.Session.Snap();
        Assert.Equal(2, errors.Count);

        harness.Source.FailWith = null;
        harness.Session.Snap();

        Assert.Equal(2, harness.Session.FrameCount);
        Assert.Equal(2, errors.Count);
    }

    [Theory]
    [InlineData(RecorderCaptureMode.PerSecond)]
    [InlineData(RecorderCaptureMode.PerMinute)]
    [InlineData(RecorderCaptureMode.PerHour)]
    [InlineData(RecorderCaptureMode.Manual)]
    public void The_idle_reading_a_host_shows_matches_a_session_that_has_not_started(RecorderCaptureMode mode)
    {
        var harness = new Harness(new RecordingSettings { Mode = mode });

        Assert.Equal(harness.Session.Snapshot(), RecordingSession.IdleStatus(mode));
    }

    private static void AssertAvailability(RecordingSession session, bool record, bool snap, bool pause, bool stop,
        bool discard, bool region, bool frequency)
    {
        Assert.Equal(record, session.CanRecord);
        Assert.Equal(snap, session.CanSnap);
        Assert.Equal(pause, session.CanPause);
        Assert.Equal(stop, session.CanStop);
        Assert.Equal(discard, session.CanDiscard);
        Assert.Equal(region, session.CanChangeRegion);
        Assert.Equal(frequency, session.CanChangeFrequency);
    }

    private sealed class Harness
    {
        public Harness(RecordingSettings settings)
        {
            Clock = new FakeRecordingClock();
            Region = RecordingSessionTests.Region;
            Source = new FakeScreenSource(new PixelSize(Region.Width, Region.Height));
            Writer = new FakeFrameWriter();
            Session = new RecordingSession(Source, Writer, Clock, settings, () => Region);
        }

        /// <summary>The rectangle the recorder frame is on right now; a test moves or resizes it.</summary>
        public PixelRect Region { get; set; }

        public FakeRecordingClock Clock { get; }

        public FakeScreenSource Source { get; }

        public FakeFrameWriter Writer { get; }

        public RecordingSession Session { get; }
    }
}
