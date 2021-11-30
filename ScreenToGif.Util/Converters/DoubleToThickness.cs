using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class DoubleToThickness : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return new Thickness(0);

        var pos = parameter as string == "+";
        var left = values[0] as double? ?? values[0] as int? ?? 0;
        var top = values[1] as double? ?? values[1] as int? ?? 0;

        if (values.Length < 4)
        {
            if (pos)
                return new Thickness(Math.Abs(Math.Max(0, left)), Math.Abs(Math.Max(0, top)), Math.Abs(Math.Max(0, left)), Math.Abs(Math.Max(0, top)));

            return new Thickness(Math.Abs(Math.Min(0, left)), Math.Abs(Math.Min(0, top)), Math.Abs(Math.Min(0, left)), Math.Abs(Math.Min(0, top)));
        }

        var right = values[2] as double? ?? values[2] as int? ?? 0;
        var bottom = values[3] as double? ?? values[3] as int? ?? 0;

        if (pos)
            return new Thickness(Math.Abs(Math.Max(0, left)), Math.Abs(Math.Max(0, top)), Math.Abs(Math.Max(0, right)), Math.Abs(Math.Max(0, bottom)));

        return new Thickness(Math.Abs(Math.Min(0, left)), Math.Abs(Math.Min(0, top)), Math.Abs(Math.Min(0, right)), Math.Abs(Math.Min(0, bottom)));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}