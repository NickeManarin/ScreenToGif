using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
          
            var height = values[0] as Int32?;
            var width = values[1] as Int32?;
            var color = values[2] as SolidColorBrush;

            var fitToCurve = values[3] as bool?;
            var isHighlighter = values[4] as bool?;
            var isRectangle = values[5] as bool?;

            return new DrawingAttributes()
            {
                Height = height.Value,
                Width = width.Value,
                Color = color != null ? color.Color : (Color)values[2],
                FitToCurve = fitToCurve.Value,
                IsHighlighter = isHighlighter.Value,
                StylusTip = isRectangle.Value ? StylusTip.Rectangle : StylusTip.Ellipse
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var tip = value as StylusTip?;

            if (!tip.HasValue) return new object[2] {true, false};

            var isRectangle = tip.Value == StylusTip.Rectangle;

            return new object[2] {isRectangle, !isRectangle};
        }
    }
}
