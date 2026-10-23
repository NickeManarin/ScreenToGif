using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Controls;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class WaylandRecordingFactAttribute : FactAttribute
{
    public WaylandRecordingFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_RECORDING_SMOKE") != "1")
            Skip = "Opt in on an unlocked native Wayland desktop with a test-owned source and portal consent.";
    }
}

public sealed class SavedNativeProjectFactAttribute : FactAttribute
{
    public SavedNativeProjectFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_EDITOR_REPLAY_SMOKE") != "1")
            Skip = "Opt in to the separate UI host for an existing test-owned native recording.";
    }
}

public sealed partial class DesktopWindowSmokeTests
{
    [WaylandRecordingFact]
    public async Task Native_Wayland_source_records_into_editor_and_exports_measured_delays()
    {
        Assert.Equal("wayland", Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"));
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Assert.True(config is not null && Path.GetFullPath(config).StartsWith(Path.GetTempPath(), StringComparison.Ordinal));
        var directory = Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR")!;
        Assert.False(string.IsNullOrWhiteSpace(directory));
        Directory.CreateDirectory(directory);
        var script = Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_SOURCE_SCRIPT")!;
        Assert.True(File.Exists(script), "Provide the absolute path to scripts/wayland-test-source.py.");
        var manual = Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_MANUAL") == "1";
        var cursorProbe = Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_CURSOR_PROBE") == "1";
        var sourceStart = new ProcessStartInfo("/usr/bin/python3")
            { RedirectStandardOutput = true, RedirectStandardError = true };
        sourceStart.ArgumentList.Add(script);
        sourceStart.Environment["GDK_BACKEND"] = "wayland";
        using var source = Process.Start(sourceStart)!;
        var sourceError = source.StandardError.ReadToEndAsync();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var uiFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread? uiThread = null;
        try
        {
            Assert.Equal("NATIVE_WAYLAND_SOURCE_READY", await source.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20)));
            await LinuxSettings.UpdateAsync(settings =>
            {
                settings.StartupWindow = LinuxStartupWindow.Startup;
                settings.RecorderCaptureMode = manual ? RecorderCaptureMode.Manual : RecorderCaptureMode.PerSecond;
                settings.RecorderManualPlaybackDelayMs = 125;
                settings.RecorderFramesPerSecond = 10;
                settings.RecorderFixedFrameRate = false;
                settings.RecorderShowCursor = Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_CURSOR") == "1";
                settings.RecorderPreStart = false;
                settings.AskBeforeCloseEditor = false;
                settings.AskBeforeDiscardProject = false;
                settings.ShowNotificationIcon = false;
                settings.KeepOpen = true;
            });
            uiThread = new Thread(() =>
            {
                try
                {
                    AppBuilder.Configure<App>().UsePlatformDetect()
                        .AfterSetup(_ => Dispatcher.UIThread.Post(() => ready.TrySetResult()))
                        .StartWithClassicDesktopLifetime([]);
                }
                catch (Exception exception) { uiFailure.TrySetResult(exception); }
            }) { IsBackground = true };
            uiThread.Start();
            var started = await Task.WhenAny(ready.Task, uiFailure.Task).WaitAsync(TimeSpan.FromSeconds(20));
            if (started == uiFailure.Task) throw await uiFailure.Task;
            var app = await Ui(() => (App)Application.Current!);
            var recorder = await Ui(() => (RecorderWindow)app.OpenCaptureShell(CaptureShellKind.Recorder)!);
            await UntilAsync(() => recorder.IsVisible && recorder.Bounds.Width > 0);
            await CaptureAsync(recorder, "wayland-idle");
            if (Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_REUSE") == "1")
            {
                await ExerciseNativeSourceReuseAsync(app, recorder, directory);
                Assert.True(uiThread.Join(TimeSpan.FromSeconds(20)));
                return;
            }
            if (Environment.GetEnvironmentVariable("SCREENTOGIF_WAYLAND_LIFECYCLE") == "1")
            {
                await ExerciseNativeLifecycleAsync(app, recorder, directory);
                Assert.True(uiThread.Join(TimeSpan.FromSeconds(20)));
                return;
            }
            File.WriteAllText(Path.Combine(directory, "phase.txt"), "Select only: ScreenToGif Wayland test source\n");
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            await CaptureAsync(recorder, "wayland-preparing");
            var consentWait = Stopwatch.StartNew();
            while (await Ui(() => Count(recorder)) < (manual ? 1 : 6))
            {
                var problem = await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().FirstOrDefault());
                if (problem is not null)
                {
                    await CaptureAsync(problem, "wayland-start-failure");
                    Assert.True(false, "Native recording failed: " + await Ui(() => recorder.FindControl<TextBlock>("WaylandStatus")!.Text));
                }
                Assert.True(consentWait.Elapsed < TimeSpan.FromMinutes(3), "Portal consent/recording did not complete; see retained phase and application evidence.");
                await Task.Delay(100);
            }
            long pauseMs = 0;
            if (cursorProbe)
            {
                File.WriteAllText(Path.Combine(directory, "phase.txt"), "Move the pointer into the CENTER of the RED square in the test source and leave it there.\n");
                Assert.Equal("POINTER_IN_RED", await source.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromMinutes(3)));
                await Task.Delay(500);
            }
            if (manual)
            {
                Assert.Equal(1, await Ui(() => Count(recorder)));
                await Task.Delay(600);
                Assert.Equal(1, await Ui(() => Count(recorder)));
                for (var count = 2; count <= 3; count++)
                {
                    await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
                    await UntilAsync(() => Count(recorder) == count);
                    await Task.Delay(200);
                }
                source.Kill(entireProcessTree: true);
                await source.WaitForExitAsync();
                await Task.Delay(500);
                await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
                await UntilAsync(() => Desktop.Windows.OfType<ConfirmDialog>().Any());
                var failure = await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().Single());
                await CaptureAsync(failure, "wayland-source-ended");
                var message = await Ui(() => string.Join(Environment.NewLine,
                    failure.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text)));
                File.WriteAllText(Path.Combine(directory, "source-ended.txt"), message);
                Assert.Contains("Stop to keep", message);
                Assert.True(await Ui(() => recorder.FindControl<Button>("StopButton")!.IsVisible));
                await Ui(() => Click(failure.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Close"))));
            }
            else
            {
                var beforePauseState = await Ui(() => new
                {
                    Label = recorder.FindControl<TextBlock>("RecordButtonLabel")!.Text,
                    Status = recorder.FindControl<TextBlock>("WaylandStatus")!.Text,
                    Problems = Desktop.Windows.OfType<ConfirmDialog>().Select(dialog => string.Join(" ",
                        dialog.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text))).ToArray()
                });
                File.WriteAllText(Path.Combine(directory, "before-pause.json"), JsonSerializer.Serialize(beforePauseState));
                Assert.Empty(beforePauseState.Problems);
                Assert.Equal("Pause", beforePauseState.Label);
                await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
                await UntilAsync(() => recorder.FindControl<TextBlock>("RecordButtonLabel")!.Text == "Resume");
                var beforePause = await Ui(() => Count(recorder));
                var pause = Stopwatch.StartNew();
                await Task.Delay(1000);
                Assert.Equal(beforePause, await Ui(() => Count(recorder)));
                await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
                pauseMs = pause.ElapsedMilliseconds;
                await UntilAsync(() => Count(recorder) >= beforePause + 4);
                await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
                await UntilAsync(() => recorder.FindControl<TextBlock>("RecordButtonLabel")!.Text == "Resume");
            }
            await CaptureAsync(recorder, "wayland-paused");
            var selected = await Ui(() => new PixelSize((int)recorder.FindControl<NumericUpDown>("CaptureWidthInput")!.Value!,
                (int)recorder.FindControl<NumericUpDown>("CaptureHeightInput")!.Value!));
            await Ui(() => Click(recorder.FindControl<Button>("StopButton")!));
            await UntilAsync(() => Desktop.Windows.OfType<MainWindow>().Any(window => window.FindControl<ListBox>("FrameListBox")!.ItemCount > 0));
            var editor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
            var list = await Ui(() => editor.FindControl<ListBox>("FrameListBox")!);
            await UntilAsync(() => editor.FindControl<TabControl>("RibbonTabControl")!.IsEnabled);
            var captured = await Ui(() => list.Items.OfType<EditorFrame>().ToArray());
            await ProjectArchive.SaveAsync(Path.Combine(directory, "native-captured.stg-linux"), captured);
            await CaptureAsync(editor, "wayland-editor-recorded");
            Assert.True(captured.Select(frame => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(frame.FilePath)))).Distinct().Count() > 1,
                "The native source must actually change.");
            if (manual)
            {
                Assert.Equal(3, captured.Length);
                Assert.All(captured, frame => Assert.Equal(125, frame.DelayMs));
            }
            else Assert.All(captured, frame => Assert.True(frame.DelayMs < pauseMs, "Paused time entered playback delays."));
            Assert.True(HasSourceMarkers(ReadPixels(captured[0].FilePath), selected.Width),
                "The chosen native window must contain the fixture's aligned red, green and blue marker blocks.");
            foreach (var frame in captured)
            {
                using var bitmap = new Bitmap(frame.FilePath);
                Assert.Equal(selected, bitmap.PixelSize);
            }
            Assert.True(HasSourceCorner(ReadPixels(captured[^1].FilePath)),
                "The selected source's bottom-right magenta marker must survive; a cropped top-left image is not a full-source recording.");
            if (cursorProbe) AssertCursorInRedMarker(captured[^1].FilePath, LinuxSettings.Current.RecorderShowCursor);
            await EditAndExportNativeProjectAsync(editor, captured, directory);
            File.WriteAllText(Path.Combine(directory, "native-result.json"), JsonSerializer.Serialize(new
            {
                NativeSource = "GdkWayland.WaylandDisplay", selected.Width, selected.Height,
                Frames = captured.Length, CaptureDelaysMs = captured.Select(frame => frame.DelayMs),
                PauseMs = pauseMs, Manual = manual, Cursor = LinuxSettings.Current.RecorderShowCursor, CursorPixelsChecked = cursorProbe,
                Export = "APNG decoded pixel order and millisecond delays equal edited project"
            }, new JsonSerializerOptions { WriteIndented = true }));
            await Ui(editor.Close);
            await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
            await Ui(app.ExitApplication);
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(20)));
            File.WriteAllText(Path.Combine(directory, "phase.txt"), manual
                ? "PASS: native Manual Snap → source loss → retained frames → Editor playback/reverse → independently decoded APNG → StartUp → Exit\n"
                : "PASS: native paced Recorder → pause/resume → Editor playback/reverse → independently decoded APNG → StartUp → Exit\n");
        }
        finally
        {
            if (uiThread is not null && uiThread.IsAlive && ready.Task.IsCompleted)
            {
                await Ui(() => Desktop.Shutdown());
                uiThread.Join(TimeSpan.FromSeconds(20));
            }
            if (!source.HasExited) source.Kill(entireProcessTree: true);
            await source.WaitForExitAsync();
            File.WriteAllText(Path.Combine(directory, "source.log"), await sourceError);
        }
    }

    private static async Task ExerciseNativeSourceReuseAsync(App app, RecorderWindow recorder, string directory)
    {
        Assert.True(WaylandSourceReusePolicy.CanConfigure, "Install the administrator helper before this opt-in native check.");
        var original = WaylandSourceReusePolicy.IsAllowed;
        var tokens = new ScreenCastRestoreTokenStore();
        try
        {
            var options = await OpenOptionsAsync(recorder);
            var toggle = await Ui(() => options.FindControl<CheckBox>("WaylandSourceReuseCheckBox")!);
            if (!original)
            {
                File.WriteAllText(Path.Combine(directory, "phase.txt"), "CANCEL the first administrator authentication dialog.\n");
                await Ui(() => toggle.IsChecked = true);
                await HumanWaitAsync(() => options.FindControl<TextBlock>("WaylandSourceReuseStatus")!.Text?.Contains("not granted") == true);
                Assert.False(WaylandSourceReusePolicy.IsAllowed);
                Assert.False(await Ui(() => toggle.IsChecked == true));
                await CaptureAsync(options, "source-reuse-auth-canceled");
                File.WriteAllText(Path.Combine(directory, "phase.txt"), "APPROVE administrator authentication to enable source reuse.\n");
                await Ui(() => toggle.IsChecked = true);
                await HumanWaitAsync(() => WaylandSourceReusePolicy.IsAllowed && toggle.IsEnabled);
                Assert.True(await Ui(() => toggle.IsChecked == true));
            }
            await CaptureAsync(options, "source-reuse-enabled");
            await Ui(options.Close);
            await UntilAsync(() => !options.IsVisible);
            Assert.False(tokens.HasToken);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                File.WriteAllText(Path.Combine(directory, "phase.txt"), attempt == 0
                    ? "SHARE only ScreenToGif Wayland test source; allow remembering the selection if GNOME asks.\n"
                    : "The second recording must start automatically. Do not approve a new sharing dialog.\n");
                await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
                if (attempt == 0) await WaitForNativeFramesAsync(recorder);
                else await UntilAsync(() => Count(recorder) >= 3);
                Assert.True(tokens.HasToken, "The desktop did not return a persistent source grant.");
                await Ui(() => Click(recorder.FindControl<Button>("StopButton")!));
                await UntilAsync(() => !recorder.IsVisible && Desktop.Windows.OfType<MainWindow>().Any());
                var editor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
                var frames = await Ui(() => editor.FindControl<ListBox>("FrameListBox")!.Items.OfType<EditorFrame>().ToArray());
                await ProjectArchive.SaveAsync(Path.Combine(directory, $"reused-source-{attempt + 1}.stg-linux"), frames);
                using var first = new Bitmap(frames[0].FilePath);
                Assert.True(HasSourceMarkers(ReadPixels(frames[0].FilePath), first.PixelSize.Width));
                Assert.True(HasSourceCorner(ReadPixels(frames[0].FilePath)));
                if (attempt == 1) await EditAndExportNativeProjectAsync(editor, frames, directory);
                await Ui(editor.Close);
                await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
                recorder = await Ui(() => (RecorderWindow)app.OpenCaptureShell(CaptureShellKind.Recorder)!);
            }
            options = await OpenOptionsAsync(recorder);
            await Ui(() => Click(options.FindControl<Button>("ForgetWaylandSourceButton")!));
            Assert.False(tokens.HasToken);
            await Ui(options.Close);
            await UntilAsync(() => !options.IsVisible);
            File.WriteAllText(Path.Combine(directory, "phase.txt"), "CANCEL the sharing dialog after Forget; source selection must be required again.\n");
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            await HumanWaitAsync(() => recorder.FindControl<TextBlock>("WaylandStatus")!.Text?.Contains("Screen sharing canceled") == true);
            Assert.Equal(0, await Ui(() => Count(recorder)));
        }
        finally
        {
            tokens.Forget();
            if (WaylandSourceReusePolicy.IsAllowed != original)
            {
                File.WriteAllText(Path.Combine(directory, "phase.txt"), "APPROVE administrator cleanup if requested; restoring the original setting.\n");
                using var cleanup = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                Assert.True(await WaylandSourceReusePolicy.SetAllowedAsync(original, cleanup.Token));
            }
            File.WriteAllText(Path.Combine(directory, "source-reuse-policy-cleanup.txt"),
                $"Original policy={original}; restored policy={WaylandSourceReusePolicy.IsAllowed}; token retained={tokens.HasToken}\n");
        }
        await Ui(app.ExitApplication);
        File.WriteAllText(Path.Combine(directory, "source-reuse-result.txt"),
            (original ? "PASS: existing administrator grant → " : "PASS: administrator cancellation/enable → ") +
            "first consent → automatic second session → exact edit/export → Forget forces source selection → canceled retry → original administrator policy restored → Exit.\n");

        async Task<OptionsWindow> OpenOptionsAsync(Window owner)
        {
            await Ui(() => { _ = app.ShowOptions(owner, OptionsSection.Recorder); });
            await UntilAsync(() => Desktop.Windows.OfType<OptionsWindow>().Any());
            var window = await Ui(() => Desktop.Windows.OfType<OptionsWindow>().Single());
            await Ui(() => window.GetVisualDescendants().OfType<Expander>().Single(expander => Equals(expander.Header, "Capture mode")).IsExpanded = true);
            await Task.Delay(200);
            await Ui(() => window.FindControl<CheckBox>("WaylandSourceReuseCheckBox")!.BringIntoView());
            return window;
        }
    }

    private static async Task HumanWaitAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        while (!await Ui(condition)) await Task.Delay(100, timeout.Token);
    }

    private static async Task ExerciseNativeLifecycleAsync(App app, RecorderWindow recorder, string directory)
    {
        File.WriteAllText(Path.Combine(directory, "phase.txt"), "CANCEL the first sharing dialog. Do not share a source yet.\n");
        await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
        using (var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3)))
        {
            while (!await Ui(() => recorder.FindControl<TextBlock>("WaylandStatus")!.Text?.Contains("Screen sharing canceled") == true))
                await Task.Delay(100, timeout.Token);
        }
        Assert.Equal(0, await Ui(() => Count(recorder)));
        await UntilAsync(() => recorder.FindControl<Button>("RecordButton")!.IsEnabled);
        await CaptureAsync(recorder, "wayland-consent-canceled");

        File.WriteAllText(Path.Combine(directory, "phase.txt"), "The next dialog closes automatically with Recorder. Then SHARE only ScreenToGif Wayland test source in the following dialog.\n");
        await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
        await UntilAsync(() => !recorder.FindControl<Button>("RecordButton")!.IsEnabled);
        await Task.Delay(2000);
        await Ui(recorder.Close);
        await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
        await Task.Delay(1000);
        Assert.False(await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().Any()));
        recorder = await Ui(() => (RecorderWindow)app.OpenCaptureShell(CaptureShellKind.Recorder)!);
        await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
        await WaitForNativeFramesAsync(recorder);
        await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
        await UntilAsync(() => recorder.FindControl<TextBlock>("RecordButtonLabel")!.Text == "Resume");
        var count = await Ui(() => Count(recorder));

        await Ui(() => Click(recorder.FindControl<Button>("DiscardButton")!));
        await UntilAsync(() => Desktop.Windows.OfType<ConfirmDialog>().Any());
        var dialog = await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().Single());
        await CaptureAsync(dialog, "wayland-discard-confirmation");
        await Ui(() => Click(dialog.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Cancel"))));
        await UntilAsync(() => !Desktop.Windows.OfType<ConfirmDialog>().Any());
        Assert.Equal(count, await Ui(() => Count(recorder)));
        await Ui(recorder.Close);
        await UntilAsync(() => Desktop.Windows.OfType<ConfirmDialog>().Any());
        dialog = await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().Single());
        await Ui(() => Click(dialog.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Cancel"))));
        await UntilAsync(() => !Desktop.Windows.OfType<ConfirmDialog>().Any());
        Assert.True(await Ui(() => recorder.IsVisible));
        Assert.Equal(count, await Ui(() => Count(recorder)));
        await Ui(() => Click(recorder.FindControl<Button>("DiscardButton")!));
        await UntilAsync(() => Desktop.Windows.OfType<ConfirmDialog>().Any());
        dialog = await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().Single());
        await Ui(() => Click(dialog.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Discard"))));
        await UntilAsync(() => recorder.FindControl<ComboBox>("ModeSelector")!.IsEnabled &&
                               !recorder.FindControl<Button>("StopButton")!.IsVisible);
        await CaptureAsync(recorder, "wayland-discarded");
        File.WriteAllText(Path.Combine(directory, "phase.txt"), "SHARE only ScreenToGif Wayland test source again. This recording checks application Exit while sharing.\n");
        await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
        await WaitForNativeFramesAsync(recorder);
        await CaptureAsync(recorder, "wayland-active-before-exit");
        await Ui(app.ExitApplication);
        File.WriteAllText(Path.Combine(directory, "lifecycle-result.txt"),
            "PASS: real portal cancel/decline → retry → close pending consent → StartUp → retry/share → discard cancel and Recorder-close cancel preserve frames → confirmed discard → new consent/recording → explicit Exit while sharing.\n");
    }

    private static async Task WaitForNativeFramesAsync(RecorderWindow recorder)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        while (await Ui(() => Count(recorder)) < 3)
        {
            Assert.False(await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().Any()), "Native recording failed during lifecycle check.");
            await Task.Delay(100, timeout.Token);
        }
    }

    [SavedNativeProjectFact]
    public async Task Saved_native_project_can_be_played_edited_exported_and_closed()
    {
        var archive = Environment.GetEnvironmentVariable("SCREENTOGIF_NATIVE_RECORDED_PROJECT")!;
        Assert.True(File.Exists(archive));
        var directory = Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR")!;
        Assert.False(string.IsNullOrWhiteSpace(directory));
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Assert.True(config is not null && Path.GetFullPath(config).StartsWith(Path.GetTempPath(), StringComparison.Ordinal));
        await LinuxSettings.UpdateAsync(settings =>
        {
            settings.StartupWindow = LinuxStartupWindow.Startup;
            settings.AskBeforeCloseEditor = false;
            settings.ShowNotificationIcon = false;
            settings.KeepOpen = true;
        });
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var uiFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var uiThread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<App>().UsePlatformDetect()
                    .AfterSetup(_ => Dispatcher.UIThread.Post(() => ready.TrySetResult()))
                    .StartWithClassicDesktopLifetime([]);
            }
            catch (Exception exception) { uiFailure.TrySetResult(exception); }
        }) { IsBackground = true };
        uiThread.Start();
        try
        {
            var started = await Task.WhenAny(ready.Task, uiFailure.Task).WaitAsync(TimeSpan.FromSeconds(20));
            if (started == uiFailure.Task) throw await uiFailure.Task;
            using var loaded = await ProjectArchive.LoadAsync(archive);
            var captured = loaded.Frames.ToArray();
            var app = await Ui(() => (App)Application.Current!);
            await Ui(() => Assert.True(app.AdoptRecording(loaded, CaptureShellAdoption.ReplaceProject)));
            await UntilAsync(() => Desktop.Windows.OfType<MainWindow>().Any(window => window.FindControl<ListBox>("FrameListBox")!.ItemCount == captured.Length));
            var editor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
            await EditAndExportNativeProjectAsync(editor, captured, directory);
            await Ui(editor.Close);
            await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
            await Ui(app.ExitApplication);
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(20)));
            File.WriteAllText(Path.Combine(directory, "saved-native-result.txt"),
                "PASS: saved native recording playback/reverse → independently decoded APNG → StartUp → Exit. No new capture or consent in this replay.\n");
        }
        finally
        {
            if (uiThread.IsAlive && ready.Task.IsCompleted)
            {
                await Ui(() => Desktop.Shutdown());
                uiThread.Join(TimeSpan.FromSeconds(20));
            }
        }
    }

    private static async Task EditAndExportNativeProjectAsync(MainWindow editor, EditorFrame[] captured, string directory)
    {
        var list = await Ui(() => editor.FindControl<ListBox>("FrameListBox")!);
        await UntilAsync(() => editor.FindControl<TabControl>("RibbonTabControl")!.IsEnabled);
        var initialFrame = await Ui(() => editor.FindControl<TextBlock>("CurrentFrameText")!.Text);
        await Ui(() => Click(editor.FindControl<Button>("RibbonPlayButton")!));
        await UntilAsync(() => editor.FindControl<RibbonButton>("RibbonPlayButton")!.Label == "Stop" &&
                               editor.FindControl<TextBlock>("CurrentFrameText")!.Text != initialFrame);
        await Ui(() => Click(editor.FindControl<Button>("RibbonPlayButton")!));
        await UntilAsync(() => editor.FindControl<RibbonButton>("RibbonPlayButton")!.Label == "Play");
        await Ui(() => editor.FindControl<TabControl>("RibbonTabControl")!.SelectedIndex = 3);
        await UntilAsync(() => editor.GetVisualDescendants().OfType<RibbonButton>().Any(button => button.Label == "Reverse"));
        await Ui(() =>
        {
            list.SelectAll();
            Click(editor.GetVisualDescendants().OfType<RibbonButton>().Single(button => button.Label == "Reverse"));
        });
        await UntilAsync(() => list.Items.OfType<EditorFrame>().First().FilePath == captured[^1].FilePath &&
                               editor.FindControl<TabControl>("RibbonTabControl")!.IsEnabled);
        var edited = await Ui(() => list.Items.OfType<EditorFrame>().ToArray());
        await ProjectArchive.SaveAsync(Path.Combine(directory, "native-edited.stg-linux"), edited);
        var ffmpeg = new FfmpegTool();
        var exported = Path.Combine(directory, "native-edited.apng");
        await new FfmpegExporter(ffmpeg).ExportAsync(edited, exported);
        using var decodedWorkspace = EditorWorkspace.Create();
        var decoded = await new MediaImporter(ffmpeg).ImportAsync([exported], decodedWorkspace);
        Assert.Equal(edited.Select(frame => frame.DelayMs), decoded.Select(frame => frame.DelayMs));
        for (var index = 0; index < edited.Length; index++)
            Assert.Equal(ReadPixels(edited[index].FilePath), ReadPixels(decoded[index].FilePath));
        await CaptureAsync(editor, "wayland-editor-edited");
    }

    private static void AssertCursorInRedMarker(string path, bool shown)
    {
        using var bitmap = new Bitmap(path);
        var pixels = ReadPixels(path);
        var width = bitmap.PixelSize.Width;
        var red = Enumerable.Range(0, pixels.Length / 4).Where(index =>
            pixels[index * 4 + 2] > 240 && pixels[index * 4 + 1] < 10 && pixels[index * 4] < 10).ToArray();
        Assert.NotEmpty(red);
        var left = red.Min(index => index % width);
        var right = red.Max(index => index % width);
        var top = red.Min(index => index / width);
        var bottom = red.Max(index => index / width);
        var margin = Math.Max(2, (right - left) / 8);
        var nonRed = 0;
        for (var y = top + margin; y <= bottom - margin; y++)
        for (var x = left + margin; x <= right - margin; x++)
        {
            var offset = (y * width + x) * 4;
            if (pixels[offset + 2] <= 240 || pixels[offset + 1] >= 10 || pixels[offset] >= 10) nonRed++;
        }
        Assert.True(shown ? nonRed > 0 : nonRed == 0,
            $"Cursor shown={shown}: expected cursor pixels only when embedded; red-marker interior has {nonRed} non-red pixels.");
    }

    private static int Count(RecorderWindow recorder)
    {
        var status = recorder.FindControl<TextBlock>("StatusLabel")!.Text ?? string.Empty;
        return int.TryParse(status.Split(' ')[0], out var count) ? count : 0;
    }

    private static bool HasSourceCorner(byte[] pixels)
    {
        for (var offset = 0; offset < pixels.Length; offset += 4)
            if (pixels[offset] > 240 && pixels[offset + 2] > 240 && pixels[offset + 1] < 10) return true;
        return false;
    }

    private static bool HasSourceMarkers(byte[] pixels, int width)
    {
        for (var row = 0; row < pixels.Length / (width * 4); row++)
        {
            var starts = new List<int>();
            var lastColor = -1;
            for (var column = 0; column < width; column++)
            {
                var offset = (row * width + column) * 4;
                var color = pixels[offset + 2] > 240 && pixels[offset + 1] < 10 && pixels[offset] < 10 ? 0
                    : pixels[offset + 1] > 240 && pixels[offset + 2] < 10 && pixels[offset] < 10 ? 1
                    : pixels[offset] > 240 && pixels[offset + 1] < 10 && pixels[offset + 2] < 10 ? 2 : -1;
                if (color >= 0 && color != lastColor)
                {
                    if (color == starts.Count) starts.Add(column);
                    else if (color == 0) { starts.Clear(); starts.Add(column); }
                    lastColor = color;
                }
            }
            if (starts.Count == 3 && starts[1] > starts[0] &&
                Math.Abs((starts[2] - starts[1]) - (starts[1] - starts[0])) <= 1) return true;
        }
        return false;
    }

    private static byte[] ReadPixels(string path)
    {
        using var bitmap = new Bitmap(path);
        var bytes = new byte[bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4];
        unsafe
        {
            fixed (byte* buffer = bytes)
                bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), (IntPtr)buffer, bytes.Length, bitmap.PixelSize.Width * 4);
        }
        return bytes;
    }
}
