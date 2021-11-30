using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ScreenToGif.Util.Converters;

public class KeysToString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var keys = value as Key?;

        return keys?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}