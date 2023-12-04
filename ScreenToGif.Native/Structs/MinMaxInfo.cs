using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

/// <summary>
/// Contains information about a window's maximized size and position and its minimum and maximum tracking size.
/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-minmaxinfo
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MinMaxInfo
{
    /// <summary>
    /// Reserved, do not use.
    /// </summary>
    public PointW Reserved;

    /// <summary>
    /// The maximized width (x member) and the maximized height (y member) of the window.
    /// For top-level windows, this value is based on the width of the primary monitor.
    /// </summary>
    public PointW MaxSize;

    /// <summary>
    /// The position of the left side of the maximized window (x member) and the position of the top of the maximized window (y member).
    /// For top-level windows, this value is based on the position of the primary monitor.
    /// </summary>
    public PointW MaxPosition;

    /// <summary>
    /// The minimum tracking width (x member) and the minimum tracking height (y member) of the window.
    /// This value can be obtained programmatically from the system metrics SM_CXMINTRACK and SM_CYMINTRACK (see the GetSystemMetrics function).
    /// </summary>
    public PointW MinTrackSize;

    /// <summary>
    /// The maximum tracking width (x member) and the maximum tracking height (y member) of the window.
    /// This value is based on the size of the virtual screen and can be obtained programmatically from the system metrics SM_CXMAXTRACK and SM_CYMAXTRACK (see the GetSystemMetrics function).
    /// </summary>
    public PointW MaxTrackSize;
}