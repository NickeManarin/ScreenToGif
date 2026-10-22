using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ScreenToGif.Linux.Services;
using System.Diagnostics;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class DesktopWindowFactAttribute : FactAttribute
{
    public DesktopWindowFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SMOKE") != "1")
            Skip = "Set SCREENTOGIF_DESKTOP_SMOKE=1 with an isolated config directory and a graphical display.";
    }
}

/// <summary>Runs the real application; run separately from other opt-in Avalonia smoke hosts.</summary>
public sealed partial class DesktopWindowSmokeTests
{
    [DesktopWindowFact]
    public async Task Board_layout_and_editor_return_work_in_the_real_application()
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Assert.True(config is not null && Path.GetFullPath(config).StartsWith(Path.GetTempPath(), StringComparison.Ordinal),
            "Set XDG_CONFIG_HOME to an isolated directory under /tmp.");
        var evidence = Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR");
        Assert.False(string.IsNullOrWhiteSpace(evidence), "Set SCREENTOGIF_DESKTOP_SCREENSHOT_DIR to retain evidence.");
        Directory.CreateDirectory(evidence!);
        await LinuxSettings.UpdateAsync(settings =>
        {
            settings.StartupWindow = LinuxStartupWindow.Startup;
            settings.AskBeforeCloseEditor = false;
            settings.AskBeforeDiscardProject = false;
            settings.ShowNotificationIcon = true;
            settings.KeepOpen = true;
        });

        var failures = new List<string>();
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
            catch (Exception exception)
            {
                uiFailure.TrySetResult(exception);
            }
        }) { IsBackground = true };
        uiThread.Start();

        try
        {
            var started = await Task.WhenAny(ready.Task, uiFailure.Task).WaitAsync(TimeSpan.FromSeconds(20));
            if (started == uiFailure.Task)
                throw await uiFailure.Task;
            await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Any(window => window.IsVisible));
            var app = await Ui(() => (App)Application.Current!);
            var startup = await Ui(() => Desktop.Windows.OfType<StartupWindow>().Single());
            await CaptureAsync(startup, "startup");

            var board = await Ui(() => (BoardWindow)app.OpenCaptureShell(CaptureShellKind.Board)!);
            await UntilAsync(() => board.IsVisible && board.Bounds.Width > 0);
            await CaptureAsync(board, "board-pen");
            await Ui(() =>
            {
                var fit = board.FindControl<CheckBox>("FitToCurveCheckBox")!;
                var highlighter = board.FindControl<CheckBox>("HighlighterCheckBox")!;
                var fitOrigin = fit.TranslatePoint(default, board)!.Value;
                var highlightOrigin = highlighter.TranslatePoint(default, board)!.Value;
                Check(Math.Abs(fitOrigin.Y - highlightOrigin.Y) < 1 && highlightOrigin.X >= fitOrigin.X + fit.Bounds.Width,
                    "Board: Fit to Curve and Highlighter must appear side by side.");
                fit.IsChecked = true;
                highlighter.IsChecked = true;
                board.FindControl<Button>("EraserButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            });
            await CaptureAsync(board, "board-eraser");
            await Ui(() => board.FindControl<Button>("PenButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await UntilAsync(() => LinuxSettings.Current.BoardFitToCurve && LinuxSettings.Current.BoardHighlighter);
            await Ui(board.Close);
            await UntilAsync(() => startup.IsVisible);

            await Ui(() => startup.FindControl<Button>("EditorShortcut")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            var editor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
            await CaptureAsync(editor, "editor-empty");
            await ExerciseSurfacesAsync(app, editor);
            await Ui(editor.Close);
            await Task.Delay(250);
            var returned = await Ui(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible));
            Check(returned == 1, $"Editor close: expected one visible StartUp, found {returned}.");
            if (returned == 1)
                await CaptureAsync(await Ui(() => Desktop.Windows.OfType<StartupWindow>().Single()), "editor-return");

            if (returned == 1)
                await ExerciseEditorRoutesAsync(app);

            File.WriteAllLines(Path.Combine(evidence!, "checks.txt"), failures.Count == 0 ? ["PASS: Board layout and Editor return"] : failures);
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }
        finally
        {
            if (!uiThread.Join(TimeSpan.FromMilliseconds(100)) && ready.Task.IsCompleted)
                await Ui(() => Desktop.Shutdown());
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(20)), "The test-owned UI thread did not stop.");
        }
        if (uiFailure.Task.IsCompleted)
            throw await uiFailure.Task;

        void Check(bool condition, string message)
        {
            if (!condition)
                failures.Add(message);
        }
    }

    private static IClassicDesktopStyleApplicationLifetime Desktop =>
        (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

    private static async Task Ui(Action action) =>
        await Dispatcher.UIThread.InvokeAsync(action).GetTask().WaitAsync(TimeSpan.FromSeconds(20));
    private static async Task<T> Ui<T>(Func<T> action) =>
        await Dispatcher.UIThread.InvokeAsync(action).GetTask().WaitAsync(TimeSpan.FromSeconds(20));

    private static async Task CaptureAsync(TopLevel window, string name)
    {
        await Task.Delay(150);
        await Ui(() =>
        {
            var directory = Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR")!;
            var size = window.ClientSize;
            var scaling = window.RenderScaling;
            using var image = new RenderTargetBitmap(
                new PixelSize((int)Math.Ceiling(size.Width * scaling), (int)Math.Ceiling(size.Height * scaling)),
                new Vector(96 * scaling, 96 * scaling));
            image.Render(window);
            using var file = File.Create(Path.Combine(directory, name + ".png"));
            image.Save(file, PngBitmapEncoderOptions.Default);
            File.AppendAllText(Path.Combine(directory, "index.txt"),
                $"{name}.png | {window.GetType().Name} | {size.Width}x{size.Height} | scale={scaling} | {window.TryGetPlatformHandle()?.HandleDescriptor}\n");
        });
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!await Ui(condition))
            await Task.Delay(50, timeout.Token);
    }

    private static async Task ActivateAsync(Window window)
    {
        var handle = await Ui(() => window.TryGetPlatformHandle()!);
        Assert.Equal("XID", handle.HandleDescriptor);
        var id = handle.Handle.ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var owner = new ProcessStartInfo("xdotool") { RedirectStandardOutput = true };
        owner.ArgumentList.Add("getwindowpid");
        owner.ArgumentList.Add(id);
        using (var query = Process.Start(owner)!)
        {
            try
            {
                var pid = await query.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(10));
                await query.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(0, query.ExitCode);
                Assert.Equal(Environment.ProcessId.ToString(), pid.Trim());
            }
            finally
            {
                if (!query.HasExited)
                {
                    query.Kill();
                    await query.WaitForExitAsync();
                }
            }
        }
        var start = new ProcessStartInfo("xdotool");
        start.ArgumentList.Add("windowactivate");
        start.ArgumentList.Add("--sync");
        start.ArgumentList.Add(id);
        using var activate = Process.Start(start)!;
        try
        {
            await activate.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(0, activate.ExitCode);
            await UntilAsync(() => window.IsActive);
        }
        finally
        {
            if (!activate.HasExited)
            {
                activate.Kill();
                await activate.WaitForExitAsync();
            }
        }
    }
}
