namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Type of capture frequency mode for the screen recorder.
/// </summary>
public enum CaptureFrequencies
{
    Manual,
    Interaction,
    PerSecond,
    PerMinute,
    PerHour
}