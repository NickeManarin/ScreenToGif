using System;
using System.Globalization;
using System.Windows.Data;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Util.Converters;

public class ShortcutKeys : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = parameter as string;

        switch (param)
        {
            case "1": //Start/Pause
                return Native.Helpers.Other.GetSelectKeyText(UserSettings.All.StartPauseShortcut, UserSettings.All.StartPauseModifiers, true, true);
            case "2": //Stop
                return Native.Helpers.Other.GetSelectKeyText(UserSettings.All.StopShortcut, UserSettings.All.StopModifiers, true, true);
            case "3": //Discard
                return Native.Helpers.Other.GetSelectKeyText(UserSettings.All.DiscardShortcut, UserSettings.All.DiscardModifiers, true, true);
        }

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}