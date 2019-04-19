using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Bool to Int property converter. It compares the the parameter with the provided value.
    /// </summary>
    public class IntToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int integer))
                return DependencyProperty.UnsetValue;

            return (int?) integer == int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string parameterString) || value.Equals(false)) 
                return DependencyProperty.UnsetValue;

            return parameter;
        }
    }
}