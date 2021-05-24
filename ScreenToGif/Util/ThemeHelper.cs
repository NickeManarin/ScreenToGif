using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ScreenToGif.Settings;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Deals with the selection of the app's theme.
    /// </summary>
    internal static class ThemeHelper
    {
        private static bool SystemUsesDarkTheme()
        {
            using (var sub = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (sub?.GetValue("AppsUseLightTheme") is int key)
                {
                    return key == 0;
                }
            }
            return false;
        }
        public static void SelectTheme(AppTheme theme = AppTheme.Light)
        {
            if (theme == AppTheme.FollowSystem)
            {
                theme = SystemUsesDarkTheme() ? AppTheme.Dark : AppTheme.Light;
            }

            //Checks if the theme is already the current in use.
            var last = Application.Current.Resources.MergedDictionaries.LastOrDefault(l => l.Source != null && l.Source.ToString().Contains("Colors/"));

            if (last?.Source.ToString().EndsWith($"/{theme}.xaml") == true)
                return;

            //Tries to switch to the new theme.
            var res = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith($"Colors/{theme}.xaml"));

            if (res == null)
            {
                res = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith("Colors/Light.xaml"));
                UserSettings.All.MainTheme = AppTheme.Light;
            }

            Application.Current.Resources.MergedDictionaries.Remove(res);
            Application.Current.Resources.MergedDictionaries.Add(res);

            //Forces the refresh of the vectors with dynamic resources inside.
            var glyphs = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith("Resources/Glyphs.xaml"));

            Application.Current.Resources.MergedDictionaries.Remove(glyphs);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new System.Uri("Resources/Glyphs.xaml", System.UriKind.RelativeOrAbsolute) });

            RefreshNotificationIcon();
        }

        private static void RefreshNotificationIcon()
        {
            if (App.NotifyIcon == null)
                return;

            App.NotifyIcon.RefreshVisual();
        }
    }
}