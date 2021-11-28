using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using Monitor = ScreenToGif.Domain.Models.Native.Monitor;

namespace ScreenToGif.Native.Helpers;

public static class MonitorHelper
{
    private static Monitor ParseMonitor(IntPtr monitorHandle, IntPtr hdc)
    {
        var info = new MonitorInfoEx(); //TODO: MonitorInfo not getting filled with data.
        var a = User32.GetMonitorInfo(new HandleRef(null, monitorHandle), info);

        var name = new string(info.szDevice).TrimEnd((char)0);

        var monitor = new Monitor
        {
            Handle = monitorHandle,
            Name = name,
            FriendlyName = name,
            NativeBounds = new Rect(info.rcMonitor.Left, info.rcMonitor.Top,
                info.rcMonitor.Right - info.rcMonitor.Left,
                info.rcMonitor.Bottom - info.rcMonitor.Top),
            Bounds = new Rect(info.rcMonitor.Left, info.rcMonitor.Top,
                info.rcMonitor.Right - info.rcMonitor.Left,
                info.rcMonitor.Bottom - info.rcMonitor.Top),
            WorkingArea = new Rect(info.rcWork.Left, info.rcWork.Top,
                info.rcWork.Right - info.rcWork.Left,
                info.rcWork.Bottom - info.rcWork.Top),
            IsPrimary = (info.dwFlags & Constants.MonitorinfoPrimary) != 0
        };

        #region Extra details

        try
        {
            var display = new DisplayDevices(true);

            for (uint id = 0; User32.EnumDisplayDevices(null, id, ref display, 0); id++)
            {
                var found = display.DeviceName == monitor.Name;
                var adapter = display.DeviceString;

                User32.EnumDisplayDevices(display.DeviceName, id, ref display, 0);

                if (!found)
                    continue;

                monitor.AdapterName = adapter;
                monitor.FriendlyName = string.IsNullOrWhiteSpace(display.DeviceString) ? LocalizationHelper.Get("S.Recorder.Screen.Name.Internal") :
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
            ShCore.GetDpiForMonitor(monitorHandle, DpiTypes.Effective, out var aux, out _);
            monitor.Dpi = aux > 0 ? (int)aux : 96;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to detect screen DPI.");

            try
            {
                var h = Gdi32.CreateCompatibleDC(IntPtr.Zero);
                monitor.Dpi = Gdi32.GetDeviceCaps(h, (int)DeviceCaps.LogPixelsX);
                Gdi32.DeleteDC(h);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Error getting fallback of screen DPI.");
            }
        }

        #endregion

        return monitor;
    }

    public static List<Monitor> AllMonitors
    {
        get
        {
            var closure = new MonitorEnumCallback();
            var proc = new Delegates.MonitorEnumProc(closure.Callback);

            User32.EnumDisplayMonitors(Constants.NullHandleRef, IntPtr.Zero, proc, IntPtr.Zero);

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
        var handle = User32.MonitorFromPoint(new PointW { X = left, Y = top }, Constants.MonitorDefaultToNearest);

        return ParseMonitor(handle, IntPtr.Zero);
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
            Monitors.Add(ParseMonitor(monitor, hdc));
            return true;
        }
    }
}