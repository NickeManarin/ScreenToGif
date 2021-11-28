using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Binding = System.Windows.Data.Binding;

namespace ScreenToGif.Util.Converters;

public class SelectionToDrawingAttributes : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 6)
            return DependencyProperty.UnsetValue;

        var colorBrush = values[2] as SolidColorBrush; //First try as Brush, else Color.
        var color = values[2] as Color?;

        if (values[0] is not int height || values[1] is not int width || values[3] is not bool fitToCurve || values[4] is not bool isHighlighter ||
            values[5] is not bool isRectangle || (colorBrush == null && color == null))
            return DependencyProperty.UnsetValue;

        return new DrawingAttributes
        {
            Height = height,
            Width = width,
            Color = colorBrush?.Color ?? color.Value,
            FitToCurve = fitToCurve,
            IsHighlighter = isHighlighter,
            StylusTip = isRectangle ? StylusTip.Rectangle : StylusTip.Ellipse
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new[] { Binding.DoNothing, Binding.DoNothing, Binding.DoNothing, Binding.DoNothing, Binding.DoNothing, Binding.DoNothing, Binding.DoNothing };

        //var tip = value as StylusTip?;

        //if (!tip.HasValue) return new object[2] {true, false};

        //var isRectangle = tip.Value == StylusTip.Rectangle;

        //return new object[2] {isRectangle, !isRectangle};
    }
}