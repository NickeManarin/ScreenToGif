using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenToGif.Domain.Models.Native;

[StructLayout(LayoutKind.Sequential)]
public struct NativeRect
{
    public int Left;        // x position of upper-left corner
    public int Top;         // y position of upper-left corner
    public int Right;       // x position of lower-right corner
    public int Bottom;      // y position of lower-right corner

    public Int32Rect ToRectangle()
    {
        return new Int32Rect(Left, Top, Right - Left, Bottom - Top);
    }

    public Rect ToRect(double offset = 0, double scale = 1d)
    {
        return new Rect((Left - offset) / scale, (Top - offset) / scale, (Right - Left + offset * 2) / scale, (Bottom - Top + offset * 2) / scale);
    }

    public Rect TryToRect(double offset = 0, double scale = 1d)
    {
        var left = (Left - offset) / scale;
        var top = (Top - offset) / scale;
        var width = (Right - Left + offset * 2) / scale;
        var height = (Bottom - Top + offset * 2) / scale;

        if (double.IsNaN(left) || double.IsNaN(top) || width < 0 || height < 0)
            return Rect.Empty;

        return new Rect(left, top, width, height);
    }

    public bool IsValid()
    {
        return Right - Left > 0 && Bottom - Top > 0;
    }
}