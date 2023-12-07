namespace ScreenToGif.Domain.Enums.Native;

public enum DwmWindowAttributes
{
    NcRenderingEnabled = 1,
    NcRenderingPolicy,
    TransitionsForcedisabled,
    AllowNcPaint,
    CaptionButtonBounds,
    NonclientRtlLayout,
    ForceIconicRepresentation,
    Flip3DPolicy,
    ExtendedFrameBounds,
    HasIconicBitmap,
    DisallowPeek,
    ExcludedFromPeek,
    Cloak,
    Cloaked,
    FreezeRepresentation,
    PassiveUpdateMode,
    UseHostBackdropBrush,
    UseImmersiveDarkModeBefore20H1 = 19,  //For Windows 10 versions before 2004.
    UseImmersiveDarkMode = 20,
    WindowCornerPreference = 33,
    BorderColor,
    CaptionColor,
    TextColor,
    VisibleFrameBorderThickness,

    /// <summary>
    /// Retrieves or specifies the system-drawn backdrop material of a window, including behind the non-client area.
    /// The pvAttribute parameter points to a value of type SystemBackdropTypes.
    /// </summary>
    SystemBackdropType,

    MicaEffect = 1029,
    Last
}