using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ScreenToGif.Linux.Controls;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using System.Text.Json;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed partial class DesktopWindowSmokeTests
{
    private static async Task ExerciseSurfacesAsync(App app, MainWindow editor)
    {
        await AtBothSizesAsync(editor, "editor-empty");
        var tabs = await Ui(() => editor.FindControl<TabControl>("RibbonTabControl")!);
        for (var index = 0; index < await Ui(() => tabs.ItemCount); index++)
        {
            await Ui(() => tabs.SelectedIndex = index);
            await CaptureAsync(editor, $"editor-tab-{index}");
        }

        var options = await Ui(() => new OptionsWindow());
        await Ui(() => options.Show(editor));
        await AtBothSizesAsync(options, "options");
        var sections = await Ui(() => options.FindControl<ListBox>("SectionsList")!);
        var items = await Ui(() => sections.Items.OfType<ListBoxItem>().Where(item => item.IsEnabled).ToArray());
        var normalSize = await Ui(() => options.ClientSize);
        var layoutFailures = new List<string>();
        foreach (var item in items)
        {
            await Ui(() => sections.SelectedItem = item);
            var section = await Ui(() => item.Tag as string);
            var name = "options-" + section;
            await CaptureAsync(options, name);
            await Ui(() => CheckOptionsTextLayout(options, section, layoutFailures));
            await ResizeAsync(options, await Ui(() => new Size(options.MinWidth, options.MinHeight)));
            await CaptureAsync(options, name + "-minimum");
            await Ui(() => CheckOptionsTextLayout(options, section, layoutFailures));
            if (section == "Language")
            {
                var scroll = await Ui(() => options.FindControl<Control>("LanguagePage")!
                    .GetVisualDescendants().OfType<ScrollViewer>().Single());
                await Ui(scroll.ScrollToEnd);
                await CaptureAsync(options, name + "-end");
            }
            await ResizeAsync(options, normalSize);
        }
        await Ui(options.Close);
        Assert.True(layoutFailures.Count == 0, string.Join(Environment.NewLine, layoutFailures));

        foreach (var kind in new[] { CaptureShellKind.Recorder, CaptureShellKind.Webcam, CaptureShellKind.Board })
        {
            // Device preview uses an empty, test-owned catalog; controlled video has a separate smoke test.
            var window = await Ui(() => kind == CaptureShellKind.Webcam
                ? new WebcamWindow(new CameraDeviceCatalog(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))), new FfmpegTool())
                : (Window)app.OpenCaptureShell(kind)!);
            if (kind == CaptureShellKind.Webcam)
                await Ui(window.Show);
            await UntilAsync(() => window.IsVisible && window.Bounds.Width > 0);
            await AtBothSizesAsync(window, kind.ToString().ToLowerInvariant());
            if (kind == CaptureShellKind.Recorder && Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland")
                Assert.False(await Ui(() => window.FindControl<Button>("RecordButton")!.IsEnabled),
                    "XWayland must not offer capture of the native Wayland desktop.");
            await Ui(window.Close);
        }

        var dialogs = await Ui(() => new Window[]
        {
            new ParameterDialog("Resize frames", ("Width (pixels)", "798"), ("Height (pixels)", "387")),
            new ChoiceDialog("Slide direction", ["Left", "Right", "Up", "Down"]),
            new ConfirmDialog("Discard recording", "Discard the recorded frames permanently?", "Discard recording"),
            new MessageDialog("Recording paused", "Frames could not be saved. The captured work is still available."),
            new ColorPickerDialog(Colors.Black),
            new FrameInsertionDialog(12, 3)
        });
        foreach (var dialog in dialogs)
        {
            var checkFocus = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland";
            if (checkFocus)
            {
                await ActivateAsync(editor);
            }
            Task? closed = null;
            await Ui(() => closed = dialog.ShowDialog(editor));
            await CaptureAsync(dialog, "dialog-" + dialog.GetType().Name);
            var combo = await Ui(() => dialog.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault());
            if (combo is not null)
            {
                await Ui(() => combo.IsDropDownOpen = true);
                var popup = await Ui(() => combo.GetVisualDescendants().OfType<Popup>().Single());
                var popupRoot = await Ui(() => TopLevel.GetTopLevel(popup.Child!)!);
                await CaptureAsync(popupRoot, "popup-" + dialog.GetType().Name);
                await Ui(() => combo.IsDropDownOpen = false);
            }
            await Ui(dialog.Close);
            await closed!;
            if (checkFocus)
                await UntilAsync(() => editor.IsActive);
        }

        await ExerciseBoardWorkflowAsync(app, editor);
    }

    private static async Task AtBothSizesAsync(Window window, string name)
    {
        var original = await Ui(() => window.ClientSize);
        await CaptureAsync(window, name + "-normal");
        if (await Ui(() => window.CanResize))
            await ResizeAsync(window, await Ui(() => new Size(window.MinWidth, window.MinHeight)));
        await CaptureAsync(window, name + "-minimum");
        if (await Ui(() => window.CanResize))
            await ResizeAsync(window, original);
    }

    private static async Task ResizeAsync(Window window, Size size)
    {
        await Ui(() => { window.Width = size.Width; window.Height = size.Height; });
        await UntilAsync(() => Math.Abs(window.ClientSize.Width - size.Width) < 1 &&
            Math.Abs(window.ClientSize.Height - size.Height) < 1);
    }

    private static void CheckOptionsTextLayout(OptionsWindow options, string? section, List<string> failures)
    {
        if (section is "Shortcuts" or "Language")
        {
            var page = options.FindControl<Control>(section + "Page")!;
            foreach (var text in page.GetVisualDescendants().OfType<TextBlock>())
            {
                if (string.IsNullOrEmpty(text.Text) || !text.IsEffectivelyVisible)
                    continue;
                var probe = new TextBlock
                {
                    Text = text.Text, FontFamily = text.FontFamily, FontSize = text.FontSize,
                    FontWeight = text.FontWeight, FontStyle = text.FontStyle, TextWrapping = text.TextWrapping
                };
                probe.Measure(new Size(text.TextWrapping == TextWrapping.NoWrap ? double.PositiveInfinity : text.Bounds.Width,
                    double.PositiveInfinity));
                if (probe.DesiredSize.Width > text.Bounds.Width + 1 || probe.DesiredSize.Height > text.Bounds.Height + 1)
                    failures.Add($"{section}: '{text.Text}' does not fit its arranged text box.");
            }
        }
        if (section == "About")
        {
            var groups = options.FindControl<Control>("AboutPage")!.GetVisualDescendants().OfType<Expander>().ToArray();
            for (var index = 1; index < groups.Length; index++)
            {
                var content = (Control)groups[index - 1].Content!;
                var bottom = content.TranslatePoint(new Point(0, content.Bounds.Height), options)!.Value.Y;
                var nextTop = groups[index].TranslatePoint(default, options)!.Value.Y;
                if (bottom > nextTop)
                    failures.Add($"About: {groups[index - 1].Header} content ends at {bottom}, over {groups[index].Header} at {nextTop}.");
            }
        }
    }

    private static async Task ExerciseBoardWorkflowAsync(App app, MainWindow editor)
    {
        var board = await Ui(() => (BoardWindow)app.OpenCaptureShell(CaptureShellKind.Board)!);
        var surface = await Ui(() => board.FindControl<BoardCanvas>("BoardSurface")!);
        await Ui(() =>
        {
            board.FindControl<ToggleButton>("AutoRecordButton")!.IsChecked = false;
            Click(board.FindControl<Button>("RecordButton")!);
            var pointer = new Avalonia.Input.Pointer(1, PointerType.Mouse, true);
            var start = surface.TranslatePoint(new Point(40, 40), board)!.Value;
            surface.RaiseEvent(new PointerPressedEventArgs(surface, pointer, board, start, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            surface.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, surface, pointer, board,
                start + new Vector(160, 100), 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            surface.RaiseEvent(new PointerReleasedEventArgs(surface, pointer, board, start + new Vector(160, 100), 2,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        });
        await UntilAsync(() => surface.Strokes.Count == 1 && board.FindControl<Button>("DiscardButton")!.IsEnabled);
        await Task.Delay(350);
        await Ui(() => Click(board.FindControl<Button>("RecordButton")!));
        await CaptureAsync(board, "board-recorded");
        await Ui(() => Click(board.FindControl<Button>("StopButton")!));
        var frames = await Ui(() => editor.FindControl<ListBox>("FrameListBox")!);
        await UntilAsync(() => frames.Items.OfType<EditorFrame>().Any() && !board.IsVisible);
        await CaptureAsync(editor, "editor-recorded");
        await Ui(() => Click(editor.FindControl<Button>("RibbonPlayButton")!));
        await Task.Delay(200);
        await Ui(() => Click(editor.FindControl<Button>("RibbonPlayButton")!));

        var tabs = await Ui(() => editor.FindControl<TabControl>("RibbonTabControl")!);
        await Ui(() => tabs.SelectedIndex = 3);
        var reverse = await Ui(() => editor.GetVisualDescendants().OfType<RibbonButton>().Single(button => button.Label == "Reverse"));
        await Ui(() => { frames.SelectAll(); Click(reverse); });
        await UntilAsync(() => editor.FindControl<TabControl>("RibbonTabControl")!.IsEnabled);
        await Ui(() =>
        {
            editor.FindControl<TextBox>("DelayTextBox")!.Text = "120";
            Click(editor.GetVisualDescendants().OfType<RibbonButton>().Single(button => button.Label == "Apply all"));
        });
        await UntilAsync(() => frames.Items.OfType<EditorFrame>().All(frame => frame.DelayMs == 120));
        var projectFrames = await Ui(() => frames.Items.OfType<EditorFrame>().ToArray());
        var directory = Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR")!;
        var archive = Path.Combine(directory, "recording.stg-linux");
        await ProjectArchive.SaveAsync(archive, projectFrames);
        using var reopened = await ProjectArchive.LoadAsync(archive);
        Assert.Equal(projectFrames.Select(frame => frame.DelayMs), reopened.Frames.Select(frame => frame.DelayMs));
        for (var index = 0; index < projectFrames.Length; index++)
            Assert.Equal(await File.ReadAllBytesAsync(projectFrames[index].FilePath),
                await File.ReadAllBytesAsync(reopened.Frames[index].FilePath));
        await Ui(() => frames.SelectedIndex = 0);
        await CaptureAsync(editor, "editor-selected");
        var export = Path.Combine(directory, "recording.apng");
        var ffmpeg = new FfmpegTool();
        await new FfmpegExporter(ffmpeg).ExportAsync(projectFrames, export);
        var probe = await ffmpeg.RunFfprobeCheckedAsync(["-v", "error", "-count_frames", "-show_packets",
            "-show_entries", "stream=nb_read_frames,width,height:packet=pts_time,duration_time", "-of", "json", export]);
        using var json = JsonDocument.Parse(probe.StandardOutput);
        Assert.Equal(projectFrames.Length, int.Parse(json.RootElement.GetProperty("streams")[0].GetProperty("nb_read_frames").GetString()!));
        var packets = json.RootElement.GetProperty("packets").EnumerateArray().ToArray();
        Assert.Equal(projectFrames.Length, packets.Length);
        var expectedTime = 0d;
        for (var index = 0; index < packets.Length; index++)
        {
            var timestamp = double.Parse(packets[index].GetProperty("pts_time").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            var duration = double.Parse(packets[index].GetProperty("duration_time").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(Math.Abs(timestamp - expectedTime), 0, 0.005);
            Assert.InRange(Math.Abs(duration - projectFrames[index].DelayMs / 1000d), 0, 0.005);
            expectedTime += projectFrames[index].DelayMs / 1000d;
        }
        using var workspace = EditorWorkspace.Create();
        var imported = await new MediaImporter(ffmpeg).ImportAsync([export], workspace);
        Assert.Equal(projectFrames.Length, imported.Count);
        await Ui(() => editor.AdoptRecordedProjectAsync(reopened)).Unwrap();
        Assert.Equal(0, await Ui(() => frames.SelectedIndex));
        await CaptureAsync(editor, "editor-reopened");
    }

    private static async Task ExerciseEditorRoutesAsync(App app)
    {
        foreach (var keepOpen in new[] { true, false })
        {
            await LinuxSettings.UpdateAsync(settings => settings.KeepOpen = keepOpen);
            for (var repeat = 0; repeat < 2; repeat++)
            {
                await Ui(() => app.OpenWindow(LinuxTrayWindow.Editor));
                var editor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
                await Ui(editor.Close);
                await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
                Assert.Empty(await Ui(() => Desktop.Windows.OfType<MainWindow>().ToArray()));
            }
        }

        await Ui(() => app.OpenWindow(LinuxTrayWindow.Editor));
        var busyEditor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = await Ui<Task>(() => busyEditor.RunOperationAsync("Controlled operation",
            async token =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            }));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await Ui(() => { busyEditor.Close(); Assert.True(busyEditor.IsVisible); });
        await operation.WaitAsync(TimeSpan.FromSeconds(20));
        await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);

        // This route creates an editor directly from a capture result rather than from StartUp.
        var archive = Path.Combine(Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR")!, "recording.stg-linux");
        using var project = await ProjectArchive.LoadAsync(archive);
        // The screen recorder constructs an editor and adopts it after opening its recording.
        var directEditor = await Ui(() => new MainWindow());
        await Ui(directEditor.Show);
        await Ui(() => directEditor.OpenRecordingAsync(project)).Unwrap();
        await Ui(() => app.AdoptEditor(directEditor));
        await Ui(app.CloseStartup);
        await Ui(directEditor.Close);
        await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);

        using var capturedProject = await ProjectArchive.LoadAsync(archive);
        await Ui(() => Assert.True(app.AdoptRecording(capturedProject, CaptureShellAdoption.ReplaceProject)));
        var capturedEditor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
        await UntilAsync(() => capturedEditor.FindControl<ListBox>("FrameListBox")!.ItemCount > 0);
        await LinuxSettings.UpdateAsync(settings => settings.AskBeforeCloseEditor = true);
        await Ui(capturedEditor.Close);
        await UntilAsync(() => Desktop.Windows.OfType<ConfirmDialog>().Any());
        var prompt = await Ui(() => Desktop.Windows.OfType<ConfirmDialog>().Single());
        await CaptureAsync(prompt, "editor-close-confirmation");
        await Ui(() => Click(prompt.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Cancel"))));
        Assert.True(await Ui(() => capturedEditor.IsVisible));
        Assert.True(await Ui(() => capturedEditor.FindControl<ListBox>("FrameListBox")!.ItemCount > 0));
        await LinuxSettings.UpdateAsync(settings => settings.AskBeforeCloseEditor = false);
        await Ui(capturedEditor.Close);
        await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
        var startupCountAtExit = -1;
        await Ui(() =>
        {
            var startup = Desktop.Windows.OfType<StartupWindow>().Single();
            Click(startup.FindControl<Button>("EditorShortcut")!);
            Assert.Empty(Desktop.Windows.OfType<StartupWindow>());
            var exitEditor = Desktop.Windows.OfType<MainWindow>().Single();
            exitEditor.Closed += (_, _) => startupCountAtExit = Desktop.Windows.OfType<StartupWindow>().Count();
            app.ExitApplication();
        });
        Assert.Equal(0, startupCountAtExit);
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
}
