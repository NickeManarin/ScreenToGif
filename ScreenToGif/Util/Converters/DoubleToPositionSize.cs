using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using ScreenToGif.Windows;

namespace ScreenToGif.Util.Converters
{
    class DoubleToPositionSize : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var editorWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.Name.Equals("EditorWindow"));

            if (editorWindow == null)
                return DependencyProperty.UnsetValue;

            var point = value as double?;

            if (!point.HasValue || point == -1)
                return DependencyProperty.UnsetValue;

            //TODO: Test with high dpi.
            int border = Environment.OSVersion.Version.Major == 10 ? 10 : 0;

            switch ((string) parameter)
            {
                case "Left":

                    if (point - border <= SystemParameters.VirtualScreenWidth && point + border >= SystemParameters.VirtualScreenLeft)
                        return value;

                    if (point - border >= SystemParameters.VirtualScreenWidth)
                        return SystemParameters.VirtualScreenWidth - editorWindow.ActualWidth;

                    if (point + border <= SystemParameters.VirtualScreenLeft)
                        return SystemParameters.VirtualScreenLeft;

                    break;

                case "Top":

                    if (point <= SystemParameters.VirtualScreenHeight && point >= SystemParameters.VirtualScreenTop)
                        return value;

                    if (point >= SystemParameters.VirtualScreenHeight)
                        return SystemParameters.VirtualScreenHeight - editorWindow.ActualHeight;

                    if (point <= SystemParameters.VirtualScreenTop)
                        return SystemParameters.VirtualScreenTop;

                    break;

                case "Height":

                    if (point <= SystemParameters.VirtualScreenHeight)
                    {
                        //if (editorWindow.Top + point > SystemParameters.VirtualScreenHeight)
                        //    editorWindow.Top = SystemParameters.VirtualScreenHeight - point.Value;

                        return point;
                    }

                    if (point >= SystemParameters.VirtualScreenHeight)
                    {
                        return SystemParameters.VirtualScreenHeight - editorWindow.ActualHeight;
                    }

                    break;

                case "Width":

                    if (point <= SystemParameters.VirtualScreenWidth)
                    {
                        //if (editorWindow.Left + point - border > SystemParameters.VirtualScreenWidth)
                        //    editorWindow.Left = SystemParameters.VirtualScreenWidth - point.Value;

                        return point;
                    }

                    if (point >= SystemParameters.VirtualScreenWidth)
                    {
                        return SystemParameters.VirtualScreenWidth - editorWindow.ActualWidth;
                    }

                    break;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
