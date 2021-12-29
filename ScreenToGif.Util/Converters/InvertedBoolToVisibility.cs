using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// The inverted BoolToVisibility converter.
/// </summary>
public class InvertedBoolToVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool vis)
            return DependencyProperty.UnsetValue;

        return vis ? Visibility.Collapsed: Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Visibility vis)
            return DependencyProperty.UnsetValue;

        return !vis.Equals(Visibility.Visible);
    }
}