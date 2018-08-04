using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class DownloadDialog : Window
    {
        public XElement Element { get; set; }

        internal UpdateModel Details { get; set; }

        public bool IsChocolatey { get; set; }

        public bool IsInstaller { get; set; }

        public DownloadDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Validation

            if (Global.UpdateModel == null)
            {
                WhatsNewParagraph.Inlines.Add("Something wrong happened.");
                return;
            }

            #endregion

            try
            {
                //Detect if this is portable or installed. Download the proper file.
                IsChocolatey = AppDomain.CurrentDomain.BaseDirectory.EndsWith(@"Chocolatey\lib\screentogif\content\");
                IsInstaller = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory).Any(x => x.ToLowerInvariant().EndsWith("screentogif.visualelementsmanifest.xml"));

                VersionRun.Text = "Version " + Global.UpdateModel.Version;
                SizeRun.Text = Humanizer.BytesToString(IsInstaller ? Global.UpdateModel.InstallerSize : Global.UpdateModel.PortableSize);

                TypeRun.Text = IsInstaller ? LocalizationHelper.Get("Update.Installer") : LocalizationHelper.Get("Update.Portable");

                var splited = Global.UpdateModel.Description.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                WhatsNewParagraph.Inlines.Add(splited[0].Replace(" What's new?\r\n\r\n", ""));
                FixesParagraph.Inlines.Add(splited.Length > 1 ? splited[1].Replace(" Bug fixes:\r\n\r\n", "") : "Aparently nothing.");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Loading download informations");

                WhatsNewParagraph.Inlines.Add("Something wrong happened.");
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            #region Save as

            var save = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "ScreenToGif " + Global.UpdateModel.Version + (IsInstaller ? " Setup" : ""),
                DefaultExt = IsInstaller ? ".msi" : ".exe",
                Filter = IsInstaller ? "ScreenToGif setup|*.msi" : "ScreenToGif executable|*.exe"
            };

            var result = save.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            if (save.FileName == Assembly.GetExecutingAssembly().Location)
            {
                Dialog.Ok(Title, LocalizationHelper.Get("Update.Filename.Warning"), LocalizationHelper.Get("Update.Filename.Warning2"), Icons.Warning);
                return;
            }

            #endregion

            //After downloading, remove the notification and set the global variable to null;

            DownloadButton.IsEnabled = false;
            StatusBand.Info("Downloading...");
            DownloadProgressBar.Visibility = Visibility.Visible;

            var tempFilename = !IsInstaller ? save.FileName.Replace(".exe", DateTime.Now.ToString(" hh-mm-ss fff") + ".zip") : save.FileName;

            #region Download

            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                    webClient.Proxy = WebHelper.GetProxy();

                    await webClient.DownloadFileTaskAsync(new Uri(IsInstaller ? Global.UpdateModel.InstallerDownloadUrl : Global.UpdateModel.PortableDownloadUrl), tempFilename);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Download updates");

                DownloadButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Hidden;
                StatusBand.Hide();

                Dialog.Ok("Update", "Error while downloading", ex.Message);
                return;
            }

            #endregion

            //If cancelled.
            if (!IsLoaded)
            {
                StatusBand.Hide();
                return;
            }

            #region Installer

            if (IsInstaller)
            {
                if (!Dialog.Ask(Title, LocalizationHelper.Get("Update.Install.Header"), LocalizationHelper.Get("Update.Install.Description")))
                    return;

                try
                {
                    Process.Start(tempFilename);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Starting the installer");
                    StatusBand.Hide();

                    Dialog.Ok(Title, "Error while starting the installer", ex.Message);
                    return;
                }

                Global.UpdateModel = null;
                Environment.Exit(25);
            }

            #endregion

            #region Unzip

            try
            {
                //Unzips the only file.
                using (var zipArchive = ZipFile.Open(tempFilename, ZipArchiveMode.Read))
                    zipArchive.Entries.First(x => x.Name.EndsWith(".exe")).ExtractToFile(save.FileName, true);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Unziping update");

                DownloadButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Hidden;
                StatusBand.Hide();

                Dialog.Ok("Update", "Error while unzipping", ex.Message);
                return;
            }

            #endregion

            Global.UpdateModel = null;

            #region Delete temporary zip and run

            try
            {
                File.Delete(tempFilename);

                Process.Start(save.FileName);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Finishing update");

                DownloadButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Hidden;
                StatusBand.Hide();

                Dialog.Ok(Title, "Error while finishing the update", ex.Message);
                return;
            }

            #endregion

            GC.Collect();
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            DialogResult = false;
        }
    }
}