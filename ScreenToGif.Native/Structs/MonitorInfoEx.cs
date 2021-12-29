using System.Runtime.InteropServices;
using ScreenToGif.Domain.Models.Native;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
internal class MonitorInfoEx
{
    /// <summary>
    /// The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the GetMonitorInfo function.
    /// Doing so lets the function determine the type of structure you are passing to it.
    /// </summary>
    public int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));

    /// <summary>
    /// A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates.
    /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
    /// </summary>
    public NativeRect rcMonitor;

    /// <summary>
    /// A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications,
    /// expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor.
    /// The rest of the area in rcMonitor contains system windows such as the task bar and side bars.
    /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
    /// </summary>
    public NativeRect rcWork;

    /// <summary>
    /// The attributes of the display monitor.
    ///
    /// This member can be the following value:
    ///   1 : MONITORINFOF_PRIMARY
    /// </summary>
    public int dwFlags = 0;

    /// <summary>
    /// A string that specifies the device name of the monitor being used.
    /// Most applications have no use for a display monitor name, and so can save some bytes by using a MONITORINFO structure.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public char[] szDevice = new char[32];
}