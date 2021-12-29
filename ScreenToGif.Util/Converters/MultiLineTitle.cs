using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class MultiLineTitle : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;

        if (string.IsNullOrEmpty(text))
            return value;

        return text.Replace(@"\n", Environment.NewLine);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}