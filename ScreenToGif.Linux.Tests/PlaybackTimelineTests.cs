using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class PlaybackTimelineTests
{
    [Fact]
    public void Navigation_is_boundary_safe_for_empty_single_and_multi_frame_timelines()
    {
        Assert.Equal(-1, PlaybackTimeline.First(0));
        Assert.Equal(-1, PlaybackTimeline.Last(0));
        Assert.Equal(0, PlaybackTimeline.Previous(0, 1));
        Assert.Equal(0, PlaybackTimeline.Next(0, 1));
        Assert.Equal(0, PlaybackTimeline.First(4));
        Assert.Equal(3, PlaybackTimeline.Last(4));
        Assert.Equal(0, PlaybackTimeline.Previous(0, 4));
        Assert.Equal(3, PlaybackTimeline.Next(3, 4));
    }

    [Fact]
    public void Variable_delays_resolve_exact_timeline_boundaries()
    {
        var delays = new[] { 40, 160, 80 };
        Assert.Equal(0, PlaybackTimeline.Resolve(delays, 0, 0, false).FrameIndex);
        Assert.Equal(1, PlaybackTimeline.Resolve(delays, 0, 40, false).FrameIndex);
        Assert.Equal(1, PlaybackTimeline.Resolve(delays, 0, 199, false).FrameIndex);
        Assert.Equal(2, PlaybackTimeline.Resolve(delays, 0, 200, false).FrameIndex);
        Assert.Equal(50, PlaybackTimeline.Resolve([100, 100, 100], 0, 250, false).RemainingMs);
        var complete = PlaybackTimeline.Resolve(delays, 0, 280, false);
        Assert.True(complete.Completed);
        Assert.Equal(2, complete.FrameIndex);
    }

    [Fact]
    public void Looping_wraps_and_scheduler_lag_drops_directly_to_elapsed_frame()
    {
        var delays = new[] { 30, 70, 50 };
        Assert.Equal(0, PlaybackTimeline.Resolve(delays, 0, 150, true).FrameIndex);
        Assert.Equal(1, PlaybackTimeline.Resolve(delays, 0, 190, true).FrameIndex);
        Assert.Equal(2, PlaybackTimeline.Resolve(delays, 0, 260, true).FrameIndex);
        Assert.Equal(2, PlaybackTimeline.Resolve(delays, 0, 149, false).FrameIndex);
    }

    [Fact]
    public void Restart_from_selection_and_mutated_timing_use_current_snapshot()
    {
        Assert.Equal(1, PlaybackTimeline.Resolve([100, 200, 300], 1, 0, false).FrameIndex);
        Assert.Equal(2, PlaybackTimeline.Resolve([100, 200, 300], 1, 200, false).FrameIndex);
        Assert.Equal(1, PlaybackTimeline.Resolve([20, 500, 20], 1, 200, false).FrameIndex);
        Assert.True(PlaybackTimeline.Resolve([], 0, 0, false).Completed);
        Assert.Throws<ArgumentOutOfRangeException>(() => PlaybackTimeline.Resolve([0], 0, 0, false));
    }
}
