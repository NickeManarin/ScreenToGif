using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Converts a String to Int32 and vice-versa.
    /// </summary>
    public class StringToInt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;

            if (String.IsNullOrEmpty(stringValue))
                return DependencyProperty.UnsetValue;

            return int.Parse(stringValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as int?;

            if (!parameterString.HasValue)
                return DependencyProperty.UnsetValue;

            return parameter.ToString();
        }
    }
}
