using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util;
using ScreenToGif.Util.Helpers;
using ScreenToGif.Util.Settings;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Compressors.Xz;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScreenToGif.ViewModel.Settings;

public partial class PluginSettingsViewModel: BaseViewModel
{
    private bool _isProcessingFFmpeg;
    private Exception _ffmpegError;
    private string _fFmpegPath = UserSettings.All.FfmpegLocation;
    private string _detectedFFmpegVersion = UserSettings.All.FfmpegVersionText;
    private bool _hasOlderFfmpegVersion = UserSettings.All.HasOlderFfmpegVersion;
    private string _gifskiPath = UserSettings.All.GifskiLocation;
    private bool _isProcessingGifski;
    private Exception _gifskiError;
    private bool _isGifskiPresent;

    //FFmpeg
    public string FFmpegPath
    {
        get => _fFmpegPath;
        set
        {
            SetProperty(ref _fFmpegPath, value);

            UserSettings.All.FfmpegLocation = value;

            OnPropertyChanged(nameof(DownloadFFmpegButtonVisibility));
            OnPropertyChanged(nameof(RemoveFFmpegButtonVisibility));
        }
    }

    public bool IsProcessingFFmpeg
    {
        get => _isProcessingFFmpeg;
        set
        {
            SetProperty(ref _isProcessingFFmpeg, value);

            OnPropertyChanged(nameof(AreFFmpegPropertiesEnabled));
            OnPropertyChanged(nameof(IsProcessingFFmpegVisibility));
        }
    }

    public Visibility IsProcessingFFmpegVisibility => IsProcessingFFmpeg ? Visibility.Visible : Visibility.Collapsed;

    public bool AreFFmpegPropertiesEnabled => !IsProcessingFFmpeg;

    public bool FFmpegHasError => FFmpegError != null;

    public Exception FFmpegError
    {
        get => _ffmpegError;
        set
        {
            SetProperty(ref _ffmpegError, value);

            OnPropertyChanged(nameof(FFmpegErrorVisibility));
        }
    }

    public string DetectedFFmpegVersion
    {
        get => _detectedFFmpegVersion;
        set
        {
            SetProperty(ref _detectedFFmpegVersion, value);

            UserSettings.All.FfmpegVersionText = value;

            OnPropertyChanged(nameof(IsFFmpegPresent));
            OnPropertyChanged(nameof(DownloadFFmpegButtonVisibility));
            OnPropertyChanged(nameof(RemoveFFmpegButtonVisibility));
        }
    }

    public bool HasOlderFfmpegVersion
    {
        get => _hasOlderFfmpegVersion;
        set
        {
            SetProperty(ref _hasOlderFfmpegVersion, value);

            UserSettings.All.HasOlderFfmpegVersion = value;
        }
    }

    public bool IsFFmpegPresent => DetectedFFmpegVersion != null;

    public Visibility DownloadFFmpegButtonVisibility => IsFFmpegPresent ? Visibility.Collapsed : Visibility.Visible;

    public Visibility RemoveFFmpegButtonVisibility => IsFFmpegPresent ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FFmpegErrorVisibility => FFmpegError == null ? Visibility.Collapsed : Visibility.Visible;

    //Gifski
    public string GifskiPath
    {
        get => _gifskiPath;
        set
        {
            SetProperty(ref _gifskiPath, value);

            UserSettings.All.GifskiLocation = value;

            OnPropertyChanged(nameof(DownloadGifskiButtonVisibility));
            OnPropertyChanged(nameof(RemoveGifskiButtonVisibility));
        }
    }

    public bool IsProcessingGifski
    {
        get => _isProcessingGifski;
        set
        {
            SetProperty(ref _isProcessingGifski, value);

            OnPropertyChanged(nameof(AreGifskiPropertiesEnabled));
            OnPropertyChanged(nameof(IsProcessingGifskiVisibility));
        }
    }

    public Visibility IsProcessingGifskiVisibility => IsProcessingGifski ? Visibility.Visible : Visibility.Collapsed;

    //Gifski is only supported in x64.
    public bool AreGifskiPropertiesEnabled => Environment.Is64BitProcess && !IsProcessingGifski;

    public bool GifskiHasError => GifskiError != null;

    public Exception GifskiError
    {
        get => _gifskiError;
        set
        {
            SetProperty(ref _gifskiError, value);

            OnPropertyChanged(nameof(GifskiErrorVisibility));
        }
    }

    public bool IsGifskiPresent
    {
        get => _isGifskiPresent;
        set
        {
            SetProperty(ref _isGifskiPresent, value);

            OnPropertyChanged(nameof(DownloadGifskiButtonVisibility));
            OnPropertyChanged(nameof(RemoveGifskiButtonVisibility));
        }
    }

    public Visibility DownloadGifskiButtonVisibility => Environment.Is64BitProcess && IsGifskiPresent ? Visibility.Collapsed : Visibility.Visible;

    public Visibility RemoveGifskiButtonVisibility => Environment.Is64BitProcess && IsGifskiPresent ? Visibility.Visible : Visibility.Collapsed;

    public Visibility GifskiErrorVisibility => GifskiError == null ? Visibility.Collapsed : Visibility.Visible;

    //Commands.
    public RoutedUICommand DownloadFFmpegCommand { get; set; } = new();

    public RoutedUICommand RemoveFFmpegCommand { get; set; } = new();

    public RoutedUICommand SelectFFmpegPathCommand { get; set; } = new();

    public RoutedUICommand BrowseFFmpegCommand { get; set; } = new();

    public RoutedUICommand SeeErrorFFmpegCommand { get; set; } = new();

    public RoutedUICommand DownloadGifskiCommand { get; set; } = new();

    public RoutedUICommand RemoveGifskiCommand { get; set; } = new();

    public RoutedUICommand SelectGifskiPathCommand { get; set; } = new();

    public RoutedUICommand BrowseGifskiCommand { get; set; } = new();

    public RoutedUICommand SeeErrorGifskiCommand { get; set; } = new();
    
    public async void DownloadFFmpeg()
    {
        try
        {
            IsProcessingFFmpeg = true;

#if FULL_MULTI_MSIX_STORE
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Extras.DownloadRestriction"));
            ProcessHelper.StartWithShell("https://github.com/GyanD/codexffmpeg/releases");
            return;
#else
            var release = await GitHubHelper.GetLatestRelease("GyanD/codexffmpeg");
            var asset = release?.GetAsset(".7z");

            if (asset == null)
            {
                FFmpegError = new Exception("No .7z asset found in the latest release.");
                return;
            }

            var packedFolder = Path.Combine(UserSettings.All.TemporaryFolderResolved, "Downloads");
            var packedPath = Path.Combine(packedFolder, asset.Name);

            Directory.CreateDirectory(packedFolder);

            if (File.Exists(packedPath))
                File.Delete(packedPath);

            await using (var fileStream = new FileStream(packedPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, true))
            {
                await using (var stream = await WebHelper.GetStream(asset.BrowserDownloadUrl))
                    await stream.CopyToAsync(fileStream);
            }

            FFmpegPath = await UnpackFFmpeg(packedPath);
            await VerifyFFmpeg(true);

            File.Delete(packedPath);
#endif
        }
        catch (Exception e)
        {
            FFmpegError = e;
            LogWriter.Log(e, "Not possible to download FFmpeg release.");

            DetectedFFmpegVersion = null;
        }
        finally
        {
            IsProcessingFFmpeg = false;
        }
    }

    public async void DownloadGifski()
    {
        try
        {
            IsProcessingGifski = true;

#if FULL_MULTI_MSIX_STORE
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Extras.DownloadRestriction"));
            ProcessHelper.StartWithShell("https://github.com/ImageOptim/gifski/releases");
            return;
#else
            var release = await GitHubHelper.GetLatestRelease("ImageOptim/gifski");
            var asset = release?.GetAsset(".tar.xz");

            if (asset == null)
            {
                FFmpegError = new Exception("No .tar.xz asset found in the latest release.");
                return;
            }

            var packedFolder = Path.Combine(UserSettings.All.TemporaryFolderResolved, "Downloads");
            var packedPath = Path.Combine(packedFolder, asset.Name);

            Directory.CreateDirectory(packedFolder);

            if (File.Exists(packedPath))
                File.Delete(packedPath);

            await using (var fileStream = new FileStream(packedPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, true))
            {
                await using (var stream = await WebHelper.GetStream(asset.BrowserDownloadUrl))
                    await stream.CopyToAsync(fileStream);
            }

            GifskiPath = await UnpackGifski(packedPath);
            VerifyGifski(true);

            File.Delete(packedPath);
#endif
        }
        catch (Exception e)
        {
            GifskiError = e;
            LogWriter.Log(e, "Not possible to download Gifski release.");
        }
        finally
        {
            IsProcessingGifski = false;
        }
    }

    public async Task<string> UnpackFFmpeg(string path)
    {
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = SevenZipArchive.Open(fileStream);

        var entry = archive.Entries.FirstOrDefault(x => x.Key.Contains("ffmpeg.exe"));

        if (entry == null)
            return null;

        var destinationPath = Environment.ExpandEnvironmentVariables("%appdata%\\ScreenToGif\\Plugins");
        var destination = Path.Combine(destinationPath, "ffmpeg.exe");

        Directory.CreateDirectory(destinationPath);

        if (File.Exists(destination))
            File.Delete(destination);

        await Task.Run(() =>
        {
            entry.WriteToDirectory(destinationPath, new ExtractionOptions
            {
                PreserveFileTime = true,
                Overwrite = true
            });
        });

        return destination;
    }

    public async Task<string> UnpackGifski(string path)
    {
        var tempPath = Path.Combine(Path.GetDirectoryName(path)!, "gifski.tar");

        await using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await using (XZStream xzStream = new(fileStream))
            {
                await using (var outTar = File.Create(tempPath))
                    await xzStream.CopyToAsync(outTar);
            }
        }

        var destinationPath = Environment.ExpandEnvironmentVariables("%appdata%\\ScreenToGif\\Plugins");
        var destination = Path.Combine(destinationPath, "gifski.dll");

        Directory.CreateDirectory(destinationPath);

        if (File.Exists(destination))
            File.Delete(destination);

        using (var archive = TarArchive.Open(tempPath))
        {
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory && entry.Key?.EndsWith("gifski.dll") == true)
                {
                    entry.WriteToFile(destination, new ExtractionOptions { Overwrite = true });
                    return destination;
                }
            }
        }

        File.Delete(tempPath);

        return null;
    }

    public async Task VerifyFFmpeg(bool bubbleUpError = false)
    {
        try
        {
            IsProcessingFFmpeg = true;

            if (FFmpegPath == null || !File.Exists(FFmpegPath))
            {
                DetectedFFmpegVersion = null;
                FFmpegPath = null;
                return;
            }

            //Call FFmpeg to check its version.
            var output = await ProcessHelper.Start(FFmpegPath + " -version");

            //Check the output to determine the FFmpeg version.
            DetectedFFmpegVersion = FfmpegHelper.IdentifyVersion(output);
            HasOlderFfmpegVersion = FfmpegHelper.IsOlder(output);
        }
        catch (Exception e)
        {
            if (bubbleUpError)
                throw;

            LogWriter.Log(e, "Verifying if FFmpeg is available");
        }
        finally
        {
            IsProcessingFFmpeg = false;

            //Since the props are being updated inside a thread, I need to call the InvalidateRequerySuggested here.
            Dispatcher.CurrentDispatcher.Invoke(CommandManager.InvalidateRequerySuggested, DispatcherPriority.Render);
        }
    }

    public void VerifyGifski(bool bubbleUpError = false)
    {
        try
        {
            IsProcessingGifski = true;

            if (GifskiPath == null || !File.Exists(GifskiPath))
            {
                GifskiPath = null;
                IsGifskiPresent = false;
                return;
            }

            using var interop = new GifskiInterop(GifskiPath);

            IsGifskiPresent = interop.IsProperlySetup;
        }
        catch (Exception e)
        {
            if (bubbleUpError)
                throw;

            LogWriter.Log(e, "Verifying if Gifski is available");
        }
        finally
        {
            IsProcessingGifski = false;

            //Since the props are being updated inside a thread, I need to call the InvalidateRequerySuggested here.
            Dispatcher.CurrentDispatcher.Invoke(CommandManager.InvalidateRequerySuggested, DispatcherPriority.Render);
        }
    }

    public void RemoveFFmpeg()
    {
        if (string.IsNullOrWhiteSpace(FFmpegPath))
            return;

        if (File.Exists(FFmpegPath))
            File.Delete(FFmpegPath);

        FFmpegPath = null;
    }

    public void RemoveGifski()
    {
        if (string.IsNullOrWhiteSpace(GifskiPath))
            return;

        if (File.Exists(GifskiPath))
            File.Delete(GifskiPath);

        GifskiPath = null;
        IsGifskiPresent = false;
    }
}