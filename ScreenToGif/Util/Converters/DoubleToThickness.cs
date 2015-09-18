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
    public class DoubleToThickness : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return new Thickness(0);

            var left = values[0] as double? ?? values[0] as Int32? ?? 0;
            var top = values[1] as double? ?? values[1] as Int32? ?? 0;

            if (values.Length < 4)
                return new Thickness(left, top, left, top);

            var right = values[2] as double? ?? values[2] as Int32? ?? 0;
            var bottom = values[3] as double? ?? values[3] as Int32? ?? 0;

            return new Thickness(left, top, right, bottom);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
