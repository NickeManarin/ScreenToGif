namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Stage status of the recording process.
/// </summary>
[Flags]
public enum RecorderStages
{
    /// <summary>
    /// Recording stopped.
    /// </summary>
    Stopped = 1, //1 << 0, 0b_000001

    /// <summary>
    /// Recording active.
    /// </summary>
    Recording = 2, //1 << 1, 0b_000010

    /// <summary>
    /// Recording paused.
    /// </summary>
    Paused = 4, //1 << 2, 0b_000100

    /// <summary>
    /// Pre start countdown active.
    /// </summary>
    PreStarting = 8, //1 << 3, 0b_001000

    /// <summary>
    /// The recording is being discarded.
    /// </summary>
    Discarding = 16, //1 << 4, 0b_010000



    /// <summary>
    /// Single shot mode.
    /// </summary>
    [Obsolete]
    Snapping = 32, //1 << 5, 0b_100000 //Remove later.
}