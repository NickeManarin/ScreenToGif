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
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class DownloadDialog : Window
    {
        public XElement Element { get; set; }

        public bool IsChocolatey { get; set; }

        public bool IsInstaller { get; set; }

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
                //Detect if this is portable or installed. Download the proper file.
                IsChocolatey = AppDomain.CurrentDomain.BaseDirectory.EndsWith(@"Chocolatey\lib\screentogif\content\");
                IsInstaller = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory).Any(x => x.EndsWith("ScreenToGif.visualelementsmanifest.xml"));

                VersionRun.Text = "Version " + Element.XPathSelectElement("tag_name").Value;
                SizeRun.Text = Humanizer.BytesToString(Convert.ToInt32((IsInstaller ? Element.XPathSelectElement("assets").LastNode : 
                    Element.XPathSelectElement("assets").FirstNode).XPathSelectElement("size").Value));

                TypeRun.Text = IsInstaller ? this.TextResource("Update.Installer") : this.TextResource("Update.Portable");

                var body = Element.XPathSelectElement("body").Value;

                var splited = body.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                WhatsNewParagraph.Inlines.Add(splited[0].Replace(" What's new?\r\n\r\n", ""));
                FixesParagraph.Inlines.Add(splited.Length > 1 ? splited[1].Replace(" Bug fixes:\r\n\r\n", "") : "Aparently, nothing.");
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
                FileName = "ScreenToGif" + (IsInstaller ? " Setup " + Element.XPathSelectElement("tag_name").Value : ""),
                DefaultExt = IsInstaller ? ".msi" : ".exe",
                Filter = IsInstaller ? "ScreenToGif setup (.msi)|*.msi" : "ScreenToGif executable (.exe)|*.exe"
            };

            var result = save.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            if (save.FileName == Assembly.GetExecutingAssembly().Location)
            {
                Dialog.Ok(Title, this.TextResource("Update.Filename.Warning"), this.TextResource("Update.Filename.Warning2"), Icons.Warning);
                return;
            }
            
            #endregion

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

                    await webClient.DownloadFileTaskAsync(new Uri((IsInstaller ? Element.XPathSelectElement("assets").LastNode : 
                        Element.XPathSelectElement("assets").FirstNode).XPathSelectElement("browser_download_url").Value), tempFilename);
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

            #region Installer

            if (IsInstaller)
            {
                if (!Dialog.Ask(Title, this.TextResource("Update.Install.Header"), this.TextResource("Update.Install.Description")))
                    return;

                try
                {
                    Process.Start(tempFilename);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Starting the installer");
                    Dialog.Ok(Title, "Error while starting the installer", ex.Message);
                    return;
                }

                Environment.Exit(25);
            }

            #endregion

            #region Unzip

            try
            {
                //Deletes if already exists.
                if (File.Exists(save.FileName))
                    File.Delete(save.FileName);

                //Unzips the only file.
                using (var zipArchive = ZipFile.Open(tempFilename, ZipArchiveMode.Read))
                    zipArchive.Entries.First(x => x.Name.EndsWith(".exe")).ExtractToFile(save.FileName);
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