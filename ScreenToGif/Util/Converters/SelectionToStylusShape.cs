using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Ink;

namespace ScreenToGif.Util.Converters
{
    public class SelectionToStylusShape : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return new RectangleStylusShape(10, 10, 0);

            var width = values[0] as Int32? ?? values[0] as Double?;
            var height = values[1] as Int32? ?? values[1] as Double?;
            var isRectangle = values[2] as bool?;

            if (!width.HasValue) return new RectangleStylusShape(10, 10, 0);
            if (!height.HasValue || !isRectangle.HasValue) return new RectangleStylusShape(width.Value, 10, 0);

            if (isRectangle.Value)
            {
                return new RectangleStylusShape(width.Value, height.Value, 0);
            }
            
            return new EllipseStylusShape(width.Value, height.Value, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var shape = value as StylusShape;

            if (shape == null) return new object[3] { 10d, 10d, true };

            return new object[3] {shape.Width, shape.Height, shape is RectangleStylusShape};
        }
    }
}
