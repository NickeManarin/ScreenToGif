using System.Diagnostics;

namespace ScreenToGif.Linux.Services;

public interface IPlaybackClock
{
    long ElapsedMilliseconds { get; }
    void Restart();
    void Stop();
}

public sealed class StopwatchPlaybackClock : IPlaybackClock
{
    private readonly Stopwatch _stopwatch = new();

    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    public void Restart() => _stopwatch.Restart();
    public void Stop() => _stopwatch.Stop();
}

public sealed record PlaybackStep(int FrameIndex, int DelayMs, bool Completed);

/// <summary>Deterministic playback state; UI timers only schedule returned steps.</summary>
public sealed class PlaybackSession(IPlaybackClock clock)
{
    private int _startIndex;
    private int _nextIndex;

    public bool IsPlaying { get; private set; }

    public void Start(int currentIndex)
    {
        _startIndex = Math.Max(0, currentIndex);
        _nextIndex = _startIndex;
        IsPlaying = true;
        clock.Restart();
    }

    public void Stop()
    {
        IsPlaying = false;
        clock.Stop();
    }

    public PlaybackStep Advance(IReadOnlyList<int> delays, bool loop, bool dropFrames)
    {
        if (!IsPlaying || delays.Count == 0)
            return new PlaybackStep(-1, 0, true);

        if (dropFrames)
        {
            var position = PlaybackTimeline.Resolve(delays, _startIndex, clock.ElapsedMilliseconds, loop);
            if (position.Completed)
                return new PlaybackStep(position.FrameIndex, 0, true);
            _nextIndex = position.FrameIndex + 1;
            return new PlaybackStep(position.FrameIndex, position.RemainingMs, false);
        }

        if (_nextIndex >= delays.Count)
        {
            if (!loop)
                return new PlaybackStep(delays.Count - 1, 0, true);
            _nextIndex = 0;
        }

        var index = _nextIndex++;
        return new PlaybackStep(index, delays[index], false);
    }
}

public sealed record PlaybackControlState(
    bool FirstEnabled,
    bool PreviousEnabled,
    bool NextEnabled,
    bool LastEnabled,
    bool PlayEnabled,
    bool OptionsEnabled)
{
    public static PlaybackControlState Calculate(int frameCount, int currentIndex, bool operationRunning)
    {
        var hasFrames = frameCount > 0;
        var current = Math.Clamp(currentIndex, 0, Math.Max(0, frameCount - 1));
        return new PlaybackControlState(
            hasFrames && current > 0,
            hasFrames && current > 0,
            hasFrames && current < frameCount - 1,
            hasFrames && current < frameCount - 1,
            hasFrames && !operationRunning,
            hasFrames);
    }
}
