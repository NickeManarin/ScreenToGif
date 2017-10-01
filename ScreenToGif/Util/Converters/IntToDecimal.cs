using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Converts 100 to 1,0.
    /// </summary>
    public class IntToDecimal : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var doubleValue = value as double?;

            if (!doubleValue.HasValue)
                return DependencyProperty.UnsetValue;

            return doubleValue * 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var intValue = value as int?;

            if (!intValue.HasValue)
                return DependencyProperty.UnsetValue;

            return intValue / 100D;
        }
    }
}
