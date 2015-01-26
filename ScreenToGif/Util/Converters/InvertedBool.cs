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
    public class InvertedBool: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vis = value as bool?;

            if (!vis.HasValue)
                return DependencyProperty.UnsetValue;

            return !vis.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vis = value as bool?;

            if (!vis.HasValue)
                return DependencyProperty.UnsetValue;

            return !vis.Value;
        }
    }
}
