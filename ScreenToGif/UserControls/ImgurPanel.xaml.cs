using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Cloud;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.ViewModel.UploadPresets.Imgur;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.UserControls;

public partial class ImgurPanel : UserControl, IPanel
{
    private string _originalTitle = "";

    public ImgurPanel()
    {
        InitializeComponent();
    }


    private void ImgurPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _originalTitle = NameTextBox.Text.Trim();

        NameTextBox.Focus();

        UpdateAlbumList(true);
    }
        
    private void TokenHyperlink_RequestNavigate(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            StatusBand.Hide();
            ProcessHelper.StartWithShell(Imgur.GetAuthorizationAdress());
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Creating the link and opening a Imgur related page.");
            StatusBand.Error(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.GetToken"));
        }
    }

    private async void AuthorizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ImgurPreset preset)
            return;

        if (string.IsNullOrWhiteSpace(preset.OAuthToken))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Token"));
            return;
        }

        try
        {
            ThisPanel.IsEnabled = false;
            StatusBand.Hide();

            if (await Imgur.GetTokens(preset))
            {
                preset.OAuthToken = null;
                StatusBand.Info(LocalizationHelper.Get("S.Options.Upload.Preset.Info.Authorized"));
                return;
            }

            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.AuthError"));
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Authorizing access - Imgur");

            StatusBand.Error(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.AuthError"), () => new ExceptionViewer(ex).ShowDialog());
        }
        finally
        {
            ThisPanel.IsEnabled = true;
            UpdateAlbumList();
        }
    }

    private void LoadAlbums_Click(object sender, RoutedEventArgs e)
    {
        UpdateAlbumList();
    }


    private async void UpdateAlbumList(bool offline = false)
    {
        try
        {
            ThisPanel.IsEnabled = false;

            if (DataContext is not ImgurPreset preset)
                return;

            if (!offline && !await Imgur.IsAuthorized(preset))
                return;

            var list = offline && preset.Albums != null ? preset.Albums.Cast<ImgurAlbum>().ToList() : offline ? null : await Imgur.GetAlbums(preset);

            if (list == null)
            {
                list = new List<ImgurAlbum>();

                if (!offline)
                    StatusBand.Error(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.AlbumLoad"));
            }

            if (!offline || list.All(a => a.Id != "♥♦♣♠"))
                list.Insert(0, new ImgurAlbum { Id = "♥♦♣♠", Title = LocalizationHelper.Get("S.Options.Upload.Preset.Imgur.AskMe") });

            AlbumComboBox.ItemsSource = list;

            if (AlbumComboBox.SelectedIndex == -1)
                AlbumComboBox.SelectedIndex = 0;
        }
        finally
        {
            ThisPanel.IsEnabled = true;
        }
    }
        
    public async Task<bool> IsValid()
    {
        if (DataContext is not ImgurPreset preset)
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

        if (!preset.IsAnonymous && !await Imgur.IsAuthorized(preset))
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.Authenticate"));
            return false;
        }

        return true;
    }

    public UploadPreset GetPreset()
    {
        return DataContext as ImgurPreset;
    }
}