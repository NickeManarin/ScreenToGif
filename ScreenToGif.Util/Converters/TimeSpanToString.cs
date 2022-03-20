using System.Globalization;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class TimeSpanToString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TimeSpan time)
            return Binding.DoNothing;

        if (time.Days > 0)
            return time.ToString("d\\:hh\\:mm\\:ss", culture);

        if (time.Hours > 0)
            return time.ToString("h\\:mm\\:ss", culture);

        return time.ToString("mm\\:ss", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}