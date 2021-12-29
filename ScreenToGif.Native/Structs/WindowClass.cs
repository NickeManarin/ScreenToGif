using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

/// <summary>
/// Win API WNDCLASS struct - represents a single window.
/// Used to receive window messages.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct WindowClass
{
    public uint style;
    public Delegates.WindowProcedureHandler lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public IntPtr hInstance;
    public IntPtr hIcon;
    public IntPtr hCursor;
    public IntPtr hbrBackground;
    [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
    [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
}