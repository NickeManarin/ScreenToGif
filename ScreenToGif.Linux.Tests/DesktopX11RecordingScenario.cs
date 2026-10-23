using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using System.Text.Json;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed partial class DesktopWindowSmokeTests
{
    private static async Task ExerciseX11RecorderAsync(App app)
    {
        Assert.Equal("x11", Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"));
        await LinuxSettings.UpdateAsync(settings =>
        {
            settings.RecorderCaptureMode = RecorderCaptureMode.PerSecond;
            settings.RecorderFramesPerSecond = 10;
            settings.RecorderFixedFrameRate = false;
            settings.RecorderShowCursor = false;
            settings.RecorderPreStart = false;
        });
        var source = await Ui(() => new Window
        {
            Title = "ScreenToGif - X11 owned source", Width = 1024, Height = 768,
            WindowStartupLocation = WindowStartupLocation.CenterScreen, Background = Brushes.Red
        });
        await Ui(source.Show);
        RecorderWindow? recorder = null;
        try
        {
            await UntilAsync(() => source.IsVisible && source.Bounds.Width > 0);
            recorder = await Ui(() => (RecorderWindow)app.OpenCaptureShell(CaptureShellKind.Recorder)!);
            await UntilAsync(() => recorder.IsVisible && recorder.Bounds.Width > 0);
            await Task.Delay(200);
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            await UntilAsync(() => Count(recorder) >= 3);
            await Pause();
            var firstPause = await Ui(() => Count(recorder));
            await Ui(() => source.Background = Brushes.Lime);
            await Task.Delay(1000);
            Assert.Equal(firstPause, await Ui(() => Count(recorder)));
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            await UntilAsync(() => Count(recorder) >= firstPause + 3);
            await Pause();
            var secondPause = await Ui(() => Count(recorder));
            await Ui(() => source.Background = Brushes.Blue);
            await Task.Delay(200);
            await Ui(() => Click(recorder.FindControl<Button>("RecordButton")!));
            await UntilAsync(() => Count(recorder) >= secondPause + 3);
            await Pause();
            await CaptureAsync(recorder, "x11-recorder-paused");
            await Ui(() => Click(recorder.FindControl<Button>("StopButton")!));
            await UntilAsync(() => Desktop.Windows.OfType<MainWindow>().Any(window => window.FindControl<ListBox>("FrameListBox")!.ItemCount > 0));
            var editor = await Ui(() => Desktop.Windows.OfType<MainWindow>().Single());
            var frames = await Ui(() => editor.FindControl<ListBox>("FrameListBox")!.Items.OfType<EditorFrame>().ToArray());
            var colors = frames.Select(frame => Convert.ToHexString(ReadPixels(frame.FilePath).AsSpan(0, 4))).ToArray();
            Assert.Equal(new[] { "0000FFFF", "00FF00FF", "FF0000FF" }, colors.Distinct());
            Assert.All(frames, frame => Assert.InRange(frame.DelayMs, 1, 999));
            var directory = Environment.GetEnvironmentVariable("SCREENTOGIF_DESKTOP_SCREENSHOT_DIR")!;
            await ProjectArchive.SaveAsync(Path.Combine(directory, "x11-recording.stg-linux"), frames);
            var ffmpeg = new FfmpegTool();
            var output = Path.Combine(directory, "x11-recording.apng");
            await new FfmpegExporter(ffmpeg).ExportAsync(frames, output);
            using var decodedWorkspace = EditorWorkspace.Create();
            var decoded = await new MediaImporter(ffmpeg).ImportAsync([output], decodedWorkspace);
            Assert.Equal(frames.Select(frame => frame.DelayMs), decoded.Select(frame => frame.DelayMs));
            for (var index = 0; index < frames.Length; index++)
                Assert.Equal(ReadPixels(frames[index].FilePath), ReadPixels(decoded[index].FilePath));
            await CaptureAsync(editor, "x11-editor-recorded");
            File.WriteAllText(Path.Combine(directory, "x11-recording.json"), JsonSerializer.Serialize(new
            {
                Frames = frames.Length, Colors = colors, DelaysMs = frames.Select(frame => frame.DelayMs),
                Result = "Owned X11 window, Recorder pause/resume/Stop, Editor and decoded APNG pixels/timing verified"
            }, new JsonSerializerOptions { WriteIndented = true }));
            await Ui(editor.Close);
            await UntilAsync(() => Desktop.Windows.OfType<StartupWindow>().Count(window => window.IsVisible) == 1);
        }
        finally
        {
            await Ui(source.Close);
        }

        async Task Pause()
        {
            await Ui(() => Click(recorder!.FindControl<Button>("RecordButton")!));
            await UntilAsync(() => recorder!.FindControl<TextBlock>("RecordButtonLabel")!.Text == "Resume");
        }
    }
}
