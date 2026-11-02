namespace ScreenToGif.Linux.Services;

public sealed record FrameMetric(int Width, int Height, int DelayMs);

public sealed record EditorStatisticsSnapshot(
    int FrameCount,
    long TotalDurationMs,
    string Dimensions,
    long? CurrentTimeMs,
    double AverageDelayMs,
    string SelectedDelay,
    double DisplayScale);

public static class EditorStatistics
{
    public static EditorStatisticsSnapshot Calculate(
        IReadOnlyList<FrameMetric> frames,
        IEnumerable<int> selectedIndices,
        int currentIndex,
        double displayScale)
    {
        var selected = selectedIndices.Where(index => index >= 0 && index < frames.Count).Distinct().Order().ToArray();
        var dimensions = frames.Count == 0
            ? "—"
            : frames.Select(frame => (frame.Width, frame.Height)).Distinct().Count() == 1
                ? $"{frames[0].Width} × {frames[0].Height}"
                : "Mixed";
        var selectedDelays = selected.Select(index => frames[index].DelayMs).Distinct().ToArray();
        var selectedDelay = selectedDelays.Length switch
        {
            0 => "—",
            1 => $"{selectedDelays[0]} ms",
            _ => "Mixed"
        };
        long? currentTime = currentIndex >= 0 && currentIndex < frames.Count
            ? frames.Take(currentIndex).Sum(frame => (long)frame.DelayMs)
            : null;

        return new EditorStatisticsSnapshot(
            frames.Count,
            frames.Sum(frame => (long)frame.DelayMs),
            dimensions,
            currentTime,
            frames.Count == 0 ? 0 : frames.Average(frame => frame.DelayMs),
            selectedDelay,
            displayScale);
    }
}

public sealed class EditorStatisticsTracker
{
    public EditorStatisticsSnapshot? Current { get; private set; }
    public event EventHandler<EditorStatisticsSnapshot>? Changed;

    public void Update(EditorStatisticsSnapshot snapshot)
    {
        if (snapshot == Current)
            return;
        Current = snapshot;
        Changed?.Invoke(this, snapshot);
    }
}

public sealed class EditorStatisticsRefreshBinding
{
    public EditorStatisticsRefreshBinding(
        Action<Action> subscribeToEditorChanges,
        Action<EventHandler> subscribeToDisplayScaleChanges,
        Action refresh)
    {
        subscribeToEditorChanges(refresh);
        subscribeToDisplayScaleChanges((_, _) => refresh());
    }
}
