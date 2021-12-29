using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Simple Double-Int32 converter.
/// </summary>
public class DoubleToInt : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double number || double.IsNaN(number))
            return DependencyProperty.UnsetValue;

        return System.Convert.ToInt32(number);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int number)
            return DependencyProperty.UnsetValue;

        return System.Convert.ToDouble(number);
    }
}