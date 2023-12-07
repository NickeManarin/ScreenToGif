using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.External;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenToGif.Native.Helpers;

public static class WindowHelper
{
    private const int GetWindowsLongStyle = -16;
    private const int GetWindowsLongExStyle = -20;

    public static IntPtr GetWindowPtr(this Window window)
    {
        return new WindowInteropHelper(window).Handle;
    }

    public static void DisableMaximize(this Window window)
    {
        var handle = GetWindowPtr(window);

        User32.SetWindowLong(handle, GetWindowsLongStyle, User32.GetWindowLong(handle, GetWindowsLongStyle) & ~(int)WindowStyles.MaximizeBox);

        UpdateMenuStatus(handle, window.WindowState);
    }

    public static void DisableMinimize(this Window window)
    {
        var handle = GetWindowPtr(window);

        User32.SetWindowLong(handle, GetWindowsLongStyle, User32.GetWindowLong(handle, GetWindowsLongStyle) & ~(int)WindowStyles.MinimizeBox);

        UpdateMenuStatus(handle, window.WindowState);
    }

    public static void EnableMaximize(this Window window)
    {
        var handle = GetWindowPtr(window);

        User32.SetWindowLong(handle, GetWindowsLongStyle, User32.GetWindowLong(handle, GetWindowsLongStyle) & (int)WindowStyles.MaximizeBox);

        UpdateMenuStatus(handle, window.WindowState);
    }

    public static void EnableMinimize(this Window window)
    {
        var handle = GetWindowPtr(window);

        User32.SetWindowLong(handle, GetWindowsLongStyle, User32.GetWindowLong(handle, GetWindowsLongStyle) & (int)WindowStyles.MinimizeBox);

        UpdateMenuStatus(handle, window.WindowState);
    }

    public static void SetCornerPreference(this Window window, CornerPreferences preference)
    {
        var ptr = GetWindowPtr(window);

        var attr = (int)preference;

        DwmApi.DwmSetWindowAttribute(ptr, DwmWindowAttributes.WindowCornerPreference, ref attr, Marshal.SizeOf(typeof(int)));
    }

    public static void SetResizeMode(this Window window)
    {
        var handle = GetWindowPtr(window);

        if (window.ResizeMode is ResizeMode.CanMinimize or ResizeMode.NoResize)
            UpdateStyle(handle, WindowStyles.ThickFrame, 0);
        else
            UpdateStyle(handle, 0, WindowStyles.ThickFrame);
    }

    public static void UpdateMenuStatus(this Window window)
    {
        var handle = GetWindowPtr(window);
        var state = window.WindowState;

        UpdateMenuStatus(handle, state);
    }

    public static IntPtr NearestMonitorForWindow(IntPtr window)
    {
        return User32.MonitorFromWindow(window, Constants.MonitorDefaultToNearest);
    }

    private static bool UpdateStyle(IntPtr handle, WindowStyles removeStyle, WindowStyles addStyle)
    {
        var style = User32.GetWindowLong(handle, GetWindowsLongStyle);

        var newStyle = (style & ~((int)removeStyle)) | ((int)addStyle);

        if (style == newStyle)
            return false;

        User32.SetWindowLong(handle, GetWindowsLongStyle, newStyle);
        return true;
    }

    private static void UpdateMenuStatus(IntPtr handle, WindowState state)
    {
        const uint enabled = (uint)(MenuFunctions.Enabled | MenuFunctions.ByCommand);
        const uint disabled = (uint)(MenuFunctions.Grayed | MenuFunctions.Disabled | MenuFunctions.ByCommand);

        var menu = User32.GetSystemMenu(handle, false);

        if (IntPtr.Zero == menu)
            return;

        var dwStyle = User32.GetWindowLong(handle, GetWindowsLongStyle);

        var canMinimize = (dwStyle & (int)WindowStyles.MinimizeBox) != 0;
        var canMaximize = (dwStyle & (int)WindowStyles.MaximizeBox) != 0;
        var canSize = (dwStyle & (int)WindowStyles.ThickFrame) != 0;

        switch (state)
        {
            case WindowState.Maximized:
                User32.EnableMenuItem(menu, SysCommands.Restore, enabled);
                User32.EnableMenuItem(menu, SysCommands.Move, disabled);
                User32.EnableMenuItem(menu, SysCommands.Size, disabled);
                User32.EnableMenuItem(menu, SysCommands.Minimize, canMinimize ? enabled : disabled);
                User32.EnableMenuItem(menu, SysCommands.Maximize, disabled);
                break;
            case WindowState.Minimized:
                User32.EnableMenuItem(menu, SysCommands.Restore, enabled);
                User32.EnableMenuItem(menu, SysCommands.Move, disabled);
                User32.EnableMenuItem(menu, SysCommands.Size, disabled);
                User32.EnableMenuItem(menu, SysCommands.Minimize, disabled);
                User32.EnableMenuItem(menu, SysCommands.Maximize, canMaximize ? enabled : disabled);
                break;
            default:
                User32.EnableMenuItem(menu, SysCommands.Restore, disabled);
                User32.EnableMenuItem(menu, SysCommands.Move, enabled);
                User32.EnableMenuItem(menu, SysCommands.Size, canSize ? enabled : disabled);
                User32.EnableMenuItem(menu, SysCommands.Minimize, canMinimize ? enabled : disabled);
                User32.EnableMenuItem(menu, SysCommands.Maximize, canMaximize ? enabled : disabled);
                break;
        }
    }
}