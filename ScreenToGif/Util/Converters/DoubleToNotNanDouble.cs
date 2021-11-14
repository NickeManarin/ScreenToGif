using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Double to NotNanDouble converter.
    /// </summary>
    public class DoubleToNotNanDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value as double?;

            if (!number.HasValue || Double.IsNaN(number.Value))
                return DependencyProperty.UnsetValue;

            return number;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value as double?;

            if (!number.HasValue || Double.IsNaN(number.Value))
                return DependencyProperty.UnsetValue;

            return number;
        }
    }
}
