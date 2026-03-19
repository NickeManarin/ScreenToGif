using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using System.Windows;
using System.Windows.Input;

namespace ScreenToGif.Util.Helpers;

public static class CursorHelper
{
    public static Point GetMousePosition(double scale = 1, double offsetX = 0, double offsetY = 0)
    {
        var point = new PointW();
        User32.GetCursorPos(ref point);
        return new Point(point.X / scale - offsetX, point.Y / scale - offsetY);
    }

    public static void SetToPosition(FrameworkElement element, bool centerOnElement = false)
    {
        var relativePoint = centerOnElement ? new Point(element.ActualWidth / 2, element.ActualHeight / 2) : Mouse.GetPosition(element);
        var screenPoint = element.PointToScreen(new Point(0, 0));
        var scale = element.GetVisualScale();

        User32.SetCursorPos((int)(screenPoint.X + relativePoint.X * scale), (int)(screenPoint.Y + relativePoint.Y * scale));
    }
}
