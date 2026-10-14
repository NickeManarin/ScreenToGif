using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

/// <summary>
/// Owns the editor-wide bookkeeping that must follow every committed mutation.
/// UI handlers perform their focused mutation, then hand control here so history,
/// dirty state, and all derived editor surfaces advance together.
/// </summary>
public sealed partial class EditorMutationCoordinator(
    FrameEditHistory history,
    Func<EditorSnapshot> captureSnapshot,
    Action refreshEditor)
{
    private EditorSnapshot? _cleanSnapshot;

    public bool HasUnsavedChanges { get; private set; }

    public void MarkClean()
    {
        _cleanSnapshot = captureSnapshot();
        HasUnsavedChanges = false;
    }

    public void Commit(string description, EditorSnapshot before)
    {
        history.Record(description, before, captureSnapshot());
        Refresh();
    }

    public void Refresh()
    {
        HasUnsavedChanges = _cleanSnapshot is null ||
            !captureSnapshot().Frames.SequenceEqual(_cleanSnapshot.Frames);
        refreshEditor();
    }
}
