namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Paste behavior for the editor.
/// </summary>
public enum PasteBehaviors
{
    /// <summary>
    /// It will paste before the selected frame.
    /// </summary>
    BeforeSelected,

    /// <summary>
    /// It will paste after the selected frame.
    /// </summary>
    AfterSelected
}