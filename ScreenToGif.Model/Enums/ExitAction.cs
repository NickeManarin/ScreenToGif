namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Exit actions after closing the Recording Window.
/// </summary>
public enum ExitAction
{
    /// <summary>
    /// Return to the StartUp Window.
    /// </summary>
    Return = 0,

    /// <summary>
    /// Something was recorded. Go to the Editor.
    /// </summary>
    Recorded = 1,

    /// <summary>
    /// Exit the application.
    /// </summary>
    Exit = 2,
}