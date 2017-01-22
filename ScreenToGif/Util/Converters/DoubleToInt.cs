using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Simple Double-Int32 converter.
    /// </summary>
    public class DoubleToInt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value as double?;

            if (!number.HasValue || Double.IsNaN(number.Value))
                return DependencyProperty.UnsetValue;

            return System.Convert.ToInt32(number.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value as int?;

            if (!number.HasValue)
                return DependencyProperty.UnsetValue;

            return System.Convert.ToDouble(number.Value);
        }
    }
}
