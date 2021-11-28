using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Cloud;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.ViewModel.UploadPresets.Yandex;

namespace ScreenToGif.UserControls;

public partial class YandexPanel : UserControl, IPanel
{
    private string _originalTitle = "";

    public YandexPanel()
    {
        InitializeComponent();
    }

    private void YandexPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _originalTitle = NameTextBox.Text.Trim();

        NameTextBox.Focus();
    }
        
    private void TokenHyperlink_RequestNavigate(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusBand.Hide();
            ProcessHelper.StartWithShell(YandexDisk.GetAuthorizationAdress());
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Creating the link and opening a Yandex Disk related page.");
            StatusBand.Error(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.GetToken"));
        }
    }


    public Task<bool> IsValid()
    {
        if (DataContext is not YandexPreset preset)
            return Task.FromResult(false);

        if (string.IsNullOrWhiteSpace(preset.Title))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Title"));
            NameTextBox.Focus();
            return Task.FromResult(false);
        }

        if (UserSettings.All.UploadPresets.OfType<UploadPreset>().Any(a => a.Title != _originalTitle && a.Title == preset.Title.Trim()))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Repeated"));
            NameTextBox.Focus();
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(preset.OAuthToken))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Token"));
            TokenTextBox.Focus();
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public UploadPreset GetPreset()
    {
        return DataContext as YandexPreset;
    }
}