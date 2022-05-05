namespace ScreenToGif.Domain.Enums.Native;

/// <summary>
/// Extended Window Styles.
/// </summary>
public enum WindowStylesEx : uint
{
    DlgModalFrame = 0x0001,
    NoParentNotify = 0x0004,
    TopMost = 0x0008,
    AcceptFiles = 0x0010,
    Transparent = 0x0020,
    MdiChild = 0x0040,
    ToolWindow = 0x0080,
    WindowEdge = 0x0100,
    ClientEdge = 0x0200,
    ContextHelp = 0x0400,
    Right = 0x1000,
    Left = 0x0000,
    RtlReading = 0x2000,
    LtrReading = 0x0000,
    LeftScrollbar = 0x4000,
    RightScrollbar = 0x0000,
    ControlParent = 0x10000,
    StaticEdge = 0x20000,
    AppWindow = 0x40000,
    OverlappedWindow = (WindowEdge | ClientEdge),
    PaletteWindow = (WindowEdge | ToolWindow | TopMost),
    Layered = 0x00080000,
    NoInheritLayout = 0x00100000, // Disable inheritance of mirroring by children
    LayoutRtl = 0x00400000, // Right to left mirroring
    Composited = 0x02000000,
    NoActivate = 0x08000000,
}