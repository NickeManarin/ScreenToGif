using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.Views.Settings;
using ScreenToGif.Windows.Other;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace ScreenToGif.Windows;

public partial class Options : INotification
{
    internal const int ApplicationIndex = 0;
    internal const int RecorderIndex = 1;
    internal const int EditorIndex = 2;
    internal const int TasksIndex = 3;
    internal const int ShortcutsIndex = 4;
    internal const int LanguageIndex = 5;
    internal const int StorageIndex = 6;
    internal const int UploadIndex = 7;
    internal const int PluginsIndex = 8;
    internal const int DonateIndex = 9;
    internal const int AboutIndex = 10;
    
    public Options()
    {
        InitializeComponent();

#if FULL_MULTI_MSIX_STORE
        UpdatesCheckBox.Visibility = Visibility.Collapsed;
        CheckForUpdatesLabel.Visibility = Visibility.Collapsed;
        StoreTextBlock.Visibility = Visibility.Visible;
        DownloadWithMeteredNetworkCheckBox.Visibility = Visibility.Collapsed;
#elif FULL_MULTI_MSIX
        PortableUpdateCheckBox.Visibility = Visibility.Collapsed;
        AdminUpdateCheckBox.Visibility = Visibility.Collapsed;
#endif
    }

    public Options(int index) : this()
    {
        Navigate(index);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Navigate(SectionsListView.SelectedIndex);

        SizeToContent = SizeToContent.Manual;
    }

    private void SectionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            Navigate(SectionsListView.SelectedIndex);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        #region Validation

        if (UserSettings.All.CursorFollowing && UserSettings.All.FollowShortcut == Key.None)
        {
            Dialog.Ok(LocalizationHelper.Get("S.Options.Title"), LocalizationHelper.Get("S.Options.Warning.Follow.Header"),
                LocalizationHelper.Get("S.Options.Warning.Follow.Message"), Icons.Warning);

            SectionsListView.SelectedIndex = ShortcutsIndex;

            e.Cancel = true;
            return;
        }

        #endregion

        Global.IgnoreHotKeys = false;

        BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure = UserSettings.All.WorkaroundQuota ? BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Reset : BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Continue;
        RenderOptions.ProcessRenderMode = UserSettings.All.DisableHardwareAcceleration ? RenderMode.SoftwareOnly : RenderMode.Default;

        UserSettings.Save();
    }
    
    private void Navigate(int tabIndex)
    {
        switch (tabIndex)
        {
            case ApplicationIndex:
                Frame.Navigate(new ApplicationSettings());
                break;

            case RecorderIndex:
                Frame.Navigate(new RecorderSettings());
                break;

            case EditorIndex:
                Frame.Navigate(new EditorSettings());
                break;

            case TasksIndex:
                Frame.Navigate(new TasksSettings());
                break;

            case ShortcutsIndex:
                Frame.Navigate(new ShortcutsSettings());
                break;

            case LanguageIndex:
                Frame.Navigate(new LanguageSettings());
                break;

            case StorageIndex:
                Frame.Navigate(new StorageSettings());
                break;

            case UploadIndex:
                Frame.Navigate(new UploadSettings());
                break;

            case PluginsIndex:
                Frame.Navigate(new PluginSettings());
                break;

            case DonateIndex:
                Frame.Navigate(new DonateSettings());
                break;

            case AboutIndex:
                Frame.Navigate(new AboutSettings());
                break;

            default:
                Frame.Navigate(null);
                break;
        }
    }

    public void NotificationUpdated()
    {
        //LowSpaceTextBlock.Visibility = Global.AvailableDiskSpace > 2_000_000_000 ? Visibility.Collapsed : Visibility.Visible; //2 GB.
    }

    internal void SelectTab(int index)
    {
        if (index <= -1 || index >= SectionsListView.Items.Count - 1)
            return;

        SectionsListView.SelectedIndex = index;
    }
}