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
using ScreenToGif.ViewModel.UploadPresets.Gfycat;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.UserControls;

public partial class GfycatPanel : UserControl, IPanel
{
    private string _originalTitle = "";

    public GfycatPanel()
    {
        InitializeComponent();
    }


    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
        _originalTitle = NameTextBox.Text.Trim();

        NameTextBox.Focus();
    }

    private async void AuthorizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GfycatPreset preset)
            return;

        if (string.IsNullOrWhiteSpace(UserTextBox.Text) || PasswordTextBox.SecurePassword.Length < 1)
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Credentials"));
            return;
        }

        try
        {
            ThisPanel.IsEnabled = false;
            StatusBand.Hide();

            //When in authenticated mode, the user must authorize the app by using the username and password.
            if (await Gfycat.GetTokens(preset, UserTextBox.Text, PasswordTextBox.Password))
            {
                StatusBand.Info(LocalizationHelper.Get("S.Options.Upload.Preset.Info.Authorized"));
                UserTextBox.Clear();
                PasswordTextBox.Clear();
                return;
            }

            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.AuthError"));
            UserTextBox.Focus();
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Authorizing access - Gfycat");

            StatusBand.Error(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.AuthError"), () => new ExceptionViewer(ex).ShowDialog());
            UserTextBox.Focus();
        }
        finally
        {
            ThisPanel.IsEnabled = true;
        }
    }


    public async Task<bool> IsValid()
    {
        if (DataContext is not GfycatPreset preset)
            return false;

        if (string.IsNullOrWhiteSpace(preset.Title))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Title"));
            return false;
        }

        if (UserSettings.All.UploadPresets.OfType<UploadPreset>().Any(a => a.Title != _originalTitle && a.Title == preset.Title.Trim()))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Repeated"));
            return false;
        }

        if (!preset.IsAnonymous && !await Gfycat.IsAuthorized(preset))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Authenticate"));
            return false;
        }

        return true;
    }

    public UploadPreset GetPreset()
    {
        return DataContext as GfycatPreset;
    }
}