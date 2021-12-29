using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Domain.Models;
using ScreenToGif.Domain.Models.Native;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;
using Monitor = ScreenToGif.Domain.Models.Native.Monitor;
using Size = System.Windows.Size;

namespace ScreenToGif.Native.Helpers
{
    public static class Windows
    {
        public static void MoveToScreen(this System.Windows.Window window, Monitor next, bool fullScreen = false)
        {
            if (fullScreen)
            {
                User32.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(window).Handle, (IntPtr)SpecialWindowHandles.Top,
                    (int)next.NativeBounds.Left, (int)next.NativeBounds.Top, (int)next.NativeBounds.Width, (int)next.NativeBounds.Height, SetWindowPosFlags.ShowWindow);
                return;
            }

            User32.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(window).Handle, (IntPtr)SpecialWindowHandles.Top,
                (int)next.NativeBounds.Left, (int)next.NativeBounds.Top, (int)window.Width, (int)window.Height, SetWindowPosFlags.ShowWindow);
        }

        public static int GetZOrder(IntPtr hWnd)
        {
            var z = 0;
            for (var h = hWnd; h != IntPtr.Zero; h = User32.GetWindow(h, GetWindowType.HwndPrev))
                z++;

            return z;
        }

        /// <summary>
        /// Gets the z-order for one or more windows atomically with respect to each other. 
        /// In Windows, smaller z-order is higher. If the window is not top level, the z order is returned as -1. 
        /// </summary>
        public static int[] GetZOrder(params IntPtr[] hWnds)
        {
            var z = new int[hWnds.Length];
            for (var i = 0; i < hWnds.Length; i++)
                z[i] = -1;

            var index = 0;
            var numRemaining = hWnds.Length;

            User32.EnumWindows((wnd, param) =>
            {
                var searchIndex = Array.IndexOf(hWnds, wnd);

                if (searchIndex != -1)
                {
                    z[searchIndex] = index;
                    numRemaining--;
                    if (numRemaining == 0) return false;
                }

                index++;
                return true;
            }, IntPtr.Zero);

            return z;
        }

        /// <summary>
        /// Returns a dictionary that contains the handle and title of all the open windows.
        /// </summary>
        /// <returns>
        /// A dictionary that contains the handle and title of all the open windows.
        /// </returns>
        public static List<DetectedRegion> EnumerateWindows(double scale = 1)
        {
            var shellWindow = User32.GetShellWindow();

            var windows = new List<DetectedRegion>();

            //EnumWindows(delegate (IntPtr handle, int lParam)
            User32.EnumDesktopWindows(IntPtr.Zero, delegate (IntPtr handle, IntPtr lParam)
            {
                if (handle == shellWindow)
                    return true;

                if (!User32.IsWindowVisible(handle))
                    return true;

                if (User32.IsIconic(handle))
                    return true;

                var length = User32.GetWindowTextLength(handle);

                if (length == 0)
                    return true;

                var builder = new StringBuilder(length);

                User32.GetWindowText(handle, builder, length + 1);

                var info = new WindowInfo(false);
                User32.GetWindowInfo(handle, ref info);

                //If disabled, ignore.
                if (((long)info.dwStyle & (uint)WindowStyles.Disabled) == (uint)WindowStyles.Disabled)
                    return true;

                //Window class name.
                var className = new StringBuilder(256); //Maximum class name.
                if (User32.GetClassName(handle, className, className.Capacity) != 0)
                {
                    if (className.ToString().Contains("ScreenToGif.exe"))
                        return true;
                }

                //Title bar visibility.
                var infoTile = new TitlebarInfo(false);
                User32.GetTitleBarInfo(handle, ref infoTile);

                //Removed: WindowStyle=None windows were getting ignored.
                // ((infoTile.rgstate[0] & StateSystemInvisible) == StateSystemInvisible)
                //    return true;

                if ((infoTile.rgstate[0] & Constants.StateSystemUnavailable) == Constants.StateSystemUnavailable)
                    return true;

                ////Removed: MahApps windows were getting ignored.
                //if ((infoTile.rgstate[0] & StateSystemOffscreen) == StateSystemOffscreen)
                //    return true;

                DwmApi.DwmGetWindowAttribute(handle, (int)DwmWindowAttributes.Cloaked, out bool isCloacked, Marshal.SizeOf(typeof(bool)));

                if (isCloacked)
                    return true;

                DwmApi.DwmGetWindowAttribute(handle, (int)DwmWindowAttributes.ExtendedFrameBounds, out NativeRect frameBounds, Marshal.SizeOf(typeof(NativeRect)));

                var bounds = frameBounds.TryToRect(MathExtensions.RoundUpValue(scale), scale);

                if (bounds.IsEmpty)
                    return true;

                windows.Add(new DetectedRegion(handle, bounds, builder.ToString(), GetZOrder(handle)));

                return true;
            }, IntPtr.Zero);

            return windows.OrderBy(o => o.Order).ToList();
        }

        /// <summary>
        /// Returns a dictionary that contains the handle and title of all the open windows inside a given monitor.
        /// </summary>
        /// <returns>
        /// A dictionary that contains the handle and title of all the open windows.
        /// </returns>
        public static List<DetectedRegion> EnumerateWindowsByMonitor(Monitor monitor)
        {
            var shellWindow = User32.GetShellWindow();

            var windows = new List<DetectedRegion>();

            //EnumWindows(delegate (IntPtr handle, int lParam)
            User32.EnumDesktopWindows(IntPtr.Zero, delegate (IntPtr handle, IntPtr lParam)
            {
                if (handle == shellWindow)
                    return true;

                if (!User32.IsWindowVisible(handle))
                    return true;

                if (User32.IsIconic(handle))
                    return true;

                var length = User32.GetWindowTextLength(handle);

                if (length == 0)
                    return true;

                var builder = new StringBuilder(length);

                User32.GetWindowText(handle, builder, length + 1);
                var title = builder.ToString();

                var info = new WindowInfo(false);
                User32.GetWindowInfo(handle, ref info);

                //If disabled, ignore.
                if (((long)info.dwStyle & (uint)WindowStyles.Disabled) == (uint)WindowStyles.Disabled)
                    return true;

                //Window class name.
                var className = new StringBuilder(256); //Maximum class name.
                if (User32.GetClassName(handle, className, className.Capacity) != 0)
                {
                    if (className.ToString().Contains("ScreenToGif.exe"))
                        return true;
                }

                var infoTile = new TitlebarInfo(false);
                User32.GetTitleBarInfo(handle, ref infoTile);

                //Removed: WindowStyle=None windows were getting ignored.
                // ((infoTile.rgstate[0] & StateSystemInvisible) == StateSystemInvisible)
                //    return true;

                if ((infoTile.rgstate[0] & Constants.StateSystemUnavailable) == Constants.StateSystemUnavailable)
                    return true;

                ////Removed: MahApps windows were getting ignored.
                //if ((infoTile.rgstate[0] & StateSystemOffscreen) == StateSystemOffscreen)
                //    return true;

                DwmApi.DwmGetWindowAttribute(handle, (int)DwmWindowAttributes.Cloaked, out bool isCloacked, Marshal.SizeOf(typeof(bool)));

                if (isCloacked)
                    return true;

                DwmApi.DwmGetWindowAttribute(handle, (int)DwmWindowAttributes.ExtendedFrameBounds, out NativeRect frameBounds, Marshal.SizeOf(typeof(NativeRect)));

                var bounds = frameBounds.TryToRect(MathExtensions.RoundUpValue(monitor.Scale), monitor.Scale);

                if (bounds.IsEmpty)
                    return true;

                var place = WindowPlacement.Default;
                User32.GetWindowPlacement(handle, ref place);

                //Hack for detecting the correct size of VisualStudio when it's maximized.
                if (place.ShowCmd == ShowWindowCommands.Maximize && title.Contains("Microsoft Visual Studio"))
                    bounds = frameBounds.TryToRect(-info.cxWindowBorders, monitor.Scale);
                //bounds = new System.Windows.Rect(new Point(monitor.Bounds.Left / monitor.Scale, monitor.Bounds.Top / monitor.Scale), new Size(info.rcClient.Right / monitor.Scale, info.rcClient.Bottom / monitor.Scale));

                if (bounds.IsEmpty)
                    return true;

                //Windows to the left are not being detected as inside the bounds.
                if (!bounds.IntersectsWith(monitor.Bounds))
                    return true;

                windows.Add(new DetectedRegion(handle, bounds, title, GetZOrder(handle)));

                return true;
            }, IntPtr.Zero);

            return windows.OrderBy(o => o.Order).ToList();
        }

        /// <summary>
        /// Gets all first level window handles from a given process.
        /// The windows must be visible.
        /// </summary>
        public static List<IntPtr> GetWindowHandlesFromProcess(Process process)
        {
            var list = new List<IntPtr>();

            //Each thread can create a window.
            foreach (ProcessThread info in process.Threads)
            {
                //With given thread ID, search for windows.
                var windows = GetWindowHandlesForThread((IntPtr)info.Id);

                if (windows != null)
                    list.AddRange(windows);
            }

            return list;
        }

        private static IntPtr[] GetWindowHandlesForThread(IntPtr threadHandle)
        {
            var results = new List<IntPtr>();

            //Enumerate all top level desktop windows.
            User32.EnumWindows(delegate (IntPtr window, IntPtr thread)
            {
                //Get the ID of the thread that created the window.
                var threadId = User32.GetWindowThreadProcessId(window, out _);

                //Check if the selected thread created this window.
                if ((IntPtr)threadId != thread)
                    return true;

                if (!User32.IsWindowVisible(window))
                    return true;

                results.Add(window);
                return true;
            }, threadHandle);

            return results.ToArray();
        }

        private static bool ExtendedFrameBounds(IntPtr handle, out Int32Rect rectangle)
        {
            var result = DwmApi.DwmGetWindowAttribute(handle, (int)DwmWindowAttributes.ExtendedFrameBounds, out NativeRect rect, Marshal.SizeOf(typeof(NativeRect)));

            rectangle = rect.ToRectangle();

            return result >= 0;
        }

        internal static Int32Rect GetWindowRect(IntPtr handle)
        {
            User32.GetWindowRect(handle, out NativeRect rect);
            return rect.ToRectangle();
        }

        public static Int32Rect TrueWindowRectangle(IntPtr handle)
        {
            return ExtendedFrameBounds(handle, out Int32Rect rectangle) ? rectangle : GetWindowRect(handle);
        }

        public static Size ScreenSizeFromWindow(System.Windows.Window window)
        {
            return ScreenSizeFromWindow(new WindowInteropHelper(window).Handle);
        }

        public static Size ScreenSizeFromWindow(IntPtr handle)
        {
            var pointer = User32.MonitorFromWindow(handle, Constants.MonitorDefaultToNearest);

            var info = new MonitorInfoEx();
            User32.GetMonitorInfo(new HandleRef(null, pointer), info);

            var rect = info.rcWork.ToRectangle();

            Gdi32.DeleteObject(pointer);

            return new Size(rect.Width, rect.Height);
        }

        internal static Size ScreenSizeFromPoint(int left, int top)
        {
            var pointer = User32.MonitorFromPoint(new PointW { X = left, Y = top }, Constants.MonitorDefaultToNearest);

            var info = new MonitorInfoEx();
            User32.GetMonitorInfo(new HandleRef(null, pointer), info);

            var rect = info.rcWork.ToRectangle();

            return new Size(rect.Width, rect.Height);
        }

    }
}