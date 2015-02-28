using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Converts a Integer to a String formated as Delay (Example: 1 ms)
    /// </summary>
    public class IntToDelayString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var intValue = value as int?;

            if (!intValue.HasValue)
                return DependencyProperty.UnsetValue;

            return String.Format("{0} ms", intValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
