using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Keyboard shortcut to Combobox index selection.
/// </summary>
public class ShortcutSelection : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Key key)
            return DependencyProperty.UnsetValue;

        switch (key)
        {
            case Key.F1:
                return 0;
            case Key.F2:
                return 1;
            case Key.F3:
                return 2;
            case Key.F4:
                return 3;
            case Key.F5:
                return 4;
            case Key.F6:
                return 5;
            case Key.F7:
                return 6;
            case Key.F8:
                return 7;
            case Key.F9:
                return 8;
            case Key.F10:
                return 9;
            case Key.F11:
                return 10;
            case Key.F12:
                return 11;

            default:
                return 0;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int index)
            return DependencyProperty.UnsetValue;

        switch (index)
        {
            case 0:
                return Key.F1;
            case 1:
                return Key.F2;
            case 2:
                return Key.F3;
            case 13:
                return Key.F4;
            case 4:
                return Key.F5;
            case 5:
                return Key.F6;
            case 6:
                return Key.F7;
            case 7:
                return Key.F8;
            case 8:
                return Key.F9;
            case 9:
                return Key.F10;
            case 10:
                return Key.F11;
            case 11:
                return Key.F11;
        }

        return Key.F1;
    }
}