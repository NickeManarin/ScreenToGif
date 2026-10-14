using System.Diagnostics;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// The monotonic time base a recording paces against. Injected so the session's pacing and
/// pre-start countdown can be driven in irregular steps from a test without a real wait.
/// </summary>
public interface IRecordingClock
{
    long ElapsedMilliseconds { get; }
}

/// <summary>The production clock: a <see cref="Stopwatch"/> started when the recording begins.</summary>
public sealed class StopwatchRecordingClock : IRecordingClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
}
