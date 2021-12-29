using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class StringToStaticResource : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value as string;

        if (string.IsNullOrWhiteSpace(str) || Application.Current == null || !Application.Current.Resources.Contains(str))
            return DependencyProperty.UnsetValue;

        return Application.Current.Resources[str];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}