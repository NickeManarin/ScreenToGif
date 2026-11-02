using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using System.Text.Json;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class LinuxApplicationSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"screentogif-settings-{Guid.NewGuid():N}");

    [Fact]
    public async Task CameraResolutionsRoundTripSeparatelyAndCopyDoesNotShareTheMap()
    {
        var store = new LinuxApplicationSettingsStore(Path.Combine(_directory, "settings.json"));
        var original = new LinuxApplicationSettings { WebcamFps = 24 };
        original.WebcamResolutions["camera-a"] = new CameraResolution(1920, 1080);
        original.WebcamResolutions["camera-b"] = new CameraResolution(640, 480);

        await store.SaveAsync(original);
        var loaded = store.Load();
        var copied = loaded.Copy();
        copied.WebcamResolutions["camera-a"] = new CameraResolution(3840, 2160);

        Assert.Equal(new CameraResolution(1920, 1080), loaded.WebcamResolutions["camera-a"]);
        Assert.Equal(new CameraResolution(640, 480), loaded.WebcamResolutions["camera-b"]);
        Assert.Equal(24, loaded.WebcamFps);
    }

    [Fact]
    public void OlderSettingsHaveNoAutomaticCameraChoiceAndKeepTheirValues()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """{"WebcamFps": 30, "KeepOpen": false}""");

        var settings = new LinuxApplicationSettingsStore(path).Load();

        Assert.Empty(settings.WebcamResolutions);
        Assert.Equal(30, settings.WebcamFps);
        Assert.False(settings.KeepOpen);
    }

    [Fact]
    public async Task EveryRecorderSettingRoundTripsThroughTheSettingsFile()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);
        var expected = new LinuxApplicationSettings
        {
            RecorderCaptureMode = RecorderCaptureMode.PerHour,
            RecorderFramesPerSecond = 42,
            RecorderFixedFrameRate = true,
            RecorderShowCursor = false,
            RecorderPreStart = true,
            RecorderPreStartSeconds = 7,
            RecorderManualPlaybackDelayMs = 250,
            RecorderAskBeforeDiscarding = false,
            RecorderRememberSize = false,
            RecorderRememberPosition = false,
            RecorderWidth = 1280,
            RecorderHeight = 720,
            RecorderLeft = 314,
            RecorderTop = 271
        };

        await store.SaveAsync(expected);
        var actual = store.Load();

        AssertRecorderSettingsMatch(expected, actual);
    }

    [Fact]
    public void CopyCarriesEveryRecorderSetting()
    {
        // LinuxSettings.SaveAsync republishes through Copy, so a field missing here is a field
        // that round-trips through the file and is still lost in the running application.
        var expected = new LinuxApplicationSettings
        {
            RecorderCaptureMode = RecorderCaptureMode.Manual,
            RecorderFramesPerSecond = 3,
            RecorderFixedFrameRate = true,
            RecorderShowCursor = false,
            RecorderPreStart = true,
            RecorderPreStartSeconds = 9,
            RecorderManualPlaybackDelayMs = 1500,
            RecorderAskBeforeDiscarding = false,
            RecorderRememberSize = false,
            RecorderRememberPosition = false,
            RecorderWidth = 640,
            RecorderHeight = 480,
            RecorderLeft = -12,
            RecorderTop = 0
        };

        AssertRecorderSettingsMatch(expected, expected.Copy());
    }

    [Fact]
    public void ASettingsFileWrittenBeforeTheRecorderExistedLoadsWithTheWindowsDefaults()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """
            {
              "SingleInstance": false,
              "UndoLimit": 25,
              "FfmpegPath": "ffmpeg"
            }
            """);

        var loaded = new LinuxApplicationSettingsStore(path).Load();

        Assert.False(loaded.SingleInstance);
        Assert.Equal(25, loaded.UndoLimit);
        AssertRecorderDefaultsMirrorWindows(loaded);
    }

    [Fact]
    public void ACorruptSettingsFileFallsBackToTheRecorderDefaults()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{ this is not json");

        AssertRecorderDefaultsMirrorWindows(new LinuxApplicationSettingsStore(path).Load());
    }

    /// <summary>
    /// Pins the defaults to the values the Windows recorder ships, rather than to whatever the
    /// Linux type currently declares: comparing a loaded file against a fresh instance passes
    /// however the declarations drift, which is the one thing this assertion exists to catch.
    /// </summary>
    private static void AssertRecorderDefaultsMirrorWindows(LinuxApplicationSettings actual)
    {
        Assert.Equal(RecorderCaptureMode.PerSecond, actual.RecorderCaptureMode);
        Assert.Equal(15, actual.RecorderFramesPerSecond);
        Assert.False(actual.RecorderFixedFrameRate);
        Assert.True(actual.RecorderShowCursor);
        Assert.False(actual.RecorderPreStart);
        Assert.Equal(3, actual.RecorderPreStartSeconds);
        Assert.Equal(1000, actual.RecorderManualPlaybackDelayMs);
        Assert.True(actual.RecorderAskBeforeDiscarding);
        Assert.True(actual.RecorderRememberSize);
        Assert.True(actual.RecorderRememberPosition);
        Assert.Equal(502, actual.RecorderWidth);
        Assert.Equal(203, actual.RecorderHeight);
        Assert.Null(actual.RecorderLeft);
        Assert.Null(actual.RecorderTop);
    }

    [Fact]
    public async Task AnUnplacedRecorderPersistsNoPositionRatherThanAFalseOne()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);

        await store.SaveAsync(new LinuxApplicationSettings());

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("RecorderLeft").ValueKind);
        Assert.Null(store.Load().RecorderLeft);
        Assert.Null(store.Load().RecorderTop);
    }

    private static void AssertRecorderSettingsMatch(LinuxApplicationSettings expected, LinuxApplicationSettings actual)
    {
        Assert.Equal(expected.RecorderCaptureMode, actual.RecorderCaptureMode);
        Assert.Equal(expected.RecorderFramesPerSecond, actual.RecorderFramesPerSecond);
        Assert.Equal(expected.RecorderFixedFrameRate, actual.RecorderFixedFrameRate);
        Assert.Equal(expected.RecorderShowCursor, actual.RecorderShowCursor);
        Assert.Equal(expected.RecorderPreStart, actual.RecorderPreStart);
        Assert.Equal(expected.RecorderPreStartSeconds, actual.RecorderPreStartSeconds);
        Assert.Equal(expected.RecorderManualPlaybackDelayMs, actual.RecorderManualPlaybackDelayMs);
        Assert.Equal(expected.RecorderAskBeforeDiscarding, actual.RecorderAskBeforeDiscarding);
        Assert.Equal(expected.RecorderRememberSize, actual.RecorderRememberSize);
        Assert.Equal(expected.RecorderRememberPosition, actual.RecorderRememberPosition);
        Assert.Equal(expected.RecorderWidth, actual.RecorderWidth);
        Assert.Equal(expected.RecorderHeight, actual.RecorderHeight);
        Assert.Equal(expected.RecorderLeft, actual.RecorderLeft);
        Assert.Equal(expected.RecorderTop, actual.RecorderTop);
    }

    [Fact]
    public async Task SettingsRoundTripEverySupportedApplicationChoice()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);
        var expected = new LinuxApplicationSettings
        {
            SingleInstance = false,
            StartMinimized = true,
            StartupWindow = LinuxStartupWindow.Editor,
            Theme = LinuxAppTheme.FollowSystem,
            ShowNotificationIcon = true,
            KeepOpen = false,
            LeftClickAction = LinuxTrayAction.OpenWindow,
            LeftClickWindow = LinuxTrayWindow.Editor,
            DoubleLeftClickAction = LinuxTrayAction.Nothing,
            DoubleLeftClickWindow = LinuxTrayWindow.None,
            MiddleClickAction = LinuxTrayAction.RestoreWindows,
            MiddleClickWindow = LinuxTrayWindow.Startup,
            DisableHardwareAcceleration = true,
            AskBeforeDeleteFrames = false,
            AskBeforeDiscardProject = false,
            AskBeforeCloseEditor = false,
            DropFramesWhenBehind = true,
            UndoLimit = 12,
            DeleteCacheOnClose = true,
            RemoveOldProjects = false,
            ProjectRetentionDays = 14,
            FfmpegPath = "/opt/ffmpeg/bin/ffmpeg",
            WebcamFps = 24,
            BoardBrushColor = "#3366FF",
            BoardBrushWidth = 23,
            BoardBrushHeight = 7,
            BoardBrushTip = BoardStylusTip.Rectangle,
            BoardFitToCurve = true,
            BoardHighlighter = true,
            BoardEraserWidth = 41,
            BoardEraserHeight = 12,
            BoardEraserTip = BoardStylusTip.Ellipse,
            BoardWidth = 1024,
            BoardHeight = 640,
            BoardFps = 24
        };

        await store.SaveAsync(expected);
        var actual = store.Load();
        var persisted = await File.ReadAllTextAsync(path);

        Assert.DoesNotContain("NotifyBeforeClosing", persisted, StringComparison.Ordinal);
        Assert.Equal(expected.SingleInstance, actual.SingleInstance);
        Assert.Equal(expected.StartMinimized, actual.StartMinimized);
        Assert.Equal(expected.StartupWindow, actual.StartupWindow);
        Assert.Equal(expected.Theme, actual.Theme);
        Assert.Equal(expected.ShowNotificationIcon, actual.ShowNotificationIcon);
        Assert.Equal(expected.KeepOpen, actual.KeepOpen);
        Assert.Equal(expected.LeftClickAction, actual.LeftClickAction);
        Assert.Equal(expected.LeftClickWindow, actual.LeftClickWindow);
        Assert.Equal(expected.DoubleLeftClickAction, actual.DoubleLeftClickAction);
        Assert.Equal(expected.DoubleLeftClickWindow, actual.DoubleLeftClickWindow);
        Assert.Equal(expected.MiddleClickAction, actual.MiddleClickAction);
        Assert.Equal(expected.MiddleClickWindow, actual.MiddleClickWindow);
        Assert.Equal(expected.DisableHardwareAcceleration, actual.DisableHardwareAcceleration);
        Assert.Equal(expected.AskBeforeDeleteFrames, actual.AskBeforeDeleteFrames);
        Assert.Equal(expected.AskBeforeDiscardProject, actual.AskBeforeDiscardProject);
        Assert.Equal(expected.AskBeforeCloseEditor, actual.AskBeforeCloseEditor);
        Assert.Equal(expected.DropFramesWhenBehind, actual.DropFramesWhenBehind);
        Assert.Equal(expected.UndoLimit, actual.UndoLimit);
        Assert.Equal(expected.DeleteCacheOnClose, actual.DeleteCacheOnClose);
        Assert.Equal(expected.RemoveOldProjects, actual.RemoveOldProjects);
        Assert.Equal(expected.ProjectRetentionDays, actual.ProjectRetentionDays);
        Assert.Equal(expected.FfmpegPath, actual.FfmpegPath);
        Assert.Equal(expected.WebcamFps, actual.WebcamFps);
        Assert.Equal(expected.BoardBrushColor, actual.BoardBrushColor);
        Assert.Equal(expected.BoardBrushWidth, actual.BoardBrushWidth);
        Assert.Equal(expected.BoardBrushHeight, actual.BoardBrushHeight);
        Assert.Equal(expected.BoardBrushTip, actual.BoardBrushTip);
        Assert.Equal(expected.BoardFitToCurve, actual.BoardFitToCurve);
        Assert.Equal(expected.BoardHighlighter, actual.BoardHighlighter);
        Assert.Equal(expected.BoardEraserWidth, actual.BoardEraserWidth);
        Assert.Equal(expected.BoardEraserHeight, actual.BoardEraserHeight);
        Assert.Equal(expected.BoardEraserTip, actual.BoardEraserTip);
        Assert.Equal(expected.BoardWidth, actual.BoardWidth);
        Assert.Equal(expected.BoardHeight, actual.BoardHeight);
        Assert.Equal(expected.BoardFps, actual.BoardFps);
    }

    [Fact]
    public void CopyCarriesEveryBoardChoice()
    {
        var original = new LinuxApplicationSettings
        {
            BoardBrushColor = "#ABCDEF",
            BoardBrushWidth = 31,
            BoardBrushHeight = 9,
            BoardBrushTip = BoardStylusTip.Rectangle,
            BoardFitToCurve = true,
            BoardHighlighter = true,
            BoardEraserWidth = 55,
            BoardEraserHeight = 3,
            BoardEraserTip = BoardStylusTip.Ellipse,
            BoardWidth = 1234,
            BoardHeight = 567,
            BoardFps = 42
        };

        var copy = original.Copy();

        Assert.Equal(original.BoardBrushColor, copy.BoardBrushColor);
        Assert.Equal(original.BoardBrushWidth, copy.BoardBrushWidth);
        Assert.Equal(original.BoardBrushHeight, copy.BoardBrushHeight);
        Assert.Equal(original.BoardBrushTip, copy.BoardBrushTip);
        Assert.Equal(original.BoardFitToCurve, copy.BoardFitToCurve);
        Assert.Equal(original.BoardHighlighter, copy.BoardHighlighter);
        Assert.Equal(original.BoardEraserWidth, copy.BoardEraserWidth);
        Assert.Equal(original.BoardEraserHeight, copy.BoardEraserHeight);
        Assert.Equal(original.BoardEraserTip, copy.BoardEraserTip);
        Assert.Equal(original.BoardWidth, copy.BoardWidth);
        Assert.Equal(original.BoardHeight, copy.BoardHeight);
        Assert.Equal(original.BoardFps, copy.BoardFps);
    }

    [Fact]
    public void AnOlderSettingsFileStillLoadsAndTakesTheBoardDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, """{ "SingleInstance": false, "UndoLimit": 7 }""");

        var settings = new LinuxApplicationSettingsStore(path).Load();

        Assert.False(settings.SingleInstance);
        Assert.Equal(7, settings.UndoLimit);
        AssertBoardDefaults(settings);
    }

    [Fact]
    public void CorruptSettingsFallBackToSafeDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "{not-json");

        var settings = new LinuxApplicationSettingsStore(path).Load();

        Assert.True(settings.SingleInstance);
        Assert.Equal(LinuxAppTheme.Dark, settings.Theme);
        Assert.Equal(LinuxStartupWindow.Startup, settings.StartupWindow);
        Assert.False(settings.StartMinimized);
        Assert.Equal(15, settings.WebcamFps);
        AssertBoardDefaults(settings);
    }

    [Fact]
    public void WebcamFpsDefaultsToFifteen()
    {
        Assert.Equal(15, new LinuxApplicationSettings().WebcamFps);
    }

    [Fact]
    public void SettingsWrittenBeforeWebcamFpsExistedLoadAsFifteen()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, """{ "Theme": "Light", "UndoLimit": 12 }""");

        var settings = new LinuxApplicationSettingsStore(path).Load();

        Assert.Equal(LinuxAppTheme.Light, settings.Theme);
        Assert.Equal(12, settings.UndoLimit);
        Assert.Equal(15, settings.WebcamFps);
    }

    [Theory]
    [InlineData(0, WebcamRecordingSession.MinimumFps)]
    [InlineData(-5, WebcamRecordingSession.MinimumFps)]
    [InlineData(999, WebcamRecordingSession.MaximumFps)]
    public void StoredWebcamFpsOutsideTheSupportedRangeIsClampedOnLoad(int stored, int expected)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, $$"""{ "WebcamFps": {{stored}} }""");

        var settings = new LinuxApplicationSettingsStore(path).Load();

        Assert.Equal(expected, settings.WebcamFps);
    }

    [Fact]
    public async Task WebcamFpsRoundTripsThroughSaveAndLoad()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);

        await store.SaveAsync(new LinuxApplicationSettings { WebcamFps = 30 });

        Assert.Equal(30, store.Load().WebcamFps);
    }

    [Fact]
    public void CopyCarriesWebcamFps()
    {
        Assert.Equal(48, new LinuxApplicationSettings { WebcamFps = 48 }.Copy().WebcamFps);
    }

    [Fact]
    public async Task ASettingsFileThatCannotBeWrittenSurfacesTheFailureAndKeepsTheOldFile()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);
        await store.SaveAsync(new LinuxApplicationSettings { BoardFps = 12 });

        // A directory where the temporary file must go makes the write fail the way a full or
        // read-only disk does, without depending on the permissions of the machine running the test.
        Directory.CreateDirectory(path + ".tmp");

        var failure = await Record.ExceptionAsync(() => store.SaveAsync(new LinuxApplicationSettings { BoardFps = 40 }));

        // The two exception types the Board reports a failed settings write under.
        Assert.True(failure is IOException or UnauthorizedAccessException, $"Unexpected failure: {failure}");
        Assert.Equal(12, store.Load().BoardFps);
    }

    [Fact]
    public void MissingSettingsFallBackToTheBoardDefaults() =>
        AssertBoardDefaults(new LinuxApplicationSettingsStore(Path.Combine(_directory, "absent.json")).Load());

    private static void AssertBoardDefaults(LinuxApplicationSettings settings)
    {
        Assert.Equal("#000000", settings.BoardBrushColor);
        Assert.Equal(10, settings.BoardBrushWidth);
        Assert.Equal(10, settings.BoardBrushHeight);
        Assert.Equal(BoardStylusTip.Ellipse, settings.BoardBrushTip);
        Assert.False(settings.BoardFitToCurve);
        Assert.False(settings.BoardHighlighter);
        Assert.Equal(10, settings.BoardEraserWidth);
        Assert.Equal(10, settings.BoardEraserHeight);
        Assert.Equal(BoardStylusTip.Rectangle, settings.BoardEraserTip);
        Assert.Equal(15, settings.BoardFps);
        Assert.True(settings.RecorderAskBeforeDiscarding);
    }

    [Fact]
    public async Task AutostartWritesManagedDesktopEntryAndCanRemoveIt()
    {
        var service = new LinuxAutostartService(_directory, () => "\"/opt/Screen To Gif/ScreenToGif.Linux\"");

        await service.SetEnabledAsync(true);

        Assert.True(service.IsEnabled());
        var desktopEntry = File.ReadAllText(service.DesktopFilePath);
        Assert.Contains("Exec=\"/opt/Screen To Gif/ScreenToGif.Linux\"", desktopEntry);
        Assert.Contains("X-ScreenToGif-Managed=true", desktopEntry);

        await service.SetEnabledAsync(false);

        Assert.False(File.Exists(service.DesktopFilePath));
    }

    [Fact]
    public async Task AutostartDoesNotOverwriteOrDeleteAnUnmanagedEntry()
    {
        var service = new LinuxAutostartService(_directory, () => "\"/opt/screentogif\"");
        Directory.CreateDirectory(Path.GetDirectoryName(service.DesktopFilePath)!);
        await File.WriteAllTextAsync(service.DesktopFilePath, "[Desktop Entry]\nName=User owned\n");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetEnabledAsync(true));
        await service.SetEnabledAsync(false);

        Assert.Contains("User owned", await File.ReadAllTextAsync(service.DesktopFilePath));
    }

    [Theory]
    [InlineData("/opt/Screen To Gif/app", "\"/opt/Screen To Gif/app\"")]
    [InlineData("/opt/a\\b\"c`d$e", "\"/opt/a\\\\b\\\"c\\`d\\$e\"")]
    public void DesktopArgumentsAreQuotedWithoutShellExpansion(string value, string expected)
    {
        Assert.Equal(expected, LinuxAutostartService.QuoteDesktopArgument(value));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
