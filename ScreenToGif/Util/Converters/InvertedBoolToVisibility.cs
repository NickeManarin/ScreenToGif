using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// The inverted BoolToVisibility converter.
    /// </summary>
    public class InvertedBoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vis = value as bool?;

            if (!vis.HasValue)
                return DependencyProperty.UnsetValue;

            return vis.Value ? Visibility.Collapsed: Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vis = value as Visibility?;

            if (!vis.HasValue)
                return DependencyProperty.UnsetValue;

            return !vis.Value.Equals(Visibility.Visible);
        }
    }
}
