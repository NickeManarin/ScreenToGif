using System.Runtime.InteropServices;
using ScreenToGif.Domain.Enums.Native;

namespace ScreenToGif.Native.External
{
    public static class ShCore
    {
        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        /// </summary>
        /// <param name="hmonitor">Handle of the monitor being queried.</param>
        /// <param name="dpiType">The type of DPI being queried. Possible values are from the MONITOR_DPI_TYPE enumeration.</param>
        /// <param name="dpiX">The value of the DPI along the X axis. This value always refers to the horizontal edge, even when the screen is rotated.</param>
        /// <param name="dpiY">The value of the DPI along the Y axis. This value always refers to the vertical edge, even when the screen is rotated.</param>
        /// <returns>If OK, 0x00000000 | Else, 0x80070057</returns>
        [DllImport("Shcore.dll")]
        internal static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiTypes dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

        //[DllImport("SHCore.dll", SetLastError = true)]
        //public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        //[DllImport("SHCore.dll", SetLastError = true)]
        //public static extern void GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS awareness);
    }
}