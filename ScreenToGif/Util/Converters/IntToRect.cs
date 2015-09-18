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
    class IntToRect : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return DependencyProperty.UnsetValue;

            var width = values[0] as Double? ?? values[0] as Int32? ?? 0;
            var height = values[1] as Double? ?? values[1] as Int32? ?? 0;

            if (values.Length < 4)
            {
                return new Rect(new Point(0, 0), new Point(width, height));
            }

            var xAxis = values[2] as Double? ?? values[2] as Int32? ?? 0;
            var yAxis = values[3] as Double? ?? values[3] as Int32? ?? 0;

            return new Rect(new Point(xAxis, yAxis), new Point(width, height));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
