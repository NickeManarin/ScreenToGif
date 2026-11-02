namespace ScreenToGif.Linux.Services;

public sealed record PlaybackPosition(int FrameIndex, long TimelineTimeMs, int RemainingMs, bool Completed);

public static class PlaybackTimeline
{
    public static int First(int frameCount) => frameCount == 0 ? -1 : 0;
    public static int Previous(int currentIndex, int frameCount) => frameCount == 0 ? -1 : Math.Clamp(currentIndex - 1, 0, frameCount - 1);
    public static int Next(int currentIndex, int frameCount) => frameCount == 0 ? -1 : Math.Clamp(currentIndex + 1, 0, frameCount - 1);
    public static int Last(int frameCount) => frameCount == 0 ? -1 : frameCount - 1;

    public static PlaybackPosition Resolve(
        IReadOnlyList<int> delays,
        int startIndex,
        long elapsedMs,
        bool loop)
    {
        if (delays.Count == 0)
            return new PlaybackPosition(-1, 0, 0, true);
        if (delays.Any(delay => delay <= 0))
            throw new ArgumentOutOfRangeException(nameof(delays), "Playback delays must be positive.");

        startIndex = Math.Clamp(startIndex, 0, delays.Count - 1);
        elapsedMs = Math.Max(0, elapsedMs);
        var total = delays.Sum(delay => (long)delay);
        var startTime = delays.Take(startIndex).Sum(delay => (long)delay);
        var absolute = startTime + elapsedMs;

        if (!loop && absolute >= total)
            return new PlaybackPosition(delays.Count - 1, total, 0, true);

        if (loop)
            absolute %= total;

        long boundary = 0;
        for (var index = 0; index < delays.Count; index++)
        {
            boundary += delays[index];
            if (absolute < boundary)
                return new PlaybackPosition(index, absolute, checked((int)(boundary - absolute)), false);
        }

        return new PlaybackPosition(delays.Count - 1, total, 0, true);
    }
}
