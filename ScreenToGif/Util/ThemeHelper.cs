using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Util;

/// <summary>
/// Deals with the selection of the app's theme.
/// </summary>
internal static class ThemeHelper
{
    public static void SelectTheme(AppThemes theme = AppThemes.Light)
    {
        if (theme == AppThemes.FollowSystem)
            theme = IsSystemUsingDarkTheme() ? AppThemes.Dark : AppThemes.Light;

        //Checks if the theme is already the current in use.
        var last = Application.Current.Resources.MergedDictionaries.LastOrDefault(l => l.Source != null && l.Source.ToString().Contains("Colors/"));

        if (last?.Source.ToString().EndsWith($"/{theme}.xaml") == true)
            return;

        //Tries to switch to the new theme.
        var res = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith($"Colors/{theme}.xaml"));

        if (res == null)
        {
            res = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith("Colors/Light.xaml"));
            UserSettings.All.MainTheme = AppThemes.Light;
        }

        Application.Current.Resources.MergedDictionaries.Remove(res);
        Application.Current.Resources.MergedDictionaries.Add(res);

        //Forces the refresh of the vectors with dynamic resources inside.
        var glyphs = Application.Current.Resources.MergedDictionaries.FirstOrDefault(f => f.Source != null && f.Source.ToString().EndsWith("Resources/Glyphs.xaml"));

        Application.Current.Resources.MergedDictionaries.Remove(glyphs);
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new System.Uri("Resources/Glyphs.xaml", System.UriKind.RelativeOrAbsolute) });

        RefreshNotificationIcon();
    }

    internal static bool IsSystemUsingDarkTheme()
    {
        try
        {
            using (var sub = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                       .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (sub?.GetValue("AppsUseLightTheme") is int key)
                    return key == 0;
            }
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Not possible to get system's theme setting.");
        }

        return false;
    }

    private static void RefreshNotificationIcon()
    {
        if (App.NotifyIcon == null)
            return;

        App.NotifyIcon.RefreshVisual();
    }
}