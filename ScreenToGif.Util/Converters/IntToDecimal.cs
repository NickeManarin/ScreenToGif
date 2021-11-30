using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Converts 100 to 1,0.
/// </summary>
public class IntToDecimal : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double doubleValue)
            return DependencyProperty.UnsetValue;

        return (double?)doubleValue * 100;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue)
            return DependencyProperty.UnsetValue;

        return (int?)intValue / 100D;
    }
}