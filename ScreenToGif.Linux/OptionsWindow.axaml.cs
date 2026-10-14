using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using System.Diagnostics;

namespace ScreenToGif.Linux;

/// <summary>The Options sections another window can ask to be shown, by the tag the list carries.</summary>
public enum OptionsSection
{
    Application,
    Recorder,
    Editor,
    Shortcuts,
    Storage
}

public partial class OptionsWindow : Window
{
    private readonly LinuxAutostartService _autostart = new();
    private RecorderCaptureMode _seededCaptureMode;
    private bool _ready;
    private bool _saving;
    private bool _allowClose;

    public OptionsWindow() : this(OptionsSection.Application)
    {
    }

    public OptionsWindow(OptionsSection section)
    {
        InitializeComponent();
        LoadSettings();
        LoadStorageStatus();
        Closing += WindowClosing;
        _ready = true;
        UpdateDependencies();
        ShowSection(section);
    }

    /// <summary>Selects a section, so another window can open Options where its own settings live.</summary>
    public void ShowSection(OptionsSection section)
    {
        var tag = section.ToString();
        var item = SectionsList.Items.OfType<ListBoxItem>()
            .FirstOrDefault(candidate => string.Equals(candidate.Tag as string, tag, StringComparison.Ordinal));

        if (item is not null)
            SectionsList.SelectedItem = item;
    }

    private void LoadSettings()
    {
        var settings = LinuxSettings.Current;
        StartManuallyRadio.IsChecked = !_autostart.IsEnabled();
        StartAutomaticallyRadio.IsChecked = !StartManuallyRadio.IsChecked;
        SingleInstanceRadio.IsChecked = settings.SingleInstance;
        MultipleInstancesRadio.IsChecked = !settings.SingleInstance;
        StartMinimizedCheckBox.IsChecked = settings.StartMinimized;
        StartupWindowComboBox.SelectedIndex = (int)settings.StartupWindow;
        ThemeComboBox.SelectedIndex = (int)settings.Theme;
        ShowTrayIconCheckBox.IsChecked = settings.ShowNotificationIcon;
        KeepOpenCheckBox.IsChecked = settings.KeepOpen;
        LeftActionComboBox.SelectedIndex = (int)settings.LeftClickAction;
        LeftWindowComboBox.SelectedIndex = (int)settings.LeftClickWindow;
        DoubleLeftActionComboBox.SelectedIndex = (int)settings.DoubleLeftClickAction;
        DoubleLeftWindowComboBox.SelectedIndex = (int)settings.DoubleLeftClickWindow;
        MiddleActionComboBox.SelectedIndex = (int)settings.MiddleClickAction;
        MiddleWindowComboBox.SelectedIndex = (int)settings.MiddleClickWindow;
        DisableHardwareAccelerationCheckBox.IsChecked = settings.DisableHardwareAcceleration;
        AskBeforeDeleteFramesCheckBox.IsChecked = settings.AskBeforeDeleteFrames;
        AskBeforeDiscardProjectCheckBox.IsChecked = settings.AskBeforeDiscardProject;
        AskBeforeCloseEditorCheckBox.IsChecked = settings.AskBeforeCloseEditor;
        DropFramesSettingCheckBox.IsChecked = settings.DropFramesWhenBehind;
        LimitUndoCheckBox.IsChecked = settings.UndoLimit > 0;
        UndoLimitUpDown.Value = Math.Clamp(settings.UndoLimit, 1, 100);
        DeleteCacheOnCloseCheckBox.IsChecked = settings.DeleteCacheOnClose;
        RemoveOldProjectsCheckBox.IsChecked = settings.RemoveOldProjects;
        RetentionDaysUpDown.Value = Math.Clamp(settings.ProjectRetentionDays, 1, 30);
        FfmpegPathTextBox.Text = settings.FfmpegPath;
        LoadRecorderSettings(settings);
    }

    private void LoadRecorderSettings(LinuxApplicationSettings settings)
    {
        _seededCaptureMode = settings.RecorderCaptureMode;
        RecorderManualRadio.IsChecked = settings.RecorderCaptureMode == RecorderCaptureMode.Manual;
        RecorderPerSecondRadio.IsChecked = settings.RecorderCaptureMode == RecorderCaptureMode.PerSecond;
        RecorderPerMinuteRadio.IsChecked = settings.RecorderCaptureMode == RecorderCaptureMode.PerMinute;
        RecorderPerHourRadio.IsChecked = settings.RecorderCaptureMode == RecorderCaptureMode.PerHour;
        RecorderFixedFrameRateCheckBox.IsChecked = settings.RecorderFixedFrameRate;
        RecorderManualDelayUpDown.Value = settings.RecorderManualPlaybackDelayMs;
        RecorderShowCursorCheckBox.IsChecked = settings.RecorderShowCursor;
        RecorderRememberSizeCheckBox.IsChecked = settings.RecorderRememberSize;
        RecorderRememberPositionCheckBox.IsChecked = settings.RecorderRememberPosition;
        RecorderPreStartCheckBox.IsChecked = settings.RecorderPreStart;
        RecorderPreStartSecondsUpDown.Value = settings.RecorderPreStartSeconds;
        RecorderAskBeforeDiscardingCheckBox.IsChecked = settings.RecorderAskBeforeDiscarding;
    }

    private void RecorderFrequencyChanged(object? sender, RoutedEventArgs e)
    {
        if (_ready)
            UpdateDependencies();
    }

    private void RecorderDependencyChanged(object? sender, RoutedEventArgs e) => UpdateDependencies();

    private void SectionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
            return;

        if (SectionsList.SelectedItem is not ListBoxItem item || item.Tag is not string section)
            return;

        var pages = new Dictionary<string, Control>(StringComparer.Ordinal)
        {
            ["Application"] = ApplicationPage,
            ["Recorder"] = RecorderPage,
            ["Editor"] = EditorPage,
            ["Tasks"] = TasksPage,
            ["Shortcuts"] = ShortcutsPage,
            ["Language"] = LanguagePage,
            ["Storage"] = StoragePage,
            ["Cloud"] = CloudPage,
            ["Extras"] = ExtrasPage,
            ["Donate"] = DonatePage,
            ["About"] = AboutPage
        };
        foreach (var page in pages.Values)
            page.IsVisible = false;
        UnavailablePage.IsVisible = false;

        if (pages.TryGetValue(section, out var selectedPage))
            selectedPage.IsVisible = true;
        else
        {
            UnavailablePage.IsVisible = true;
            UnavailableTitle.Text = section;
            UnavailableMessage.Text = "This settings page is unavailable.";
        }
    }

    private void StartupModeChanged(object? sender, RoutedEventArgs e)
    {
        if (_ready)
            StatusText.Text = "Startup mode will take effect after you press Ok.";
    }

    private void StartupDependencyChanged(object? sender, RoutedEventArgs e)
    {
        if (StartMinimizedCheckBox.IsChecked == true)
            ShowTrayIconCheckBox.IsChecked = true;
        UpdateDependencies();
    }

    /// <summary>
    /// Startup, the webcam recorder and the editor can all be opened first. The screen and board
    /// recorders are still previews, so choosing one falls back to the StartUp window.
    /// </summary>
    private void StartupWindowChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready || StartupWindowComboBox.SelectedIndex
            is (int)LinuxStartupWindow.Startup or (int)LinuxStartupWindow.Webcam or (int)LinuxStartupWindow.Board or (int)LinuxStartupWindow.Editor)
            return;

        StartupWindowComboBox.SelectedIndex = (int)LinuxStartupWindow.Startup;
    }

    private void TrayDependencyChanged(object? sender, RoutedEventArgs e)
    {
        if (ShowTrayIconCheckBox.IsChecked != true)
            StartMinimizedCheckBox.IsChecked = false;
        UpdateDependencies();
    }

    private void TrayActionChanged(object? sender, SelectionChangedEventArgs e) => UpdateDependencies();

    private void UpdateDependencies()
    {
        if (KeepOpenCheckBox is null || TrayInteractionsGrid is null)
            return;

        var trayEnabled = ShowTrayIconCheckBox.IsChecked == true;
        KeepOpenCheckBox.IsEnabled = trayEnabled;
        TrayInteractionsGrid.IsEnabled = trayEnabled;
        StartupWindowComboBox.IsEnabled = StartMinimizedCheckBox.IsChecked != true;

        var opensWindow = LeftActionComboBox.SelectedIndex == (int)LinuxTrayAction.OpenWindow;
        LeftWindowLabel.Text = opensWindow ? "Window:" : "Or else, opens:";
        LeftWindowComboBox.IsEnabled = trayEnabled;

        // A fixed frame rate and a manual delay each belong to one capture mode only.
        RecorderFixedFrameRateCheckBox.IsEnabled = RecorderPerSecondRadio.IsChecked == true;
        RecorderManualDelayRow.IsEnabled = RecorderManualRadio.IsChecked == true;
        RecorderPreStartSecondsRow.IsEnabled = RecorderPreStartCheckBox.IsChecked == true;

        // The location is remembered only alongside the size, as on Windows, and is cleared when
        // the size stops being remembered so the page never shows an option that will not apply.
        var rememberSize = RecorderRememberSizeCheckBox.IsChecked == true;
        RecorderRememberPositionCheckBox.IsEnabled = rememberSize;
        if (!rememberSize)
            RecorderRememberPositionCheckBox.IsChecked = false;
    }

    private void ThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready || ThemeComboBox.SelectedIndex < 0)
            return;

        App.ApplyTheme((LinuxAppTheme)ThemeComboBox.SelectedIndex);
    }

    /// <summary>
    /// Applies every control's value onto <paramref name="settings"/>. Fields this window has no
    /// control for are left as they are, so a value another window wrote while this one was open,
    /// such as the Recorder's frame rate, is not written back over.
    /// </summary>
    private void ReadSettings(LinuxApplicationSettings settings)
    {
        settings.SingleInstance = SingleInstanceRadio.IsChecked == true;
        settings.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
        settings.StartupWindow = (LinuxStartupWindow)Math.Max(0, StartupWindowComboBox.SelectedIndex);
        settings.Theme = (LinuxAppTheme)Math.Max(0, ThemeComboBox.SelectedIndex);
        settings.ShowNotificationIcon = ShowTrayIconCheckBox.IsChecked == true;
        settings.KeepOpen = KeepOpenCheckBox.IsChecked == true;
        settings.LeftClickAction = (LinuxTrayAction)Math.Max(0, LeftActionComboBox.SelectedIndex);
        settings.LeftClickWindow = (LinuxTrayWindow)Math.Max(0, LeftWindowComboBox.SelectedIndex);
        settings.DoubleLeftClickAction = (LinuxTrayAction)Math.Max(0, DoubleLeftActionComboBox.SelectedIndex);
        settings.DoubleLeftClickWindow = (LinuxTrayWindow)Math.Max(0, DoubleLeftWindowComboBox.SelectedIndex);
        settings.MiddleClickAction = (LinuxTrayAction)Math.Max(0, MiddleActionComboBox.SelectedIndex);
        settings.MiddleClickWindow = (LinuxTrayWindow)Math.Max(0, MiddleWindowComboBox.SelectedIndex);
        settings.DisableHardwareAcceleration = DisableHardwareAccelerationCheckBox.IsChecked == true;
        settings.AskBeforeDeleteFrames = AskBeforeDeleteFramesCheckBox.IsChecked == true;
        settings.AskBeforeDiscardProject = AskBeforeDiscardProjectCheckBox.IsChecked == true;
        settings.AskBeforeCloseEditor = AskBeforeCloseEditorCheckBox.IsChecked == true;
        settings.DropFramesWhenBehind = DropFramesSettingCheckBox.IsChecked == true;
        settings.UndoLimit = LimitUndoCheckBox.IsChecked == true ? (int)(UndoLimitUpDown.Value ?? 50) : 0;
        settings.DeleteCacheOnClose = DeleteCacheOnCloseCheckBox.IsChecked == true;
        settings.RemoveOldProjects = RemoveOldProjectsCheckBox.IsChecked == true;
        settings.ProjectRetentionDays = (int)(RetentionDaysUpDown.Value ?? 5);
        settings.FfmpegPath = string.IsNullOrWhiteSpace(FfmpegPathTextBox.Text) ? "ffmpeg" : FfmpegPathTextBox.Text.Trim();
        ReadRecorderSettings(settings);
    }

    private void ReadRecorderSettings(LinuxApplicationSettings settings)
    {
        // The capture mode is the one field the Recorder also edits, so it is written only when
        // these radios were changed; a mode chosen in the Recorder while Options was open survives Ok.
        var captureMode = SelectedRecorderCaptureMode();
        if (captureMode != _seededCaptureMode)
            settings.RecorderCaptureMode = captureMode;
        settings.RecorderFixedFrameRate = RecorderFixedFrameRateCheckBox.IsChecked == true;
        settings.RecorderManualPlaybackDelayMs = (int)(RecorderManualDelayUpDown.Value ?? 1000);
        settings.RecorderShowCursor = RecorderShowCursorCheckBox.IsChecked == true;
        settings.RecorderRememberSize = RecorderRememberSizeCheckBox.IsChecked == true;
        settings.RecorderRememberPosition = RecorderRememberPositionCheckBox.IsChecked == true;
        settings.RecorderPreStart = RecorderPreStartCheckBox.IsChecked == true;
        settings.RecorderPreStartSeconds = (int)(RecorderPreStartSecondsUpDown.Value ?? 3);
        settings.RecorderAskBeforeDiscarding = RecorderAskBeforeDiscardingCheckBox.IsChecked == true;
    }

    private RecorderCaptureMode SelectedRecorderCaptureMode()
    {
        if (RecorderManualRadio.IsChecked == true)
            return RecorderCaptureMode.Manual;
        if (RecorderPerMinuteRadio.IsChecked == true)
            return RecorderCaptureMode.PerMinute;
        if (RecorderPerHourRadio.IsChecked == true)
            return RecorderCaptureMode.PerHour;

        return RecorderCaptureMode.PerSecond;
    }

    private void LoadStorageStatus()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), "screentogif-linux");
        CachePathTextBox.Text = cachePath;
        SettingsPathText.Text = new LinuxApplicationSettingsStore().Path;
        try
        {
            var root = Path.GetPathRoot(cachePath);
            if (string.IsNullOrWhiteSpace(root))
                return;
            var drive = new DriveInfo(root);
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            StorageVolumeText.Text = $"{drive.Name}     {FormatBytes(drive.AvailableFreeSpace)} free of {FormatBytes(drive.TotalSize)}";
            StorageProgressBar.Value = drive.TotalSize == 0 ? 0 : used * 100d / drive.TotalSize;
            var workspaceCount = Directory.Exists(cachePath) ? Directory.EnumerateDirectories(cachePath).Count() : 0;
            StorageUsageText.Text = $"{workspaceCount} temporary project folder(s)";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StorageVolumeText.Text = "Storage status unavailable";
            StorageUsageText.Text = ex.Message;
        }
    }

    private async void TestFfmpegClick(object? sender, RoutedEventArgs e)
    {
        var executable = string.IsNullOrWhiteSpace(FfmpegPathTextBox.Text) ? "ffmpeg" : FfmpegPathTextBox.Text.Trim();
        FfmpegStatusText.Text = "Checking...";
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            info.ArgumentList.Add("-version");
            using var process = Process.Start(info) ?? throw new InvalidOperationException("FFmpeg could not be started.");
            await process.WaitForExitAsync();
            FfmpegStatusText.Text = process.ExitCode == 0 ? "Available and ready" : $"Exited with code {process.ExitCode}";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            FfmpegStatusText.Text = ex.Message;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }

    private void OkClick(object? sender, RoutedEventArgs e) => Close();

    private async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        if (_saving)
            return;

        _saving = true;
        StatusText.Text = "Saving settings...";
        try
        {
            // Committed onto the store as it is now, not as it was when this window opened, so
            // fields edited elsewhere in the meantime keep their latest value.
            var settings = LinuxSettings.Current.Copy();
            ReadSettings(settings);
            await _autostart.SetEnabledAsync(StartAutomaticallyRadio.IsChecked == true);
            await LinuxSettings.SaveAsync(settings);
            App.ApplyTheme(settings.Theme);
            App.CurrentApp?.RefreshTrayIcon();
            _allowClose = true;
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusText.Text = $"Settings were not saved: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }
}
