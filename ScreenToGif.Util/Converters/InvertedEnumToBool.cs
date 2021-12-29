using System;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class InvertedEnumToBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value?.Equals(parameter) == false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value?.Equals(true) == false ? parameter : Binding.DoNothing;
    }
}