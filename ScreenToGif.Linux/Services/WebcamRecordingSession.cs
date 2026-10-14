using ScreenToGif.Linux.Models;
using System.Buffers;
using System.Threading.Channels;

namespace ScreenToGif.Linux.Services;

/// <summary>One decoded camera frame as tightly packed 8-bit BGRA.</summary>
public sealed record CameraFrame(int Width, int Height, byte[] Bgra)
{
    public int ByteLength => Width * Height * 4;
}

public enum WebcamRecordingStage
{
    Stopped,
    Recording,
    Paused
}

/// <summary>
/// Deterministic recording state: decides when a frame is due on the recording clock, remembers what was
/// captured, and turns captures into editor frames whose delays measure the recorded time between them
/// while excluding pauses. UI timers only ask whether a capture is due.
/// </summary>
public sealed class WebcamRecordingSession(IPlaybackClock clock)
{
    public const int MinimumFps = 1;
    public const int MaximumFps = 60;

    private readonly List<(string Path, long CapturedAtMs)> _frames = [];
    private long _accumulatedMs;
    private long _nextDueMs;
    private int _nextFrameIndex;

    public WebcamRecordingStage Stage { get; private set; } = WebcamRecordingStage.Stopped;

    public int IntervalMs { get; private set; }

    /// <summary>How many frames this recording may hold, from <see cref="Start"/>.</summary>
    public int MaximumFrames { get; private set; } = int.MaxValue;

    /// <summary>True once the recording holds every frame it is allowed to.</summary>
    public bool IsAtFrameLimit => _frames.Count >= MaximumFrames;

    public int FrameCount => _frames.Count;

    public bool HasFrames => _frames.Count > 0;

    public IReadOnlyList<string> FramePaths => _frames.Select(frame => frame.Path).ToArray();

    /// <summary>Milliseconds spent recording, excluding pauses.</summary>
    public long ActiveMilliseconds =>
        Stage == WebcamRecordingStage.Recording ? _accumulatedMs + clock.ElapsedMilliseconds : _accumulatedMs;

    public bool IsCaptureDue => Stage == WebcamRecordingStage.Recording && ActiveMilliseconds >= _nextDueMs;

    /// <summary>How many frames of one capture size a recording may take; see <see cref="EditorResourceLimits.MaximumFramesAt"/>.</summary>
    public static int MaximumFramesFor(CameraCaptureFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return EditorResourceLimits.MaximumFramesAt(format.Width, format.Height);
    }

    public static int IntervalFor(int fps)
    {
        if (fps is < MinimumFps or > MaximumFps)
            throw new ArgumentOutOfRangeException(nameof(fps), fps, $"Frame rate must be between {MinimumFps} and {MaximumFps}.");
        return Math.Max(1, (int)Math.Round(1000.0 / fps));
    }

    public void Start(int fps, int maximumFrames)
    {
        if (Stage != WebcamRecordingStage.Stopped)
            throw new InvalidOperationException("A recording is already in progress.");
        if (maximumFrames < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumFrames), maximumFrames, "A recording must be allowed at least one frame.");

        IntervalMs = IntervalFor(fps);
        MaximumFrames = maximumFrames;
        _frames.Clear();
        _accumulatedMs = 0;
        _nextDueMs = 0;
        _nextFrameIndex = 0;
        clock.Restart();
        Stage = WebcamRecordingStage.Recording;
    }

    /// <summary>
    /// The file the next kept frame will take, without claiming it. The index advances only when a
    /// capture is registered, so a dropped frame leaves no gap in the batch the editor loads.
    /// </summary>
    public string NextFramePath(string batchPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchPath);
        return Path.Combine(batchPath, $"{_nextFrameIndex:000000}.png");
    }

    /// <summary>Records that a frame was written for the capture that was due; schedules the next one.</summary>
    public void RegisterCapture(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var now = RequireRecording();
        _frames.Add((path, now));
        _nextFrameIndex++;
        ScheduleNextCapture(now);
    }

    /// <summary>
    /// Records that the capture that was due produced no file, because the frame writer's queue was full.
    /// The schedule advances as it would for a kept frame, so a dropped capture does not immediately come
    /// due again; the next kept frame's measured delay absorbs the interval the dropped one would have held.
    /// </summary>
    public void RegisterDroppedCapture() => ScheduleNextCapture(RequireRecording());

    private long RequireRecording() => Stage == WebcamRecordingStage.Recording
        ? ActiveMilliseconds
        : throw new InvalidOperationException("Captures can only be registered while recording.");

    /// <summary>
    /// Keeps the nominal cadence while the ticks keep up, and restarts it from now when one arrives a
    /// whole interval or more late, so catching up never fires a burst of near-zero delays.
    /// </summary>
    private void ScheduleNextCapture(long now)
    {
        _nextDueMs += IntervalMs;
        if (_nextDueMs <= now)
            _nextDueMs = now + IntervalMs;
    }

    public void Pause()
    {
        if (Stage != WebcamRecordingStage.Recording)
            return;

        _accumulatedMs += clock.ElapsedMilliseconds;
        clock.Stop();
        Stage = WebcamRecordingStage.Paused;
    }

    public void Resume()
    {
        if (Stage != WebcamRecordingStage.Paused)
            return;

        _nextDueMs = _accumulatedMs;
        clock.Restart();
        Stage = WebcamRecordingStage.Recording;
    }

    /// <summary>Forgets every capture and returns the files the caller owns and should delete.</summary>
    public IReadOnlyList<string> Discard()
    {
        var paths = FramePaths;
        _frames.Clear();
        _accumulatedMs = 0;
        _nextDueMs = 0;
        _nextFrameIndex = 0;
        clock.Stop();
        Stage = WebcamRecordingStage.Stopped;
        return paths;
    }

    /// <summary>Ends the recording and returns its frames; the last frame keeps the nominal interval.</summary>
    public IReadOnlyList<EditorFrame> Stop()
    {
        if (Stage == WebcamRecordingStage.Stopped)
            throw new InvalidOperationException("There is no recording to stop.");

        if (Stage == WebcamRecordingStage.Recording)
            _accumulatedMs += clock.ElapsedMilliseconds;
        clock.Stop();
        Stage = WebcamRecordingStage.Stopped;

        var frames = new EditorFrame[_frames.Count];
        for (var index = 0; index < _frames.Count; index++)
        {
            var delay = index + 1 < _frames.Count
                ? _frames[index + 1].CapturedAtMs - _frames[index].CapturedAtMs
                : IntervalMs;
            frames[index] = new EditorFrame(_frames[index].Path, (int)Math.Clamp(delay, 1, int.MaxValue));
        }

        _frames.Clear();
        _accumulatedMs = 0;
        return frames;
    }
}

/// <summary>
/// Encodes captured frames to PNG files off the UI thread through a bounded queue. When encoding falls
/// behind, the newest frame is dropped rather than stalling capture; the session's measured delays absorb the gap.
/// </summary>
public sealed class WebcamFrameWriter : IAsyncDisposable
{
    private readonly Channel<(string Path, int Width, int Height, byte[] Bgra, int Length)> _queue;
    private readonly Task[] _workers;
    private readonly object _failureLock = new();
    private Exception? _firstFailure;
    private bool _completed;

    public WebcamFrameWriter(int capacity = 60, int? workerCount = null)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _queue = Channel.CreateBounded<(string, int, int, byte[], int)>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });
        var workers = workerCount ?? Math.Clamp(Environment.ProcessorCount / 2, 1, 4);
        _workers = Enumerable.Range(0, Math.Max(1, workers)).Select(_ => Task.Run(DrainAsync)).ToArray();
    }

    public int DroppedFrames { get; private set; }

    public Exception? FirstFailure
    {
        get
        {
            lock (_failureLock)
                return _firstFailure;
        }
    }

    /// <summary>Copies the frame synchronously and queues it; returns false when the queue is full and the frame was dropped.</summary>
    public bool TryEnqueue(string path, CameraFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (_completed)
            return false;

        var length = frame.ByteLength;
        var copy = ArrayPool<byte>.Shared.Rent(length);
        frame.Bgra.AsSpan(0, length).CopyTo(copy);
        if (_queue.Writer.TryWrite((path, frame.Width, frame.Height, copy, length)))
            return true;

        ArrayPool<byte>.Shared.Return(copy);
        DroppedFrames++;
        return false;
    }

    /// <summary>Waits until every queued frame is on disk; throws the first encoding failure.</summary>
    public async Task CompleteAsync()
    {
        _completed = true;
        _queue.Writer.TryComplete();
        await Task.WhenAll(_workers);
        if (FirstFailure is { } failure)
            throw new IOException("A recorded frame could not be written.", failure);
    }

    public async ValueTask DisposeAsync()
    {
        _completed = true;
        _queue.Writer.TryComplete();
        await Task.WhenAll(_workers);
    }

    private async Task DrainAsync()
    {
        await foreach (var (path, width, height, bgra, length) in _queue.Reader.ReadAllAsync())
        {
            try
            {
                using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
                RgbaPngEncoder.WriteBgra(file, width, height, bgra.AsSpan(0, length));
            }
            catch (Exception ex)
            {
                lock (_failureLock)
                    _firstFailure ??= ex;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bgra);
            }
        }
    }
}
