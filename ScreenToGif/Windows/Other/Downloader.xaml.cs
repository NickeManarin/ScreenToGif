using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other;

public partial class Downloader : Window
{
    #region Properties

    private bool _cancel;

    public string DownloadMode { get; set; }

    public string DestinationPath { get; set; }

    #endregion

    public Downloader()
    {
        InitializeComponent();
    }


    private string GetDownloadUrl()
    {
        switch (DownloadMode)
        {
            case "gifski":
                return "https://www.screentogif.com/downloads/Gifski.zip";
            case "ffmpeg":
            {
                return Environment.Is64BitProcess ? "https://www.screentogif.com/downloads/FFmpeg-4.4.1-x64.zip" :
                    "https://www.screentogif.com/downloads/FFmpeg-4.3.1-x86.zip";
            }
        }

        return null;
    }

    private async Task Download()
    {
        try
        {
            //Save to a temp folder.
            var temp = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            var downloadUrl = GetDownloadUrl();

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                StatusBand.Error("Download URL not provided...");
                return;
            }

            //Download.
            using (var client = new WebClient { Proxy = WebRequest.GetSystemWebProxy() })
            {
                client.DownloadProgressChanged += (_, args) =>
                {
                    TotalTextBlock.Text = LocalizationHelper.GetWithFormat("S.Downloader.Size", "{0} of {1}", Humanizer.BytesToString(args.BytesReceived), Humanizer.BytesToString(args.TotalBytesToReceive));
                    MainProgressBar.Value = args.ProgressPercentage;

                    if (_cancel)
                    {
                        client.CancelAsync();
                        Environment.Exit(35);
                    }
                };

                await client.DownloadFileTaskAsync(new Uri(downloadUrl), temp);
            }

            if (_cancel)
            {
                Environment.Exit(90);
                return;
            }

            //Decompress.
            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Read))
            {
                switch (DownloadMode)
                {
                    case "gifski":
                    {
                        var entry = zip.Entries.FirstOrDefault(x => x.Name.Contains("gifski.dll"));

                        if (File.Exists(DestinationPath))
                            File.Delete(DestinationPath);

                        entry?.ExtractToFile(DestinationPath, true);
                        break;
                    }
                    case "ffmpeg":
                    {
                        var entry = zip.Entries.FirstOrDefault(x => x.Name.Contains("ffmpeg.exe"));

                        if (File.Exists(DestinationPath))
                            File.Delete(DestinationPath);

                        entry?.ExtractToFile(DestinationPath, true);
                        break;
                    }
                    case "sharpdx":
                    {
                        foreach (var entry in zip.Entries)
                        {
                            if (File.Exists(Path.Combine(DestinationPath, entry.Name)))
                                File.Delete(Path.Combine(DestinationPath, entry.Name));

                            entry?.ExtractToFile(Path.Combine(DestinationPath, entry.Name), true);
                        }

                        break;
                    }
                }
            }

            File.Delete(temp);
            Environment.Exit(10);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to download");

            StatusBand.Error(e.Message);
            RetryButton.IsEnabled = true;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await Download();
    }
        
    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RetryButton.IsEnabled || !IsLoaded)
            return;

        RetryButton.IsEnabled = false;

        await Download();

        CancelButton.IsEnabled = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CancelButton.IsEnabled || !IsLoaded)
            return;

        CancelButton.IsEnabled = false;
        _cancel = true;
    }
}