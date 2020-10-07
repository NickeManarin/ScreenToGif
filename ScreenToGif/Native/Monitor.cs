using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Native
{
    public class Monitor
    {

        #region Native

        [Flags]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>
            /// The device is part of the desktop.
            /// </summary>
            AttachedToDesktop = 0x1,
            
            MultiDriver = 0x2,
            
            /// <summary>
            /// The device is part of the desktop.
            /// </summary>
            PrimaryDevice = 0x4,
            
            /// <summary>
            /// Represents a pseudo device used to mirror application drawing for remoting or other purposes.
            /// </summary>
            MirroringDriver = 0x8,
            
            /// <summary>
            /// The device is VGA compatible.
            /// </summary>
            VgaCompatible = 0x10,
            
            /// <summary>
            /// The device is removable; it cannot be the primary display.
            /// </summary>
            Removable = 0x20,
            
            /// <summary>
            /// The device has more display modes than its output devices support.
            /// </summary>
            ModesPruned = 0x8000000,
            
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DisplayDevice
        {
            internal DisplayDevice(bool? filler) : this()
            {
                //Allows automatic initialization of "Size" with "new DisplayDevice(null/true/false)".
                Size = Marshal.SizeOf(typeof(DisplayDevice));
            }

            [MarshalAs(UnmanagedType.U4)]
            public int Size;
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

        #endregion

        #region Properties

        public IntPtr Handle { get; }

        public Rect Bounds { get; private set; }
        
        public Rect NativeBounds { get; private set; }

        public Rect WorkingArea { get; private set; }

        public string Name { get; }

        public string AdapterName { get; }

        public string FriendlyName { get; }

        public int Dpi { get; }

        public double Scale => Dpi / 96d;

        public bool IsPrimary { get; }

        #endregion

        private Monitor(IntPtr monitor, IntPtr hdc)
        {
            var info = new Util.Native.MonitorInfoEx();
            Util.Native.GetMonitorInfo(new HandleRef(null, monitor), info);

            Handle = monitor;

            NativeBounds = new Rect(info.rcMonitor.Left, info.rcMonitor.Top,
                            info.rcMonitor.Right - info.rcMonitor.Left,
                            info.rcMonitor.Bottom - info.rcMonitor.Top);

            Bounds = new Rect(info.rcMonitor.Left, info.rcMonitor.Top,
                        info.rcMonitor.Right - info.rcMonitor.Left,
                        info.rcMonitor.Bottom - info.rcMonitor.Top);

            WorkingArea = new Rect(info.rcWork.Left, info.rcWork.Top,
                        info.rcWork.Right - info.rcWork.Left,
                        info.rcWork.Bottom - info.rcWork.Top);

            IsPrimary = (info.dwFlags & Util.Native.MonitorinfoPrimary) != 0;

            FriendlyName = Name = new string(info.szDevice).TrimEnd((char)0);

            #region Extra details

            try
            {
                var display = new DisplayDevice(true);

                for (uint id = 0; EnumDisplayDevices(null, id, ref display, 0); id++)
                {
                    var found = display.DeviceName == Name;
                    var adapter = display.DeviceString;
                    
                    EnumDisplayDevices(display.DeviceName, id, ref display, 0);

                    if (!found)
                        continue;
                    
                    AdapterName = adapter;
                    FriendlyName = string.IsNullOrWhiteSpace(display.DeviceString) ? LocalizationHelper.Get("S.Recorder.Screen.Name.Internal") : 
                        display.DeviceString == "Generic PnP Monitor" ? LocalizationHelper.Get("S.Recorder.Screen.Name.Generic") : display.DeviceString;
                    break;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get extra details of screen.");
            }

            #endregion

            #region Screen DPI

            try
            {
                Util.Native.GetDpiForMonitor(monitor, Util.Native.DpiType.Effective, out var aux, out _);
                Dpi = aux > 0 ? (int)aux : 96;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to detect screen DPI.");

                try
                {
                    var h = Util.Native.CreateCompatibleDC(IntPtr.Zero);
                    Dpi = Util.Native.GetDeviceCaps(h, (int)Util.Native.DeviceCaps.LogPixelsX);
                    Util.Native.DeleteDC(h);
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "Error getting fallback of screen DPI.");
                }
            }

            #endregion
        }

        public static List<Monitor> AllMonitors
        {
            get
            {
                var closure = new MonitorEnumCallback();
                var proc = new Util.Native.MonitorEnumProc(closure.Callback);

                Util.Native.EnumDisplayMonitors(Util.Native.NullHandleRef, IntPtr.Zero, proc, IntPtr.Zero);

                return closure.Monitors.Cast<Monitor>().ToList();
            }
        }

        public static List<Monitor> AllMonitorsScaled(double scale, bool offset = false)
        {
            //TODO: I should probably take each monitor scale.
            var monitors = AllMonitors;

            if (offset)
            {
                foreach (var monitor in monitors)
                {
                    monitor.Bounds = new Rect(monitor.Bounds.X / scale - SystemParameters.VirtualScreenLeft, monitor.Bounds.Y / scale - SystemParameters.VirtualScreenTop, monitor.Bounds.Width / scale, monitor.Bounds.Height / scale);
                    monitor.WorkingArea = new Rect(monitor.WorkingArea.X / scale - SystemParameters.VirtualScreenLeft, monitor.WorkingArea.Y / scale - SystemParameters.VirtualScreenTop, monitor.WorkingArea.Width / scale, monitor.WorkingArea.Height / scale);
                }

                return monitors;
            }

            foreach (var monitor in monitors)
            {
                monitor.Bounds = new Rect(monitor.Bounds.X / scale, monitor.Bounds.Y / scale, monitor.Bounds.Width / scale, monitor.Bounds.Height / scale);
                monitor.WorkingArea = new Rect(monitor.WorkingArea.X / scale, monitor.WorkingArea.Y / scale, monitor.WorkingArea.Width / scale, monitor.WorkingArea.Height / scale);
            }

            return monitors;
        }

        public static List<Monitor> AllMonitorsGranular(bool offset = false)
        {
            var monitors = AllMonitors;

            if (offset)
            {
                foreach (var monitor in monitors)
                {
                    monitor.NativeBounds = new Rect(monitor.Bounds.X - SystemParameters.VirtualScreenLeft, monitor.Bounds.Y - SystemParameters.VirtualScreenTop, monitor.Bounds.Width, monitor.Bounds.Height);
                    monitor.Bounds = new Rect(monitor.Bounds.X / monitor.Scale - SystemParameters.VirtualScreenLeft, monitor.Bounds.Y / monitor.Scale - SystemParameters.VirtualScreenTop, monitor.Bounds.Width / monitor.Scale, monitor.Bounds.Height / monitor.Scale);
                    monitor.WorkingArea = new Rect(monitor.WorkingArea.X / monitor.Scale - SystemParameters.VirtualScreenLeft, monitor.WorkingArea.Y / monitor.Scale - SystemParameters.VirtualScreenTop, monitor.WorkingArea.Width / monitor.Scale, monitor.WorkingArea.Height / monitor.Scale);
                }

                return monitors;
            }

            foreach (var monitor in monitors)
            {
                monitor.Bounds = new Rect(monitor.Bounds.X / monitor.Scale, monitor.Bounds.Y / monitor.Scale, monitor.Bounds.Width / monitor.Scale, monitor.Bounds.Height / monitor.Scale);
                monitor.WorkingArea = new Rect(monitor.WorkingArea.X / monitor.Scale, monitor.WorkingArea.Y / monitor.Scale, monitor.WorkingArea.Width / monitor.Scale, monitor.WorkingArea.Height / monitor.Scale);
            }

            return monitors;
        }

        public static Monitor FromPoint(int left, int top)
        {
            var handle = Util.Native.MonitorFromPoint(new Util.Native.PointW { X = left, Y = top }, Util.Native.MonitorDefaultToNearest);

            return new Monitor(handle, IntPtr.Zero);
        }

        public static Monitor MostIntersected(List<Monitor> monitors, Rect region)
        {
            return monitors.OrderByDescending(f =>
            {
                //var inter = Rect.Intersect(region, f.NativeBounds);
                //This methods does not work properly with multi DPI.

                var x = Math.Max(region.Left, f.NativeBounds.Left);
                var num1 = Math.Min(region.Left + region.Width, f.NativeBounds.Right);
                var y = Math.Max(region.Top, f.NativeBounds.Top);
                var num2 = Math.Min(region.Top + region.Height, f.NativeBounds.Bottom);

                if (num1 >= x && num2 >= y)
                    return num1 - x + num2 - y;

                return 0;
            }).ThenBy(t => t.IsPrimary).FirstOrDefault();
        }


        private class MonitorEnumCallback
        {
            public ArrayList Monitors { get; private set; }

            public MonitorEnumCallback()
            {
                Monitors = new ArrayList();
            }

            public bool Callback(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lparam)
            {
                Monitors.Add(new Monitor(monitor, hdc));
                return true;
            }
        }
    }
}