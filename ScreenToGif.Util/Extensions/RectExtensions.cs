using System.Windows;

namespace ScreenToGif.Util.Extensions;

public static class RectExtensions
{
    public static bool Contains(this Int32Rect first, Int32Rect second)
    {
        if (first.IsEmpty || second.IsEmpty || (first.X > second.X || first.Y > second.Y) || first.X + first.Width < second.X + second.Width)
            return false;

        return first.Y + first.Height >= second.Y + second.Height;
    }

    public static Rect Offset(this Rect rect, double offset)
    {
        return new Rect(Math.Round(rect.Left + offset, MidpointRounding.AwayFromZero), Math.Round(rect.Top + offset, MidpointRounding.AwayFromZero),
            Math.Round(rect.Width - (offset * 2d), MidpointRounding.AwayFromZero), Math.Round(rect.Height - (offset * 2d), MidpointRounding.AwayFromZero));

        //return new Rect(rect.Left + offset, rect.Top + offset, rect.Width - (offset * 2d), rect.Height - (offset * 2d));
    }

    public static Rect Translate(this Rect rect, double offsetX, double offsetY)
    {
        return rect.IsEmpty ? rect : new Rect(Math.Round(rect.Left + offsetX, MidpointRounding.AwayFromZero), Math.Round(rect.Top + offsetY, MidpointRounding.AwayFromZero),
            Math.Round(rect.Width, MidpointRounding.AwayFromZero), Math.Round(rect.Height, MidpointRounding.AwayFromZero));

        //return rect.IsEmpty ? rect : new Rect(rect.Left + offsetX, rect.Top + offsetY, rect.Width, rect.Height);
    }

    public static Rect Scale(this Rect rect, double scale)
    {
        return new Rect(Math.Round(rect.Left * scale, MidpointRounding.AwayFromZero), Math.Round(rect.Top * scale, MidpointRounding.AwayFromZero),
            Math.Round(rect.Width * scale, MidpointRounding.AwayFromZero), Math.Round(rect.Height * scale, MidpointRounding.AwayFromZero));
    }

    public static Rect Limit(this Rect rect, double width, double height)
    {
        var newX = rect.X < 0 ? 0 : rect.X;
        var newY = rect.Y < 0 ? 0 : rect.Y;

        var newWidth = newX + rect.Width > width ? width - newX : rect.Width;
        var newHeight = newY + rect.Height > height ? height - newY : rect.Height;

        return new Rect(newX, newY, newWidth, newHeight);
    }
}