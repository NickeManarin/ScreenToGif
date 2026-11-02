namespace ScreenToGif.Linux.Services;

public enum CaptureShellKind
{
    Recorder,
    Webcam,
    Board
}

/// <summary>What the editor should do with a recording when the shell that made it closes.</summary>
public enum CaptureShellAdoption
{
    /// <summary>Open the recording as the project, replacing what the editor held.</summary>
    ReplaceProject,

    /// <summary>Append the recording to the project the editor holds, as one undoable step.</summary>
    AppendToProject
}

public interface ICaptureShellWindow
{
    event EventHandler? Closed;

    /// <summary>
    /// True once this shell has passed its result to another window, which then owns the session.
    /// Startup stays closed in that case rather than reappearing behind the editor. Shells that
    /// produce nothing never set it, which is why it has a default.
    /// </summary>
    bool HandedOffToEditor => false;

    void Show();

    void Activate();

    /// <summary>
    /// The project this shell captured, handed over exactly once after it closes; null when it captured
    /// nothing or handed its result over itself. The caller owns what it returns and must adopt or
    /// dispose it. Shells that never return a project need not implement it.
    /// </summary>
    LoadedProject? TakeRecording() => null;
}

public interface ICaptureShellHost
{
    ICaptureShellWindow CreateShell(CaptureShellKind kind);

    void HideStartup();

    void RestoreStartup();

    /// <summary>Closes Startup for good, because another window has taken over the session.</summary>
    void CloseStartup();

    /// <summary>
    /// Opens the editor on a capture shell's recording, replacing its project or appending to it as
    /// <paramref name="adoption"/> says. Takes ownership and returns true when it did; returns false
    /// when it could not, having already told the user why, and leaves the recording to the caller to
    /// dispose. It reports its own failures rather than throwing, because it runs from a
    /// window-closed notification where nothing is left to catch.
    /// </summary>
    bool AdoptRecording(LoadedProject recording, CaptureShellAdoption adoption);

    /// <summary>
    /// Tells the user about a failure the coordinator could not route anywhere else. It is called from
    /// a window-closed notification, so it must not throw.
    /// </summary>
    void ReportShellFailure(Exception failure);
}

/// <summary>
/// Owns the single-child capture-shell lifecycle while retaining one Startup window.
/// </summary>
public sealed class CaptureShellCoordinator(ICaptureShellHost host)
{
    private ICaptureShellWindow? _activeShell;
    private CaptureShellAdoption _activeAdoption;

    public CaptureShellKind? ActiveKind { get; private set; }

    /// <summary>The shell currently open, or null when none is. One owner of this fact, cleared on close.</summary>
    public ICaptureShellWindow? ActiveShell => _activeShell;

    /// <summary>
    /// Opens a shell of <paramref name="kind"/>, or activates the one already open. The recording it
    /// makes is adopted as <paramref name="adoption"/> says; a request that only activates an open
    /// shell leaves that shell's adoption as it was.
    /// </summary>
    public bool Open(CaptureShellKind kind, CaptureShellAdoption adoption = CaptureShellAdoption.ReplaceProject)
    {
        if (_activeShell is not null)
        {
            _activeShell.Activate();
            return false;
        }

        var shell = host.CreateShell(kind);
        _activeShell = shell;
        _activeAdoption = adoption;
        ActiveKind = kind;
        shell.Closed += ShellClosed;

        try
        {
            host.HideStartup();
            shell.Show();
            return true;
        }
        catch
        {
            shell.Closed -= ShellClosed;
            _activeShell = null;
            ActiveKind = null;
            host.RestoreStartup();
            throw;
        }
    }

    /// <summary>
    /// A shell that captured frames hands them to the editor and Startup stays closed behind it; a shell
    /// that captured nothing simply gives Startup back.
    /// </summary>
    private void ShellClosed(object? sender, EventArgs e)
    {
        var shell = _activeShell;
        if (shell is null || !ReferenceEquals(sender, shell))
            return;

        var adoption = _activeAdoption;
        shell.Closed -= ShellClosed;
        _activeShell = null;
        ActiveKind = null;

        // A shell that handed its result to the editor itself leaves nothing to adopt; Startup just
        // stays closed behind the window that now owns the session.
        if (shell.HandedOffToEditor)
        {
            host.CloseStartup();
            return;
        }

        LoadedProject? recording;
        try
        {
            recording = shell.TakeRecording();
        }
        catch
        {
            host.RestoreStartup();
            throw;
        }

        if (recording is null)
        {
            host.RestoreStartup();
            return;
        }

        var adopted = false;
        try
        {
            adopted = host.AdoptRecording(recording, adoption);
        }
        catch (Exception ex)
        {
            // Nothing above a window-closed notification can catch this, and losing the recording
            // silently would be worse than reporting it, so the host is asked to say so instead.
            host.ReportShellFailure(ex);
        }
        finally
        {
            if (!adopted)
            {
                recording.Dispose();
                host.RestoreStartup();
            }
        }
    }
}
