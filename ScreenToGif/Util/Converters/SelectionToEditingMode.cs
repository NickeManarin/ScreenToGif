using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    public class SelectionToEditingMode : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3) return DependencyProperty.UnsetValue;

            var penBool =  values[0] as bool?;
            var eraserBool = values[1] as bool?;
            var selectorBool = values[2] as bool?;

            if (!penBool.HasValue || !eraserBool.HasValue || !selectorBool.HasValue)
                return DependencyProperty.UnsetValue;

            return penBool.Value ? InkCanvasEditingMode.Ink :
                   selectorBool.Value ? InkCanvasEditingMode.Select :
                   eraserBool.Value ? InkCanvasEditingMode.EraseByPoint : 
                   InkCanvasEditingMode.EraseByStroke;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}