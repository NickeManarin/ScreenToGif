using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux;

public partial class MainWindow
{
    private bool _deferSelectionPreview;
    private Func<Task>? _deleteShortcut;
    private Func<Task>? _undoShortcut;
    private Func<Task>? _redoShortcut;
    private Action? _selectAllShortcut;

    private void FrameSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateFrameInfo();

        if (_deferSelectionPreview)
            return;

        // In multiple-selection mode Avalonia's SelectedItem is the first item
        // in the range. Shift+Arrow moves focus to the active end of the range,
        // so wait until focus has moved before choosing the preview frame.
        Dispatcher.UIThread.Post(UpdateCurrentFramePreview);
    }

    private void UpdateCurrentFramePreview()
    {
        if (_deferSelectionPreview)
            return;

        var frame = GetFocusedTimelineFrame() ?? FrameListBox.SelectedItem as EditorFrame;
        SetCurrentFrameIndex(frame is null ? -1 : _frames.IndexOf(frame));

        if (frame is null)
        {
            SetPreview(null);
            UpdateFrameInfo();
            return;
        }

        DelayTextBox.Text = frame.DelayMs.ToString();
        SetPreview(frame);
        UpdateFrameInfo();
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

    private async void FrameListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && e.KeyModifiers == KeyModifiers.None && _deleteShortcut is not null)
        {
            await _deleteShortcut();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control && _undoShortcut is not null)
        {
            await _undoShortcut();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Y && e.KeyModifiers == KeyModifiers.Control && _redoShortcut is not null)
        {
            await _redoShortcut();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control && _selectAllShortcut is not null)
        {
            _selectAllShortcut();
            e.Handled = true;
        }
    }
}
