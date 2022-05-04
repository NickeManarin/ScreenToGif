using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class IntToString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int number|| number == 0)
            return "";

        return number.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}