using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class StartupWindow : Window
{
    private bool _openingEditor;

    public StartupWindow()
    {
        InitializeComponent();
        Closed += StartupClosed;
    }

    /// <summary>Set while this window closes to hand over to the editor, so closing does not end the application.</summary>
    internal void CloseForEditor()
    {
        _openingEditor = true;
        Close();
    }

    private void OpenRecorderClick(object? sender, RoutedEventArgs e) =>
        App.CurrentApp?.OpenCaptureShell(CaptureShellKind.Recorder);

    private void OpenWebcamClick(object? sender, RoutedEventArgs e) =>
        App.CurrentApp?.OpenCaptureShell(CaptureShellKind.Webcam);

    private void OpenBoardClick(object? sender, RoutedEventArgs e) =>
        App.CurrentApp?.OpenCaptureShell(CaptureShellKind.Board);

    private void OpenEditorClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var editor = App.CreateEditorWindow();
        editor.Closed += (_, _) => App.CurrentApp?.HandleWindowClosed();
        desktop.MainWindow = editor;
        editor.Show();
        CloseForEditor();
    }

    private void OpenOptionsClick(object? sender, RoutedEventArgs e) => _ = App.CurrentApp?.ShowOptions(this);

    private void StartupClosed(object? sender, EventArgs e)
    {
        if (!_openingEditor)
            App.CurrentApp?.HandleWindowClosed();
    }
}
