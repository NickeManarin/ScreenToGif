using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;

namespace ScreenToGif.Util.Converters
{
    public class SelectionToDrawingAttributes : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 6) return DependencyProperty.UnsetValue;

            var height = values[0] as int?;
            var width = values[1] as int?;
            var colorBrush = values[2] as SolidColorBrush; //First try as Brush, else Color.
            var color = values[2] as Color?;

            var fitToCurve = values[3] as bool?;
            var isHighlighter = values[4] as bool?;
            var isRectangle = values[5] as bool?;

            if (!height.HasValue || !width.HasValue || !fitToCurve.HasValue || !isHighlighter.HasValue ||
                !isRectangle.HasValue || (colorBrush == null && color == null))
                return DependencyProperty.UnsetValue;

            return new DrawingAttributes
            {
                Height = height.Value,
                Width = width.Value,
                Color = colorBrush?.Color ?? color.Value,
                FitToCurve = fitToCurve.Value,
                IsHighlighter = isHighlighter.Value,
                StylusTip = isRectangle.Value ? StylusTip.Rectangle : StylusTip.Ellipse
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
}
