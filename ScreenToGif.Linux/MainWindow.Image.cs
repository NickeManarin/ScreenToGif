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
    private async void ResizeClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        var selected = GetSelectedFrames();
        if (selected.Length == 0)
        {
            SetStatus("Select at least one frame to resize.");
            return;
        }

        PixelSize size;
        try
        {
            using var bitmap = new Bitmap(selected[0].FilePath);
            size = bitmap.PixelSize;
        }
        catch (Exception ex)
        {
            SetError(ex);
            return;
        }

        var values = await new Controls.ParameterDialog(
            "Resize selected frames",
            ("Width (pixels)", size.Width.ToString()),
            ("Height (pixels)", size.Height.ToString())).ShowForAsync(this);

        if (values is null)
            return;

        if (!TryTransformDimension(values[0], "Width", out var width) ||
            !TryTransformDimension(values[1], "Height", out var height))
            return;

        await ApplyTransformAsync(selected, new FrameTransformRequest(FrameTransformKind.Resize, width, height), "Resize frames");
    }

    private async void CropClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        var selected = GetSelectedFrames();
        if (selected.Length == 0)
        {
            SetStatus("Select at least one frame to crop.");
            return;
        }

        PixelSize size;
        try
        {
            using var bitmap = new Bitmap(selected[0].FilePath);
            size = bitmap.PixelSize;
        }
        catch (Exception ex)
        {
            SetError(ex);
            return;
        }

        var values = await new Controls.ParameterDialog(
            "Crop selected frames",
            ("Left (pixels)", "0"),
            ("Top (pixels)", "0"),
            ("Width (pixels)", size.Width.ToString()),
            ("Height (pixels)", size.Height.ToString())).ShowForAsync(this);

        if (values is null)
            return;

        if (!TryNonNegative(values[0], "Left", out var x) ||
            !TryNonNegative(values[1], "Top", out var y) ||
            !TryTransformDimension(values[2], "Width", out var width) ||
            !TryTransformDimension(values[3], "Height", out var height))
            return;

        await ApplyTransformAsync(selected, new FrameTransformRequest(FrameTransformKind.Crop, width, height, x, y), "Crop frames");
    }

    private async void FlipRotateClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        var selected = GetSelectedFrames();
        if (selected.Length == 0)
        {
            SetStatus("Select at least one frame to flip or rotate.");
            return;
        }

        var choice = await new Controls.ChoiceDialog("Flip or rotate selected frames",
        [
            "Flip horizontally",
            "Flip vertically",
            "Rotate 90° clockwise",
            "Rotate 90° counter-clockwise"
        ]).ShowForAsync(this);

        if (choice is null)
            return;

        var kind = choice.Value switch
        {
            0 => FrameTransformKind.FlipHorizontal,
            1 => FrameTransformKind.FlipVertical,
            2 => FrameTransformKind.RotateClockwise,
            _ => FrameTransformKind.RotateCounterClockwise
        };
        await ApplyTransformAsync(selected, new FrameTransformRequest(kind), "Flip or rotate frames");
    }

    private async void BorderClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        var selected = GetSelectedFrames();
        if (selected.Length == 0)
        {
            SetStatus("Select at least one frame to add a border.");
            return;
        }

        var values = await new Controls.ParameterDialog("Add black border", ("Border width (pixels)", "8")).ShowForAsync(this);
        if (values is null || !TryTransformDimension(
                values[0], "Border width", out var width, FrameTransformService.MaximumDimension / 2))
            return;
        await ApplyTransformAsync(selected, new FrameTransformRequest(FrameTransformKind.Border, width), "Add border");
    }

    private async void ShadowClick(object? sender, RoutedEventArgs e)
    {
        StopPreview();
        var selected = GetSelectedFrames();
        if (selected.Length == 0)
        {
            SetStatus("Select at least one frame to add a shadow.");
            return;
        }

        await ApplyTransformAsync(selected, new FrameTransformRequest(FrameTransformKind.Shadow), "Add shadow");
    }

    private async Task ApplyTransformAsync(EditorFrame[] selected, FrameTransformRequest request, string description)
    {
        await RunOperationAsync(description, async cancellationToken =>
        {
            StopPreview();
            SetStatus($"{description}... Use Stop to cancel.");
            var before = CaptureSnapshot();
            var transformed = await _transformer.TransformAsync(selected, _workspace, request, cancellationToken);
            var replacements = transformed.ToDictionary(item => item.Source, item => item.FilePath);
            await EditorProjectBudget.EnsureTimelineBudgetAsync(
                _frames.Select(frame => replacements.GetValueOrDefault(frame, frame.FilePath)),
                description,
                cancellationToken);
            var bitmaps = await PrepareBitmapsAsync(
                transformed.Select(item => item.FilePath), cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                foreach (var bitmap in bitmaps)
                    bitmap.Thumbnail.Dispose();
                throw;
            }

            for (var index = 0; index < transformed.Count; index++)
            {
                transformed[index].Source.FilePath = transformed[index].FilePath;
                transformed[index].Source.SourcePixelSize = bitmaps[index].SourcePixelSize;
                transformed[index].Source.Thumbnail = bitmaps[index].Thumbnail;
            }

            CommitEditorMutation(description, before, $"{description} completed for {transformed.Count} frame(s).");
        });
    }
}
