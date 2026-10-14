using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using ScreenToGif.Linux.Controls;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

/// <summary>
/// Owns the application-wide state: the tray icon, the window the run opens with, and the single
/// capture-shell lifecycle. The coordinator lives here rather than on the StartUp window because "one
/// capture shell at a time" is an application rule, and a second StartUp window would otherwise bring
/// a second coordinator that knows nothing of the first.
/// </summary>
public partial class App : Application, ICaptureShellHost
{
    private TrayIcon? _trayIcon;
    private TrayIcons? _trayIcons;
    private CaptureShellCoordinator? _captureShells;
    private bool _exiting;

    internal static App? CurrentApp => Current as App;

    private CaptureShellCoordinator CaptureShells => _captureShells ??= new CaptureShellCoordinator(this);

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyTheme(LinuxSettings.Current.Theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) =>
            {
                if (LinuxSettings.Current.DeleteCacheOnClose)
                    ProjectArchive.ScavengeStaleWorkspaces(retentionDays: 0);
            };
            var mainWindow = CreateStartupTargetWindow();
            mainWindow.Closed += (_, _) => HandleWindowClosed();
            desktop.MainWindow = mainWindow;
            RefreshTrayIcon();

            if (LinuxSettings.Current.StartMinimized && LinuxSettings.Current.ShowNotificationIcon)
                mainWindow.WindowState = WindowState.Minimized;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// The window the application opens with: a command-line argument first, then the remembered
    /// startup-window setting. A capture shell goes through the same coordinator every other entry
    /// point uses, so closing it takes one route whatever opened it.
    /// </summary>
    private Window CreateStartupTargetWindow()
    {
        if (Program.StartInOptions)
            return new OptionsWindow();

        if (Program.StartInEditor)
            return CreateEditorWindow();

        var captureKind = Program.StartInWebcam
            ? CaptureShellKind.Webcam
            : Program.StartInBoard
                ? CaptureShellKind.Board
                : LinuxSettings.Current.StartupWindow switch
                {
                    LinuxStartupWindow.Webcam => CaptureShellKind.Webcam,
                    LinuxStartupWindow.Board => CaptureShellKind.Board,
                    _ => (CaptureShellKind?)null
                };
        if (captureKind is null && LinuxSettings.Current.StartupWindow == LinuxStartupWindow.Editor)
            return CreateEditorWindow();

        // A shell that fails to open has already had StartUp restored by the coordinator, so that one is
        // reused rather than a second StartUp opened beside it.
        return (captureKind is { } kind ? OpenCaptureShell(kind) : null)
               ?? StartupWindows.FirstOrDefault()
               ?? new StartupWindow();
    }

    /// <summary>
    /// The one way a capture shell opens, whichever surface asked. Returns the shell's window when this
    /// call opened one, and null when a shell was already open or the shell refused to show.
    /// </summary>
    internal Window? OpenCaptureShell(CaptureShellKind kind, CaptureShellAdoption adoption = CaptureShellAdoption.ReplaceProject)
    {
        try
        {
            return CaptureShells.Open(kind, adoption) ? CaptureShells.ActiveShell as Window : null;
        }
        catch (Exception exception)
        {
            // Without this the coordinator's failure reaches the dispatcher and the user sees a button
            // that does nothing. The coordinator has already restored StartUp, which owns the message.
            ReportOpenFailure(kind, exception);
            return null;
        }
    }

    private void ReportOpenFailure(CaptureShellKind kind, Exception failure)
    {
        var name = kind switch
        {
            CaptureShellKind.Recorder => "Recorder",
            CaptureShellKind.Webcam => "Webcam",
            _ => "Board"
        };
        var dialog = new Controls.MessageDialog($"The {name} could not open", failure.Message);
        var owner = StartupWindows.FirstOrDefault(window => window.IsVisible);
        if (owner is not null)
            _ = dialog.ShowForAsync(owner);
        else
            dialog.Show();
    }

    public ICaptureShellWindow CreateShell(CaptureShellKind kind) => kind switch
    {
        CaptureShellKind.Recorder => new RecorderWindow(),
        CaptureShellKind.Webcam => new WebcamWindow(),
        CaptureShellKind.Board => new BoardWindow(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public void HideStartup() => StartupWindows.FirstOrDefault()?.Hide();

    /// <summary>
    /// Brings StartUp back after a shell closes with nothing recorded, opening one when the run started
    /// in a shell and has none, so the application stays reachable.
    /// </summary>
    public void RestoreStartup()
    {
        if (_exiting || Desktop is null)
            return;

        var startup = StartupWindows.FirstOrDefault();
        if (startup is null)
        {
            // A shell opened over the editor closes back to the editor, not to a StartUp window the
            // user never had open.
            if (Desktop.Windows.Any(window => window is MainWindow && window.IsVisible))
                return;

            startup = new StartupWindow();
            startup.Closed += (_, _) => HandleWindowClosed();
            Desktop.MainWindow = startup;
        }

        Restore(startup);
    }

    /// <summary>
    /// Closes StartUp for good once a shell has handed its result to an editor window itself, so the
    /// application does not end and StartUp does not reappear behind the editor.
    /// </summary>
    public void CloseStartup() => StartupWindows.FirstOrDefault()?.CloseForEditor();

    /// <summary>
    /// Opens the editor on a capture shell's recording and closes StartUp behind it, the way choosing
    /// Editor from StartUp does. Reports its own failures rather than throwing, because it runs from a
    /// window-closed notification.
    /// </summary>
    public bool AdoptRecording(LoadedProject recording, CaptureShellAdoption adoption)
    {
        ArgumentNullException.ThrowIfNull(recording);

        if (Desktop is null)
            return false;

        // A recorder opened from the editor hands its frames back to that editor, replacing its
        // project or appending to it as the editor asked; only a shell opened from StartUp needs a
        // new editor, and with no project open there is nothing to append to either way.
        if (Desktop.Windows.OfType<MainWindow>().FirstOrDefault() is { } openEditor)
        {
            Restore(openEditor);
            _ = adoption == CaptureShellAdoption.AppendToProject
                ? openEditor.InsertRecordedFramesAsync(recording)
                : openEditor.AdoptRecordedProjectAsync(recording);
            return true;
        }

        MainWindow editor;
        try
        {
            editor = new MainWindow();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReportShellFailure(ex);
            return false;
        }

        editor.Closed += (_, _) => HandleWindowClosed();
        Desktop.MainWindow = editor;
        editor.Show();
        StartupWindows.FirstOrDefault()?.CloseForEditor();
        _ = editor.AdoptRecordedProjectAsync(recording);
        return true;
    }

    /// <summary>
    /// Reports a capture-shell failure on a window that is actually on screen. The shell has closed and
    /// StartUp was hidden behind it, so restoring StartUp first is what gives the message an owner.
    /// </summary>
    public void ReportShellFailure(Exception failure)
    {
        try
        {
            RestoreStartup();
            var owner = StartupWindows.FirstOrDefault(window => window.IsVisible);
            var dialog = new Controls.MessageDialog(
                "Open the recording",
                $"The recording could not be opened in the editor: {failure.Message}");
            if (owner is not null)
                _ = dialog.ShowForAsync(owner);
            else
                dialog.Show();
        }
        catch (Exception ex) when (ex is InvalidOperationException or NullReferenceException)
        {
            // Reporting is best effort; a shutting-down UI must not turn a handled failure into a crash.
        }
    }

    private IEnumerable<StartupWindow> StartupWindows =>
        Desktop?.Windows.OfType<StartupWindow>() ?? [];

    internal static Window CreateEditorWindow()
    {
        try
        {
            return new MainWindow();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new StartupFailureWindow();
        }
    }

    internal static void ApplyTheme(LinuxAppTheme theme)
    {
        if (Current is null)
            return;

        Current.RequestedThemeVariant = theme switch
        {
            LinuxAppTheme.Dark => ThemeVariant.Dark,
            LinuxAppTheme.FollowSystem => ThemeVariant.Default,
            _ => ThemeVariant.Light
        };
    }

    /// <summary>
    /// Shows or hides the notification icon to match the settings. The icon is created once and
    /// only hidden afterwards: disposing Avalonia's D-Bus tray icon cancels a watch it never
    /// observes, and the unhandled cancellation ends the process, taking every open window and any
    /// recording in progress with it.
    /// </summary>
    internal void RefreshTrayIcon()
    {
        var show = LinuxSettings.Current.ShowNotificationIcon;
        if (_trayIcon is not null)
        {
            _trayIcon.IsVisible = show;
            return;
        }

        if (!show)
            return;

        var menu = new NativeMenu();
        menu.Add(CreateMenuItem("Startup window", (_, _) => OpenWindow(LinuxTrayWindow.Startup)));
        menu.Add(CreateMenuItem("Webcam recorder", (_, _) => OpenWindow(LinuxTrayWindow.Webcam)));
        menu.Add(CreateMenuItem("Editor", (_, _) => OpenWindow(LinuxTrayWindow.Editor)));
        menu.Add(CreateMenuItem("Options", (_, _) => _ = ShowOptions()));
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(CreateMenuItem("Exit", (_, _) => ExitApplication()));

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://ScreenToGif.Linux/Resources/Logo.ico"))),
            ToolTipText = "ScreenToGif",
            IsVisible = true,
            Menu = menu
        };
        _trayIcon.Clicked += (_, _) => RunTrayAction(LinuxSettings.Current.LeftClickAction, LinuxSettings.Current.LeftClickWindow);
        _trayIcons = [_trayIcon];
        TrayIcon.SetIcons(this, _trayIcons);
    }

    /// <summary>
    /// Opens Options, on <paramref name="section"/>. The returned task completes when a dialog the
    /// caller owns closes, so a caller whose own fields mirror those settings can follow the edit;
    /// it completes at once when Options is shown without an owner or was already open.
    /// </summary>
    internal Task ShowOptions(Window? owner = null, OptionsSection section = OptionsSection.Application)
    {
        var existing = Desktop?.Windows.OfType<OptionsWindow>().FirstOrDefault();
        if (existing is not null)
        {
            existing.ShowSection(section);
            Restore(existing);
            return Task.CompletedTask;
        }

        var options = new OptionsWindow(section);
        if (owner is not null)
            return options.ShowDialog(owner);

        options.Show();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Makes a window the application's main window, for a window that opened from a capture shell
    /// rather than from Startup.
    /// </summary>
    internal void AdoptEditor(Window editor)
    {
        if (Desktop is not null)
            Desktop.MainWindow = editor;
    }

    internal void HandleWindowClosed()
    {
        if (_exiting || Desktop is null)
            return;

        var hasOpenWindows = Desktop.Windows.Any(window => window.IsVisible);
        if (!hasOpenWindows && !(LinuxSettings.Current.ShowNotificationIcon && LinuxSettings.Current.KeepOpen))
            Desktop.Shutdown();
    }

    private IClassicDesktopStyleApplicationLifetime? Desktop => ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    private static NativeMenuItem CreateMenuItem(string header, EventHandler handler)
    {
        var item = new NativeMenuItem(header);
        item.Click += handler;
        return item;
    }

    private void RunTrayAction(LinuxTrayAction action, LinuxTrayWindow fallbackWindow)
    {
        var windows = Desktop?.Windows.Where(window => window is not OptionsWindow).ToArray() ?? [];
        switch (action)
        {
            case LinuxTrayAction.Nothing:
                return;
            case LinuxTrayAction.OpenWindow:
                OpenWindow(fallbackWindow);
                return;
            case LinuxTrayAction.ToggleWindows:
                if (windows.Length == 0)
                {
                    OpenWindow(fallbackWindow == LinuxTrayWindow.None ? LinuxTrayWindow.Startup : fallbackWindow);
                    return;
                }

                var minimize = windows.Any(window => window.WindowState != WindowState.Minimized && window.IsVisible);
                foreach (var window in windows)
                    window.WindowState = minimize ? WindowState.Minimized : WindowState.Normal;
                if (!minimize)
                    windows[0].Activate();
                return;
            case LinuxTrayAction.MinimizeWindows:
                foreach (var window in windows)
                    window.WindowState = WindowState.Minimized;
                return;
            case LinuxTrayAction.RestoreWindows:
                if (windows.Length == 0)
                    OpenWindow(fallbackWindow == LinuxTrayWindow.None ? LinuxTrayWindow.Startup : fallbackWindow);
                else
                    foreach (var window in windows)
                        Restore(window);
                return;
        }
    }

    private void OpenWindow(LinuxTrayWindow target)
    {
        if (Desktop is null)
            return;

        if (target is LinuxTrayWindow.Webcam or LinuxTrayWindow.Board)
        {
            OpenCaptureShell(target == LinuxTrayWindow.Board ? CaptureShellKind.Board : CaptureShellKind.Webcam);
            return;
        }

        Window? existing = target switch
        {
            LinuxTrayWindow.Editor => Desktop.Windows.OfType<MainWindow>().FirstOrDefault(),
            _ => Desktop.Windows.OfType<StartupWindow>().FirstOrDefault()
        };
        if (existing is not null)
        {
            Restore(existing);
            return;
        }

        var window = target switch
        {
            LinuxTrayWindow.Editor => CreateEditorWindow(),
            _ => new StartupWindow()
        };
        if (window is null)
            return;

        window.Closed += (_, _) => HandleWindowClosed();
        Desktop.MainWindow = window;
        window.Show();
    }

    private void ExitApplication()
    {
        _exiting = true;
        _trayIcon?.Dispose();
        Desktop?.Shutdown();
    }

    private static void Restore(Window window)
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
