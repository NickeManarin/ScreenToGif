namespace ScreenToGif.Domain.Enums;

public enum PartialExportModes
{
    /// <summary>
    /// An expression like '4, 5, 9 - 11'.
    /// </summary>
    FrameExpression,

    /// <summary>
    /// Start and end frame number.
    /// </summary>
    FrameRange,

    /// <summary>
    /// Start and end times.
    /// </summary>
    TimeRange,

    /// <summary>
    /// All selected frames in the timeline.
    /// </summary>
    Selection
}