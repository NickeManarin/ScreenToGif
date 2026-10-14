using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using System.Buffers.Binary;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class WebcamWindowFactAttribute : FactAttribute
{
    public WebcamWindowFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENTOGIF_WEBCAM_SMOKE") != "1")
            Skip = "Set SCREENTOGIF_WEBCAM_SMOKE=1 and provide a graphical display to run the native window drive.";
    }
}

/// <summary>Opt-in native-window drive; run under Xvfb with SCREENTOGIF_WEBCAM_SMOKE=1.</summary>
public sealed class WebcamWindowSmokeTests
{
    [WebcamWindowFact]
    public async Task Choosing1080pRecordsAndRestoresThatSize()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Assert.True(configHome is not null && Path.GetFullPath(configHome).StartsWith(Path.GetTempPath(), StringComparison.Ordinal),
            "Set XDG_CONFIG_HOME to an isolated directory under /tmp for this smoke test.");

        var root = Path.Combine(Path.GetTempPath(), $"screentogif-webcam-ui-{Guid.NewGuid():N}");
        var node = Path.Combine(root, "video0");
        Directory.CreateDirectory(node);
        File.WriteAllText(Path.Combine(node, "name"), "Controlled camera");
        var secondNode = Path.Combine(root, "video1");
        Directory.CreateDirectory(secondNode);
        File.WriteAllText(Path.Combine(secondNode, "name"), "Controlled camera");
        var catalog = new CameraDeviceCatalog(root, "/controlled", new CaptureProbe());
        await LinuxSettings.UpdateAsync(settings => settings.AskBeforeCloseEditor = false);
        var fail1080p = false;
        var drop640 = false;
        WebcamWindow NewWindow() => new(catalog, new FormatListing(() => drop640), (_, format, rate) =>
            CameraFrameStream.CreateForArguments(
                ["-nostdin", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                 fail1080p && format.Width == 1920 ? "no_such_camera_source" : $"color=c=blue:size={format.SizeArgument}:rate={rate}",
                 "-f", "rawvideo", "-pix_fmt", "bgra", "-"],
                format.Width, format.Height, "controlled camera"));

        var started = new TaskCompletionSource<WebcamWindow>(TaskCreationOptions.RunContinuationsAsynchronously);
        WebcamSmokeApp.WindowFactory = () =>
        {
            var window = NewWindow();
            started.TrySetResult(window);
            return window;
        };
        var uiThread = new Thread(() => AppBuilder.Configure<WebcamSmokeApp>().UsePlatformDetect()
            .StartWithClassicDesktopLifetime([]));
        uiThread.Start();

        try
        {
            var window = await started.Task.WaitAsync(TimeSpan.FromSeconds(20));
            var resolutions = await Dispatcher.UIThread.InvokeAsync(() => window.FindControl<ComboBox>("ResolutionSelector")!);
            var devices = await Dispatcher.UIThread.InvokeAsync(() => window.FindControl<ComboBox>("DeviceSelector")!);
            var record = await Dispatcher.UIThread.InvokeAsync(() => window.FindControl<Button>("RecordButton")!);
            var stop = await Dispatcher.UIThread.InvokeAsync(() => window.FindControl<Button>("StopButton")!);
            var opening = await Dispatcher.UIThread.InvokeAsync(() => window.FindControl<TextBlock>("StatusHeading")!);
            await UntilAsync(() => opening.Text == "Opening camera");
            await ScreenshotAsync(window, "opening");

            await UntilAsync(() => resolutions.SelectedItem is CameraResolution(1280, 720) && record.IsEnabled);
            Assert.Equal(new CameraResolution(1280, 720),
                LinuxSettings.Current.WebcamResolutions["/controlled/video0"]);
            Assert.True(await Dispatcher.UIThread.InvokeAsync(() =>
                resolutions.Items.OfType<CameraResolution>().Contains(new CameraResolution(1920, 1080))));
            await Dispatcher.UIThread.InvokeAsync(() => resolutions.SelectedItem = new CameraResolution(1920, 1080));
            await UntilAsync(() => resolutions.SelectedItem is CameraResolution(1920, 1080) && record.IsEnabled);
            Assert.Equal(new CameraResolution(1920, 1080),
                new LinuxApplicationSettingsStore().Load().WebcamResolutions["/controlled/video0"]);
            await Dispatcher.UIThread.InvokeAsync(() => devices.SelectedIndex = 1);
            await UntilAsync(() => resolutions.SelectedItem is CameraResolution(1280, 720) && record.IsEnabled);
            await Dispatcher.UIThread.InvokeAsync(() => resolutions.SelectedItem = new CameraResolution(640, 480));
            await UntilAsync(() => resolutions.SelectedItem is CameraResolution(640, 480) && record.IsEnabled);
            await Dispatcher.UIThread.InvokeAsync(() => devices.SelectedIndex = 0);
            await UntilAsync(() => resolutions.SelectedItem is CameraResolution(1920, 1080) && record.IsEnabled);
            await Dispatcher.UIThread.InvokeAsync(() => devices.SelectedIndex = 1);
            await UntilAsync(() => resolutions.SelectedItem is CameraResolution(640, 480) && record.IsEnabled);
            await Dispatcher.UIThread.InvokeAsync(() => devices.SelectedIndex = 0);
            await UntilAsync(() => resolutions.SelectedItem is CameraResolution(1920, 1080) && record.IsEnabled);
            var scale = await Dispatcher.UIThread.InvokeAsync(() => window.FindControl<Slider>("ScaleSlider")!);
            await Dispatcher.UIThread.InvokeAsync(() => scale.Value = 0.7);
            Assert.True(await Dispatcher.UIThread.InvokeAsync(() => window.Width > 960));
            await Task.Delay(100);
            await ScreenshotAsync(window, "ready-1080p");
            await Dispatcher.UIThread.InvokeAsync(() => record.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await UntilAsync(() => stop.IsEnabled);
            Assert.False(await Dispatcher.UIThread.InvokeAsync(() => resolutions.IsEnabled));
            await ScreenshotAsync(window, "recording-1080p");

            var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await Dispatcher.UIThread.InvokeAsync(() => window.Closed += (_, _) => closed.TrySetResult());
            await Dispatcher.UIThread.InvokeAsync(() => stop.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(20));

            using var recording = Assert.IsType<LoadedProject>(await Dispatcher.UIThread.InvokeAsync(window.TakeRecording));
            Assert.NotEmpty(recording.Frames);
            foreach (var frame in recording.Frames)
            {
                var header = new byte[24];
                using var file = File.OpenRead(frame.FilePath);
                Assert.Equal(header.Length, file.Read(header));
                Assert.Equal(1920, BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)));
                Assert.Equal(1080, BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4)));
            }

            var editor = await Dispatcher.UIThread.InvokeAsync(() => new MainWindow());
            await Dispatcher.UIThread.InvokeAsync(editor.Show);
            await Dispatcher.UIThread.InvokeAsync(() => editor.AdoptRecordedProjectAsync(recording));
            var timeline = await Dispatcher.UIThread.InvokeAsync(() => editor.FindControl<ListBox>("FrameListBox")!);
            Assert.True(await Dispatcher.UIThread.InvokeAsync(() => timeline.Items.OfType<EditorFrame>().Any()));
            Assert.True(await Dispatcher.UIThread.InvokeAsync(() => timeline.Items.OfType<EditorFrame>()
                .All(frame => frame.SourcePixelSize == new PixelSize(1920, 1080))));
            await Dispatcher.UIThread.InvokeAsync(editor.Close);

            fail1080p = true;
            var reopened = await Dispatcher.UIThread.InvokeAsync(NewWindow);
            await Dispatcher.UIThread.InvokeAsync(reopened.Show);
            var reopenedSizes = await Dispatcher.UIThread.InvokeAsync(() => reopened.FindControl<ComboBox>("ResolutionSelector")!);
            var reopenedDevices = await Dispatcher.UIThread.InvokeAsync(() => reopened.FindControl<ComboBox>("DeviceSelector")!);
            await UntilAsync(() => reopenedSizes.SelectedItem is CameraResolution(1920, 1080));
            var reopenedRecord = await Dispatcher.UIThread.InvokeAsync(() => reopened.FindControl<Button>("RecordButton")!);
            var heading = await Dispatcher.UIThread.InvokeAsync(() => reopened.FindControl<TextBlock>("StatusHeading")!);
            await UntilAsync(() => heading.Text == "Camera unavailable" && reopenedSizes.IsEnabled && !reopenedRecord.IsEnabled);
            await ScreenshotAsync(reopened, "failed-1080p");
            await Dispatcher.UIThread.InvokeAsync(() => reopenedDevices.SelectedIndex = 1);
            await UntilAsync(() => reopenedSizes.SelectedItem is CameraResolution(640, 480) && reopenedRecord.IsEnabled);
            drop640 = true;
            var refresh = await Dispatcher.UIThread.InvokeAsync(() => reopened.FindControl<Button>("RefreshButton")!);
            await Dispatcher.UIThread.InvokeAsync(() => refresh.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await UntilAsync(() => reopenedDevices.SelectedIndex == 0 && heading.Text == "Camera unavailable"
                && reopenedDevices.IsEnabled);
            await Dispatcher.UIThread.InvokeAsync(() => reopenedDevices.SelectedIndex = 1);
            await UntilAsync(() => reopenedSizes.SelectedItem is CameraResolution(1280, 720) && reopenedRecord.IsEnabled);
            Assert.Equal(new CameraResolution(1280, 720),
                LinuxSettings.Current.WebcamResolutions["/controlled/video1"]);
            await Dispatcher.UIThread.InvokeAsync(() => reopenedDevices.SelectedIndex = 0);
            await UntilAsync(() => heading.Text == "Camera unavailable" && reopenedSizes.SelectedItem is CameraResolution(1920, 1080));
            await Dispatcher.UIThread.InvokeAsync(() => reopenedSizes.SelectedItem = new CameraResolution(1280, 720));
            await UntilAsync(() => reopenedSizes.SelectedItem is CameraResolution(1280, 720) && reopenedRecord.IsEnabled);
            await ScreenshotAsync(reopened, "recovered-720p");
            var reopenedStop = await Dispatcher.UIThread.InvokeAsync(() => reopened.FindControl<Button>("StopButton")!);
            await Dispatcher.UIThread.InvokeAsync(() => reopenedRecord.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await UntilAsync(() => reopenedStop.IsEnabled);
            await ScreenshotAsync(reopened, "recording-720p");
            Assert.True(await Dispatcher.UIThread.InvokeAsync(() =>
                reopenedStop.TranslatePoint(new Point(reopenedStop.Bounds.Width, 0), reopened)!.Value.X
                <= reopened.ClientSize.Width));
            var reopenedClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await Dispatcher.UIThread.InvokeAsync(() => reopened.Closed += (_, _) => reopenedClosed.TrySetResult());
            await Dispatcher.UIThread.InvokeAsync(() => reopenedStop.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await reopenedClosed.Task.WaitAsync(TimeSpan.FromSeconds(20));
            using var secondRecording = await Dispatcher.UIThread.InvokeAsync(reopened.TakeRecording);

            var empty = await Dispatcher.UIThread.InvokeAsync(() => new WebcamWindow(
                new CameraDeviceCatalog(Path.Combine(root, "absent")), new FormatListing(() => false)));
            await Dispatcher.UIThread.InvokeAsync(empty.Show);
            var emptyHeading = await Dispatcher.UIThread.InvokeAsync(() => empty.FindControl<TextBlock>("StatusHeading")!);
            await UntilAsync(() => emptyHeading.Text == "No camera found");
            await ScreenshotAsync(empty, "no-camera");
            await Dispatcher.UIThread.InvokeAsync(empty.Close);
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).Shutdown());
            uiThread.Join(TimeSpan.FromSeconds(20));
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task ScreenshotAsync(Window window, string name)
    {
        var directory = Environment.GetEnvironmentVariable("SCREENTOGIF_WEBCAM_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory))
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Directory.CreateDirectory(directory);
            using var image = new RenderTargetBitmap(
                new PixelSize((int)Math.Ceiling(window.Bounds.Width), (int)Math.Ceiling(window.Bounds.Height)),
                new Vector(96, 96));
            image.Render(window);
            image.Save(Path.Combine(directory, name + ".png"), PngBitmapEncoderOptions.Default);
        });
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!timeout.IsCancellationRequested)
        {
            if (await Dispatcher.UIThread.InvokeAsync(condition))
                return;
            await Task.Delay(100, timeout.Token);
        }

        throw new TimeoutException("The webcam control did not reach its expected state.");
    }

    private sealed class CaptureProbe : ICameraCapabilityProbe
    {
        public CameraProbeOutcome Probe(string _) => CameraProbeOutcome.VideoCapture;
    }

    private sealed class FormatListing(Func<bool> drop640) : IFfmpegTool
    {
        private const string FirstListing = """
            [video4linux2,v4l2 @ 0x123] Compressed: mjpeg : Motion-JPEG : 1920x1080 1280x720
            """;
        private const string SecondListing = """
            [video4linux2,v4l2 @ 0x123] Compressed: mjpeg : Motion-JPEG : 1920x1080 1280x720 640x480
            """;

        public async Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            await Task.Delay(400, cancellationToken);
            return new ProcessResult(1, string.Empty,
                arguments.Last() == "/controlled/video1" && !drop640() ? SecondListing : FirstListing);
        }

        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> _, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RunFfmpegCheckedAsync(IEnumerable<string> _, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> _, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

public sealed class WebcamSmokeApp : Application
{
    public static Func<WebcamWindow>? WindowFactory { get; set; }

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://ScreenToGif.Linux/"))
        {
            Source = new Uri("avares://ScreenToGif.Linux/Themes/RibbonButton.axaml")
        });
        Styles.Add(new StyleInclude(new Uri("avares://ScreenToGif.Linux/"))
        {
            Source = new Uri("avares://ScreenToGif.Linux/Themes/RibbonTabItem.axaml")
        });
        Styles.Add(new StyleInclude(new Uri("avares://ScreenToGif.Linux/"))
        {
            Source = new Uri("avares://ScreenToGif.Linux/Themes/CaptureShell.axaml")
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = WindowFactory!();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
