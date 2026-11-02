using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class EditorStatisticsTests
{
    [Fact]
    public void Empty_and_single_frame_statistics_use_clear_missing_states()
    {
        var empty = EditorStatistics.Calculate([], [], -1, 1.25);
        Assert.Equal(0, empty.FrameCount);
        Assert.Equal(0, empty.TotalDurationMs);
        Assert.Equal("—", empty.Dimensions);
        Assert.Null(empty.CurrentTimeMs);
        Assert.Equal("—", empty.SelectedDelay);
        Assert.Equal(1.25, empty.DisplayScale);

        var single = EditorStatistics.Calculate([new FrameMetric(80, 60, 125)], [0], 0, 1);
        Assert.Equal("80 × 60", single.Dimensions);
        Assert.Equal(125, single.TotalDurationMs);
        Assert.Equal(0, single.CurrentTimeMs);
        Assert.Equal(125, single.AverageDelayMs);
        Assert.Equal("125 ms", single.SelectedDelay);
    }

    [Fact]
    public void Mixed_size_delay_and_current_time_follow_changing_timeline_state()
    {
        var frames = new[]
        {
            new FrameMetric(80, 60, 40),
            new FrameMetric(120, 90, 160),
            new FrameMetric(80, 60, 100)
        };
        var snapshot = EditorStatistics.Calculate(frames, [0, 2], 2, 1.5);

        Assert.Equal(3, snapshot.FrameCount);
        Assert.Equal(300, snapshot.TotalDurationMs);
        Assert.Equal("Mixed", snapshot.Dimensions);
        Assert.Equal(200, snapshot.CurrentTimeMs);
        Assert.Equal(100, snapshot.AverageDelayMs);
        Assert.Equal("Mixed", snapshot.SelectedDelay);
        Assert.Equal(1.5, snapshot.DisplayScale);
    }

    [Fact]
    public void Tracker_notifies_only_when_a_projected_value_changes()
    {
        var tracker = new EditorStatisticsTracker();
        var notifications = new List<EditorStatisticsSnapshot>();
        tracker.Changed += (_, snapshot) => notifications.Add(snapshot);
        var empty = EditorStatistics.Calculate([], [], -1, 1);
        var single = EditorStatistics.Calculate([new FrameMetric(8, 6, 100)], [0], 0, 1);
        var mixedFrames = new[] { new FrameMetric(8, 6, 40), new FrameMetric(12, 9, 160) };
        var mixedUnselected = EditorStatistics.Calculate(mixedFrames, [], 0, 1);
        var mixedSelected = EditorStatistics.Calculate(mixedFrames, [0, 1], 0, 1);
        var secondSelected = EditorStatistics.Calculate(mixedFrames, [1], 0, 1);

        foreach (var snapshot in new[]
                 {
                     empty, empty, single, mixedUnselected, mixedSelected, mixedSelected, secondSelected, empty
                 })
            tracker.Update(snapshot);

        Assert.Equal([empty, single, mixedUnselected, mixedSelected, secondSelected, empty], notifications);
        Assert.Equal("Mixed", mixedUnselected.Dimensions);
        Assert.Equal("—", mixedUnselected.SelectedDelay);
        Assert.Equal("Mixed", mixedSelected.SelectedDelay);
        Assert.Equal("160 ms", secondSelected.SelectedDelay);
        Assert.Equal(0, notifications[^1].FrameCount);
    }

    [Fact]
    public void Refresh_binding_projects_a_display_scale_change_without_an_editor_change()
    {
        var tracker = new EditorStatisticsTracker();
        var notifications = new List<EditorStatisticsSnapshot>();
        tracker.Changed += (_, snapshot) => notifications.Add(snapshot);
        var frames = new[] { new FrameMetric(80, 60, 100) };
        Action? editorChanged = null;
        EventHandler? displayScaleChanged = null;
        var displayScale = 1d;
        var binding = new EditorStatisticsRefreshBinding(
            handler => editorChanged += handler,
            handler => displayScaleChanged += handler,
            () => tracker.Update(EditorStatistics.Calculate(frames, [0], 0, displayScale)));

        editorChanged!();
        displayScale = 1.5;
        displayScaleChanged!(null, EventArgs.Empty);
        displayScaleChanged(null, EventArgs.Empty);

        Assert.Equal(2, notifications.Count);
        Assert.Equal(1.5, notifications[^1].DisplayScale);
        GC.KeepAlive(binding);
    }
}
