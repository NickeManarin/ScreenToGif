using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class TimeSpanToTotalMilliseconds : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan)
            return Binding.DoNothing;

        return timeSpan.TotalMilliseconds;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double milliseconds)
            return Binding.DoNothing;

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}