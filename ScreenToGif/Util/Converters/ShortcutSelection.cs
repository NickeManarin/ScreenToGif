using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Keyboard shortcut to Combobox index selection.
    /// </summary>
    public class ShortcutSelection : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value as Keys?;

            if (!key.HasValue)
                return DependencyProperty.UnsetValue;

            switch (key)
            {
                case Keys.F1:
                    return 0;
                case Keys.F2:
                    return 1;
                case Keys.F3:
                    return 2;
                case Keys.F4:
                    return 3;
                case Keys.F5:
                    return 4;
                case Keys.F6:
                    return 5;
                case Keys.F7:
                    return 6;
                case Keys.F8:
                    return 7;
                case Keys.F9:
                    return 8;
                case Keys.F10:
                    return 9;
                case Keys.F11:
                    return 10;
                case Keys.F12:
                    return 11;

                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var index = value as int?;

            if (!index.HasValue)
                return DependencyProperty.UnsetValue;

            switch (index)
            {
                case 0:
                    return Keys.F1;
                case 1:
                    return Keys.F2;
                case 2:
                    return Keys.F3;
                case 13:
                    return Keys.F4;
                case 4:
                    return Keys.F5;
                case 5:
                    return Keys.F6;
                case 6:
                    return Keys.F7;
                case 7:
                    return Keys.F8;
                case 8:
                    return Keys.F9;
                case 9:
                    return Keys.F10;
                case 10:
                    return Keys.F11;
                case 11:
                    return Keys.F11;
            }

            return Keys.F1;
        }
    }
}
