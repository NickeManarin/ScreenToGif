using Avalonia;
using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services.Capture;

public sealed class RecordingErrorEventArgs(string message, Exception? cause = null) : EventArgs
{
    /// <summary>Plain words for the user. Never an exception dump.</summary>
    public string Message { get; } = message;

    public Exception? Cause { get; } = cause;
}

/// <summary>
/// The platform-neutral recording state machine. It owns stage transitions, capture pacing, and
/// per-frame delay accounting, and reaches the outside world only through an injected clock, screen
/// source, and frame writer, so every rule below is exercised without a display.
///
/// Nothing here waits: <see cref="Tick"/> performs whatever the clock says is due and reports when
/// it should next be called. A host drives it from a loop; a test drives it directly.
/// </summary>
public sealed class RecordingSession : IAsyncDisposable
{
    private const int MillisecondsPerSecond = 1000;

    /// <summary>
    /// How much captured-but-unencoded pixel data the recording may hold before it pauses itself.
    /// Sixty-four frames of a 1080p desktop, which is several seconds of headroom at any frame rate
    /// the recorder offers, and far short of what would trouble a machine that can run the editor.
    /// </summary>
    private const long MaximumPendingEncodeBytes = 512L * 1024 * 1024;

    private readonly IScreenSource _source;
    private readonly IFrameWriter _writer;
    private readonly IRecordingClock _clock;
    private readonly Func<PixelRect> _regionProvider;
    private readonly List<int> _delays = [];

    private RecordingStage _stage = RecordingStage.Stopped;
    private long _nextCaptureDueMs;
    private long _lastCaptureMs;
    private long _preStartDeadlineMs;
    private long _pausedAtMs;
    private PixelSize _lockedSize;
    private bool _faulted;
    private bool _handedOff;
    private bool _disposed;

    public RecordingSession(
        IScreenSource source,
        IFrameWriter writer,
        IRecordingClock clock,
        RecordingSettings settings,
        Func<PixelRect> regionProvider)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
    }

    /// <summary>Raised whenever the stage, the frame count, or the countdown changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised once when the recording faults. Frames already written are kept.</summary>
    public event EventHandler<RecordingErrorEventArgs>? Error;

    public RecordingSettings Settings { get; }

    public RecordingStage Stage => _stage;

    public int FrameCount => _delays.Count;

    /// <summary>Whole seconds left before capture starts, or zero when no countdown is running.</summary>
    public int PreStartRemainingSeconds { get; private set; }

    /// <summary>
    /// Whether this mode captures on a schedule. Manual captures only when asked, which is why it
    /// offers Snap instead of Record and has nothing to pause.
    /// </summary>
    private static bool IsPaced(RecorderCaptureMode mode) => mode != RecorderCaptureMode.Manual;

    public bool CanRecord => !_disposed && IsPaced(Settings.Mode) &&
                             _stage is RecordingStage.Stopped or RecordingStage.Paused;

    public bool CanSnap => !_disposed && !IsPaced(Settings.Mode) &&
                           _stage is RecordingStage.Stopped or RecordingStage.Recording;

    public bool CanPause => !_disposed && IsPaced(Settings.Mode) &&
                            _stage is RecordingStage.PreStarting or RecordingStage.Recording;

    public bool CanStop => !_disposed &&
                           (_stage is RecordingStage.PreStarting or RecordingStage.Recording or RecordingStage.Paused ||
                            FrameCount > 0);

    public bool CanDiscard => !_disposed &&
                              (FrameCount > 0 || _stage is RecordingStage.Recording or RecordingStage.Paused);

    /// <summary>
    /// The capture size is fixed once a recording starts, as it is on Windows. The origin is not:
    /// the frame can still be moved, and what it captures moves with it.
    /// </summary>
    public bool CanChangeRegion => !_disposed && _stage == RecordingStage.Stopped;

    /// <summary>Pausing re-opens the frequency field; recording does not.</summary>
    public bool CanChangeFrequency => !_disposed && _stage is RecordingStage.Stopped or RecordingStage.Paused;

    /// <summary>
    /// Starts, or resumes after a pause. With pre-start on, a fresh start counts down first;
    /// resuming never counts down again.
    /// </summary>
    public void Record()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!CanRecord)
            return;

        var now = _clock.ElapsedMilliseconds;

        if (_stage == RecordingStage.Paused)
        {
            ResumeFromPause(now);
            return;
        }

        _faulted = false;

        // The size is taken here rather than at the first capture because the countdown is already
        // part of the recording: CanChangeRegion is false throughout PreStarting, so the window has
        // stopped accepting resizes by then, and a pause and resume during the countdown reaches
        // capture without passing through the start path again.
        LockRegionSize();

        if (Settings.PreStartEnabled && Settings.PreStartSeconds > 0)
        {
            _preStartDeadlineMs = now + (long)Settings.PreStartSeconds * MillisecondsPerSecond;
            PreStartRemainingSeconds = Settings.PreStartSeconds;
            SetStage(RecordingStage.PreStarting);
            return;
        }

        BeginCapturing(now);
    }

    /// <summary>Captures exactly one frame. Manual mode only.</summary>
    public void Snap()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!CanSnap)
            return;

        var now = _clock.ElapsedMilliseconds;

        // Every Snap is the user asking for a frame, so every Snap after a failure is the retry
        // that Resume is for the paced modes, and deserves an answer of its own.
        _faulted = false;

        if (_stage == RecordingStage.Stopped)
        {
            LockRegionSize();
            _lastCaptureMs = now;
            SetStage(RecordingStage.Recording);
        }

        CaptureFrame(now);
        RaiseStateChanged();
    }

    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!CanPause)
            return;

        Park(RecordingStage.Paused);
    }

    /// <summary>
    /// Ends the recording and yields its frames. A recording that never captured anything, or one
    /// stopped during the countdown, yields nothing and removes its own files. Stopping a session
    /// that already handed its frames over yields nothing and deletes nothing, because those frames
    /// belong to whoever took them.
    /// </summary>
    public async Task<IReadOnlyList<EditorFrame>> StopAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Stop is reachable twice: the button and the F8 key both stay live while the first stop
        // drains the writer, and the second command runs on the recording thread before the first
        // one's answer reaches the window. Without this the second stop sees an empty frame list,
        // takes the discard branch below, and deletes the files the first stop just returned.
        if (_handedOff)
            return [];

        if (FrameCount == 0)
        {
            _writer.Discard();
            ResetToStopped();
            return [];
        }

        var paths = await _writer.CompleteAsync(cancellationToken);

        // A writer that failed part way still yields what it managed to encode. The failure is
        // reported here as well as at capture time, because a write that failed after the last
        // capture would otherwise reach the user nowhere; RaiseError reports one fault only once.
        if (_writer.Failure is { } failure)
            RaiseError(failure.Message, failure);

        var frames = paths
            .Select((path, index) => new EditorFrame(path, _delays[Math.Min(index, _delays.Count - 1)]))
            .ToArray();

        _handedOff = frames.Length > 0;
        ResetToStopped();
        return frames;
    }

    /// <summary>
    /// Throws the recording away, deleting only its own batch directory. A recording already handed
    /// over is not this session's to throw away, so Discard leaves it alone for the same reason
    /// Stop and Dispose do.
    /// </summary>
    public void Discard()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_handedOff)
            return;

        SetStage(RecordingStage.Discarding);
        _writer.Discard();
        ResetToStopped();
    }

    /// <summary>
    /// Performs the work the clock says is due and returns how many milliseconds until the next
    /// due moment, or <see cref="Timeout.Infinite"/> when nothing is scheduled.
    /// </summary>
    public int Tick()
    {
        if (_disposed)
            return Timeout.Infinite;

        var now = _clock.ElapsedMilliseconds;

        switch (_stage)
        {
            case RecordingStage.PreStarting:
                return TickPreStart(now);
            case RecordingStage.Recording when Settings.Mode == RecorderCaptureMode.Manual:
                return Timeout.Infinite;
            case RecordingStage.Recording:
                return TickCapture(now);
            default:
                return Timeout.Infinite;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Frames handed to the editor are the editor's now; only an abandoned recording is deleted.
        if (!_handedOff)
            _writer.Discard();

        await _writer.DisposeAsync();
        _source.Dispose();
    }

    private int TickPreStart(long now)
    {
        var remainingMs = _preStartDeadlineMs - now;
        if (remainingMs > 0)
        {
            var remainingSeconds = (int)((remainingMs + MillisecondsPerSecond - 1) / MillisecondsPerSecond);
            if (remainingSeconds != PreStartRemainingSeconds)
            {
                PreStartRemainingSeconds = remainingSeconds;
                RaiseStateChanged();
            }

            return (int)Math.Min(remainingMs, MillisecondsPerSecond);
        }

        BeginCapturing(now);
        return _stage == RecordingStage.Recording ? TickCapture(now) : Timeout.Infinite;
    }

    private int TickCapture(long now)
    {
        if (now < _nextCaptureDueMs)
            return (int)Math.Min(_nextCaptureDueMs - now, int.MaxValue);

        CaptureFrame(now);
        if (_stage != RecordingStage.Recording)
            return Timeout.Infinite;

        var interval = Settings.CaptureIntervalMilliseconds;
        _nextCaptureDueMs += interval;

        // A capture slower than its interval is never skipped and never bursts to catch up.
        if (_nextCaptureDueMs <= now)
            _nextCaptureDueMs = now + interval;

        RaiseStateChanged();
        return (int)Math.Max(0, Math.Min(_nextCaptureDueMs - _clock.ElapsedMilliseconds, int.MaxValue));
    }

    private void BeginCapturing(long now)
    {
        PreStartRemainingSeconds = 0;
        _lastCaptureMs = now;
        _nextCaptureDueMs = now;
        SetStage(RecordingStage.Recording);
    }

    private void ResumeFromPause(long now)
    {
        // Resuming is the user asking to try again, including after a fault paused the recording.
        // Without clearing the latch the next failure would be swallowed and the bar would show a
        // healthy paused recording that is no longer capturing anything.
        _faulted = false;

        var pausedForMs = now - _pausedAtMs;
        _nextCaptureDueMs += pausedForMs;
        _lastCaptureMs += pausedForMs;
        SetStage(RecordingStage.Recording);
    }

    private void CaptureFrame(long now)
    {
        // The writer already phrased its failure for the user; repeating it here would lose the cause.
        if (_writer.Failure is { } pending)
        {
            FaultWith(pending.Message, pending);
            return;
        }

        // Resuming at the bound is allowed, since Paused is where every failure leaves the user, but
        // it must not take the one frame that would make the project unsavable.
        if (_delays.Count >= MaximumFrames)
        {
            PauseAtFrameCap();
            return;
        }

        CapturedFrame frame;
        try
        {
            frame = _source.Capture(CurrentRegion(), Settings.ShowCursor);
        }
        catch (ScreenCaptureException exception)
        {
            FaultWith(exception.Message, exception);
            return;
        }

        // The measured gap belongs to the previous frame: it is how long that frame was on screen.
        if (Settings.MeasuresRealDelays && _delays.Count > 0)
            _delays[^1] = (int)Math.Max(1, now - _lastCaptureMs);

        _delays.Add(Settings.NominalPlaybackDelayMilliseconds);
        _lastCaptureMs = now;
        _writer.Write(frame);

        if (_delays.Count >= MaximumFrames)
        {
            PauseAtFrameCap();
            return;
        }

        // Encoding runs off this thread, so a region and frame rate the encoder cannot keep up with
        // makes the backlog grow for as long as the recording runs. Left alone it ends in the
        // process being killed, which takes the recording with it. Pausing is the same answer a
        // full disk gets: the frames already encoded are kept and Stop still opens them.
        if (IsPaced(Settings.Mode) && _writer.PendingBytes > MaximumPendingEncodeBytes)
            PauseAtEncoderBacklog();
    }

    /// <summary>
    /// The most frames the editor can still save at the locked region size. The frame limit alone is
    /// not enough: at 1920x1080 the decoded-pixel limit arrives after 482 frames.
    /// </summary>
    private int MaximumFrames => EditorResourceLimits.MaximumFramesAt(_lockedSize.Width, _lockedSize.Height);

    private void PauseAtFrameCap()
    {
        Park(RecordingStage.Paused);
        RaiseError(
            $"Recording paused at {MaximumFrames:N0} frames, the most the editor can save at this size. " +
            "Stop to keep what was recorded.");
    }

    private void PauseAtEncoderBacklog()
    {
        Park(RecordingStage.Paused);
        RaiseError(
            "Recording paused: frames are arriving faster than they can be saved. Stop to keep what " +
            "was recorded, or record a smaller area or at a lower frame rate.");
    }

    /// <summary>
    /// Stops capturing and starts the pause clock. Every route into a stage the recording can be
    /// resumed from comes through here, because <see cref="ResumeFromPause"/> is the only reader of
    /// that timestamp and nothing re-derives it: a route that set the stage without it would charge
    /// the whole interval since the previous pause to the frame spanning the resume, and write a
    /// wrong delay into the project with nothing to signal it.
    /// </summary>
    private void Park()
    {
        _pausedAtMs = _clock.ElapsedMilliseconds;
        PreStartRemainingSeconds = 0;
    }

    private void Park(RecordingStage stage)
    {
        Park();
        SetStage(stage);
    }

    private void FaultWith(string message, Exception cause)
    {
        // Pausing is how a paced recording stops capturing until the user asks for more. A Manual
        // recording captures only when asked, so it has nothing to stop, and Paused is a stage it
        // could not leave: Snap is unavailable there and Record is unavailable in Manual at all.
        // Leaving the stage alone is what makes the next Snap the retry.
        if (IsPaced(Settings.Mode))
            Park(_stage == RecordingStage.PreStarting ? RecordingStage.Stopped : RecordingStage.Paused);
        else
            Park();

        RaiseError(message, cause);
    }

    /// <summary>
    /// A recording's frames must all be one size, or the project they form is not one the editor
    /// can open, so the size is taken once and the frame can only be moved after that.
    /// </summary>
    private void LockRegionSize() => _lockedSize = _regionProvider().Size;

    private PixelRect CurrentRegion()
    {
        var region = _regionProvider();
        return new PixelRect(region.X, region.Y, _lockedSize.Width, _lockedSize.Height);
    }

    private void ResetToStopped()
    {
        _delays.Clear();
        PreStartRemainingSeconds = 0;
        _nextCaptureDueMs = 0;
        _lastCaptureMs = 0;
        _preStartDeadlineMs = 0;
        SetStage(RecordingStage.Stopped);
    }

    private void SetStage(RecordingStage stage)
    {
        if (_stage == stage)
            return;

        _stage = stage;
        RaiseStateChanged();
    }

    /// <summary>
    /// An immutable reading of everything a host renders. The session runs on one thread, so a host
    /// on another thread reads this rather than the live properties.
    /// </summary>
    public RecordingStatus Snapshot() => new(
        Stage, FrameCount, PreStartRemainingSeconds,
        CanRecord, CanSnap, CanPause, CanStop, CanDiscard, CanChangeRegion, CanChangeFrequency);

    /// <summary>
    /// The reading a host shows before any session exists: stopped, nothing captured, and whichever
    /// of Record and Snap the mode offers. It lives beside the rules it encodes rather than in the
    /// window, because the window's copy was the one nothing would catch drifting.
    /// </summary>
    public static RecordingStatus IdleStatus(RecorderCaptureMode mode) => new(
        RecordingStage.Stopped, FrameCount: 0, PreStartRemainingSeconds: 0,
        CanRecord: IsPaced(mode),
        CanSnap: !IsPaced(mode),
        CanPause: false,
        CanStop: false,
        CanDiscard: false,
        CanChangeRegion: true,
        CanChangeFrequency: true);

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private void RaiseError(string message, Exception? cause = null)
    {
        // One recording reports one failure; a loop that keeps failing must not flood the user.
        if (cause is not null)
        {
            if (_faulted)
                return;
            _faulted = true;
        }

        Error?.Invoke(this, new RecordingErrorEventArgs(message, cause));
    }
}
