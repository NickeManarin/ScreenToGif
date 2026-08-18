using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public sealed class FrameDeletionHistory
{
    private readonly Stack<DeletedFrames> _undo = new();

    public void Record(
        IEnumerable<EditorFrame> frames,
        IEnumerable<int> indices,
        IEnumerable<EditorFrame> selectedFrames,
        int selectedIndex)
    {
        var deletedFrames = frames.ToArray();
        var deletedIndices = indices.ToArray();

        if (deletedFrames.Length != deletedIndices.Length)
            throw new ArgumentException("Each deleted frame must have a corresponding index.");

        _undo.Push(new DeletedFrames(
            deletedFrames,
            deletedIndices,
            selectedFrames.ToArray(),
            selectedIndex));
    }

    public bool TryRestore(IList<EditorFrame> currentFrames, out DeletedFrames deletedFrames)
    {
        if (_undo.Count == 0)
        {
            deletedFrames = null!;
            return false;
        }

        deletedFrames = _undo.Pop();

        for (var index = 0; index < deletedFrames.Frames.Count; index++)
        {
            var insertionIndex = Math.Clamp(deletedFrames.Indices[index], 0, currentFrames.Count);
            currentFrames.Insert(insertionIndex, deletedFrames.Frames[index]);
        }

        return true;
    }

    public void Clear() => _undo.Clear();

    public sealed record DeletedFrames(
        IReadOnlyList<EditorFrame> Frames,
        IReadOnlyList<int> Indices,
        IReadOnlyList<EditorFrame> SelectedFrames,
        int SelectedIndex);
}
