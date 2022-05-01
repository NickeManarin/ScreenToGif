namespace ScreenToGif.Domain.Enums;

/// <summary>
/// The types of Panel of the Editor window.
/// Positive values means that there's no preview overlay.
/// </summary>
public enum PanelTypes
{
    /// <summary>
    /// Save As Panel.
    /// </summary>
    SaveAs = 1,

    /// <summary>
    /// New Animation Panel.
    /// </summary>
    NewAnimation = 2,

    /// <summary>
    /// Clipboard Panel.
    /// </summary>
    Clipboard = 3,

    /// <summary>
    /// Resize Panel.
    /// </summary>
    Resize = 4,

    /// <summary>
    /// Flip/Rotate Panel.
    /// </summary>
    FlipRotate = 5,

    /// <summary>
    /// Override Delay Panel.
    /// </summary>
    OverrideDelay = 6,

    /// <summary>
    /// Change Delay Panel.
    /// </summary>
    IncreaseDecreaseDelay = 7,

    ScaleDelay = 8,

    /// <summary>
    /// Fade Transition Panel.
    /// </summary>
    Fade = 9,

    /// <summary>
    /// Slide Transition Panel.
    /// </summary>
    Slide = 10,

    /// <summary>
    /// Reduce Frame Count Panel.
    /// </summary>
    ReduceFrames = 11,

    /// <summary>
    /// Load Recent Panel.
    /// </summary>
    LoadRecent = 12,

    /// <summary>
    /// Remove Duplicates Panel.
    /// </summary>
    RemoveDuplicates = 13,

    /// <summary>
    /// Mouse Events Panel.
    /// </summary>
    MouseEvents = 14,

    /// <summary>
    /// Smooth Loop Panel.
    /// </summary>
    SmoothLoop = 15,

    /// <summary>
    /// Crop Panel.
    /// </summary>
    Crop = -1,

    /// <summary>
    /// Caption Panel.
    /// </summary>
    Caption = -2,

    /// <summary>
    /// Free Text Panel.
    /// </summary>
    FreeText = -3,

    /// <summary>
    /// Title Frame Panel.
    /// </summary>
    TitleFrame = -4,

    /// <summary>
    /// Free Drawing Panel.
    /// </summary>
    FreeDrawing = -5,

    /// <summary>
    /// Shapes Panel.
    /// </summary>
    Shapes = -6,

    /// <summary>
    /// Watermark Panel.
    /// </summary>
    Watermark = -7,

    /// <summary>
    /// Border Panel.
    /// </summary>
    Border = -8,

    /// <summary>
    /// Cinemagraph Panel.
    /// </summary>
    Cinemagraph = -9,

    /// <summary>
    /// Progress Panel.
    /// </summary>
    Progress = -10,

    /// <summary>
    /// Key Strokes Panel.
    /// </summary>
    KeyStrokes = -11,

    /// <summary>
    /// Obfuscate Panel.
    /// </summary>
    Obfuscate = -12,

    /// <summary>
    /// Shadow Panel.
    /// </summary>
    Shadow = -13,
}