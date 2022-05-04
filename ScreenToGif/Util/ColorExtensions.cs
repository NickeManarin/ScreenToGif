using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using ScreenToGif.Controls;

namespace ScreenToGif.Util;

internal static class ColorExtensions
{
    public static bool IsEnoughContrast(this Color color1, Color color2)
    {
        Console.WriteLine(GetBrightness(color1) + ": " + GetBrightness2(color1) + " - " + GetBrightness2(color2) + " = " + Math.Abs(GetBrightness2(color1) - GetBrightness2(color2)));

        return Math.Abs(GetBrightness2(color1) - GetBrightness2(color2)) > 125;
    }

    public static int GetBrightness4(this Color color)
    {
        return color.R + (2*color.G) + color.B;
    }

    public static float GetBrightness3(this Color color)
    {
        var num = color.R / 255f;
        var num2 = color.G / 255f;
        var num3 = color.B / 255f;
        var num4 = num;
        var num5 = num;
        if (num2 > num4)
            num4 = num2;
        if (num3 > num4)
            num4 = num3;
        if (num2 < num5)
            num5 = num2;
        if (num3 < num5)
            num5 = num3;
        return (num4 + num5) / 2f;
    }

    public static double GetBrightness2(this Color c)
    {
        return 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
        //return (0.299*c.R + 0.587*c.G + 0.114*c.B);
    }

    public static int GetBrightness1(this Color c)
    {
        return (int)Math.Sqrt(
            c.R * c.R * .241 +
            c.G * c.G * .691 +
            c.B * c.B * .068);
    }

    public static int GetBrightness(this Color c)
    {
        return (2 * c.R) + (5 * c.G) + c.B;
    }

    public static float GetHue(this Color color)
    {
        if ((color.R == color.G) && (color.G == color.B))
            return 0f;
        var num = color.R / 255f;
        var num2 = color.G / 255f;
        var num3 = color.B / 255f;
        var num7 = 0f;
        var num4 = num;
        var num5 = num;
        if (num2 > num4)
            num4 = num2;
        if (num3 > num4)
            num4 = num3;
        if (num2 < num5)
            num5 = num2;
        if (num3 < num5)
            num5 = num3;
        var num6 = num4 - num5;
        if (num == num4)
            num7 = (num2 - num3) / num6;
        else if (num2 == num4)
            num7 = 2f + (num3 - num) / num6;
        else if (num3 == num4)
            num7 = 4f + (num - num2) / num6;
        num7 *= 60f;
        if (num7 < 0f)
            num7 += 360f;
        return num7;
    }

    public static float GetSaturation(this Color color)
    {
        var num = color.R / 255f;
        var num2 = color.G / 255f;
        var num3 = color.B / 255f;

        var num7 = 0f;
        var num4 = num;
        var num5 = num;
        if (num2 > num4)
            num4 = num2;
        if (num3 > num4)
            num4 = num3;
        if (num2 < num5)
            num5 = num2;
        if (num3 < num5)
            num5 = num3;
        if (num4 == num5)
            return num7;

        var num6 = (num4 + num5) / 2f;
        if (num6 <= 0.5)
            return (num4 - num5) / (num4 + num5);
        return (num4 - num5) / (2f - num4 - num5);
    }


    #region Color Comparison

    /// <summary>
    /// Closest match for hues only.
    /// </summary>
    /// <param name="colors"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static int ClosestColorHue(List<Color> colors, Color target)
    {
        var hue1 = target.GetHue();
        var diffs = colors.Select(n => GetHueDistance(n.GetHue(), hue1));
        var diffMin = diffs.Min(n => n);

        return diffs.ToList().FindIndex(n => n == diffMin);
    }

    /// <summary>
    /// Closest match in RGB space.
    /// </summary>
    /// <param name="colors"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static int ClosestColorRgb(List<Color> colors, Color target)
    {
        //var colorDiffs = colors.AsParallel().Select(n => ColorDiff(n, target)).Min(n => n);
        //return colors.FindIndex(n => ColorDiff(n, target) == colorDiffs);

        var distance = int.MaxValue;
        var indexOfMin = -1;
        Parallel.For(0, colors.Count, (i, x) =>
        {
            var diff = ColorDiff(colors[i], target);

            if (diff < distance)
            {
                distance = diff;
                indexOfMin = i;
            }

            if (distance == 0)
                x.Break();
        });

        return indexOfMin;

        //return colors.AsParallel().Select(n=> ColorDiff(n, target)).IndexOfMin();
    }

    public static int IndexOfMin<T>(this IEnumerable<T> list) where T : IComparable
    {
        //or
        /* int min = self[0];
           int minIndex = 0;

           for (int i = 1; i < self.Count; ++i) {
               if (self[i] < min) {
                   min = self[i];
                   minIndex = i;
               }
           }*/

        var enumerator = list.GetEnumerator();
        enumerator.MoveNext();

        var minValue = enumerator.Current;

        var minOffset = 0;
        for (var i = 1; enumerator.MoveNext(); ++i)
        {
            if (enumerator.Current.CompareTo(minValue) >= 0)
                continue;

            minValue = enumerator.Current;
            minOffset = i;
        }

        return minOffset;
    }

    /// <summary>
    /// Weighed distance using hue, saturation and brightness.
    /// </summary>
    /// <param name="colors"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static int ClosestColorHsb(List<Color> colors, Color target)
    {
        var hue1 = target.GetHue();
        var num1 = ColorNum(target);
        var diffs = colors.Select(n => Math.Abs(ColorNum(n) - num1) +
                                       GetHueDistance(n.GetHue(), hue1));
        var diffMin = diffs.Min(x => x);
        return diffs.ToList().FindIndex(n => n == diffMin);
    }

    /// <summary>
    /// Color brightness as perceived.
    /// </summary>
    /// <param name="c">The Color</param>
    /// <returns>The brightness.</returns>
    public static float GetLuminance(Color c)
    {
        return (c.R * 0.299f + c.G * 0.587f + c.B * 0.114f) / 256f;
    }

    /// <summary>
    /// Gets the distance between two hues.
    /// </summary>
    /// <param name="hue1">Hue 1</param>
    /// <param name="hue2">Hue 2</param>
    /// <returns>The distance.</returns>
    public static float GetHueDistance(float hue1, float hue2)
    {
        var d = Math.Abs(hue1 - hue2); return d > 180 ? 360 - d : d;
    }

    public static float ColorNum(Color c)
    {
        var factorSat = 3;
        var factorBri = 3;
        return c.GetSaturation() * factorSat + GetBrightness(c) * factorBri;
    }

    /// <summary>
    /// Gets the distance in the RGB space.
    /// </summary>
    /// <param name="first">Color 1</param>
    /// <param name="second">Color 2</param>
    /// <returns>The distance.</returns>
    public static int ColorDiff(Color first, Color second)
    {
        return (int)Math.Sqrt((first.R - second.R) * (first.R - second.R) + (first.G - second.G) * (first.G - second.G) + (first.B - second.B) * (first.B - second.B));
    }

    #endregion

    /// <summary>
    /// Converts an RGB color to an HSV color
    /// </summary>
    /// <param name="color">Color</param>
    /// <returns>A HsvColor object.</returns>
    public static HsvColor ConvertRgbToHsv(this Color color)
    {
        double h = 0, s;

        double min = Math.Min(Math.Min(color.R, color.G), color.B);
        double v = Math.Max(Math.Max(color.R, color.G), color.B);
        var delta = v - min;

        if (v == 0.0)
            s = 0;
        else
            s = delta / v;

        if (s == 0)
            h = 0.0;
        else
        {
            if (color.R == v)
                h = (color.G - color.B) / delta;
            else if (color.G == v)
                h = 2 + (color.B - color.R) / delta;
            else if (color.B == v)
                h = 4 + (color.R - color.G) / delta;

            h *= 60;
            if (h < 0.0)
                h = h + 360;
        }

        var hsvColor = new HsvColor();
        hsvColor.H = h;
        hsvColor.S = s;
        hsvColor.V = v / 255;

        return hsvColor;
    }

    /// <summary>
    /// Converts an RGB color to an HSV color
    /// </summary>
    /// <param name="r">Red</param>
    /// <param name="b">Blue</param>
    /// <param name="g">Green</param>
    /// <returns>A HsvColor object.</returns>
    public static HsvColor ConvertRgbToHsv(int r, int b, int g)
    {
        double h = 0, s;

        double min = Math.Min(Math.Min(r, g), b);
        double v = Math.Max(Math.Max(r, g), b);
        var delta = v - min;

        if (v == 0.0)
            s = 0;
        else
            s = delta / v;

        if (s == 0)
            h = 0.0;
        else
        {
            if (r == v)
                h = (g - b) / delta;
            else if (g == v)
                h = 2 + (b - r) / delta;
            else if (b == v)
                h = 4 + (r - g) / delta;

            h *= 60;
            if (h < 0.0)
                h = h + 360;
        }

        var hsvColor = new HsvColor();
        hsvColor.H = h;
        hsvColor.S = s;
        hsvColor.V = v / 255;

        return hsvColor;
    }

    /// <summary>
    /// Converts an HSV color to an RGB color.
    /// </summary>
    /// <param name="h">Hue</param>
    /// <param name="s">Saturation</param>
    /// <param name="v">Value</param>
    /// <param name="alpha">Alpha</param>
    /// <returns></returns>
    public static Color ConvertHsvToRgb(double h, double s, double v, double alpha)
    {
        double r, g, b;

        if (s == 0)
        {
            r = v;
            g = v;
            b = v;
        }
        else
        {
            if (h == 360)
                h = 0;
            else
                h = h / 60;

            var i = (int)Math.Truncate(h);
            var f = h - i;

            var p = v * (1.0 - s);
            var q = v * (1.0 - s * f);
            var t = v * (1.0 - s * (1.0 - f));

            switch (i)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;

                default:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }
        }

        return Color.FromArgb((byte)alpha, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    /// <summary>
    /// Generates a list of colors with hues ranging from 0-360 and a saturation and value of 1.
    /// </summary>
    /// <returns>The List of Colors</returns>
    public static List<Color> GenerateHsvSpectrum(int count)
    {
        var colorsList = new List<Color>();

        var stop = 360d / count;
        var isDecimal = stop % 1 > 0;
            
        for (var i = 0; i <= (isDecimal ? count - 1 : count); i++)
            colorsList.Add(ConvertHsvToRgb(i * stop, 1, 1, 255));

        if (isDecimal)
            colorsList.Add(ConvertHsvToRgb(360, 1, 1, 255));

        //for (var i = 0; i < 29; i++)
        //    colorsList.Add(ConvertHsvToRgb(i * 12, 1, 1, 255));

        //colorsList.Add(ConvertHsvToRgb(0, 1, 1, 255));
        return colorsList;
    }

    /// <summary>
    /// Generates a list of colors with transparency ranging from 0-255.
    /// </summary>
    public static List<Color> GenerateAlphaSpectrum(Color color, int count = 2)
    {
        var colorsList = new List<Color>();

        var stop = 255d / count;
        var isDecimal = stop % 1 > 0;

        for (var i = 0; i <= (isDecimal ? count - 1 : count); i++)
            colorsList.Add(Color.FromArgb((byte)(i * stop), color.R, color.G, color.B));

        if (isDecimal)
            colorsList.Add(Color.FromArgb(255, color.R, color.G, color.B));

        colorsList.Reverse();

        return colorsList;
    }

    public static System.Drawing.Color ToDrawingColor(this Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
}