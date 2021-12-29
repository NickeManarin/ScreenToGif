using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ScreenToGif.Util.Converters;

public class CommandToKeyGesture : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var command = value as RoutedUICommand;

        if (command?.InputGestures == null)
            return Binding.DoNothing;

        //var keys = Native.GetSelectKeyText(gesture.Key, gesture.Modifiers);

        foreach (KeyGesture gesture in command.InputGestures)
            if (gesture.Key != Key.None)
                return $"{Native.Helpers.Other.GetSelectKeyText(gesture.Key, gesture.Modifiers)}";

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}