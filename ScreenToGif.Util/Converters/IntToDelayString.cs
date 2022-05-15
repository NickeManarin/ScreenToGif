using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Converts a Integer to a String formatted as Delay (Example: 1 ms)
/// </summary>
public class IntToDelayString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue)
            return DependencyProperty.UnsetValue;

        return $"{intValue} ms";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}