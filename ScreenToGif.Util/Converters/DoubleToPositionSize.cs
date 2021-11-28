using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class DoubleToPositionSize : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var editorWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.Name.Equals("EditorWindow"));

        if (editorWindow == null)
            return DependencyProperty.UnsetValue;

        if (value is not double point || (double?)point == -1)
            return DependencyProperty.UnsetValue;

        //TODO: Test with high dpi.
        var border = Environment.OSVersion.Version.Major == 10 ? 10 : 0;

        switch ((string) parameter)
        {
            case "Left":

                if ((double?)point - border <= SystemParameters.VirtualScreenWidth && (double?)point + border >= SystemParameters.VirtualScreenLeft)
                    return value;

                if ((double?)point - border >= SystemParameters.VirtualScreenWidth)
                    return SystemParameters.VirtualScreenWidth - editorWindow.ActualWidth;

                if ((double?)point + border <= SystemParameters.VirtualScreenLeft)
                    return SystemParameters.VirtualScreenLeft;

                break;

            case "Top":

                if ((double?)point <= SystemParameters.VirtualScreenHeight && (double?)point >= SystemParameters.VirtualScreenTop)
                    return value;

                if ((double?)point >= SystemParameters.VirtualScreenHeight)
                    return SystemParameters.VirtualScreenHeight - editorWindow.ActualHeight;

                if ((double?)point <= SystemParameters.VirtualScreenTop)
                    return SystemParameters.VirtualScreenTop;

                break;

            case "Height":

                if ((double?)point <= SystemParameters.VirtualScreenHeight)
                {
                    //if (editorWindow.Top + point > SystemParameters.VirtualScreenHeight)
                    //    editorWindow.Top = SystemParameters.VirtualScreenHeight - point.Value;

                    return point;
                }

                if ((double?)point >= SystemParameters.VirtualScreenHeight)
                {
                    return SystemParameters.VirtualScreenHeight - editorWindow.ActualHeight;
                }

                break;

            case "Width":

                if ((double?)point <= SystemParameters.VirtualScreenWidth)
                {
                    //if (editorWindow.Left + point - border > SystemParameters.VirtualScreenWidth)
                    //    editorWindow.Left = SystemParameters.VirtualScreenWidth - point.Value;

                    return point;
                }

                if ((double?)point >= SystemParameters.VirtualScreenWidth)
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