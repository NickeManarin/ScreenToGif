using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class CenterPopupConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return DependencyProperty.UnsetValue;

        if (values[0] is not double targetWidth || values[1] is not double popupWidth)
            return DependencyProperty.UnsetValue;

        return targetWidth / 2.0 - popupWidth / 2.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}