using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class IntToVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = parameter as string;

        if (value is not int number || string.IsNullOrWhiteSpace(param) || !int.TryParse(param, out var number2))
            return Visibility.Collapsed;

        return Equals(number, number2) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}