using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class MainWindow
{
    private readonly EditorStatisticsTracker _statistics = new();

    partial void InitializeStatistics()
    {
        _statistics.Changed += (_, snapshot) => ApplyStatistics(snapshot);
        _ = new EditorStatisticsRefreshBinding(
            handler => FrameInfoUpdated += handler,
            handler => ScalingChanged += handler,
            UpdateStatistics);
    }

    private void UpdateStatistics()
    {
        var metrics = _frames.Select(frame =>
        {
            var size = frame.SourcePixelSize;
            return new FrameMetric(size.Width, size.Height, frame.DelayMs);
        }).ToArray();
        _statistics.Update(EditorStatistics.Calculate(metrics, SelectedIndices(), _currentFrameIndex, RenderScaling));
    }

    private void ApplyStatistics(EditorStatisticsSnapshot snapshot)
    {
        StatsFrameCount.Text = snapshot.FrameCount.ToString();
        StatsDuration.Text = FormatTime(snapshot.TotalDurationMs);
        StatsDimensions.Text = snapshot.Dimensions;
        StatsScale.Text = $"{snapshot.DisplayScale * 100:0}%";
        StatsCurrentTime.Text = snapshot.CurrentTimeMs is { } current ? FormatTime(current) : "—";
        StatsAverageDelay.Text = snapshot.FrameCount == 0 ? "—" : $"{snapshot.AverageDelayMs:0.#} ms";
        StatsSelectedDelay.Text = snapshot.SelectedDelay;
    }

    private static string FormatTime(long milliseconds)
    {
        var value = TimeSpan.FromMilliseconds(milliseconds);
        return value.TotalHours >= 1
            ? value.ToString(@"h\:mm\:ss\.fff")
            : value.ToString(@"m\:ss\.fff");
    }
}
