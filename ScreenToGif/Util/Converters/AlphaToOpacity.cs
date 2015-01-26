using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Converts the Alpha value to a Opacity Double and vice-versa.
    /// </summary>
    public class AlphaToOpacity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cent = value as long?;

            if (!cent.HasValue)
                return DependencyProperty.UnsetValue;

            var brush = cent.Value / 255F;

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as double?;

            if (parameterString == null || value.Equals(false))
                return DependencyProperty.UnsetValue;

            return parameter;
        }
    }
}
