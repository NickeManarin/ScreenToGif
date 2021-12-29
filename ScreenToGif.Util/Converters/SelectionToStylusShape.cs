using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Ink;

namespace ScreenToGif.Util.Converters;

public class SelectionToStylusShape : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) 
            return new RectangleStylusShape(10, 10, 0);

        var width = values[0] as int? ?? values[0] as double?;
        var height = values[1] as int? ?? values[1] as double?;
        var isRectangle = values[2] as bool?;

        if (!width.HasValue) 
            return new RectangleStylusShape(10, 10, 0);

        if (!height.HasValue || !isRectangle.HasValue) 
            return new RectangleStylusShape(width.Value, 10, 0);

        if (isRectangle.Value)
            return new RectangleStylusShape(width.Value, height.Value, 0);
            
        return new EllipseStylusShape(width.Value, height.Value, 0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (value is not StylusShape shape) 
            return new object[] { 10d, 10d, true };

        return new object[] {shape.Width, shape.Height, shape is RectangleStylusShape};
    }
}