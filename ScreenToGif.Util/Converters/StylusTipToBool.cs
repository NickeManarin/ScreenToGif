using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Ink;

namespace ScreenToGif.Util.Converters;

public class StylusTipToBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not StylusTip tip || parameter is not string param)
            return DependencyProperty.UnsetValue;

        return tip.ToString().Contains(param);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = parameter as string;

        if (value is not bool selection || !selection)
            return DependencyProperty.UnsetValue;

        if (string.IsNullOrEmpty(param))
            return StylusTip.Rectangle;

        return param.Equals("Ellipse") ? StylusTip.Ellipse : StylusTip.Rectangle;
    }
}