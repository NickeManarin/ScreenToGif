using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows.Other;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Views.Settings;

public partial class StorageSettings : Page
{
    /// <summary>
    /// Used to decide if a size check is necessary, when a new path is detected.
    /// </summary>
    private string _previousPath = "";

    /// <summary>
    /// True when the cache folder is being checked.
    /// </summary>
    private bool _isBusy;

    /// <summary>
    /// The Path of the cache folder.
    /// </summary>
    private List<DirectoryInfo> _folderList = [];

    /// <summary>
    /// The file count of the cache folder.
    /// </summary>
    private int _fileCount;

    /// <summary>
    /// The size in bytes of the cache folder.
    /// </summary>
    private long _cacheSize;
    
    public StorageSettings()
    {
        InitializeComponent();
    }

    private void StorageSettings_Loaded(object sender, RoutedEventArgs e)
    {
        if (TempPanel.Visibility != Visibility.Visible)
            return;

        _previousPath = UserSettings.All.TemporaryFolderResolved;

        CheckSpace();

        #region Settings

        //Paths.
        AppDataPathTextBlock.Text = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");
        LocalPathTextBlock.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

        //Remove all text decorations (Strikethrough).
        AppDataPathTextBlock.TextDecorations.Clear();
        LocalPathTextBlock.TextDecorations.Clear();

        //Clear the tooltips.
        AppDataPathTextBlock.ClearValue(ToolTipProperty);
        LocalPathTextBlock.ClearValue(ToolTipProperty);

        //AppData.
        if (!File.Exists(AppDataPathTextBlock.Text))
        {
            AppDataPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough, new Pen(Brushes.DarkSlateGray, 1),
                0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

            AppDataPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
        }

        //Local.
        if (!File.Exists(LocalPathTextBlock.Text))
        {
            LocalPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough, new Pen(Brushes.DarkSlateGray, 1),
                0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

            LocalPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
        }

        #endregion
    }
    
    private void Cache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && !string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder) && !_isBusy;
    }

    private void BrowseCache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && !string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder) && Directory.Exists(UserSettings.All.TemporaryFolderResolved) && !_isBusy;
    }

    private void BrowseLogs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && !string.IsNullOrWhiteSpace(UserSettings.All.LogsFolder) && Directory.Exists(UserSettings.All.LogsFolder);
    }

    private void CreateLocalSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && !File.Exists(LocalPathTextBlock.Text);
    }

    private void RemoveLocalSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && File.Exists(LocalPathTextBlock.Text);
    }

    private void RemoveAppDataSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && File.Exists(AppDataPathTextBlock.Text);
    }


    private void CheckCache_Execute(object sender, RoutedEventArgs e)
    {
        CheckSpace();
    }

    private async void ClearCache_Execute(object sender, RoutedEventArgs e)
    {
        _isBusy = true;
        StatusProgressBar.State = ExtendedProgressBar.ProgressState.Primary;
        StatusProgressBar.IsIndeterminate = true;
        FilesTextBlock.Visibility = Visibility.Collapsed;

        try
        {
            var parent = PathHelper.AdjustPath(UserSettings.All.TemporaryFolderResolved);
            var path = Path.Combine(parent, "ScreenToGif", "Recording");

            if (!Directory.Exists(path))
                return;

            //Force to be 1 day or more.
            UserSettings.All.AutomaticCleanUpDays = UserSettings.All.AutomaticCleanUpDays > 0 ? UserSettings.All.AutomaticCleanUpDays : 5;

            //Asks if the user wants to remove all files or just the old ones.
            if (!CacheDialog.Ask(true, out var ignoreRecent))
                return;

            _folderList = await Task.Factory.StartNew(() => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Select(x => new DirectoryInfo(x)).ToList());

            if (ignoreRecent)
                _folderList = await Task.Factory.StartNew(() => _folderList.Where(w => (DateTime.Now - w.CreationTime).Days > UserSettings.All.AutomaticCleanUpDays).ToList());

            foreach (var folder in _folderList.Where(folder => !MutexList.IsInUse(folder.Name)))
                Directory.Delete(folder.FullName, true);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while cleaning the cache folder.");
        }
        finally
        {
            App.MainViewModel.CheckDiskSpace();
            CheckSpace(true);
        }
    }

    private void ChooseCachePath_Click(object sender, RoutedEventArgs e)
    {
        var path = UserSettings.All.TemporaryFolderResolved;

        if (UserSettings.All.TemporaryFolderResolved.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            path = "";

        //It's only a relative path if not null/empty and there's no root folder declared.
        var isRelative = !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path);
        var notAlt = !string.IsNullOrWhiteSpace(path) && UserSettings.All.TemporaryFolderResolved.Contains(Path.DirectorySeparatorChar);

        path = PathHelper.AdjustPath(path);

        var initial = Directory.Exists(path) ? path : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var folderDialog = new FolderSelector();

        if (!string.IsNullOrWhiteSpace(initial))
            folderDialog.SelectedPath = initial;

        if (!folderDialog.ShowDialog())
            return;

        //Converts to a relative path again.
        if (isRelative && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
        {
            var selected = new Uri(folderDialog.SelectedPath);
            var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
            var relativeFolder = selected.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) == baseFolder.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) ?
                "." : Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

            //This app even returns you the correct slashes/backslashes.
            UserSettings.All.TemporaryFolder = notAlt ? relativeFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) :
                relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        else
        {
            UserSettings.All.TemporaryFolder = folderDialog.SelectedPath;
        }

        _previousPath = UserSettings.All.TemporaryFolderResolved;
        CheckSpace();
    }

    private void CacheTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_previousPath == UserSettings.All.TemporaryFolderResolved)
            return;

        _previousPath = UserSettings.All.TemporaryFolderResolved;
        CheckSpace();
    }

    private void BrowseCache_Execute(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(UserSettings.All.TemporaryFolderResolved);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the cache folder.");
        }
    }

    private void ChooseLogsPath_Click(object sender, RoutedEventArgs e)
    {
        var path = UserSettings.All.LogsFolder;

        if (UserSettings.All.LogsFolder.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            path = "";

        //It's only a relative path if not null/empty and there's no root folder declared.
        var isRelative = !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path);
        var notAlt = !string.IsNullOrWhiteSpace(path) && UserSettings.All.LogsFolder.Contains(Path.DirectorySeparatorChar);

        path = PathHelper.AdjustPath(path);

        var initial = Directory.Exists(path) ? path : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var folderDialog = new FolderSelector();

        if (!string.IsNullOrWhiteSpace(initial))
            folderDialog.SelectedPath = initial;

        if (!folderDialog.ShowDialog())
            return;

        //Converts to a relative path again.
        if (isRelative && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
        {
            var selected = new Uri(folderDialog.SelectedPath);
            var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
            var relativeFolder = selected.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) == baseFolder.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) ?
                "." : Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

            //This app even returns you the correct slashes/backslashes.
            UserSettings.All.LogsFolder = notAlt ? relativeFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) :
                relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        else
        {
            UserSettings.All.LogsFolder = folderDialog.SelectedPath;
        }
    }

    private void BrowseLogs_Execute(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(UserSettings.All.LogsFolder);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the logs folder.");
        }
    }


    private void OpenAppDataSettings_Execute(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
                ProcessHelper.StartWithShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml"));
            else
                Process.Start("explorer.exe", $"/select,\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml")}\"");
        }
        catch (Exception ex)
        {
            Dialog.Ok("Open AppData Settings Folder", "Impossible to open where the AppData settings is located", ex.Message);
        }
    }

    private void RemoveAppDataSettings_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            UserSettings.RemoveAppDataSettings();

            AppDataPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough,
                new Pen(Brushes.DarkSlateGray, 1), 0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

            AppDataPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
        }
        catch (Exception ex)
        {
            Dialog.Ok("Remove AppData Settings", "Impossible to remove AppData settings", ex.Message);
        }
    }

    private void CreateLocalSettings_Execute(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            UserSettings.CreateLocalSettings();

            LocalPathTextBlock.TextDecorations.Clear();
            LocalPathTextBlock.ClearValue(ToolTipProperty);
        }
        catch (Exception ex)
        {
            Dialog.Ok("Create Local Settings", "Impossible to create local settings", ex.Message);
        }
    }

    private void OpenLocalSettings_Execute(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
                ProcessHelper.StartWithShell(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml"));
            else
                Process.Start("explorer.exe", $"/select,\"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml")}\"");
        }
        catch (Exception ex)
        {
            Dialog.Ok("Open AppData Local Folder", "Impossible to open where the Local settings file is located", ex.Message);
        }
    }

    private void RemoveLocalSettings_Execute(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            UserSettings.RemoveLocalSettings();

            LocalPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough,
                new Pen(Brushes.DarkSlateGray, 1), 0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

            LocalPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
        }
        catch (Exception ex)
        {
            Dialog.Ok("Remove Local Settings", "Impossible to remove local settings", ex.Message);
        }
    }


    private async void CheckSpace(bool force = false)
    {
        if (_isBusy && !force)
            return;

        _isBusy = true;
        StatusProgressBar.State = ExtendedProgressBar.ProgressState.Primary;
        StatusProgressBar.IsIndeterminate = true;
        FilesTextBlock.Visibility = Visibility.Collapsed;

        #region Status

        var path = PathHelper.AdjustPath(UserSettings.All.TemporaryFolderResolved);
        var drive = DriveInfo.GetDrives().FirstOrDefault(w => w.RootDirectory.FullName == Path.GetPathRoot(path));

        if (drive != null)
        {
            VolumeTextBlock.Text = $"{drive.VolumeLabel} ({drive.Name.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar)})".TrimStart();
            FreeSpaceTextBlock.Text = LocalizationHelper.GetWithFormat("S.Options.Storage.Status.FreeSpace", "{0} free of {1}", Humanizer.BytesToString(drive.TotalFreeSpace), Humanizer.BytesToString(drive.TotalSize));
            StatusProgressBar.Value = 100 - ((double)drive.AvailableFreeSpace / drive.TotalSize * 100);
            StatusProgressBar.State = StatusProgressBar.Value < 90 ? ExtendedProgressBar.ProgressState.Info : ExtendedProgressBar.ProgressState.Danger;
            LowSpaceTextBlock.Visibility = drive.AvailableFreeSpace > 2_000_000_000 ? Visibility.Collapsed : Visibility.Visible; //2 GB.
        }
        else
        {
            VolumeTextBlock.Text = Path.GetPathRoot(path);
            FreeSpaceTextBlock.Text = LocalizationHelper.Get("S.Options.Storage.Status.Error");
            StatusProgressBar.Value = 0;
            LowSpaceTextBlock.Visibility = Visibility.Collapsed;
        }

        #endregion

        //Calculates the quantity of files and folders.
        await Task.Run(CheckDrive);

        try
        {
            App.MainViewModel.CheckDiskSpace();

            FilesRun.Text = _fileCount == 0 ? LocalizationHelper.Get("S.Options.Storage.Status.Files.None") :
                LocalizationHelper.GetWithFormat("S.Options.Storage.Status.Files." + (_fileCount > 1 ? "Plural" : "Singular"), "{0} files", _fileCount);
            FoldersRun.Text = _folderList.Count == 0 ? LocalizationHelper.Get("S.Options.Storage.Status.Folders.None") :
                LocalizationHelper.GetWithFormat("S.Options.Storage.Status.Folders." + (_folderList.Count > 1 ? "Plural" : "Singular"), "{0} folders", _folderList.Count);
            UsedSpaceRun.Text = LocalizationHelper.GetWithFormat("S.Options.Storage.Status.InUse", "{0} in use", Humanizer.BytesToString(_cacheSize));
            FilesTextBlock.Visibility = Visibility.Visible;
            StatusProgressBar.IsIndeterminate = false;
        }
        catch (Exception)
        { }
        finally
        {
            _isBusy = false;
        }
    }

    private void CheckDrive()
    {
        _folderList = [];

        var path = PathHelper.AdjustPath(UserSettings.All.TemporaryFolderResolved);
        var cache = Path.Combine(path, "ScreenToGif", "Recording");

        if (!Directory.Exists(cache))
        {
            _folderList = [];
            _fileCount = 0;
            _cacheSize = 0;
            return;
        }

        _folderList = Directory.GetDirectories(cache).Select(x => new DirectoryInfo(x)).ToList();
        _fileCount = _folderList.Sum(folder => Directory.EnumerateFiles(folder.FullName).Count());
        _cacheSize = _folderList.Sum(s => s.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length));
    }
}