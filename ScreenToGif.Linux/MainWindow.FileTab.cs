using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class MainWindow
{
    private readonly RecentProjectStore _recentProjects = new();
    private readonly DestructiveActionGuard _destructiveActions = new();
    private BlankProjectFactory? _blankProjectFactory;
    private bool _allowClose;

    private BlankProjectFactory BlankProjects => _blankProjectFactory ??= new BlankProjectFactory(_ffmpeg);

    partial void InitializeFileLifecycle() => Closing += WindowClosing;

    private async void OpenMediaClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open media",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("Supported media")
                    {
                        Patterns =
                        [
                            "*.apng", "*.avi", "*.avif", "*.bmp", "*.gif", "*.jpeg", "*.jpg",
                            "*.mkv", "*.mov", "*.mp4", "*.png", "*.webm", "*.webp", "*.wmv"
                        ]
                    }
                ]
            });

            var paths = files.Select(GetLocalPath).Where(path => path != null).Cast<string>().ToArray();

            if (paths.Length == 0)
                return;

            await ImportPathsAsync(paths);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        // Some Linux file managers expose file drops as text/uri-list instead of
        // Avalonia's structured DataFormat.File. Accept the drag first and let
        // GetDroppedPaths decide whether it contains usable local files.
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        e.Handled = true;

        try
        {
            var paths = GetDroppedPaths(e.DataTransfer);

            if (paths.Length == 0)
            {
                SetStatus("Drop one or more media files to import them.");
                return;
            }

            await ImportPathsAsync(paths);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private async Task ImportPathsAsync(IReadOnlyList<string> paths)
    {
        await RunOperationAsync("Import", async cancellationToken =>
        {
            SetStatus($"Importing {paths.Count} file(s)... Use Stop to cancel.");
            var before = CaptureSnapshot();
            var imported = await _importer.ImportAsync(paths, _workspace, _frames.Count, cancellationToken);
            await AddFramesAsync(imported, "Importing media", cancellationToken);
            _mutations.FinalizeImport(before);
            SetStatus($"Imported {imported.Count} frame(s).");
        });
    }

    private async void OpenProjectClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open ScreenToGif Linux project",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("ScreenToGif Linux project") { Patterns = ["*.stg-linux"] }
                ]
            });

            var path = files.Select(GetLocalPath).FirstOrDefault(value => value != null);

            if (path == null)
                return;

            await OpenProjectPathAsync(path);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private async void SaveProjectClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_frames.Count == 0)
            {
                SetStatus("There are no frames to save.");
                return;
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save ScreenToGif Linux project",
                SuggestedFileName = "screen-recording.stg-linux",
                DefaultExtension = "stg-linux",
                FileTypeChoices =
                [
                    new FilePickerFileType("ScreenToGif Linux project") { Patterns = ["*.stg-linux"] }
                ]
            });

            var path = file?.TryGetLocalPath();

            if (path == null)
                return;

            await RunOperationAsync("Save project", async cancellationToken =>
            {
                SetStatus("Saving project... Use Stop to cancel.");
                await ProjectArchive.SaveAsync(path, _frames, cancellationToken);
                MarkClean();
                var remembered = await _recentProjects.TryAddAsync(path, CancellationToken.None);
                SetStatus(remembered
                    ? $"Saved project to {Path.GetFileName(path)}."
                    : $"Saved project to {Path.GetFileName(path)}, but the recent-project list could not be updated.");
            });
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private async void ExportGifClick(object? sender, RoutedEventArgs e) => await ExportClickAsync("gif", "GIF");

    private async void ExportApngClick(object? sender, RoutedEventArgs e) => await ExportClickAsync("apng", "APNG");

    private async void ExportMp4Click(object? sender, RoutedEventArgs e) => await ExportClickAsync("mp4", "MP4");

    private async void ExportWebmClick(object? sender, RoutedEventArgs e) => await ExportClickAsync("webm", "WebM");

    private async void BlankProjectClick(object? sender, RoutedEventArgs e)
    {
        var values = await new Controls.ParameterDialog(
            "Create blank project",
            ("Width (pixels)", "800"),
            ("Height (pixels)", "600"),
            ("Frame count", "1"),
            ("Delay per frame (milliseconds)", "100")).ShowForAsync(this);
        if (values is null)
            return;
        if (!TryPositive(values[0], "Width", out var width) ||
            !TryPositive(values[1], "Height", out var height) ||
            !TryPositive(values[2], "Frame count", out var count) ||
            !TryPositive(values[3], "Delay", out var delay))
            return;

        if (!await ConfirmProjectReplacementAsync("create a blank project"))
            return;

        await RunOperationAsync("Create blank project", async cancellationToken =>
        {
            SetStatus("Creating blank project... Use Stop to cancel.");
            var loaded = await BlankProjects.CreateAsync(width, height, count, delay, cancellationToken);
            await ReplaceActiveProjectAsync(loaded, cancellationToken);
            UpdateDirtyState();
            SetStatus($"Created {count} blank {width}×{height} frame(s).");
        });
    }

    /// <summary>
    /// Opens the webcam recorder over the editor. A recording with frames replaces the current project
    /// through the same confirmation Open project uses; closing the recorder with nothing recorded, or
    /// declining that confirmation, leaves the project untouched.
    /// </summary>
    private void NewWebcamRecordingClick(object? sender, RoutedEventArgs e) =>
        OpenCaptureShellOverEditor(CaptureShellKind.Webcam, CaptureShellAdoption.ReplaceProject);

    /// <summary>Opens the Board over the editor; a recording with frames replaces the project, as the webcam's does.</summary>
    private void NewBoardClick(object? sender, RoutedEventArgs e) =>
        OpenCaptureShellOverEditor(CaptureShellKind.Board, CaptureShellAdoption.ReplaceProject);

    /// <summary>Opens the Board over the editor; a recording with frames is appended to the project as one undoable step.</summary>
    private void InsertBoardClick(object? sender, RoutedEventArgs e) =>
        OpenCaptureShellOverEditor(CaptureShellKind.Board, CaptureShellAdoption.AppendToProject);

    /// <summary>
    /// Opens a capture shell through the application's coordinator, like every other way one opens,
    /// so a second click cannot put two recorders on one device. Closing it with frames hands them
    /// back to this editor, because an editor already open is the adoption target.
    /// </summary>
    private void OpenCaptureShellOverEditor(CaptureShellKind kind, CaptureShellAdoption adoption)
    {
        try
        {
            StopPreview();
            if (App.CurrentApp?.OpenCaptureShell(kind, adoption) is null)
                SetStatus("A capture window is already open.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private async void RecentProjectsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var paths = await _recentProjects.GetExistingAsync();
            if (paths.Count == 0)
            {
                SetStatus("No saved Linux projects are available in Recents.");
                return;
            }

            var labels = paths.Select(path => $"{Path.GetFileName(path)} — {path}").ToArray();
            var choice = await new Controls.ChoiceDialog("Open recent project", labels).ShowForAsync(this);
            if (choice is not null)
                await OpenProjectPathAsync(paths[choice.Value]);
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private async void DiscardProjectClick(object? sender, RoutedEventArgs e)
    {
        if (_frames.Count == 0)
        {
            SetStatus("The project is already empty.");
            return;
        }

        if (_mutations.HasUnsavedChanges)
        {
            var confirmed = await new Controls.ConfirmDialog(
                "Discard project",
                "Discard the current frames and all unsaved edits? This cannot be undone after the workspace is replaced.",
                "Discard project").ShowForAsync(this);
            if (!confirmed)
            {
                SetStatus("Discard canceled; the project is unchanged.");
                return;
            }
        }

        await RunOperationAsync("Discard project", async cancellationToken =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workspace = ProjectArchive.CreateWorkspace();
            await ReplaceActiveProjectAsync(new LoadedProject(workspace, []), cancellationToken);
            MarkClean();
            SetStatus("Discarded the project and opened an empty workspace.");
        });
    }

    /// <summary>
    /// Opens a recording this editor did not create, taking ownership of its workspace and frames.
    /// The recorder has already closed by the time this runs, so there is nothing to confirm.
    /// </summary>
    internal async Task OpenRecordingAsync(LoadedProject recording, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recording);

        var frameCount = recording.Frames.Count;
        await ReplaceActiveProjectAsync(recording, cancellationToken);
        MarkClean();
        SetStatus(frameCount == 1
            ? "Opened 1 recorded frame."
            : $"Opened {frameCount} recorded frames.");
    }

    private async Task OpenProjectPathAsync(string path)
    {
        if (!await ConfirmProjectReplacementAsync($"open {Path.GetFileName(path)}"))
            return;

        await RunOperationAsync("Load project", async cancellationToken =>
        {
            SetStatus("Loading project... Use Stop to cancel.");
            var loaded = await ProjectArchive.LoadAsync(path, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await ReplaceActiveProjectAsync(loaded, cancellationToken);
            MarkClean();
            var remembered = await _recentProjects.TryAddAsync(path, CancellationToken.None);
            SetStatus(remembered
                ? $"Loaded {loaded.Frames.Count} frame(s) from {Path.GetFileName(path)}."
                : $"Loaded {loaded.Frames.Count} frame(s) from {Path.GetFileName(path)}, but the recent-project list could not be updated.");
        });
    }

    /// <summary>
    /// Opens a capture shell's recording in this editor, behind the same unsaved-work confirmation that
    /// opening a project asks for. Takes ownership of the recording, releasing it when the confirmation
    /// is declined or the load fails. The frames arrive unsaved, so the editor stays dirty.
    /// </summary>
    public async Task AdoptRecordedProjectAsync(LoadedProject recording)
    {
        ArgumentNullException.ThrowIfNull(recording);

        try
        {
            if (!await ConfirmProjectReplacementAsync("open the recording"))
                return;

            await RunOperationAsync("Load recording", async cancellationToken =>
            {
                SetStatus("Loading the recording...");
                await ReplaceActiveProjectAsync(recording, cancellationToken);
                UpdateDirtyState();
                SetStatus($"Loaded {_frames.Count} recorded frame(s).");
            });
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
        finally
        {
            // A no-op once the project was adopted, and the release path for every route that did not
            // reach the adoption: a declined confirmation, a busy editor, or a failed load.
            recording.Dispose();
        }
    }

    /// <summary>
    /// Appends a capture shell's recording to the open project as one undoable step. The frames are
    /// copied into this editor's workspace first, because the shell's workspace is deleted with the
    /// recording and the inserted frames and their history must outlive it. Takes ownership of the
    /// recording and releases it once the copies exist, or when the insert fails.
    /// </summary>
    public async Task InsertRecordedFramesAsync(LoadedProject recording)
    {
        ArgumentNullException.ThrowIfNull(recording);

        try
        {
            StopPreview();
            var selectedIndex = FrameListBox.SelectedIndex;
            var placement = _frames.Count == 0
                ? BoardInsertionPosition.Beginning
                : await new Controls.FrameInsertionDialog(_frames.Count, selectedIndex).ShowForAsync(this);
            if (placement is null)
            {
                SetStatus("Board insertion canceled; the current project is unchanged.");
                return;
            }

            var insertionIndex = BoardInsertionPlacement.Resolve(_frames.Count, selectedIndex, placement.Value);
            await RunOperationAsync("Insert recording", async cancellationToken =>
            {
                SetStatus("Inserting the recording... Use Stop to cancel.");
                var before = CaptureSnapshot();
                var copied = await BoardFrameTransfer.CopyIntoAsync(_workspace, recording.Frames, cancellationToken);
                await AddFramesAsync(copied, "Inserting recorded frames", cancellationToken, insertionIndex);
                _mutations.Commit("Insert recording", before);
                SetStatus($"Inserted {copied.Count} recorded frame(s).");
            });
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
        finally
        {
            recording.Dispose();
        }
    }

    private async Task<bool> ConfirmProjectReplacementAsync(string action)
    {
        var confirmed = await _destructiveActions.ConfirmAsync(
            _mutations.HasUnsavedChanges && LinuxSettings.Current.AskBeforeDiscardProject,
            () => new Controls.ConfirmDialog(
                "Replace current project",
                $"Save or discard the current edits before you {action}. Continue and permanently discard the unsaved work?",
                "Discard and continue").ShowForAsync(this));
        if (!confirmed)
            SetStatus("Project replacement canceled; the current work is unchanged.");
        return confirmed;
    }

    private async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
            return;
        if (_destructiveActions.IsPromptOpen)
        {
            e.Cancel = true;
            return;
        }

        if (_operations.RequestClose())
        {
            e.Cancel = true;
            SetStatus("Canceling the active operation before closing...");
            return;
        }

        if (!_mutations.HasUnsavedChanges || !LinuxSettings.Current.AskBeforeCloseEditor)
            return;

        e.Cancel = true;
        var confirmed = await _destructiveActions.ConfirmAsync(
            confirmationRequired: true,
            () => new Controls.ConfirmDialog(
                "Close editor",
                "Close the editor and permanently discard all unsaved changes?",
                "Discard and close").ShowForAsync(this));
        if (!confirmed)
        {
            SetStatus("Close canceled; the current work is unchanged.");
            return;
        }

        _allowClose = true;
        Close();
    }

    private async Task ReplaceActiveProjectAsync(LoadedProject loaded, CancellationToken cancellationToken)
    {
        StopPreview();
        try
        {
            await PrepareFramesAsync(loaded.Frames, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            loaded.Dispose();
            throw;
        }

        ProjectWorkspaceLifecycle.Replace(_workspace, loaded, replacement =>
        {
            ReplaceFrames(replacement.Frames);
            _workspace = replacement.Workspace;
        });
    }

    private async Task ExportClickAsync(string extension, string formatName)
    {
        try
        {
            if (_frames.Count == 0)
            {
                SetStatus("There are no frames to export.");
                return;
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Export {formatName}",
                SuggestedFileName = $"screen-recording.{extension}",
                DefaultExtension = extension,
                FileTypeChoices =
                [
                    new FilePickerFileType(formatName) { Patterns = [$"*.{extension}"] }
                ]
            });

            var path = file?.TryGetLocalPath();

            if (path == null)
                return;

            if (string.IsNullOrWhiteSpace(Path.GetExtension(path)))
                path += $".{extension}";

            await RunOperationAsync($"Export {formatName}", async cancellationToken =>
            {
                SetStatus($"Exporting {formatName}... Use Stop to cancel.");
                await _exporter.ExportAsync(_frames, path, cancellationToken);
                SetStatus($"Exported {formatName} to {Path.GetFileName(path)}.");
            });
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }
}
