using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using System.Xml.XPath;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class DownloadDialog : Window
    {
        public XElement Element { get; set; }

        public DownloadDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Validation

            if (Element == null)
            {
                WhatsNewParagraph.Inlines.Add("Something wrong happened.");
                return;
            }

            #endregion

            try
            {
                VersionRun.Text = "Version " + Element.XPathSelectElement("tag_name").Value;
                SizeRun.Text = Humanizer.BytesToString(Convert.ToInt32(Element.XPathSelectElement("assets").FirstNode.XPathSelectElement("size").Value));

                var body = Element.XPathSelectElement("body").Value;

                var splited = body.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                WhatsNewParagraph.Inlines.Add(splited[0].Replace(" What's new?\r\n\r\n", ""));

                FixesParagraph.Inlines.Add(splited[1].Replace(" Bug fixes:\r\n\r\n", ""));
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
                FileName = "ScreenToGif", // + Element.XPathSelectElement("tag_name").Value,
                DefaultExt = ".exe",
                Filter = "ScreenToGif executable (.exe)|*.exe"
            };

            var result = save.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            //TODO: Localize.
            if (save.FileName == Assembly.GetExecutingAssembly().Location)
            {
                Dialog.Ok("Download", "You need to pick another location or file name",
                    "You cannot overwrite the executable with a new version right now, please selected another name for the file.", Dialog.Icons.Warning);
                return;
            }
            
            #endregion

            DownloadButton.IsEnabled = false;
            StatusBand.Info("Downloading...");
            DownloadProgressBar.Visibility = Visibility.Visible;

            var tempFilename = save.FileName.Replace(".exe", DateTime.Now.ToString(" hh-mm-ss fff") + ".zip");

            #region Download

            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                    await webClient.DownloadFileTaskAsync(new Uri(Element.XPathSelectElement("assets").FirstNode.XPathSelectElement("browser_download_url").Value), tempFilename);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Download updates");

                DownloadButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Hidden;

                Dialog.Ok("Update", "Error while downloading", ex.Message);
                return;
            }

            #endregion

            //If cancelled.
            if (!IsLoaded)
                return;

            #region Unzip

            try
            {
                //Deletes if already exists.
                if (File.Exists(save.FileName))
                    File.Delete(save.FileName);

                //Unzips the only file.
                using (var zipArchive = ZipFile.Open(tempFilename, ZipArchiveMode.Read))
                {
                    zipArchive.Entries.First(x => x.Name.EndsWith(".exe")).ExtractToFile(save.FileName);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Unziping update");

                DownloadButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Hidden;

                Dialog.Ok("Update", "Error while unzipping", ex.Message);
                return;
            }

            #endregion

            //If cancelled.
            if (!IsLoaded)
                return;

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

                Dialog.Ok("Update", "Error while finishing the update", ex.Message);
                return;
            }

            #endregion

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
