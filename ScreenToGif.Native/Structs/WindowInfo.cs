using System.Runtime.InteropServices;
using ScreenToGif.Domain.Models.Native;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct WindowInfo
{
    /// <summary>
    /// The size of the structure, in bytes.
    /// The caller must set this member to sizeof(WINDOWINFO).
    /// </summary>
    public uint cbSize;

    /// <summary>
    /// The coordinates of the window.
    /// </summary>
    public NativeRect rcWindow;

    /// <summary>
    /// The coordinates of the client area.
    /// </summary>
    public NativeRect rcClient;

    /// <summary>
    /// The window styles.
    /// For a table of window styles, see Window Styles (https://docs.microsoft.com/en-us/windows/win32/winmsg/window-styles).
    /// </summary>
    public uint dwStyle;

    /// <summary>
    /// The extended window styles.
    /// For a table of extended window styles, see Extended Window Styles (https://docs.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles).
    /// </summary>
    public uint dwExStyle;

    /// <summary>
    /// The window status.
    /// If this member is WS_ACTIVECAPTION (0x0001), the window is active. Otherwise, this member is zero.
    /// </summary>
    public uint dwWindowStatus;

    /// <summary>
    /// The width of the window border, in pixels.
    /// </summary>
    public uint cxWindowBorders;

    /// <summary>
    /// The height of the window border, in pixels.
    /// </summary>
    public uint cyWindowBorders;

    /// <summary>
    /// The window class atom (see RegisterClass).
    /// </summary>
    public ushort atomWindowType;

    /// <summary>
    /// The Windows version of the application that created the window.
    /// </summary>
    public ushort wCreatorVersion;

    public WindowInfo(bool? filler) : this()
    {
        //Allows automatic initialization of "cbSize" with "new WindowInfo(null/true/false)".
        cbSize = (uint)Marshal.SizeOf(typeof(WindowInfo));
    }
}