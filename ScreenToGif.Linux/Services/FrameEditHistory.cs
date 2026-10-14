using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public sealed record FrameState(string FilePath, int DelayMs);

public sealed record EditorSnapshot(IReadOnlyList<FrameState> Frames, IReadOnlyList<int> SelectedIndices)
{
    public static EditorSnapshot Capture(IReadOnlyList<EditorFrame> frames, IEnumerable<int> selectedIndices) =>
        new(frames.Select(frame => new FrameState(frame.FilePath, frame.DelayMs)).ToArray(),
            selectedIndices.Distinct().Order().ToArray());
}

public sealed record FrameEdit(string Description, EditorSnapshot Before, EditorSnapshot After);

public sealed class FrameEditHistory
{
    public const int MaximumEntries = 100;

    private readonly int _maximumEntries;
    private readonly Stack<FrameEdit> _undo = [];
    private readonly Stack<FrameEdit> _redo = [];
    private EditorSnapshot? _baseline;

    public event Action? BranchDiscarded;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public FrameEditHistory(int maximumEntries = MaximumEntries)
    {
        _maximumEntries = maximumEntries < 1 ? int.MaxValue : maximumEntries;
    }

    public void SetBaseline(EditorSnapshot state)
    {
        _baseline = state;
        Clear();
    }

    public void Record(string description, EditorSnapshot before, EditorSnapshot after)
    {
        if (SameState(before, after))
            return;

        var discardedBranch = _redo.Count > 0;
        _undo.Push(new FrameEdit(description, before, after));
        _redo.Clear();
        var discardedOldestEdit = TrimUndoHistory();
        if (discardedBranch || discardedOldestEdit)
            NotifyBranchDiscarded();
    }

    public bool TryUndo(out EditorSnapshot state, out string description)
    {
        if (!_undo.TryPop(out var edit))
        {
            state = null!;
            description = string.Empty;
            return false;
        }

        _redo.Push(edit);
        state = edit.Before;
        description = edit.Description;
        return true;
    }

    public bool TryRedo(out EditorSnapshot state, out string description)
    {
        if (!_redo.TryPop(out var edit))
        {
            state = null!;
            description = string.Empty;
            return false;
        }

        _undo.Push(edit);
        state = edit.After;
        description = edit.Description;
        return true;
    }

    public bool TryGetResetTarget(EditorSnapshot current, out EditorSnapshot state)
    {
        if (_baseline is null || SameState(_baseline, current))
        {
            state = null!;
            return false;
        }

        state = _baseline;
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    public IReadOnlySet<string> ReferencedFiles()
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        Add(_baseline);
        foreach (var edit in _undo.Concat(_redo))
        {
            Add(edit.Before);
            Add(edit.After);
        }

        return paths;

        void Add(EditorSnapshot? snapshot)
        {
            if (snapshot is null)
                return;
            foreach (var frame in snapshot.Frames)
                paths.Add(Path.GetFullPath(frame.FilePath));
        }
    }

    private void NotifyBranchDiscarded()
    {
        if (BranchDiscarded is null)
            return;
        foreach (Action handler in BranchDiscarded.GetInvocationList())
        {
            try
            {
                handler();
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private bool TrimUndoHistory()
    {
        if (_undo.Count <= _maximumEntries)
            return false;

        var retained = _undo.Take(_maximumEntries).Reverse().ToArray();
        _undo.Clear();
        foreach (var edit in retained)
            _undo.Push(edit);
        return true;
    }

    private static bool SameState(EditorSnapshot left, EditorSnapshot right) =>
        left.Frames.SequenceEqual(right.Frames) && left.SelectedIndices.SequenceEqual(right.SelectedIndices);
}
