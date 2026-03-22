using Microsoft.Win32;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.Settings;
using ScreenToGif.Windows.Other;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace ScreenToGif.Views.Settings;

public partial class PluginSettings : Page
{
    private readonly PluginSettingsViewModel _viewModel;

    public PluginSettings()
    {
        InitializeComponent();

        _viewModel = DataContext as PluginSettingsViewModel ?? throw new Exception("Missing view model");

        CommandBindings.Clear();
        CommandBindings.AddRange(new[]
        {
            new CommandBinding(_viewModel.DownloadFFmpegCommand, (_, _) => _viewModel.DownloadFFmpeg(), (_, args) => args.CanExecute = !_viewModel.IsProcessingFFmpeg),
            new CommandBinding(_viewModel.SelectFFmpegPathCommand, SelectFFmpeg_Executed, (_, args) => args.CanExecute = !_viewModel.IsProcessingFFmpeg),
            new CommandBinding(_viewModel.RemoveFFmpegCommand, RemoveFFmpeg_Executed, (_, args) => args.CanExecute = !_viewModel.IsProcessingFFmpeg && !string.IsNullOrWhiteSpace(_viewModel.FFmpegPath)),
            new CommandBinding(_viewModel.SeeErrorFFmpegCommand, BrowseFfmpeg_Executed, BrowseFfmpeg_CanExecute),
            new CommandBinding(_viewModel.SeeErrorFFmpegCommand, SeeErrorFFmpeg_Executed, (_, args) => args.CanExecute = _viewModel.FFmpegHasError),
            new CommandBinding(_viewModel.DownloadGifskiCommand, (_, _) => _viewModel.DownloadGifski(), (_, args) => args.CanExecute = !_viewModel.IsProcessingGifski),
            new CommandBinding(_viewModel.SelectGifskiPathCommand, SelectGifski_Executed, (_, args) => args.CanExecute = !_viewModel.IsProcessingGifski),
            new CommandBinding(_viewModel.RemoveGifskiCommand, RemoveGifski_Executed, (_, args) => args.CanExecute = !_viewModel.IsProcessingGifski && !string.IsNullOrWhiteSpace(_viewModel.GifskiPath)),
            new CommandBinding(_viewModel.BrowseGifskiCommand, BrowseGifski_Executed, BrowseGifski_CanExecute),
            new CommandBinding(_viewModel.SeeErrorGifskiCommand, SeeErrorGifski_Executed, (_, args) => args.CanExecute = _viewModel.GifskiHasError),
        });
    }

    private async void PluginSettings_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.VerifyFFmpeg();

        _viewModel.VerifyGifski();
    }

    //FFmpeg.
    private async void RemoveFFmpeg_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            _viewModel.IsProcessingFFmpeg = true;
            _viewModel.FFmpegError = null;

            _viewModel.RemoveFFmpeg();

            await _viewModel.VerifyFFmpeg(true);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Removing FFmpeg", _viewModel.FFmpegPath);

            _viewModel.FFmpegError = ex;
        }
        finally
        {
            _viewModel.IsProcessingFFmpeg = false;
        }
    }

    private async void SelectFFmpeg_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var previousPath = _viewModel.FFmpegPath;

        _viewModel.IsProcessingFFmpeg = true;
        _viewModel.FFmpegError = null;

        try
        {
            var output = UserSettings.All.FfmpegLocation ?? "";

            if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                output = "";

            //It's only a relative path if not null/empty and there's no root folder declared.
            var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
            var notAlt = !string.IsNullOrWhiteSpace(output) && (UserSettings.All.FfmpegLocation ?? "").Contains(Path.DirectorySeparatorChar);

            //Gets the current directory folder, where the file is located. If empty, it means that the path is relative.
            var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";

            if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(directory))
                directory = AppDomain.CurrentDomain.BaseDirectory;

            var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var ofd = new OpenFileDialog
            {
                FileName = "ffmpeg",
                Filter = $"{LocalizationHelper.Get("S.Options.Extras.FfmpegLocation.File")} (*.exe, *.7z)|*.exe;*.7z",
                Title = LocalizationHelper.Get("S.Options.Extras.FfmpegLocation.Select"),
                InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
                DefaultExt = ".exe"
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            var path = ofd.FileName;

            //Converts to a relative path again.
            if (isRelative && !string.IsNullOrWhiteSpace(path))
            {
                var selected = new Uri(path);
                var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                //This app even returns you the correct slashes/backslashes.
                path = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            //If packed file selected, unpack first.
            if (path?.EndsWith("7z") == true)
                path = await _viewModel.UnpackFFmpeg(path);

            if (!string.IsNullOrWhiteSpace(path))
                _viewModel.FFmpegPath = path;

            await _viewModel.VerifyFFmpeg(true).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Selecting FFmpeg path", _viewModel.FFmpegPath);

            _viewModel.FFmpegError = ex;
            _viewModel.FFmpegPath = previousPath;
        }
        finally
        {
            _viewModel.IsProcessingFFmpeg = false;
        }
    }

    private void SeeErrorFFmpeg_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ErrorDialog.Ok("ScreenToGif", "Error downloading/unpacking FFmpeg", _viewModel.FFmpegError.Message, _viewModel.FFmpegError);
    }

    private void BrowseFfmpeg_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded;// && FfmpegImageCard.Status == ExtrasStatus.Ready;
    }

    private void BrowseFfmpeg_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var path = PathHelper.AdjustPath(UserSettings.All.FfmpegLocation);

            if (string.IsNullOrWhiteSpace(path))
                return;

            var folder = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(folder))
                return;

            ProcessHelper.StartWithShell(folder);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the FFmpeg folder.");
        }
    }

    //Gifski.
    private void RemoveGifski_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            _viewModel.IsProcessingGifski = true;
            _viewModel.GifskiError = null;

            _viewModel.RemoveGifski();

            _viewModel.VerifyGifski(true);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Removing Gifski", _viewModel.GifskiPath);

            _viewModel.GifskiError = ex;
        }
        finally
        {
            _viewModel.IsProcessingGifski = false;
        }
    }

    private async void SelectGifski_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var previousPath = _viewModel.GifskiPath;

        _viewModel.IsProcessingGifski = true;
        _viewModel.GifskiError = null;

        try
        {
            var output = UserSettings.All.GifskiLocation ?? "";

            if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                output = "";

            //It's only a relative path if not null/empty and there's no root folder declared.
            var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
            var notAlt = !string.IsNullOrWhiteSpace(output) && (UserSettings.All.GifskiLocation ?? "").Contains(Path.DirectorySeparatorChar);

            //Gets the current directory folder, where the file is located. If empty, it means that the path is relative.
            var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";

            if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(directory))
                directory = AppDomain.CurrentDomain.BaseDirectory;

            var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var ofd = new OpenFileDialog
            {
                FileName = "gifski",
                Filter = $"{LocalizationHelper.Get("S.Options.Extras.GifskiLocation.File")} (*.dll, *.tar.xz)|*.dll;*.tar.xz",
                Title = LocalizationHelper.Get("S.Options.Extras.GifskiLocation.Select"),
                InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
                DefaultExt = ".dll"
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            var path = ofd.FileName;

            //Converts to a relative path again.
            if (isRelative && !string.IsNullOrWhiteSpace(path))
            {
                var selected = new Uri(path);
                var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                //This app even returns you the correct slashes/backslashes.
                path = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            
            //If packed file selected, unpack first.
            if (path?.EndsWith("tar.xz") == true)
                path = await _viewModel.UnpackGifski(path);

            if (!string.IsNullOrWhiteSpace(path))
                _viewModel.GifskiPath = path;

            _viewModel.VerifyGifski(true);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Selecting Gifski path", _viewModel.GifskiPath);

            _viewModel.GifskiError = ex;
            _viewModel.GifskiPath = previousPath;
        }
        finally
        {
            _viewModel.IsProcessingGifski = false;
        }
    }

    private void SeeErrorGifski_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ErrorDialog.Ok("ScreenToGif", "Error downloading/unpacking Gifski", _viewModel.GifskiError.Message, _viewModel.GifskiError);
    }
    
    private void BrowseGifski_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded;// && GifskiImageCard.Status == ExtrasStatus.Ready;
    }
    
    private void BrowseGifski_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var path = PathHelper.AdjustPath(UserSettings.All.GifskiLocation);

            if (string.IsNullOrWhiteSpace(path))
                return;

            var folder = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(folder))
                return;

            ProcessHelper.StartWithShell(folder);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the Gifski folder.");
        }
    }

    //Other.
    private void ExtrasHyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(e.Uri.AbsoluteUri);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to navigate to the license website.");
        }
    }
}