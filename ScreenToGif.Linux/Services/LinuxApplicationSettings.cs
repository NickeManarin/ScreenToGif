using ScreenToGif.Linux.Services.Capture;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenToGif.Linux.Services;

public enum LinuxStartupWindow
{
    Startup,
    Recorder,
    Webcam,
    Board,
    Editor
}

public enum LinuxAppTheme
{
    Light,
    Medium,
    Dark,
    FollowSystem
}

public enum LinuxTrayAction
{
    Nothing,
    OpenWindow,
    ToggleWindows,
    MinimizeWindows,
    RestoreWindows
}

public enum LinuxTrayWindow
{
    None,
    Startup,
    Recorder,
    Webcam,
    Board,
    Editor
}

public sealed class LinuxApplicationSettings
{
    public bool SingleInstance { get; set; } = true;
    public bool StartMinimized { get; set; }
    public LinuxStartupWindow StartupWindow { get; set; } = LinuxStartupWindow.Startup;
    public LinuxAppTheme Theme { get; set; } = LinuxAppTheme.Dark;
    public bool ShowNotificationIcon { get; set; }
    public bool KeepOpen { get; set; } = true;
    public LinuxTrayAction LeftClickAction { get; set; } = LinuxTrayAction.ToggleWindows;
    public LinuxTrayWindow LeftClickWindow { get; set; } = LinuxTrayWindow.Startup;
    public LinuxTrayAction DoubleLeftClickAction { get; set; } = LinuxTrayAction.OpenWindow;
    public LinuxTrayWindow DoubleLeftClickWindow { get; set; } = LinuxTrayWindow.Editor;
    public LinuxTrayAction MiddleClickAction { get; set; } = LinuxTrayAction.MinimizeWindows;
    public LinuxTrayWindow MiddleClickWindow { get; set; } = LinuxTrayWindow.None;
    public bool DisableHardwareAcceleration { get; set; }
    public bool AskBeforeDeleteFrames { get; set; } = true;
    public bool AskBeforeDiscardProject { get; set; } = true;
    public bool AskBeforeCloseEditor { get; set; } = true;
    public bool DropFramesWhenBehind { get; set; }
    public int UndoLimit { get; set; } = 50;
    public bool DeleteCacheOnClose { get; set; }
    public bool RemoveOldProjects { get; set; } = true;
    public int ProjectRetentionDays { get; set; } = 5;
    public string FfmpegPath { get; set; } = "ffmpeg";
    public int WebcamFps { get; set; } = 15;
    public Dictionary<string, CameraResolution> WebcamResolutions { get; set; } = new(StringComparer.Ordinal);

    // Recorder. Defaults mirror the Windows recorder so a user moving between them is not surprised.
    public RecorderCaptureMode RecorderCaptureMode { get; set; } = RecorderCaptureMode.PerSecond;
    public int RecorderFramesPerSecond { get; set; } = 15;
    public bool RecorderFixedFrameRate { get; set; }
    public bool RecorderShowCursor { get; set; } = true;
    public bool RecorderPreStart { get; set; }
    public int RecorderPreStartSeconds { get; set; } = 3;
    public int RecorderManualPlaybackDelayMs { get; set; } = 1000;
    public bool RecorderAskBeforeDiscarding { get; set; } = true;
    public bool RecorderRememberSize { get; set; } = true;
    public bool RecorderRememberPosition { get; set; } = true;
    public int RecorderWidth { get; set; } = 502;
    public int RecorderHeight { get; set; } = 203;

    /// <summary>Last recorder position in physical desktop pixels, or null when it has never been placed.</summary>
    public int? RecorderLeft { get; set; }

    public int? RecorderTop { get; set; }

    // Board. Defaults mirror the Windows Board.
    public string BoardBrushColor { get; set; } = "#000000";
    public int BoardBrushWidth { get; set; } = 10;
    public int BoardBrushHeight { get; set; } = 10;
    public BoardStylusTip BoardBrushTip { get; set; } = BoardStylusTip.Ellipse;
    public bool BoardFitToCurve { get; set; }
    public bool BoardHighlighter { get; set; }
    public int BoardEraserWidth { get; set; } = 10;
    public int BoardEraserHeight { get; set; } = 10;
    public BoardStylusTip BoardEraserTip { get; set; } = BoardStylusTip.Rectangle;
    public int BoardWidth { get; set; } = 798;
    public int BoardHeight { get; set; } = 387;
    public int BoardFps { get; set; } = 15;

    public LinuxApplicationSettings Copy() => new()
    {
        SingleInstance = SingleInstance,
        StartMinimized = StartMinimized,
        StartupWindow = StartupWindow,
        Theme = Theme,
        ShowNotificationIcon = ShowNotificationIcon,
        KeepOpen = KeepOpen,
        LeftClickAction = LeftClickAction,
        LeftClickWindow = LeftClickWindow,
        DoubleLeftClickAction = DoubleLeftClickAction,
        DoubleLeftClickWindow = DoubleLeftClickWindow,
        MiddleClickAction = MiddleClickAction,
        MiddleClickWindow = MiddleClickWindow,
        DisableHardwareAcceleration = DisableHardwareAcceleration,
        AskBeforeDeleteFrames = AskBeforeDeleteFrames,
        AskBeforeDiscardProject = AskBeforeDiscardProject,
        AskBeforeCloseEditor = AskBeforeCloseEditor,
        DropFramesWhenBehind = DropFramesWhenBehind,
        UndoLimit = UndoLimit,
        DeleteCacheOnClose = DeleteCacheOnClose,
        RemoveOldProjects = RemoveOldProjects,
        ProjectRetentionDays = ProjectRetentionDays,
        FfmpegPath = FfmpegPath,
        RecorderCaptureMode = RecorderCaptureMode,
        RecorderFramesPerSecond = RecorderFramesPerSecond,
        RecorderFixedFrameRate = RecorderFixedFrameRate,
        RecorderShowCursor = RecorderShowCursor,
        RecorderPreStart = RecorderPreStart,
        RecorderPreStartSeconds = RecorderPreStartSeconds,
        RecorderManualPlaybackDelayMs = RecorderManualPlaybackDelayMs,
        RecorderAskBeforeDiscarding = RecorderAskBeforeDiscarding,
        RecorderRememberSize = RecorderRememberSize,
        RecorderRememberPosition = RecorderRememberPosition,
        RecorderWidth = RecorderWidth,
        RecorderHeight = RecorderHeight,
        RecorderLeft = RecorderLeft,
        RecorderTop = RecorderTop,
        WebcamFps = WebcamFps,
        WebcamResolutions = new Dictionary<string, CameraResolution>(WebcamResolutions, StringComparer.Ordinal),
        BoardBrushColor = BoardBrushColor,
        BoardBrushWidth = BoardBrushWidth,
        BoardBrushHeight = BoardBrushHeight,
        BoardBrushTip = BoardBrushTip,
        BoardFitToCurve = BoardFitToCurve,
        BoardHighlighter = BoardHighlighter,
        BoardEraserWidth = BoardEraserWidth,
        BoardEraserHeight = BoardEraserHeight,
        BoardEraserTip = BoardEraserTip,
        BoardWidth = BoardWidth,
        BoardHeight = BoardHeight,
        BoardFps = BoardFps
    };
}

public sealed class LinuxApplicationSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LinuxApplicationSettingsStore(string? path = null)
    {
        Path = path ?? GetDefaultPath();
    }

    public string Path { get; }

    public LinuxApplicationSettings Load()
    {
        try
        {
            if (!File.Exists(Path))
                return Normalize(new LinuxApplicationSettings());

            return Normalize(JsonSerializer.Deserialize<LinuxApplicationSettings>(File.ReadAllText(Path), JsonOptions)
                             ?? new LinuxApplicationSettings());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Normalize(new LinuxApplicationSettings());
        }
    }

    /// <summary>Brings a value that a hand-edited or outdated file may hold out of range back into it.</summary>
    private static LinuxApplicationSettings Normalize(LinuxApplicationSettings settings)
    {
        settings.WebcamFps = Math.Clamp(settings.WebcamFps, WebcamRecordingSession.MinimumFps, WebcamRecordingSession.MaximumFps);
        settings.WebcamResolutions = settings.WebcamResolutions is null
            ? new(StringComparer.Ordinal)
            : new Dictionary<string, CameraResolution>(
                settings.WebcamResolutions.Where(entry => !string.IsNullOrWhiteSpace(entry.Key)
                    && entry.Value is { Width: > 0, Height: > 0 }), StringComparer.Ordinal);
        return settings;
    }

    public async Task SaveAsync(LinuxApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        var directory = System.IO.Path.GetDirectoryName(Path)
                        ?? throw new InvalidOperationException("The settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(settings, JsonOptions),
                cancellationToken);
            File.Move(temporaryPath, Path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static string GetDefaultPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
            configHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return System.IO.Path.Combine(configHome, "ScreenToGif", "settings.json");
    }
}

public static class LinuxSettings
{
    private static readonly LinuxApplicationSettingsStore Store = new();

    private static Task _updates = Task.CompletedTask;

    public static LinuxApplicationSettings Current { get; private set; } = Store.Load();

    /// <summary>
    /// Applies one change to the stored settings, on a single chain so two changes arriving close
    /// together cannot each copy the store before the other's write lands and drop a field. The
    /// copy is taken inside the chain for that reason. The returned task carries this change's
    /// own failure, and a failed write does not stop later ones.
    /// </summary>
    public static Task UpdateAsync(Action<LinuxApplicationSettings> change)
    {
        _updates = _updates.ContinueWith(_ =>
        {
            var updated = Current.Copy();
            change(updated);
            return SaveAsync(updated);
        }, TaskScheduler.Default).Unwrap();
        return _updates;
    }

    public static async Task SaveAsync(LinuxApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        await Store.SaveAsync(settings, cancellationToken);
        Current = settings.Copy();
        ApplyEnvironment();
    }

    public static void ApplyEnvironment()
    {
        var ffmpeg = string.IsNullOrWhiteSpace(Current.FfmpegPath) ? "ffmpeg" : Current.FfmpegPath.Trim();
        Environment.SetEnvironmentVariable("SCREENTOGIF_FFMPEG", ffmpeg);
        if (Path.IsPathRooted(ffmpeg))
        {
            var directory = Path.GetDirectoryName(ffmpeg);
            if (!string.IsNullOrWhiteSpace(directory))
                Environment.SetEnvironmentVariable("SCREENTOGIF_FFPROBE", Path.Combine(directory, "ffprobe"));
        }
        else
            Environment.SetEnvironmentVariable("SCREENTOGIF_FFPROBE", "ffprobe");
    }
}
