using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class IntToRect : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return DependencyProperty.UnsetValue;

        var width = values[0] as double? ?? values[0] as int? ?? 0;
        var height = values[1] as double? ?? values[1] as int? ?? 0;

        if (values.Length < 4)
            return new Rect(new Point(0, 0), new Size(width, height));

        var xAxis = values[2] as double? ?? values[2] as int? ?? 0;
        var yAxis = values[3] as double? ?? values[3] as int? ?? 0;

        if (double.IsNegativeInfinity(width) || double.IsNegativeInfinity(height))
            return Rect.Empty;

        return new Rect(new Point(xAxis, yAxis), new Size(width, height));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}