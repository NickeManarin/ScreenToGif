using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Double to Boolean property converter. It compares the the parameter with the provided value.
    /// </summary>
    public class DoubleToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter as string;

            if (!(value is double @double) || param == null)
                return DependencyProperty.UnsetValue;

            return @double == double.Parse(param);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}