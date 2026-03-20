using ScreenToGif.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ScreenToGif.Views.Settings;

public partial class AboutSettings : Page
{
    public AboutSettings()
    {
        InitializeComponent();

#if FULL_MULTI_MSIX_STORE
        CheckForUpdatesLabel.Visibility = Visibility.Collapsed;
        StoreTextBlock.Visibility = Visibility.Visible;
#endif
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(e.Uri.AbsoluteUri);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Open Hyperlink");
        }
    }

    private async void CheckForUpdatesLabel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CheckForUpdatesLabel.IsEnabled = false;

            await App.MainViewModel.CheckForUpdates(true);

            if (Global.UpdateAvailable != null)
            {
                App.MainViewModel.PromptUpdate.Execute(null);
                return;
            }

            StatusBand.Info(LocalizationHelper.Get("S.Options.About.UpdateCheck.Nothing"));
        }
        finally
        {
            CheckForUpdatesLabel.IsEnabled = true;
        }
    }
}