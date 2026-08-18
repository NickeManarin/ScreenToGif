using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using System.Collections.ObjectModel;

namespace ScreenToGif.Linux;

public partial class MainWindow : Window
{
    private readonly FfmpegTool _ffmpeg = new();
    private readonly ObservableCollection<EditorFrame> _frames = [];
    private readonly List<string> _workspaces = [];
    private readonly MediaImporter _importer;
    private readonly FfmpegExporter _exporter;
    private readonly FrameDeletionHistory _deletionHistory = new();
    private readonly DispatcherTimer _previewTimer = new();
    private string _workspacePath;
    private Bitmap? _previewBitmap;
    private int _previewIndex;
    private bool _isPlaying;

    public MainWindow()
    {
        InitializeComponent();

        _importer = new MediaImporter(_ffmpeg);
        _exporter = new FfmpegExporter(_ffmpeg);
        _workspacePath = ProjectArchive.CreateWorkspace();
        _workspaces.Add(_workspacePath);

        FrameListBox.ItemsSource = _frames;
        UpdateFrameInfo();
        _previewTimer.Tick += PreviewTimerTick;
        Closed += (_, _) => CleanupWorkspaces();
    }

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
        SetStatus($"Importing {paths.Count} file(s)...");
        var imported = await _importer.ImportAsync(paths, _workspacePath);
        _deletionHistory.Clear();
        AddFrames(imported);
        SetStatus($"Imported {imported.Count} frame(s).");
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

            SetStatus("Loading project...");
            var loaded = await ProjectArchive.LoadAsync(path);
            var oldWorkspace = _workspacePath;
            _workspacePath = loaded.WorkspacePath;
            _workspaces.Add(_workspacePath);

            ReplaceFrames(loaded.Frames);
            ProjectArchive.TryDeleteWorkspace(oldWorkspace);
            _workspaces.Remove(oldWorkspace);
            SetStatus($"Loaded {loaded.Frames.Count} frame(s).");
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

            SetStatus("Saving project...");
            await ProjectArchive.SaveAsync(path, _frames);
            SetStatus($"Saved project to {Path.GetFileName(path)}.");
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

    private void PlayClick(object? sender, RoutedEventArgs e)
    {
        if (_frames.Count == 0)
        {
            SetStatus("There are no frames to preview.");
            return;
        }

        _previewIndex = Math.Max(0, FrameListBox.SelectedIndex);
        _isPlaying = true;
        PreviewTimerTick(this, EventArgs.Empty);
        SetStatus("Playing preview...");
    }

    private void StopClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        SetStatus("Preview stopped.");
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

            SetStatus($"Exporting {formatName}...");
            await _exporter.ExportAsync(_frames, path);
            SetStatus($"Exported {formatName} to {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private void FrameSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateFrameInfo();

        if (_isPlaying)
            return;

        // In multiple-selection mode Avalonia's SelectedItem is the first item
        // in the range. Shift+Arrow moves focus to the active end of the range,
        // so wait until focus has moved before choosing the preview frame.
        Dispatcher.UIThread.Post(UpdateCurrentFramePreview);
    }

    private void UpdateCurrentFramePreview()
    {
        if (_isPlaying)
            return;

        var frame = GetFocusedTimelineFrame() ?? FrameListBox.SelectedItem as EditorFrame;

        if (frame is null)
        {
            SetPreview(null);
            return;
        }

        DelayTextBox.Text = frame.DelayMs.ToString();
        SetPreview(frame);
    }

    private EditorFrame? GetFocusedTimelineFrame()
    {
        var focused = TopLevel.GetTopLevel(FrameListBox)?.FocusManager?.GetFocusedElement();

        for (var visual = focused as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is ListBoxItem { DataContext: EditorFrame frame } &&
                FrameListBox.SelectedItems?.Contains(frame) == true)
                return frame;
        }

        return null;
    }

    private void FrameListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && e.KeyModifiers == KeyModifiers.None)
        {
            DeleteSelectedFrames();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control)
        {
            UndoDelete();
            e.Handled = true;
        }
    }

    private void DeleteFrameClick(object? sender, RoutedEventArgs e) => DeleteSelectedFrames();

    private void DeleteSelectedFrames()
    {
        var selected = GetSelectedFrames();

        if (selected.Length == 0)
            return;

        StopPreview();
        var deleted = selected
            .Select(frame => (frame, index: _frames.IndexOf(frame)))
            .Where(item => item.index >= 0)
            .OrderBy(item => item.index)
            .ToArray();

        if (deleted.Length == 0)
            return;

        _deletionHistory.Record(
            deleted.Select(item => item.frame),
            deleted.Select(item => item.index),
            selected,
            FrameListBox.SelectedIndex);

        foreach (var item in deleted)
        {
            _frames.Remove(item.frame);
            item.frame.Dispose();
        }

        FrameListBox.SelectedItems?.Clear();
        if (_frames.Count > 0)
            FrameListBox.SelectedIndex = Math.Min(deleted[0].index, _frames.Count - 1);

        UpdateFrameInfo();
        SetStatus(deleted.Length == 1
            ? "Deleted selected frame."
            : $"Deleted {deleted.Length} selected frames.");
    }

    private void UndoDelete()
    {
        StopPreview();

        if (!_deletionHistory.TryRestore(_frames, out var deleted))
        {
            SetStatus("Nothing to undo.");
            return;
        }

        foreach (var frame in deleted.Frames)
        {
            if (frame.Thumbnail is not null || !File.Exists(frame.FilePath))
                continue;

            try
            {
                frame.Thumbnail = new Bitmap(frame.FilePath);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        RestoreSelection(deleted.SelectedFrames);
        UpdateFrameInfo();
        SetStatus(deleted.Frames.Count == 1
            ? "Restored deleted frame."
            : $"Restored {deleted.Frames.Count} deleted frames.");
    }

    private void UndoDeleteClick(object? sender, RoutedEventArgs e) => UndoDelete();

    private void MoveUpClick(object? sender, RoutedEventArgs e) => MoveSelected(-1);

    private void MoveDownClick(object? sender, RoutedEventArgs e) => MoveSelected(1);

    private void MoveSelected(int offset)
    {
        var selected = GetSelectedFrames();

        if (selected.Length == 0 || offset == 0)
            return;

        _deletionHistory.Clear();
        var selectedSet = selected.ToHashSet();
        var reordered = _frames.ToArray();

        if (offset < 0)
        {
            for (var index = 1; index < reordered.Length; index++)
            {
                if (selectedSet.Contains(reordered[index]) && !selectedSet.Contains(reordered[index - 1]))
                    (reordered[index - 1], reordered[index]) = (reordered[index], reordered[index - 1]);
            }
        }
        else
        {
            for (var index = reordered.Length - 2; index >= 0; index--)
            {
                if (selectedSet.Contains(reordered[index]) && !selectedSet.Contains(reordered[index + 1]))
                    (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
            }
        }

        for (var index = 0; index < reordered.Length; index++)
        {
            var currentIndex = _frames.IndexOf(reordered[index]);
            if (currentIndex != index)
                _frames.Move(currentIndex, index);
        }

        RestoreSelection(selected);
        UpdateFrameInfo();
        SetStatus(selected.Length == 1
            ? "Reordered selected frame."
            : $"Reordered {selected.Length} selected frames.");
    }

    private void ApplyDelayClick(object? sender, RoutedEventArgs e)
    {
        if (!TryReadDelay(out var delay))
            return;

        var selected = GetSelectedFrames();
        if (selected.Length == 0)
            return;

        foreach (var frame in selected)
            frame.DelayMs = delay;

        SetStatus(selected.Length == 1
            ? $"Set selected frame delay to {delay} ms."
            : $"Set {selected.Length} selected frame delays to {delay} ms.");
    }

    private void ApplyDelayAllClick(object? sender, RoutedEventArgs e)
    {
        if (!TryReadDelay(out var delay))
            return;

        foreach (var frame in _frames)
            frame.DelayMs = delay;

        SetStatus($"Set all frame delays to {delay} ms.");
    }

    private void ExitClick(object? sender, RoutedEventArgs e) => Close();

    private void AddFrames(IEnumerable<EditorFrame> frames)
    {
        foreach (var frame in frames)
        {
            frame.Thumbnail = new Bitmap(frame.FilePath);
            _frames.Add(frame);
        }

        if (FrameListBox.SelectedIndex < 0 && _frames.Count > 0)
            FrameListBox.SelectedIndex = 0;

        UpdateFrameInfo();
    }

    private void ReplaceFrames(IEnumerable<EditorFrame> frames)
    {
        _deletionHistory.Clear();

        foreach (var frame in _frames)
            frame.Dispose();

        _frames.Clear();
        SetPreview(null);
        AddFrames(frames);
    }

    private void SetPreview(EditorFrame? frame)
    {
        _previewBitmap?.Dispose();
        _previewBitmap = null;
        PreviewImage.Source = null;
        EmptyPreviewText.IsVisible = frame == null;

        if (frame == null)
            return;

        try
        {
            _previewBitmap = new Bitmap(frame.FilePath);
            PreviewImage.Source = _previewBitmap;
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    }

    private bool TryReadDelay(out int delay)
    {
        if (int.TryParse(DelayTextBox.Text, out delay) && delay > 0)
            return true;

        SetStatus("Delay must be a positive whole number of milliseconds.");
        return false;
    }

    private static string? GetLocalPath(IStorageItem item) => item.TryGetLocalPath();

    private static string[] GetDroppedPaths(IDataTransfer dataTransfer)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in dataTransfer.TryGetFiles() ?? [])
        {
            var path = GetLocalPath(item);
            if (path is not null && File.Exists(path))
                paths.Add(path);
        }

        if (paths.Count == 0 && dataTransfer.TryGetText() is { } text)
        {
            foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var value = line.Trim();
                if (value.Length == 0 || value.StartsWith('#'))
                    continue;

                if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
                {
                    var path = uri.LocalPath;
                    if (File.Exists(path))
                        paths.Add(path);
                }
                else if (File.Exists(value))
                {
                    paths.Add(value);
                }
            }
        }

        return paths.ToArray();
    }

    private EditorFrame[] GetSelectedFrames() =>
        FrameListBox.SelectedItems?.OfType<EditorFrame>().ToArray() ?? [];

    private void RestoreSelection(IEnumerable<EditorFrame> frames)
    {
        var selectedItems = FrameListBox.SelectedItems;
        if (selectedItems is null)
            return;

        selectedItems.Clear();

        foreach (var frame in frames)
            selectedItems.Add(frame);

        UpdateFrameInfo();
    }

    private void PreviewTimerTick(object? sender, EventArgs e)
    {
        if (!_isPlaying || _frames.Count == 0)
        {
            StopPreview();
            return;
        }

        if (_previewIndex >= _frames.Count)
            _previewIndex = 0;

        var frame = _frames[_previewIndex];
        FrameListBox.SelectedIndex = _previewIndex;
        SetPreview(frame);
        _previewTimer.Interval = TimeSpan.FromMilliseconds(frame.DelayMs);
        _previewTimer.Start();
        _previewIndex = (_previewIndex + 1) % _frames.Count;
    }

    private void StopPreview()
    {
        _isPlaying = false;
        _previewTimer.Stop();
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Foreground = null;
    }

    private void UpdateFrameInfo()
    {
        for (var index = 0; index < _frames.Count; index++)
            _frames[index].FrameNumber = index;

        FrameCountText.Text = _frames.Count.ToString();
        SelectedCountText.Text = GetSelectedFrames().Length.ToString();
        CurrentFrameText.Text = FrameListBox.SelectedIndex >= 0
            ? (FrameListBox.SelectedIndex + 1).ToString()
            : "—";
    }

    private void SetError(Exception exception)
    {
        StatusText.Text = $"Error: {exception.Message}";
        StatusText.Foreground = Avalonia.Media.Brushes.IndianRed;
    }

    private void CleanupWorkspaces()
    {
        StopPreview();
        _previewBitmap?.Dispose();

        foreach (var frame in _frames)
            frame.Dispose();

        foreach (var workspace in _workspaces.Distinct(StringComparer.Ordinal))
            ProjectArchive.TryDeleteWorkspace(workspace);
    }
}
