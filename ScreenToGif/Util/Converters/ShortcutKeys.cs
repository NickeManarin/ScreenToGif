using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ScreenToGif.Util.Converters
{
    public class ShortcutKeys : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter as string;

            switch (param)
            {
                case "1": //Start/Pause
                    var mod = UserSettings.All.StartPauseModifiers.HasFlag(ModifierKeys.Control) ? "Ctrl" : "";

                    if (UserSettings.All.StartPauseModifiers.HasFlag(ModifierKeys.Shift))
                        mod += ", Shift";
                    if (UserSettings.All.StartPauseModifiers.HasFlag(ModifierKeys.Alt))
                        mod += ", Alt";

                    return mod.TrimStart(',').Trim() + " " + UserSettings.All.StartPauseShortcut;
                case "2": //Stop
                    var mod2 = UserSettings.All.StopModifiers.HasFlag(ModifierKeys.Control) ? "Ctrl" : "";

                    if (UserSettings.All.StopModifiers.HasFlag(ModifierKeys.Shift))
                        mod2 += ", Shift";
                    if (UserSettings.All.StopModifiers.HasFlag(ModifierKeys.Alt))
                        mod2 += ", Alt";

                    return mod2.TrimStart(',').Trim() + " " + UserSettings.All.StopShortcut;
                case "3": //Discard
                    var mod3 = UserSettings.All.DiscardModifiers.HasFlag(ModifierKeys.Control) ? "Ctrl" : "";

                    if (UserSettings.All.DiscardModifiers.HasFlag(ModifierKeys.Shift))
                        mod3 += ", Shift";
                    if (UserSettings.All.DiscardModifiers.HasFlag(ModifierKeys.Alt))
                        mod3 += ", Alt";

                    return mod3.TrimStart(',').Trim() + " " + UserSettings.All.DiscardShortcut;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
