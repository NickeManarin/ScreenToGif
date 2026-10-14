using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class EditorCoordinationTests
{
    [Fact]
    public async Task Operations_serialize_cancel_and_defer_close_until_cleanup()
    {
        var coordinator = new EditorOperationCoordinator();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();
        var first = coordinator.RunAsync(async token =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(token);
        }, () => events.Add("started"), () => events.Add("finished"));
        await entered.Task;

        var busy = await coordinator.RunAsync(_ => Task.CompletedTask, () => { }, () => { });
        Assert.Equal(EditorOperationStatus.Busy, busy.Status);
        Assert.True(coordinator.RequestClose());
        release.TrySetResult();
        var result = await first;

        Assert.Equal(EditorOperationStatus.Canceled, result.Status);
        Assert.True(result.CloseRequested);
        Assert.False(coordinator.IsRunning);
        Assert.Equal(["started", "finished"], events);
    }

    [Fact]
    public async Task Operation_failure_is_returned_after_finished_callback()
    {
        var coordinator = new EditorOperationCoordinator();
        var finished = false;

        var result = await coordinator.RunAsync(
            _ => throw new InvalidOperationException("broken"),
            () => { },
            () => finished = true);

        Assert.Equal(EditorOperationStatus.Failed, result.Status);
        Assert.Equal("broken", result.Error?.Message);
        Assert.True(finished);
    }

    [Fact]
    public void Playback_session_exposes_repeatable_stop_restart_lag_and_control_states()
    {
        var clock = new FakeClock();
        var session = new PlaybackSession(clock);
        session.Start(1);
        Assert.Equal(new PlaybackStep(1, 20, false), session.Advance([10, 20, 30], loop: false, dropFrames: false));
        Assert.Equal(new PlaybackStep(2, 30, false), session.Advance([10, 20, 30], loop: false, dropFrames: false));
        Assert.Equal(new PlaybackStep(2, 0, true), session.Advance([10, 20, 30], loop: false, dropFrames: false));
        session.Stop();
        Assert.Equal(new PlaybackStep(-1, 0, true), session.Advance([10], loop: false, dropFrames: false));

        clock.ElapsedMilliseconds = 25;
        session.Start(0);
        Assert.Equal(new PlaybackStep(1, 5, false), session.Advance([10, 20, 30], loop: false, dropFrames: true));

        Assert.Equal(new PlaybackControlState(false, false, true, true, true, true),
            PlaybackControlState.Calculate(3, 0, operationRunning: false));
        Assert.False(PlaybackControlState.Calculate(3, 1, operationRunning: true).PlayEnabled);
        Assert.Equal(new PlaybackControlState(false, false, false, false, false, false),
            PlaybackControlState.Calculate(0, -1, operationRunning: false));
    }

    [Fact]
    public void Single_frame_playback_displays_once_then_completes()
    {
        var session = new PlaybackSession(new FakeClock());

        session.Start(0);

        Assert.Equal(new PlaybackStep(0, 75, false), session.Advance([75], loop: false, dropFrames: false));
        Assert.Equal(new PlaybackStep(0, 0, true), session.Advance([75], loop: false, dropFrames: false));
        session.Stop();
        Assert.False(session.IsPlaying);
    }

    [Fact]
    public void Active_playback_stops_for_timeline_mutation_and_restarts_from_the_new_snapshot()
    {
        var session = new PlaybackSession(new FakeClock());
        var delays = new List<int> { 40, 160, 80 };
        session.Start(1);
        Assert.Equal(new PlaybackStep(1, 160, false), session.Advance(delays, loop: false, dropFrames: false));

        session.Stop();
        delays.RemoveAt(2);
        delays[1] = 600;

        Assert.False(session.IsPlaying);
        Assert.Equal(new PlaybackStep(-1, 0, true), session.Advance(delays, loop: false, dropFrames: false));
        session.Start(1);
        Assert.Equal(new PlaybackStep(1, 600, false), session.Advance(delays, loop: false, dropFrames: false));
        Assert.Equal(new PlaybackStep(1, 0, true), session.Advance(delays, loop: false, dropFrames: false));
    }

    [Fact]
    public async Task Destructive_guard_skips_clean_state_and_serializes_dirty_prompts()
    {
        var guard = new DestructiveActionGuard();
        var promptCalls = 0;
        Assert.True(await guard.ConfirmAsync(false, () =>
        {
            promptCalls++;
            return Task.FromResult(false);
        }));
        Assert.Equal(0, promptCalls);

        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = guard.ConfirmAsync(true, () =>
        {
            promptCalls++;
            return pending.Task;
        });
        Assert.True(guard.IsPromptOpen);
        Assert.False(await guard.ConfirmAsync(true, () => Task.FromResult(true)));
        pending.SetResult(true);

        Assert.True(await first);
        Assert.False(guard.IsPromptOpen);
        Assert.Equal(1, promptCalls);
    }

    private sealed class FakeClock : IPlaybackClock
    {
        public long ElapsedMilliseconds { get; set; }
        public void Restart() { }
        public void Stop() { }
    }
}
