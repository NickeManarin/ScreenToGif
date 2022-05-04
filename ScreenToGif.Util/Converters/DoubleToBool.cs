using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Double to Boolean property converter. It compares the the parameter with the provided value.
/// </summary>
public class DoubleToBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double number || parameter is not string param)
            return DependencyProperty.UnsetValue;

        return Math.Abs(number - double.Parse(param)) < 0.001;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}