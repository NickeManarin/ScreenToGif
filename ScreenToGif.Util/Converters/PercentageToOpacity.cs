using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Converts a value in the order of 100s value to a Opacity double (0 to 1) and vice-versa.
/// </summary>
public class PercentageToOpacity : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double cent)
            return DependencyProperty.UnsetValue;

        return cent * 100D;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double opacity)
            return DependencyProperty.UnsetValue;

        return opacity / 100D;
    }
}