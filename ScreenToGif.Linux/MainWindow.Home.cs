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
    private FrameClipboard? _clipboard;
    private FrameClipboard EditorClipboard
    {
        get
        {
            if (_clipboard is not null)
                return _clipboard;

            _clipboard = new FrameClipboard();
            CleanupRequested += _clipboard.Dispose;
            return _clipboard;
        }
    }

    partial void InitializeHome()
    {
        HistoryStateChanged += UpdateHistoryButtons;
        _undoShortcut = UndoAsync;
        _redoShortcut = RedoAsync;
        _selectAllShortcut = SelectAllFrames;
        UpdateHistoryButtons();
    }

    private EditorFrame[] GetSelectedFramesInTimelineOrder() =>
        GetSelectedFrames().OrderBy(frame => _frames.IndexOf(frame)).ToArray();

    private async Task RestoreSnapshotAsync(EditorSnapshot snapshot, CancellationToken cancellationToken)
    {
        StopPreview();
        var restored = await PrepareFramesAsync(
            snapshot.Frames.Select(state => new EditorFrame(state.FilePath, state.DelayMs)),
            cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            foreach (var frame in restored)
                frame.Dispose();
            throw;
        }

        SetCurrentFrameIndex(-1);
        foreach (var frame in _frames)
            frame.Dispose();

        _frames.Clear();
        foreach (var frame in restored)
            _frames.Add(frame);

        var selectedItems = FrameListBox.SelectedItems;
        selectedItems?.Clear();
        foreach (var index in snapshot.SelectedIndices.Where(index => index >= 0 && index < _frames.Count))
            selectedItems?.Add(_frames[index]);

        UpdateFrameInfo();
        UpdateCurrentFramePreview();
        UpdateHistoryButtons();
    }

    private void UpdateHistoryButtons()
    {
        UndoButton.IsEnabled = _history.CanUndo;
        RedoButton.IsEnabled = _history.CanRedo;
        ToolTip.SetTip(UndoButton, _history.CanUndo ? "Undo the last edit." : "Nothing to undo.");
        ToolTip.SetTip(RedoButton, _history.CanRedo ? "Redo the last undone edit." : "Nothing to redo.");
    }

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

    private async void UndoClick(object? sender, RoutedEventArgs e) => await UndoAsync();

    private async void RedoClick(object? sender, RoutedEventArgs e) => await RedoAsync();

    private async void ResetClick(object? sender, RoutedEventArgs e)
    {
        var current = CaptureSnapshot();
        if (!_history.TryGetResetTarget(current, out var baseline))
        {
            SetStatus("The project is already at its loaded state.");
            return;
        }

        await RunOperationAsync("Reset project", async cancellationToken =>
        {
            await RestoreSnapshotAsync(baseline, cancellationToken);
            CommitEditorMutation("Reset project", current, "Reset the project. Undo is available.");
        });
    }

    private async Task UndoAsync()
    {
        await RunOperationAsync("Undo", async cancellationToken =>
        {
            if (!_history.TryUndo(out var state, out var description))
            {
                SetStatus("Nothing to undo.");
                return;
            }

            try
            {
                await RestoreSnapshotAsync(state, cancellationToken);
                UpdateDirtyState();
                SetStatus($"Undid: {description}.");
            }
            catch
            {
                _history.TryRedo(out _, out _);
                throw;
            }
        });
    }

    private async Task RedoAsync()
    {
        await RunOperationAsync("Redo", async cancellationToken =>
        {
            if (!_history.TryRedo(out var state, out var description))
            {
                SetStatus("Nothing to redo.");
                return;
            }

            try
            {
                await RestoreSnapshotAsync(state, cancellationToken);
                UpdateDirtyState();
                SetStatus($"Redid: {description}.");
            }
            catch
            {
                _history.TryUndo(out _, out _);
                throw;
            }
        });
    }

    private async void CopyClick(object? sender, RoutedEventArgs e) => await CopySelectedAsync();

    private async void CutClick(object? sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Cut frames", async cancellationToken =>
        {
            StopPreview();
            var selected = GetSelectedFramesInTimelineOrder();
            if (selected.Length == 0)
            {
                SetStatus("Select at least one frame to cut.");
                return;
            }

            var before = CaptureSnapshot();
            await EditorClipboard.CopyAsync(selected, cancellationToken);
            var firstIndex = _frames.IndexOf(selected[0]);
            foreach (var frame in selected)
            {
                _frames.Remove(frame);
                frame.Dispose();
            }

            FrameListBox.SelectedItems?.Clear();
            if (_frames.Count > 0)
                FrameListBox.SelectedIndex = Math.Min(firstIndex, _frames.Count - 1);
            CommitEditorMutation("Cut frames", before, $"Cut {selected.Length} frame(s) to the editor clipboard.");
        });
    }

    private async Task CopySelectedAsync()
    {
        await RunOperationAsync("Copy frames", async cancellationToken =>
        {
            var selected = GetSelectedFramesInTimelineOrder();
            if (selected.Length == 0)
            {
                SetStatus("Select at least one frame to copy.");
                return;
            }

            await EditorClipboard.CopyAsync(selected, cancellationToken);
            SetStatus($"Copied {selected.Length} frame(s) to the editor clipboard.");
        });
    }

    private async void PasteClick(object? sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Paste frames", async cancellationToken =>
        {
            if (!TryEnsureFrameCapacity(EditorClipboard.FrameCount, "Pasting frames"))
                return;

            StopPreview();
            var before = CaptureSnapshot();
            var pasted = await EditorClipboard.CreatePasteAsync(_workspace, cancellationToken);
            await EditorProjectBudget.EnsureTimelineBudgetAsync(
                _frames.Select(frame => frame.FilePath).Concat(pasted.Select(frame => frame.FilePath)),
                "Pasting frames",
                cancellationToken);
            var selectedIndices = GetSelectedFrames().Select(frame => _frames.IndexOf(frame)).Where(index => index >= 0).ToArray();
            var insertionIndex = selectedIndices.Length == 0 ? _frames.Count : selectedIndices.Max() + 1;
            var inserted = await PrepareFramesAsync(
                pasted.Select(state => new EditorFrame(state.FilePath, state.DelayMs)),
                cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                foreach (var frame in inserted)
                    frame.Dispose();
                throw;
            }

            foreach (var frame in inserted)
            {
                _frames.Insert(insertionIndex++, frame);
            }

            RestoreSelection(inserted);
            CommitEditorMutation("Paste frames", before, $"Pasted {inserted.Count} independent frame(s).");
        });
    }

    private void ActualSizeClick(object? sender, RoutedEventArgs e) => ApplyZoom(100);

    private void SizeToContentClick(object? sender, RoutedEventArgs e)
    {
        ApplyZoom(100);
        if (_previewBitmap is null)
        {
            SetStatus("Open or select a frame before sizing the window.");
            return;
        }

        Width = Math.Clamp(_previewBitmap.PixelSize.Width + 40, MinWidth, 1920);
        Height = Math.Clamp(_previewBitmap.PixelSize.Height + 260, MinHeight, 1200);
        SetStatus("Sized the editor window to the current image within screen-safe limits.");
    }

    private void FitImageClick(object? sender, RoutedEventArgs e)
    {
        if (_previewBitmap is null)
        {
            SetStatus("Open or select a frame before fitting the preview.");
            return;
        }

        PreviewImage.Width = double.NaN;
        PreviewImage.Height = double.NaN;
        PreviewImage.Stretch = Avalonia.Media.Stretch.Uniform;
        ZoomTextBox.Text = "Fit";
        SetStatus("Fitting the current image in the preview.");
    }

    private void ZoomKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyZoomFromInput();
            e.Handled = true;
        }
    }

    private void ZoomLostFocus(object? sender, RoutedEventArgs e) => ApplyZoomFromInput();

    private void ApplyZoomFromInput()
    {
        if (!ZoomLevel.TryParse(ZoomTextBox.Text, out var percent))
        {
            SetStatus($"Zoom must be between {ZoomLevel.Minimum}% and {ZoomLevel.Maximum}%.");
            return;
        }

        ApplyZoom(percent);
    }

    private void ApplyZoom(int percent)
    {
        if (_previewBitmap is null)
        {
            SetStatus("Open or select a frame before changing zoom.");
            return;
        }

        PreviewImage.Stretch = Avalonia.Media.Stretch.Fill;
        PreviewImage.Width = _previewBitmap.PixelSize.Width * percent / 100d;
        PreviewImage.Height = _previewBitmap.PixelSize.Height * percent / 100d;
        ZoomTextBox.Text = percent.ToString();
        SetStatus($"Preview zoom: {percent}%.");
    }

    private void SelectAllClick(object? sender, RoutedEventArgs e) => SelectAllFrames();

    private void SelectAllFrames()
    {
        FrameListBox.SelectAll();
        SetStatus($"Selected all {_frames.Count} frame(s).");
    }

    private void InverseSelectionClick(object? sender, RoutedEventArgs e)
    {
        var selectedIndices = GetSelectedFrames().Select(frame => _frames.IndexOf(frame));
        var inverse = TimelineSelection.Invert(_frames.Count, selectedIndices);
        FrameListBox.SelectedItems?.Clear();
        foreach (var index in inverse)
            FrameListBox.SelectedItems?.Add(_frames[index]);
        UpdateFrameInfo();
        UpdateCurrentFramePreview();
        SetStatus("Inverted the timeline selection.");
    }

    private void DeselectClick(object? sender, RoutedEventArgs e)
    {
        FrameListBox.SelectedItems?.Clear();
        UpdateFrameInfo();
        UpdateCurrentFramePreview();
        SetStatus("Cleared the timeline selection.");
    }

    private async void GoToClick(object? sender, RoutedEventArgs e)
    {
        if (_frames.Count == 0)
        {
            SetStatus("There are no frames to navigate to.");
            return;
        }

        var values = await new Controls.ParameterDialog("Go to frame", ("Frame number (1-based)", "1")).ShowForAsync(this);
        if (values is null)
            return;
        if (!TimelineSelection.TryResolveOneBased(values[0], _frames.Count, out var index))
        {
            SetStatus($"Frame number must be between 1 and {_frames.Count}.");
            return;
        }

        FrameListBox.SelectedItems?.Clear();
        FrameListBox.SelectedIndex = index;
        FrameListBox.ScrollIntoView(_frames[index]);
        UpdateCurrentFramePreview();
        SetStatus($"Moved to frame {index + 1}.");
    }
}
