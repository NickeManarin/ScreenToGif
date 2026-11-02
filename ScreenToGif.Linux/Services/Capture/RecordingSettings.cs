namespace ScreenToGif.Linux.Services.Capture;

/// <summary>How often a recording captures, mirroring the Windows recorder's frequency choices.</summary>
public enum RecorderCaptureMode
{
    PerSecond,
    PerMinute,
    PerHour,
    Manual
}

/// <summary>The stages a recording moves through. Discarding is transient and returns to Stopped.</summary>
public enum RecordingStage
{
    Stopped,
    PreStarting,
    Recording,
    Paused,
    Discarding
}

/// <summary>
/// The settings one recording runs under, fixed when it starts, as the frame's size is. This
/// diverges from Windows for one field: that recorder applies a frame rate edited while paused
/// when the recording resumes. Here a recording keeps the rate it began with, and the edited
/// value is what the next recording starts at.
/// </summary>
public sealed record RecordingSettings
{
    /// <summary>Playback delay stamped on frames paced per minute or per hour, as on Windows.</summary>
    public const int SlowModePlaybackDelayMs = 66;

    public const int MinimumFramesPerSecond = 1;
    public const int MaximumFramesPerSecond = 60;

    public RecorderCaptureMode Mode { get; init; } = RecorderCaptureMode.PerSecond;

    public int FramesPerSecond { get; init; } = 15;

    public bool FixedFrameRate { get; init; }

    public bool ShowCursor { get; init; } = true;

    public bool PreStartEnabled { get; init; }

    public int PreStartSeconds { get; init; } = 3;

    public int ManualPlaybackDelayMs { get; init; } = 1000;

    /// <summary>
    /// Milliseconds the capture loop waits between frames. Per second paces at 1000/fps, per minute
    /// at 60000/fps, per hour at 3600000/fps. Manual is unpaced, so it has no interval.
    /// </summary>
    public int CaptureIntervalMilliseconds => Mode switch
    {
        RecorderCaptureMode.PerSecond => Math.Max(1, 1000 / ClampedFramesPerSecond),
        RecorderCaptureMode.PerMinute => Math.Max(1, 60_000 / ClampedFramesPerSecond),
        RecorderCaptureMode.PerHour => Math.Max(1, 3_600_000 / ClampedFramesPerSecond),
        RecorderCaptureMode.Manual => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unknown capture mode.")
    };

    /// <summary>
    /// The delay a newly captured frame carries before a later frame measures the real gap.
    /// Only Per second with a free frame rate ever revises it.
    /// </summary>
    public int NominalPlaybackDelayMilliseconds => Mode switch
    {
        RecorderCaptureMode.PerSecond => Math.Max(1, 1000 / ClampedFramesPerSecond),
        RecorderCaptureMode.PerMinute or RecorderCaptureMode.PerHour => SlowModePlaybackDelayMs,
        RecorderCaptureMode.Manual => Math.Max(1, ManualPlaybackDelayMs),
        _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unknown capture mode.")
    };

    /// <summary>True when a frame's delay is rewritten to the measured gap once the next frame arrives.</summary>
    public bool MeasuresRealDelays => Mode == RecorderCaptureMode.PerSecond && !FixedFrameRate;

    private int ClampedFramesPerSecond =>
        Math.Clamp(FramesPerSecond, MinimumFramesPerSecond, MaximumFramesPerSecond);
}
