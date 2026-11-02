using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public enum BoardRecordingStage
{
    Stopped,
    Recording,
    Paused
}

/// <summary>One captured Board frame: where its PNG landed and how long it should be shown.</summary>
public sealed record BoardCapturedFrame(string Path, int DelayMs);

/// <summary>What a recording is fixed to when it starts: the rate it captures at and the size of every frame.</summary>
public sealed record BoardRecordingPlan(int FramesPerSecond, int FrameWidth, int FrameHeight);

/// <summary>Renders the Board as it is now and writes it as a PNG. The session never renders anything itself.</summary>
public interface IBoardFrameSink
{
    void Write(string path);
}

/// <summary>
/// The frames of one Board recording and the workspace they live in. It owns that workspace until
/// the editor takes the recording over, and deletes it otherwise.
/// </summary>
public sealed class BoardRecording : IDisposable
{
    private readonly List<BoardCapturedFrame> _frames = [];
    private readonly string _framesPath;
    private EditorWorkspace? _workspace;

    /// <summary>Takes ownership of <paramref name="workspace"/>, disposing it if its frames batch cannot be created.</summary>
    public BoardRecording(EditorWorkspace workspace)
    {
        try
        {
            _framesPath = workspace.CreateBatch(EditorArtifactKind.Frames);
        }
        catch
        {
            workspace.Dispose();
            throw;
        }

        _workspace = workspace;
    }

    public IReadOnlyList<BoardCapturedFrame> Frames => _frames;

    /// <summary>Where the next frame is written, numbered so the files sort in capture order.</summary>
    public string NextFramePath => Path.Combine(_framesPath, $"{_frames.Count:000000}.png");

    public static BoardRecording Create() => new(ProjectArchive.CreateWorkspace());

    public void Add(BoardCapturedFrame frame) => _frames.Add(frame);

    /// <summary>Hands the frames and their workspace to the editor, after which disposing this recording deletes nothing.</summary>
    public LoadedProject TakeProject()
    {
        var workspace = _workspace ?? throw new ObjectDisposedException(nameof(BoardRecording));
        _workspace = null;
        return new LoadedProject(workspace, _frames.Select(frame => new EditorFrame(frame.Path, frame.DelayMs)).ToArray());
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
    }
}

/// <summary>
/// The Board's recording state machine: whether pressing the pointer records, when a frame is
/// taken and how long it is shown, where a recording must stop, and what happens to it at the
/// end. An injected clock and sink keep every decision testable; the window only schedules
/// <see cref="Tick"/> and mirrors the state onto its controls.
/// </summary>
public sealed class BoardRecordingSession(
    IPlaybackClock clock,
    IBoardFrameSink sink,
    Func<BoardRecordingPlan> plan,
    Func<BoardRecording> openRecording)
{
    private BoardRecording? _recording;
    private long _lastTickMs;
    private bool _nextFrameStartsTheClock = true;
    private bool _startedByGesture;
    private bool _autoRecordInvertedByCtrl;

    public BoardRecordingStage Stage { get; private set; } = BoardRecordingStage.Stopped;

    /// <summary>Whether pressing the pointer records, including the Ctrl-held inversion; the button shows this.</summary>
    public bool AutoRecord { get; private set; } = true;

    /// <summary>The gap the timer schedules between frames, fixed when the recording starts.</summary>
    public int IntervalMs { get; private set; }

    /// <summary>The most frames the editor can still save at this recording's frame size.</summary>
    public int FrameLimit { get; private set; }

    public int FrameCount => _recording?.Frames.Count ?? 0;

    public bool HasFrames => FrameCount > 0;

    /// <summary>True once the recording holds as many frames as the editor can save; it cannot resume past that.</summary>
    public bool IsAtFrameLimit => _recording is not null && FrameCount >= FrameLimit;

    /// <summary>The user toggled Auto Record.</summary>
    public void SetAutoRecord(bool on) => AutoRecord = on;

    /// <summary>Holding Ctrl inverts Auto Record once; returns whether it changed.</summary>
    public bool CtrlPressed()
    {
        if (_autoRecordInvertedByCtrl)
            return false;

        _autoRecordInvertedByCtrl = true;
        AutoRecord = !AutoRecord;
        return true;
    }

    /// <summary>Releasing Ctrl undoes its inversion; returns whether Auto Record changed.</summary>
    public bool CtrlReleased()
    {
        if (!_autoRecordInvertedByCtrl)
            return false;

        _autoRecordInvertedByCtrl = false;
        AutoRecord = !AutoRecord;
        return true;
    }

    /// <summary>A window that loses focus never hears Ctrl come up, so it is treated as released.</summary>
    public bool Deactivated() => CtrlReleased();

    /// <summary>With Auto Record on, pressing the pointer starts or resumes recording; returns whether it did.</summary>
    public bool PointerPressed()
    {
        if (!AutoRecord || Stage == BoardRecordingStage.Recording || !Start())
            return false;

        _startedByGesture = true;
        return true;
    }

    /// <summary>
    /// Releasing the pointer pauses what pressing it started. A recording started from the Record
    /// command keeps going between strokes, which is the point of recording by hand.
    /// </summary>
    public bool PointerReleased() =>
        Stage == BoardRecordingStage.Recording && _startedByGesture && Pause();

    /// <summary>
    /// Starts a stopped session or resumes a paused one; returns false when it was already
    /// recording or already holds as many frames as the editor can save. A fresh start fixes the
    /// frame rate and the frame limit and opens the workspace; if that fails the session stays
    /// stopped and the failure reaches the caller to report.
    /// </summary>
    public bool Start()
    {
        if (Stage == BoardRecordingStage.Recording || IsAtFrameLimit)
            return false;

        if (Stage == BoardRecordingStage.Stopped)
        {
            var fixedPlan = plan();
            _recording ??= openRecording();
            IntervalMs = 1000 / Math.Clamp(fixedPlan.FramesPerSecond, 1, 60);
            FrameLimit = EditorResourceLimits.MaximumFramesAt(fixedPlan.FrameWidth, fixedPlan.FrameHeight);
            clock.Restart();
        }

        Stage = BoardRecordingStage.Recording;
        _startedByGesture = false;
        _nextFrameStartsTheClock = true;
        return true;
    }

    /// <summary>Pauses a recording session, keeping its frames; returns false when it was not recording.</summary>
    public bool Pause()
    {
        if (Stage != BoardRecordingStage.Recording)
            return false;

        Stage = BoardRecordingStage.Paused;
        return true;
    }

    /// <summary>
    /// Captures one frame. The first frame after a start or a resume carries the scheduled
    /// interval; every later frame carries the gap measured since the previous one. A sink
    /// failure pauses the session and reaches the caller; reaching the frame limit pauses it too,
    /// with <see cref="IsAtFrameLimit"/> set so the caller can say why.
    /// </summary>
    public BoardCapturedFrame Tick()
    {
        if (Stage != BoardRecordingStage.Recording || _recording is null)
            throw new InvalidOperationException("The Board is not recording.");

        var now = clock.ElapsedMilliseconds;
        var delay = _nextFrameStartsTheClock ? IntervalMs : (int)Math.Max(1, now - _lastTickMs);
        var path = _recording.NextFramePath;

        try
        {
            sink.Write(path);
        }
        catch
        {
            Stage = BoardRecordingStage.Paused;
            throw;
        }

        var frame = new BoardCapturedFrame(path, delay);
        _recording.Add(frame);
        _lastTickMs = now;
        _nextFrameStartsTheClock = false;

        if (IsAtFrameLimit)
            Stage = BoardRecordingStage.Paused;

        return frame;
    }

    /// <summary>Ends a session holding frames and returns true; with no frames it does nothing and returns false.</summary>
    public bool Stop()
    {
        if (!HasFrames)
            return false;

        EndRecording();
        return true;
    }

    /// <summary>
    /// Hands the recording to the editor exactly once, or null when there is nothing to hand over,
    /// in which case the workspace the session opened is deleted.
    /// </summary>
    public LoadedProject? TakeProject()
    {
        EndRecording();

        var recording = _recording;
        _recording = null;

        if (recording is not { Frames.Count: > 0 })
        {
            recording?.Dispose();
            return null;
        }

        return recording.TakeProject();
    }

    /// <summary>
    /// Deletes the recording and everything it wrote once <paramref name="confirm"/> agrees, and
    /// returns whether it did. A recording that keeps going while the question is open is still
    /// going if the answer is no.
    /// </summary>
    public async Task<bool> DiscardAsync(Func<Task<bool>> confirm)
    {
        if (!HasFrames)
            return false;

        if (!await confirm())
            return false;

        EndRecording();
        _recording?.Dispose();
        _recording = null;
        return true;
    }

    private void EndRecording()
    {
        if (Stage != BoardRecordingStage.Stopped)
            clock.Stop();

        Stage = BoardRecordingStage.Stopped;
        _startedByGesture = false;
        _nextFrameStartsTheClock = true;
    }
}
