using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    class InvertedVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = value as Visibility?;
            var param = parameter as string;

            if (!visibility.HasValue)
                return DependencyProperty.UnsetValue;

            return visibility.Value != Visibility.Visible ? Visibility.Visible : param != null ? Visibility.Hidden : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
