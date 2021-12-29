using System;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class HasEnumToVisibility: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value?.Equals(parameter) == true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value?.Equals(Visibility.Visible) == true ? parameter : Binding.DoNothing;
    }
}