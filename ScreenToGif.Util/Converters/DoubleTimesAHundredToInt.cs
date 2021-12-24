using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class DoubleTimesAHundredToInt : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double doubleValue)
            return DependencyProperty.UnsetValue;

        return doubleValue * 100;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue)
            return DependencyProperty.UnsetValue;

        return intValue / 100D;
    }
}