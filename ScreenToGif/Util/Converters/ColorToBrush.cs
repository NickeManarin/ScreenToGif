using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Converts the System.Drawing.Color to a WPF Brush.
    /// </summary>
    public class ColorToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = value as Color?;

            if (!color.HasValue)
                return DependencyProperty.UnsetValue;

            var brush = new SolidColorBrush(color.Value);

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;

            if (parameterString == null || value.Equals(false))
                return DependencyProperty.UnsetValue;

            return parameter;
        }
    }
}
