using Avalonia.Interactivity;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class MainWindow
{
    partial void InitializeEdit() => _deleteShortcut = DeleteSelectedFramesAsync;

    private FrameState[] CurrentStates() =>
        _frames.Select(frame => new FrameState(frame.FilePath, frame.DelayMs)).ToArray();

    private int[] SelectedIndicesOrAll()
    {
        var selected = SelectedIndices();
        return selected.Length == 0 ? Enumerable.Range(0, _frames.Count).ToArray() : selected;
    }

    private Task RunSequenceEditAsync(
        string description,
        IReadOnlyList<FrameState> states,
        IReadOnlyList<int> selectedIndices,
        string successMessage) =>
        RunOperationAsync(description, cancellationToken =>
            ApplySequenceEditAsync(description, states, selectedIndices, successMessage, cancellationToken));

    private async Task ApplySequenceEditAsync(
        string description,
        IReadOnlyList<FrameState> states,
        IReadOnlyList<int> selectedIndices,
        string successMessage,
        CancellationToken cancellationToken)
    {
        await Task.Run(
            () => EditorResourceLimits.EnsureCanInsertFrames(0, states.Count, description),
            cancellationToken);
        await EditorProjectBudget.EnsureTimelineBudgetAsync(
            states.Select(state => state.FilePath), description, cancellationToken);

        StopPreview();
        var before = CaptureSnapshot();
        var after = new EditorSnapshot(states.ToArray(), selectedIndices.ToArray());
        if (before.Frames.SequenceEqual(after.Frames) && before.SelectedIndices.SequenceEqual(after.SelectedIndices))
        {
            SetStatus($"{description} made no changes.");
            return;
        }

        await RestoreSnapshotAsync(after, cancellationToken);
        CommitEditorMutation(description, before, successMessage);
    }

    private async void DeleteFrameClick(object? sender, RoutedEventArgs e) => await DeleteSelectedFramesAsync();

    private async Task DeleteSelectedFramesAsync()
    {
        var indices = SelectedIndices();
        if (indices.Length == 0)
        {
            SetStatus("Select at least one frame to delete.");
            return;
        }

        if (!await _destructiveActions.ConfirmAsync(
                LinuxSettings.Current.AskBeforeDeleteFrames,
                () => new Controls.ConfirmDialog(
                    "Delete frames",
                    $"Delete {indices.Length} selected frame(s)? You can undo this action while it remains in history.",
                    "Delete").ShowForAsync(this)))
        {
            SetStatus("Frame deletion canceled.");
            return;
        }

        var remaining = FrameSequenceOperations.Delete(CurrentStates(), indices);
        var nextSelection = remaining.Count == 0 ? [] : new[] { Math.Min(indices[0], remaining.Count - 1) };
        await RunSequenceEditAsync("Delete frames", remaining, nextSelection,
            $"Deleted {indices.Length} selected frame(s).");
    }

    private async void MoveUpClick(object? sender, RoutedEventArgs e) => await MoveSelectedAsync(-1);

    private async void MoveDownClick(object? sender, RoutedEventArgs e) => await MoveSelectedAsync(1);

    private async Task MoveSelectedAsync(int offset)
    {
        var indices = SelectedIndices();
        if (indices.Length == 0 || offset == 0)
        {
            SetStatus("Select at least one frame to reorder.");
            return;
        }

        var states = CurrentStates();
        var reordered = FrameSequenceOperations.Move(states, indices, offset);
        var nextSelection = FrameSequenceOperations.MoveSelection(states.Length, indices, offset);
        await RunSequenceEditAsync(offset < 0 ? "Move frames left" : "Move frames right", reordered, nextSelection,
            $"Moved {indices.Length} selected frame(s) {(offset < 0 ? "left" : "right")}.");
    }

    private async void ApplyDelayClick(object? sender, RoutedEventArgs e)
    {
        if (!TryReadDelay(out var delay))
            return;

        var indices = SelectedIndices();
        if (indices.Length == 0)
        {
            SetStatus("Select at least one frame to change its delay.");
            return;
        }

        await RunSequenceEditAsync("Set selected delays",
            FrameSequenceOperations.ApplyDelay(CurrentStates(), indices, delay), indices,
            $"Set {indices.Length} selected frame delay(s) to {delay} ms.");
    }

    private async void ApplyDelayAllClick(object? sender, RoutedEventArgs e)
    {
        if (!TryReadDelay(out var delay))
            return;

        var all = Enumerable.Range(0, _frames.Count).ToArray();
        await RunSequenceEditAsync("Set all delays",
            FrameSequenceOperations.ApplyDelay(CurrentStates(), all, delay), SelectedIndices(),
            $"Set all frame delays to {delay} ms.");
    }

    private async void RemoveDuplicatesClick(object? sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Remove duplicates", async cancellationToken =>
        {
            var states = CurrentStates();
            var scope = SelectedIndicesOrAll();
            var duplicates = await FrameSequenceOperations.FindConsecutiveDuplicatesAsync(states, scope, _ffmpeg, cancellationToken);
            if (duplicates.Count == 0)
            {
                SetStatus("No consecutive pixel-identical frames were found in the selected scope.");
                return;
            }

            await ApplySequenceEditAsync(
                "Remove duplicates", FrameSequenceOperations.Delete(states, duplicates), [],
                $"Removed {duplicates.Count} consecutive duplicate frame(s).", cancellationToken);
        });
    }

    private async void ReduceClick(object? sender, RoutedEventArgs e)
    {
        var values = await new Controls.ParameterDialog("Reduce selected frames", ("Keep every Nth frame", "2")).ShowForAsync(this);
        if (values is null)
            return;
        if (!int.TryParse(values[0], out var factor) || factor < 2)
        {
            SetStatus("Reduction factor must be a whole number of 2 or greater.");
            return;
        }

        var scope = SelectedIndicesOrAll();
        if (scope.Length < 2)
        {
            SetStatus("Select at least two frames, or clear selection to reduce the whole timeline.");
            return;
        }

        await RunSequenceEditAsync("Reduce frames", FrameSequenceOperations.Reduce(CurrentStates(), scope, factor), [],
            $"Reduced the scope by keeping every {factor}th frame.");
    }

    private async void SmoothLoopClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        var scope = SelectedIndicesOrAll();
        if (scope.Length < 2)
        {
            SetStatus("Smooth loop needs at least two selected frames, or an unselected timeline with two frames.");
            return;
        }

        var values = await new Controls.ParameterDialog("Smooth loop", ("Generated frames", "5"), ("Duration (milliseconds)", "500")).ShowForAsync(this);
        if (values is null)
            return;
        if (!TryPositive(values[0], "Generated frames", out var count) || !TryPositive(values[1], "Duration", out var duration))
            return;
        if (!TryEnsureFrameCapacity(count, "Creating a smooth loop"))
            return;

        await RunOperationAsync("Smooth loop", async cancellationToken =>
        {
            var before = CaptureSnapshot();
            SetStatus("Generating smooth loop... Use Stop to cancel.");
            var generated = await TransitionService.GenerateAsync(_frames[scope[^1]], _frames[scope[0]], _workspace, new TransitionRequest(count, duration), cancellationToken);
            await EditorProjectBudget.EnsureTimelineBudgetAsync(
                _frames.Select(frame => frame.FilePath).Concat(generated.Select(frame => frame.FilePath)),
                "Creating a smooth loop",
                cancellationToken);
            var prepared = await PrepareFramesAsync(generated, cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                foreach (var frame in prepared)
                    frame.Dispose();
                throw;
            }

            FrameSequenceOperations.InsertAfter(_frames, scope[^1], generated);
            RestoreSelection(generated);
            CommitEditorMutation("Smooth loop", before, $"Inserted {generated.Count} smooth-loop frame(s).");
        });
    }

    private async void DeleteBeforeClick(object? sender, RoutedEventArgs e)
    {
        var indices = SelectedIndices();
        if (indices.Length == 0)
        {
            SetStatus("Select a boundary frame first.");
            return;
        }

        if (indices[0] == 0)
        {
            SetStatus("There are no frames before the selection.");
            return;
        }

        var states = FrameSequenceOperations.DeleteBefore(CurrentStates(), indices[0]);
        await RunSequenceEditAsync("Delete before", states, states.Count == 0 ? [] : [0], "Deleted frames before the selection.");
    }

    private async void DeleteAfterClick(object? sender, RoutedEventArgs e)
    {
        var indices = SelectedIndices();
        if (indices.Length == 0)
        {
            SetStatus("Select a boundary frame first.");
            return;
        }

        if (indices[^1] == _frames.Count - 1)
        {
            SetStatus("There are no frames after the selection.");
            return;
        }

        var states = FrameSequenceOperations.DeleteAfter(CurrentStates(), indices[^1]);
        await RunSequenceEditAsync("Delete after", states, states.Count == 0 ? [] : [states.Count - 1], "Deleted frames after the selection.");
    }

    private async void ReverseClick(object? sender, RoutedEventArgs e)
    {
        var scope = SelectedIndicesOrAll();
        if (scope.Length < 2)
        {
            SetStatus("Reverse needs at least two selected frames, or an unselected timeline with two frames.");
            return;
        }

        await RunSequenceEditAsync("Reverse frames", FrameSequenceOperations.Reverse(CurrentStates(), scope), scope, "Reversed the selected frame positions.");
    }

    private async void YoyoClick(object? sender, RoutedEventArgs e)
    {
        var scope = SelectedIndicesOrAll();
        if (scope.Length < 3)
        {
            SetStatus("Yoyo needs at least three selected frames, or an unselected timeline with three frames.");
            return;
        }

        var insertedCount = scope.Length - 2;
        if (!TryEnsureFrameCapacity(insertedCount, "Creating a yoyo"))
            return;
        var states = FrameSequenceOperations.Yoyo(CurrentStates(), scope);
        await RunSequenceEditAsync("Create yoyo", states,
            Enumerable.Range(scope[^1] + 1, insertedCount).ToArray(),
            $"Appended {insertedCount} reverse interior frame(s) for a seamless yoyo.");
    }
}
