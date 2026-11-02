namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// One reading of a recording, taken on the thread that owns the session and safe to hand to any
/// other. A host renders from this rather than reading the live session across a thread boundary.
/// </summary>
public readonly record struct RecordingStatus(
    RecordingStage Stage,
    int FrameCount,
    int PreStartRemainingSeconds,
    bool CanRecord,
    bool CanSnap,
    bool CanPause,
    bool CanStop,
    bool CanDiscard,
    bool CanChangeRegion,
    bool CanChangeFrequency)
{
    /// <summary>
    /// What the command bar reads. The countdown replaces the count while it runs, and being
    /// paused is left to the Resume button and the re-enabled frequency field to say, because the
    /// bar has room for one short line rather than two facts.
    /// </summary>
    public string StatusText => Stage switch
    {
        RecordingStage.PreStarting => $"Starting in {PreStartRemainingSeconds}\u2026",
        _ when FrameCount == 1 => "1 frame",
        _ when FrameCount > 0 => $"{FrameCount} frames",
        _ => string.Empty
    };
}
