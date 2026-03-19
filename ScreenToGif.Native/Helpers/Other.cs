using System.Runtime.InteropServices;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Domain.Models.Native;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;

namespace ScreenToGif.Native.Helpers;

public class Other
{
    /// <summary>
    /// Draws a rectangle over a Window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="scale">Window scale.</param>
    public static void DrawFrame(IntPtr hWnd, double scale)
    {
        //TODO: Adjust for high DPI.
        if (hWnd == IntPtr.Zero)
            return;

        var hdc = User32.GetWindowDC(hWnd); //GetWindowDC((IntPtr) null);

        User32.GetWindowRect(hWnd, out NativeRect rect);

        //DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DwmwaExtendedFrameBounds, out rect, Marshal.SizeOf(typeof(Rect)));
        User32.OffsetRect(ref rect, -rect.Left, -rect.Top);

        const int frameWidth = 3;

        Gdi32.PatBlt(hdc, rect.Left, rect.Top, rect.Right - rect.Left, frameWidth, Constants.DstInvert);

        Gdi32.PatBlt(hdc, rect.Left, rect.Bottom - frameWidth, frameWidth, -(rect.Bottom - rect.Top - 2 * frameWidth), Constants.DstInvert);

        Gdi32.PatBlt(hdc, rect.Right - frameWidth, rect.Top + frameWidth, frameWidth, rect.Bottom - rect.Top - 2 * frameWidth, Constants.DstInvert);

        Gdi32.PatBlt(hdc, rect.Right, rect.Bottom - frameWidth, -(rect.Right - rect.Left), frameWidth, Constants.DstInvert);
    }

    public static bool ShowFileProperties(string filename)
    {
        var info = new ShellExecuteInfo();
        info.cbSize = Marshal.SizeOf(info);
        info.lpVerb = "properties";
        info.lpFile = filename;
        //info.lpParameters = "Security";
        info.nShow = (int)ShowWindowCommands.Show;
        info.fMask = (uint)ShellExecuteMasks.InvokeIdList;
        return Shell32.ShellExecuteEx(ref info);
    }
}