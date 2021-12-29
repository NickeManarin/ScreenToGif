using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

/// <summary>
/// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard input event.
/// </summary>
/// <remarks>
/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-msllhookstruct
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal class MouseHook
{
    /// <summary>
    /// Specifies a POINT structure that contains the X and Y coordinates of the cursor, in screen coordinates. 
    /// </summary>
    public PointW Point;

    /// <summary>
    /// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta. 
    /// The low-order word is reserved. A positive value indicates that the wheel was rotated forward, 
    /// away from the user; a negative value indicates that the wheel was rotated backward, toward the user. 
    /// One wheel click is defined as WHEEL_DELTA, which is 120. 
    ///If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
    /// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
    /// and the low-order word is reserved.
    /// </summary>
    public uint MouseData;

    /// <summary>
    /// Specifies the event-injected flag. An application can use the following value to test the mouse flags.
    /// </summary>
    public int Flags;

    /// <summary>
    /// Specifies the time stamp for this message.
    /// </summary>
    public int Time;

    /// <summary>
    /// Specifies extra information associated with the message. 
    /// </summary>
    public int ExtraInfo;
}