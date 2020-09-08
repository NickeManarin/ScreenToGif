using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Integer to Boolean property converter. It compares the the parameter with the provided value.
    /// </summary>
    public class IntToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter as string;
            
            if (!(value is int integer) || param == null)
                return DependencyProperty.UnsetValue;

            return integer == int.Parse(param);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}