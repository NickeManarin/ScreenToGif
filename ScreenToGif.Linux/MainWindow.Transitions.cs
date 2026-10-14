using Avalonia.Interactivity;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class MainWindow
{
    private FrameTransitionService? _transitionService;
    private FrameTransitionService TransitionService => _transitionService ??= new FrameTransitionService(_ffmpeg);

    private bool TryEnsureFrameCapacity(int additionalFrameCount, string operation)
    {
        try
        {
            EditorResourceLimits.EnsureCanInsertFrames(_frames.Count, additionalFrameCount, operation);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message);
            return false;
        }
    }

    private async void FadeClick(object? sender, RoutedEventArgs e) => await InsertTransitionAsync(null);

    private async void SlideClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        var choice = await new Controls.ChoiceDialog("Slide direction",
            ["Slide left", "Slide right", "Slide up", "Slide down"]).ShowForAsync(this);
        if (choice is null)
            return;

        await InsertTransitionAsync((SlideDirection)choice.Value);
    }

    private async Task InsertTransitionAsync(SlideDirection? direction)
    {
        StopPreview();
        if (!TryGetTransitionBoundary(out var firstIndex))
            return;

        var values = await new Controls.ParameterDialog(
            direction is null ? "Insert fade transition" : "Insert slide transition",
            ("Generated frames", "5"),
            ("Duration (milliseconds)", "500")).ShowForAsync(this);
        if (values is null)
            return;
        if (!TryPositive(values[0], "Generated frames", out var count) ||
            !TryPositive(values[1], "Duration", out var duration))
            return;
        if (!TryEnsureFrameCapacity(count, "Inserting a transition"))
            return;

        await RunOperationAsync("Generate transition", async cancellationToken =>
        {
            StopPreview();
            SetStatus("Generating transition... Use Stop to cancel.");
            var before = CaptureSnapshot();
            var generated = await TransitionService.GenerateAsync(
                _frames[firstIndex], _frames[firstIndex + 1], _workspace,
                new TransitionRequest(count, duration, direction), cancellationToken);
            await EditorProjectBudget.EnsureTimelineBudgetAsync(
                _frames.Select(frame => frame.FilePath).Concat(generated.Select(frame => frame.FilePath)),
                "Inserting a transition",
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

            FrameSequenceOperations.InsertAfter(_frames, firstIndex, generated);

            FrameListBox.SelectedItems?.Clear();
            foreach (var frame in generated)
                FrameListBox.SelectedItems?.Add(frame);
            CommitEditorMutation(
                direction is null ? "Insert fade" : "Insert slide",
                before,
                $"Inserted {generated.Count} transition frame(s) totaling {duration} ms.");
        });
    }

    private bool TryGetTransitionBoundary(out int firstIndex)
    {
        var indices = GetSelectedFrames().Select(frame => _frames.IndexOf(frame)).Order().ToArray();
        if (indices.Length == 1 && indices[0] >= 0 && indices[0] < _frames.Count - 1)
        {
            firstIndex = indices[0];
            return true;
        }

        if (indices.Length == 2 && indices[1] == indices[0] + 1)
        {
            firstIndex = indices[0];
            return true;
        }

        firstIndex = -1;
        SetStatus("Select one frame before a boundary, or exactly two adjacent frames.");
        return false;
    }
}
