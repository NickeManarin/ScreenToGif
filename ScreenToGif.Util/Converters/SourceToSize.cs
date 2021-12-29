using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ScreenToGif.Util.Converters;

public class SourceToSize : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = parameter as string;

        if (value is not BitmapImage image || string.IsNullOrEmpty(param))
            return value;

        return param.Equals("width") ? image.Width : image.Height;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}