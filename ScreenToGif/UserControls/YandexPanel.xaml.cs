using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Cloud.YandexDisk;
using ScreenToGif.Interfaces;
using ScreenToGif.Model.UploadPresets;
using ScreenToGif.Model.UploadPresets.Yandex;
using ScreenToGif.Settings;
using ScreenToGif.Util;

namespace ScreenToGif.UserControls
{
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
                Process.Start(YandexDisk.GetAuthorizationAdress());
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Creating the link and opening a Yandex Disk related page.");
                StatusBand.Error(LocalizationHelper.Get("S.Options.Upload.Preset.Warning.GetToken"));
            }
        }


        public Task<bool> IsValid()
        {
            if (!(DataContext is YandexPreset preset))
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
}