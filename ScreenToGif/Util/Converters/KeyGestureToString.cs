using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ScreenToGif.Util.Converters;

public class KeyGestureToString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not KeyGesture gesture || gesture.Key == Key.None)
            return Binding.DoNothing;

        return $"{Native.Helpers.Other.GetSelectKeyText(gesture.Key, gesture.Modifiers)}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}