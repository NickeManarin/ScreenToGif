using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public sealed partial class EditorMutationCoordinator
{
    public bool IsPristineEmptySession(EditorSnapshot current) =>
        current.Frames.Count == 0 &&
        !HasUnsavedChanges &&
        !history.CanUndo &&
        !history.CanRedo;

    public void FinalizeImport(EditorSnapshot before, string description = "Insert media")
    {
        if (IsPristineEmptySession(before))
        {
            history.SetBaseline(captureSnapshot());
            Refresh();
            return;
        }

        Commit(description, before);
    }
}
