using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Converts the Alpha value to a Opacity Double and vice-versa.
/// </summary>
public class AlphaToOpacity : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long cent)
            return DependencyProperty.UnsetValue;

        return cent / 255F;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not double || value?.Equals(false) == true)
            return DependencyProperty.UnsetValue;

        return parameter;
    }
}