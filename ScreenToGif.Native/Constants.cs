using System.Runtime.InteropServices;

namespace ScreenToGif.Native
{
    public static class Constants
    {
        /// <summary>
        /// User32 library name.
        /// </summary>
        internal const string User32 = "user32.dll";
        internal const string Gdi32 = "gdi32.dll";
        internal const string Shell32 = "shell32.dll";
        internal const string DwmApi = "dwmapi.dll";
        internal const string MsvCrt = "msvcrt.dll";
        internal const string NtDll = "ntdll.dll";
        internal const string WinMm = "winmm.dll";
        internal const string Kernel32 = "kernel32.dll";


        internal static HandleRef NullHandleRef = new(null, IntPtr.Zero);

        internal const int MonitorDefaultToNull = 0;
        internal const int MonitorDefaultToPrimary = 1;
        internal const int MonitorDefaultToNearest = 2;

        public const int CursorShowing = 0x00000001;
        internal const int DstInvert = 0x00550009;

        internal const int DiNormal = 0x0003;

        internal const int MonitorinfoPrimary = 0x00000001;

        internal const int StateSystemFocusable = 0x00100000;
        internal const int StateSystemUnavailable = 0x0001;
        internal const int StateSystemInvisible = 0x8000;
        internal const int StateSystemOffscreen = 0x010000;

        internal const int CChildrenTitlebar = 5;
    }
}