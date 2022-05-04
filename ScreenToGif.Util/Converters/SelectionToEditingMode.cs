using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class SelectionToEditingMode : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 3) return DependencyProperty.UnsetValue;

        if (values[0] is not bool penBool || values[1] is not bool eraserBool || values[2] is not bool selectorBool)
            return DependencyProperty.UnsetValue;

        return penBool ? InkCanvasEditingMode.Ink :
            selectorBool ? InkCanvasEditingMode.Select :
            eraserBool ? InkCanvasEditingMode.EraseByPoint : 
            InkCanvasEditingMode.EraseByStroke;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}