using System;
using System.Linq;
using System.Windows;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Deals with the selection of the app's theme.
    /// </summary>
    internal static class ThemeHelper
    {
        public static void SelectTheme(string id = "Light")
        {
            var theme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith($"Colors/{id}.xaml"));

            if (theme == null)
            {
                theme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith("Colors/Light.xaml"));
                UserSettings.All.MainTheme = AppTheme.Light;
            }

            Application.Current.Resources.MergedDictionaries.Remove(theme);
            Application.Current.Resources.MergedDictionaries.Add(theme);
        }
    }
}