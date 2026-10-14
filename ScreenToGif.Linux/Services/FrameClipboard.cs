using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public sealed class FrameClipboard : IDisposable
{
    private IReadOnlyList<FrameState> _frames = [];
    private readonly EditorWorkspace _workspace;
    private string? _currentBatchPath;

    public FrameClipboard(string? rootPath = null)
    {
        _workspace = EditorWorkspace.Create(rootPath);
    }

    public bool HasFrames => _frames.Count > 0;
    public int FrameCount => _frames.Count;

    public async Task CopyAsync(IReadOnlyList<EditorFrame> frames, CancellationToken cancellationToken = default)
    {
        if (frames.Count == 0)
            throw new InvalidOperationException("Select at least one frame to copy.");

        var path = _workspace.CreateBatch(EditorArtifactKind.Clipboard);
        var copied = new List<FrameState>(frames.Count);

        try
        {
            for (var index = 0; index < frames.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var output = Path.Combine(path, $"{index:000000}.png");
                await using var source = new FileStream(frames[index].FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var destination = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(destination, cancellationToken);
                copied.Add(new FrameState(output, frames[index].DelayMs));
            }

            var previous = _currentBatchPath;
            _frames = copied;
            _currentBatchPath = path;
            if (previous is not null)
                TryDelete(previous);
        }
        catch
        {
            TryDelete(path);
            throw;
        }
    }

    public void Dispose() => _workspace.Dispose();

    public async Task<IReadOnlyList<FrameState>> CreatePasteAsync(EditorWorkspace workspace, CancellationToken cancellationToken = default)
    {
        if (_frames.Count == 0)
            throw new InvalidOperationException("The editor clipboard is empty. Copy or cut frames first.");

        var path = workspace.CreateBatch(EditorArtifactKind.Pastes);
        var pasted = new List<FrameState>(_frames.Count);

        try
        {
            for (var index = 0; index < _frames.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var output = Path.Combine(path, $"{index:000000}.png");
                await using var source = new FileStream(_frames[index].FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var destination = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(destination, cancellationToken);
                pasted.Add(new FrameState(output, _frames[index].DelayMs));
            }

            return pasted;
        }
        catch
        {
            TryDelete(path);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

public static class ZoomLevel
{
    public const int Minimum = 10;
    public const int Maximum = 800;

    public static bool TryParse(string? text, out int percent) =>
        int.TryParse(text, out percent) && percent is >= Minimum and <= Maximum;
}

public static class TimelineSelection
{
    public static IReadOnlyList<int> Invert(int frameCount, IEnumerable<int> selectedIndices)
    {
        var selected = selectedIndices.Where(index => index >= 0 && index < frameCount).ToHashSet();
        return Enumerable.Range(0, Math.Max(0, frameCount)).Where(index => !selected.Contains(index)).ToArray();
    }

    public static bool TryResolveOneBased(string? text, int frameCount, out int zeroBasedIndex)
    {
        if (int.TryParse(text, out var number) && number >= 1 && number <= frameCount)
        {
            zeroBasedIndex = number - 1;
            return true;
        }

        zeroBasedIndex = -1;
        return false;
    }
}
