using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace ScreenToGif.Linux;

public partial class StartupWindow : Window
{
    private bool _openingEditor;

    public StartupWindow()
    {
        InitializeComponent();
        Closed += StartupClosed;
    }

    private void OpenEditorClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        _openingEditor = true;
        var editor = new MainWindow();
        editor.Closed += (_, _) => desktop.Shutdown();
        desktop.MainWindow = editor;
        editor.Show();
        Close();
    }

    private void StartupClosed(object? sender, EventArgs e)
    {
        if (!_openingEditor && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
