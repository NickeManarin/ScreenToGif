using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Bool to Opacity property converter. Returns 0 if true.
    /// </summary>
    public class BoolToOpacity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolean = value as bool?;

            if (!boolean.HasValue)
                return DependencyProperty.UnsetValue;

            return boolean.Value ? 0 : 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
