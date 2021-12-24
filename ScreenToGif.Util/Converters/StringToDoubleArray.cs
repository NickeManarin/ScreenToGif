using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenToGif.Util.Converters;

public class StringToDoubleArray : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            var text = value as string;

            if (string.IsNullOrWhiteSpace(text))
                return new DoubleCollection { 1, 0 };

            return DoubleCollection.Parse(Regex.Replace(text, " {2,}", " "));
        }
        catch (Exception)
        {
            return new DoubleCollection { 1, 0 };
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var col = value as DoubleCollection;

        return col?.ToString().Replace(',', ' ');
    }
}