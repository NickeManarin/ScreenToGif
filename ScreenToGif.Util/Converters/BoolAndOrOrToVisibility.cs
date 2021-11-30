using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class BoolAndOrOrToVisibility : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        //List of params:
        //[0]: Always true.
        //[1-2]: At least one true.

        var list = values.Cast<bool>().ToList();

        if (list.Count != 3)
            return Visibility.Collapsed;

        if (!list[0] || !(list[1] || list[2]))
            return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return null;
    }
}