using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows.Other;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Localization = ScreenToGif.Windows.Other.Localization;

namespace ScreenToGif.Views.Settings;
/// <summary>
/// Interaction logic for LanguageSettings.xaml
/// </summary>
public partial class LanguageSettings : Page
{
    public LanguageSettings()
    {
        InitializeComponent();
    }

    private void LanguagePanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //To avoid being called during startup of the window and to avoid being called twice after selection changes.
        if (!IsLoaded || e.AddedItems.Count == 0)
            return;

        try
        {
            LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);

            ForceUpdateSystemTray();
        }
        catch (Exception ex)
        {
            ErrorDialog.Ok(LocalizationHelper.Get("S.Options.Title"), "Error while stopping", ex.Message, ex);
            LogWriter.Log(ex, "Error while trying to set the language.");
        }
    }

    private void TranslateHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://github.com/NickeManarin/ScreenToGif/wiki/Localization");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Open the latest resource available");
        }
    }

    private void TranslateOfflineHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            AddExtension = true,
            Filter = "Resource Dictionary (*.xaml)|*.xaml",
            Title = "Save Resource Dictionary",
            FileName = "StringResources.en"
        };

        var result = sfd.ShowDialog();

        if (result.HasValue && result.Value)
        {
            try
            {
                LocalizationHelper.SaveDefaultResource(sfd.FileName);
            }
            catch (Exception ex)
            {
                Dialog.Ok("Impossible to Save", "Impossible to save the Xaml file", ex.Message, Icons.Warning);
            }
        }
    }

    private void ImportHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        var local = new Localization();
        local.ShowDialog();
    }

    private void EmailHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("mailto:nicke@outlook.com.br");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Open MailTo");
        }
    }

    private void ForceUpdateSystemTray()
    {
        if (App.NotifyIcon == null || App.NotifyIcon.ContextMenu == null)
            return;

        var items = App.NotifyIcon.ContextMenu.Items.OfType<ExtendedMenuItem>();

        foreach (var item in items)
            item.Header = LocalizationHelper.Get((string)item.Tag);
    }
}