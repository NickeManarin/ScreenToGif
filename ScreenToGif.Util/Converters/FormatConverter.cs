using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class FormatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Binding.DoNothing;

        if (values[0] is not string format)
            return Binding.DoNothing;

        var list = values.ToList();
        list.RemoveAt(0);

        return string.Format(format, list.ToArray());
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}