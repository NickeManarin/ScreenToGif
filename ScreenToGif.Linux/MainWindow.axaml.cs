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
using System.Collections.ObjectModel;

namespace ScreenToGif.Linux;

public partial class MainWindow : Window
{
    partial void InitializeHome();
    partial void InitializeEdit();
    partial void InitializeFileLifecycle();
    partial void InitializePlayback();
    partial void InitializeStatistics();

    private const int ThumbnailDecodeWidth = 212;
    private readonly FfmpegTool _ffmpeg = new();
    private readonly ObservableCollection<EditorFrame> _frames = [];
    private readonly MediaImporter _importer;
    private readonly FfmpegExporter _exporter;
    private readonly FrameEditHistory _history = new(LinuxSettings.Current.UndoLimit);
    private readonly EditorMutationCoordinator _mutations;
    private readonly FrameTransformService _transformer;
    private readonly EditorOperationCoordinator _operations = new();
    private EditorWorkspace _workspace;
    private Bitmap? _previewBitmap;
    private EditorFrame? _currentFrame;
    private int _currentFrameIndex = -1;
    private Action _stopActivePreview = static () => { };
    private event Action? OperationStateChanged;
    private event Action? FrameInfoUpdated;
    private event Action? CleanupRequested;
    private event Action? HistoryStateChanged;

    public MainWindow()
    {
        InitializeComponent();

        _importer = new MediaImporter(_ffmpeg);
        _exporter = new FfmpegExporter(_ffmpeg);
        _transformer = new FrameTransformService(_ffmpeg);
        _mutations = new EditorMutationCoordinator(_history, CaptureSnapshot, RefreshEditorAfterMutation);
        _workspace = EditorWorkspace.Create();
        _history.BranchDiscarded += PruneUnreachableGeneratedFiles;

        FrameListBox.ItemsSource = _frames;
        InitializeHome();
        InitializeEdit();
        InitializeFileLifecycle();
        InitializePlayback();
        InitializeStatistics();
        UpdateFrameInfo();
        _history.SetBaseline(CaptureSnapshot());
        MarkClean();
        Closed += (_, _) => CleanupWorkspaces();
    }

    private void ExitClick(object? sender, RoutedEventArgs e) => Close();

    private async Task AddFramesAsync(
        IEnumerable<EditorFrame> frames,
        string operation,
        CancellationToken cancellationToken,
        int? insertionIndex = null)
    {
        var incoming = frames.ToList();
        try
        {
            if (insertionIndex is < 0 || insertionIndex > _frames.Count)
                throw new ArgumentOutOfRangeException(nameof(insertionIndex));

            EditorResourceLimits.EnsureCanInsertFrames(_frames.Count, incoming.Count, "Adding frames");
            await EditorProjectBudget.EnsureTimelineBudgetAsync(
                _frames.Select(frame => frame.FilePath).Concat(incoming.Select(frame => frame.FilePath)),
                operation,
                cancellationToken);
        }
        catch
        {
            foreach (var frame in incoming)
                frame.Dispose();
            throw;
        }

        var prepared = await PrepareFramesAsync(incoming, cancellationToken);
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

        if (insertionIndex is { } index)
        {
            BoardInsertionPlacement.InsertInto(_frames, prepared, index);
            FrameListBox.SelectedItems?.Clear();
            FrameListBox.SelectedIndex = index;
        }
        else
        {
            foreach (var frame in prepared)
                _frames.Add(frame);
        }

        if (FrameListBox.SelectedIndex < 0 && _frames.Count > 0)
            FrameListBox.SelectedIndex = 0;

        UpdateFrameInfo();
    }

    private void ReplaceFrames(IEnumerable<EditorFrame> frames)
    {
        var prepared = PrepareFrames(frames);
        SetCurrentFrameIndex(-1);
        foreach (var frame in _frames)
            frame.Dispose();

        _frames.Clear();
        SetPreview(null);
        foreach (var frame in prepared)
            _frames.Add(frame);
        if (_frames.Count > 0)
            FrameListBox.SelectedIndex = 0;
        UpdateFrameInfo();
        _history.SetBaseline(CaptureSnapshot());
    }

    private static async Task<List<EditorFrame>> PrepareFramesAsync(
        IEnumerable<EditorFrame> frames,
        CancellationToken cancellationToken)
    {
        var materializedFrames = frames.ToList();
        return await Task.Run(
            () => PrepareFrames(materializedFrames, cancellationToken),
            CancellationToken.None);
    }

    private static List<EditorFrame> PrepareFrames(
        IEnumerable<EditorFrame> frames,
        CancellationToken cancellationToken = default)
    {
        var prepared = frames.ToList();
        try
        {
            foreach (var frame in prepared)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (frame.Thumbnail is not null)
                    continue;
                var bitmap = PrepareBitmap(frame.FilePath);
                frame.SourcePixelSize = bitmap.SourcePixelSize;
                frame.Thumbnail = bitmap.Thumbnail;
                cancellationToken.ThrowIfCancellationRequested();
            }

            return prepared;
        }
        catch
        {
            foreach (var frame in prepared)
                frame.Dispose();
            throw;
        }
    }

    private static PreparedBitmap[] PrepareBitmaps(
        IEnumerable<string> paths,
        CancellationToken cancellationToken = default)
    {
        var bitmaps = new List<PreparedBitmap>();
        try
        {
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bitmaps.Add(PrepareBitmap(path));
                cancellationToken.ThrowIfCancellationRequested();
            }

            return bitmaps.ToArray();
        }
        catch
        {
            foreach (var bitmap in bitmaps)
                bitmap.Thumbnail.Dispose();
            throw;
        }
    }

    private static Task<PreparedBitmap[]> PrepareBitmapsAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = PrepareBitmaps(paths, cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
            catch
            {
                foreach (var bitmap in result)
                    bitmap.Thumbnail.Dispose();
                throw;
            }
        }, cancellationToken);

    private static PreparedBitmap PrepareBitmap(string path)
    {
        var dimensions = EditorProjectBudget.ReadPngDimensions(path);
        using var stream = File.OpenRead(path);
        var thumbnail = dimensions.Width >= dimensions.Height
            ? Bitmap.DecodeToWidth(stream, Math.Min(dimensions.Width, ThumbnailDecodeWidth), BitmapInterpolationMode.MediumQuality)
            : Bitmap.DecodeToHeight(stream, Math.Min(dimensions.Height, ThumbnailDecodeWidth), BitmapInterpolationMode.MediumQuality);
        return new PreparedBitmap(thumbnail, new PixelSize(dimensions.Width, dimensions.Height));
    }

    private sealed record PreparedBitmap(Bitmap Thumbnail, PixelSize SourcePixelSize);

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

    private bool TryPositive(string text, string name, out int value)
    {
        if (int.TryParse(text, out value) && value > 0)
            return true;

        SetStatus($"{name} must be a positive whole number.");
        return false;
    }

    private bool TryNonNegative(string text, string name, out int value)
    {
        if (int.TryParse(text, out value) && value >= 0)
            return true;

        SetStatus($"{name} cannot be negative.");
        return false;
    }

    private bool TryTransformDimension(
        string text,
        string name,
        out int value,
        int maximum = FrameTransformService.MaximumDimension)
    {
        if (TryPositive(text, name, out value) && value <= maximum)
            return true;

        if (value > maximum)
            SetStatus($"{name} must be at most {maximum} pixels.");
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

    private int[] SelectedIndices() =>
        GetSelectedFrames().Select(frame => _frames.IndexOf(frame)).Where(index => index >= 0).Distinct().Order().ToArray();

    private EditorSnapshot CaptureSnapshot() => EditorSnapshot.Capture(
        _frames,
        GetSelectedFrames().Select(frame => _frames.IndexOf(frame)).Where(index => index >= 0));

    private void MarkClean() => _mutations.MarkClean();

    private void UpdateDirtyState() => _mutations.Refresh();

    private void CommitEditorMutation(string description, EditorSnapshot before, string successMessage)
    {
        _mutations.Commit(description, before);
        SetStatus(successMessage);
    }

    private void RefreshEditorAfterMutation()
    {
        HistoryStateChanged?.Invoke();
        UpdateFrameInfo();
        UpdateCurrentFramePreview();
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.ClearValue(TextBlock.ForegroundProperty);
    }

    private void SetCurrentFrameIndex(int index)
    {
        if (_currentFrame is not null)
            _currentFrame.IsCurrent = false;
        _currentFrameIndex = index >= 0 && index < _frames.Count ? index : -1;
        _currentFrame = _currentFrameIndex >= 0 ? _frames[_currentFrameIndex] : null;
        if (_currentFrame is not null)
            _currentFrame.IsCurrent = true;
    }

    private void StopPreview() => _stopActivePreview();

    private async Task RunOperationAsync(string description, Func<CancellationToken, Task> operation)
    {
        var result = await _operations.RunAsync(operation, BeginOperationUi, EndOperationUi);
        switch (result.Status)
        {
            case EditorOperationStatus.Busy:
                SetStatus("Another operation is still running. Use Stop to cancel it first.");
                break;
            case EditorOperationStatus.Canceled:
                PruneUnreachableGeneratedFiles();
                SetStatus($"{description} canceled; the prior editor state was preserved.");
                break;
            case EditorOperationStatus.Failed:
                PruneUnreachableGeneratedFiles();
                SetError(result.Error!);
                break;
        }

        if (result.CloseRequested)
            Dispatcher.UIThread.Post(Close);
    }

    private void BeginOperationUi()
    {
        StopPreview();
        RibbonTabControl.IsEnabled = false;
        FrameListBox.IsEnabled = false;
        OperationStateChanged?.Invoke();
    }

    private void EndOperationUi()
    {
        RibbonTabControl.IsEnabled = true;
        FrameListBox.IsEnabled = true;
        OperationStateChanged?.Invoke();
    }

    private void UpdateFrameInfo()
    {
        for (var index = 0; index < _frames.Count; index++)
            _frames[index].FrameNumber = index;

        FrameCountText.Text = _frames.Count.ToString();
        SelectedCountText.Text = GetSelectedFrames().Length.ToString();
        CurrentFrameText.Text = _currentFrameIndex >= 0
            ? (_currentFrameIndex + 1).ToString()
            : "—";

        FrameInfoUpdated?.Invoke();
    }

    private void SetError(Exception exception)
    {
        var detail = exception.Message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? "The operation failed.";
        if (detail.Length > 240)
            detail = $"{detail[..237]}...";
        StatusText.Text = $"Error: {detail}";
        StatusText.Foreground = Avalonia.Media.Brushes.IndianRed;
    }

    private void CleanupWorkspaces()
    {
        CleanupRequested?.Invoke();
        _operations.RequestCancellation();
        _previewBitmap?.Dispose();

        foreach (var frame in _frames)
            frame.Dispose();

        _workspace.Dispose();
    }

    private void PruneUnreachableGeneratedFiles()
    {
        var referenced = _history.ReferencedFiles().Concat(_frames.Select(frame => frame.FilePath));
        _workspace.PruneUnreachable(referenced);
    }
}
