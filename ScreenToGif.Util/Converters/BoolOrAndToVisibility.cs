using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class BoolOrAndToVisibility : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var list = values.Cast<bool>().ToList();

        for (var i = 0; i < list.Count; i += 2)
        {
            if (list[i] && list[i + 1])
                return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return null;
    }
}