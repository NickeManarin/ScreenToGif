using Microsoft.Win32;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.InterProcessChannel;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows.Other;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Views.Settings;

public partial class ApplicationSettings : Page
{
    /// <summary>
    /// Flag used to avoid multiple calls on the startup mode change.
    /// </summary>
    private bool _ignoreStartup;

    public ApplicationSettings()
    {
        InitializeComponent();
    }

    private void ApplicationSettings_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StartupModeGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;
            _ignoreStartup = true;

            //Detect if this app is set to start with windows.
            var sub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            var key = sub?.GetValue("ScreenToGif");
            var name = ProcessHelper.GetEntryAssemblyPath();

            if (key == null || key as string != name)
            {
                //If the key does not exist or its content does not point to the same executable, it means that this app will not run when the user logins.
                StartManuallyCheckBox.IsChecked = true;
            }
            else
            {
                //If the key exists and its content point to the same executable, it means that this app will run when the user logins.
                StartAutomaticallyCheckBox.IsChecked = true;
            }

            //Detect other version of this app?

            StartupModeGrid.IsEnabled = true;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to detect if the app is starting when the user logins");
            StartupModeGrid.IsEnabled = false;
        }
        finally
        {
            _ignoreStartup = false;
            Cursor = Cursors.Arrow;
        }
    }

    private void Instance_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        //With this inter process server, this instance can listen to arguments sent by other instances.
        if (UserSettings.All.SingleInstance)
            InstanceSwitcherChannel.RegisterServer(App.InstanceSwitch_Received);
        else
            InstanceSwitcherChannel.UnregisterServer();
    }

    private void AppThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        try
        {
            if (AppThemeComboBox.SelectedValue is not AppThemes selected)
                throw new Exception("No theme was selected.");

            ThemeHelper.SelectTheme(selected);

            App.NotifyIcon?.RefreshVisual();
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while selecting the app's theme.");
            ExceptionDialog.Ok(ex, Title, "Error while selecting the app's theme", ex.Message);
        }
    }

    private void StartAutomaticallyCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_ignoreStartup)
                return;

            Cursor = Cursors.AppStarting;
            _ignoreStartup = true;

            var sub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var name = ProcessHelper.GetEntryAssemblyPath();

            if (string.IsNullOrWhiteSpace(name) || sub == null)
            {
                StatusBand.Error(LocalizationHelper.Get("S.Options.App.Startup.Mode.Warning"));
                throw new Exception("Impossible to set the app to run on startup. " + name + (sub == null ? ", null" : ""));
            }

            //Add the value in the registry so that the application runs at startup.
            sub.SetValue("ScreenToGif", name);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to set the app to run on startup.");
        }
        finally
        {
            _ignoreStartup = false;
            Cursor = Cursors.Arrow;
        }
    }

    private void StartAutomaticallyCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_ignoreStartup)
                return;

            Cursor = Cursors.AppStarting;
            _ignoreStartup = true;

            var sub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var name = ProcessHelper.GetEntryAssemblyPath();

            if (string.IsNullOrWhiteSpace(name) || sub == null)
            {
                StatusBand.Error(LocalizationHelper.Get("S.Options.App.Startup.Mode.Warning"));
                throw new Exception("Impossible to set the app to not run on startup. " + name + (sub == null ? ", null" : ""));
            }

            //Remove the value from the registry so that the application doesn't start automatically.
            sub.DeleteValue("ScreenToGif", false);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to set the app to not run on startup.");
        }
        finally
        {
            _ignoreStartup = false;
            Cursor = Cursors.Arrow;
        }
    }

    private void StartCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        UserSettings.All.ShowNotificationIcon = true;
    }

    private void NotificationIconCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        //Can't have a minimized startup, if the icon is not present on the notification area.
        if (!UserSettings.All.ShowNotificationIcon)
            UserSettings.All.StartMinimized = false;

        if (App.NotifyIcon != null)
            App.NotifyIcon.Visibility = UserSettings.All.ShowNotificationIcon ? Visibility.Visible : Visibility.Collapsed;
    }
}