using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Xml.Linq;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class DownloadDialog : Window
    {
        #region Properties

        public XElement Element { get; set; }

        internal UpdateAvailable Details { get; set; }

        public bool IsChocolatey { get; set; }

        public bool IsInstaller { get; set; }

        public bool WasPromptedManually { get; set; }

        #endregion


        public DownloadDialog()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Validation

            if (Global.UpdateAvailable == null)
            {
                WhatsNewParagraph.Inlines.Add("Something wrong happened. No update was found.");
                return;
            }

            #endregion

            try
            {
                //Detect if this is portable or installed. Download the proper file.
                IsChocolatey = AppDomain.CurrentDomain.BaseDirectory.EndsWith(@"Chocolatey\lib\screentogif\content\");
                IsInstaller = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory).Any(x => x.ToLowerInvariant().EndsWith("screentogif.visualelementsmanifest.xml"));

                VersionRun.Text = $"{LocalizationHelper.Get("S.Updater.Version")} {Global.UpdateAvailable.Version}";
                SizeRun.Text = Global.UpdateAvailable.InstallerSize > 0 ? Humanizer.BytesToString(IsInstaller ? Global.UpdateAvailable.InstallerSize : Global.UpdateAvailable.PortableSize) : "";
                TypeRun.Text = IsInstaller ? LocalizationHelper.Get("S.Updater.Installer") : LocalizationHelper.Get("S.Updater.Portable");

                //If set to download automatically, check if the installer was downloaded.
                if (UserSettings.All.InstallUpdates)
                {
                    //If the update was already downloaded.
                    if (File.Exists(Global.UpdateAvailable.InstallerPath))
                    {
                        DownloadButton.SetResourceReference(ImageButton.TextProperty, "S.Updater.Install");

                        //When the update was prompted manually, the user can set the installer to run the app afterwards.
                        if (WasPromptedManually)
                        {
                            RunAfterwardsCheckBox.Visibility = Visibility.Visible;
                            RunAfterwardsCheckBox.IsChecked = true;
                        }
                    }
                }

                //Details.
                if (Global.UpdateAvailable.IsFromGithub)
                {
                    //From Github, the description is available.
                    var splited = Global.UpdateAvailable.Description.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                    WhatsNewParagraph.Inlines.Add(splited[0].Replace(" What's new?\r\n\r\n", ""));
                    FixesParagraph.Inlines.Add(splited.Length > 1 ? splited[1].Replace(" Bug fixes:\r\n\r\n", "").Replace(" Fixed:\r\n\r\n", "") : "Aparently nothing.");
                }
                else
                {
                    //If the release detail was obtained by querying Fosshub, no release note is available. 
                    MainFlowDocument.Blocks.Remove(WhatsNewParagraphTitle);
                    MainFlowDocument.Blocks.Remove(FixesParagraphTitle);
                    MainFlowDocument.Blocks.Remove(FixesParagraph);

                    var run = new Run();
                    run.SetResourceReference(Run.TextProperty, "S.Updater.Info.NewVersionAvailable");
                    WhatsNewParagraph.Inlines.Add(run);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to load the download details");
                StatusBand.Error(LocalizationHelper.Get("S.Updater.Warning.Show"));
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            //If update already downloaded, simply close this window. The installation will happen afterwards.
            if (File.Exists(Global.UpdateAvailable.InstallerPath))
            {
                GC.Collect();
                DialogResult = true;
                return;
            }

            //When the update was not queried from Github, the dowload must be done by browser.
            if (!Global.UpdateAvailable.IsFromGithub)
            {
                try
                {
                    Process.Start(Global.UpdateAvailable.InstallerDownloadUrl);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Impossible to open the browser to download the update.", Global.UpdateAvailable?.InstallerDownloadUrl);
                }

                GC.Collect();
                DialogResult = true;
                return;
            }

            DownloadButton.IsEnabled = false;
            StatusBand.Info(LocalizationHelper.Get("S.Updater.Downloading"));
            DownloadProgressBar.Visibility = Visibility.Visible;
            RunAfterwardsCheckBox.Visibility = Visibility.Collapsed;

            var result = await Task.Run(async () => await App.MainViewModel.DownloadUpdate());

            //If cancelled.
            if (!IsLoaded)
                return;
                
            if (!result)
            {
                DownloadButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Hidden;
                StatusBand.Error(LocalizationHelper.Get("S.Updater.Warning.Download"));
                return;
            }

            //If the update was downloaded successfully, close this window to run.
            if (File.Exists(Global.UpdateAvailable.InstallerPath))
            {
                GC.Collect();
                StatusBand.Hide();
                DialogResult = true;
                return;
            }

            StatusBand.Error(LocalizationHelper.Get("S.Updater.Warning.Download"));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            DialogResult = false;
        }
    }
}