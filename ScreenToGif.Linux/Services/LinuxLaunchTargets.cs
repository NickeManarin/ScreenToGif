namespace ScreenToGif.Linux.Services;

/// <summary>
/// Which capture shell a remembered startup window or a tray click opens. One table for both, so a
/// recorder that one surface can open is never silently replaced by StartUp on the other.
/// </summary>
public static class LinuxLaunchTargets
{
    /// <summary>The shell a startup choice opens; null for StartUp and the Editor, which are not shells.</summary>
    public static CaptureShellKind? ShellFor(LinuxStartupWindow window) => window switch
    {
        LinuxStartupWindow.Recorder => CaptureShellKind.Recorder,
        LinuxStartupWindow.Webcam => CaptureShellKind.Webcam,
        LinuxStartupWindow.Board => CaptureShellKind.Board,
        _ => null
    };

    /// <summary>The shell a tray click opens; null for no window, StartUp and the Editor.</summary>
    public static CaptureShellKind? ShellFor(LinuxTrayWindow window) => window switch
    {
        LinuxTrayWindow.Recorder => CaptureShellKind.Recorder,
        LinuxTrayWindow.Webcam => CaptureShellKind.Webcam,
        LinuxTrayWindow.Board => CaptureShellKind.Board,
        _ => null
    };

    /// <summary>
    /// The windows the tray menu opens directly, in menu order: every tray window choice except None. The
    /// menu is built from this list so a window the click settings can open is never missing from it.
    /// </summary>
    public static IReadOnlyList<(LinuxTrayWindow Window, string Header)> TrayMenuEntries { get; } =
    [
        (LinuxTrayWindow.Startup, "Startup window"),
        (LinuxTrayWindow.Recorder, "Screen recorder"),
        (LinuxTrayWindow.Webcam, "Webcam recorder"),
        (LinuxTrayWindow.Board, "Board recorder"),
        (LinuxTrayWindow.Editor, "Editor")
    ];
}
