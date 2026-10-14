using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class HomeEditingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-home-tests-{Guid.NewGuid():N}");
    private readonly EditorWorkspace _workspace;

    public HomeEditingTests() => _workspace = EditorWorkspace.Create(_root);

    [Fact]
    public async Task Clipboard_owns_copies_and_each_paste_has_independent_files_and_delays()
    {
        var sourceA = CreateFile("a.png", "pixels-a");
        var sourceB = CreateFile("b.png", "pixels-b");
        var frames = new[] { new EditorFrame(sourceA, 40), new EditorFrame(sourceB, 120) };
        using var clipboard = new FrameClipboard(Path.Combine(_root, "clipboard"));

        await clipboard.CopyAsync(frames);
        Assert.Equal(2, clipboard.FrameCount);
        File.Delete(sourceA);
        File.Delete(sourceB);
        var firstPaste = await clipboard.CreatePasteAsync(_workspace);
        var secondPaste = await clipboard.CreatePasteAsync(_workspace);

        Assert.Equal([40, 120], firstPaste.Select(frame => frame.DelayMs));
        Assert.Equal(["pixels-a", "pixels-b"], firstPaste.Select(frame => File.ReadAllText(frame.FilePath)));
        Assert.Equal([40, 120], secondPaste.Select(frame => frame.DelayMs));
        Assert.Equal(["pixels-a", "pixels-b"], secondPaste.Select(frame => File.ReadAllText(frame.FilePath)));
        Assert.All(secondPaste, frame => Assert.True(File.Exists(frame.FilePath)));
        Assert.Empty(firstPaste.Select(frame => frame.FilePath).Intersect(secondPaste.Select(frame => frame.FilePath)));
        Directory.Delete(Path.GetDirectoryName(firstPaste[0].FilePath)!, recursive: true);
        Assert.All(secondPaste, frame => Assert.True(File.Exists(frame.FilePath)));
    }

    [Fact]
    public async Task Empty_clipboard_is_actionable_and_canceled_copy_preserves_previous_contents()
    {
        using var clipboard = new FrameClipboard(Path.Combine(_root, "clipboard"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => clipboard.CreatePasteAsync(_workspace));
        Assert.Contains("clipboard is empty", error.Message);

        var source = CreateFile("source.png", "pixels");
        await clipboard.CopyAsync([new EditorFrame(source, 75)]);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            clipboard.CopyAsync([new EditorFrame(source, 90)], canceled.Token));
        var pasted = await clipboard.CreatePasteAsync(_workspace);
        Assert.Equal(75, pasted.Single().DelayMs);
    }

    [Fact]
    public void History_supports_cut_paste_undo_redo_and_reset_semantics()
    {
        var baseline = Snapshot("a", "b", "c");
        var cut = Snapshot("a", "c");
        var pasted = Snapshot("a", "c", "b-copy");
        var history = new FrameEditHistory();
        history.SetBaseline(baseline);
        history.Record("Cut", baseline, cut);
        history.Record("Paste", cut, pasted);

        Assert.True(history.TryUndo(out var undoPaste, out _));
        Assert.Equal(cut, undoPaste);
        Assert.True(history.TryUndo(out var undoCut, out _));
        Assert.Equal(baseline, undoCut);
        Assert.True(history.TryRedo(out var redoCut, out _));
        Assert.Equal(cut, redoCut);
        Assert.True(history.TryGetResetTarget(cut, out var reset));
        Assert.Equal(baseline, reset);
        history.Record("Reset project", cut, reset);
        Assert.True(history.TryUndo(out var undoReset, out _));
        Assert.Equal(cut, undoReset);
    }

    [Fact]
    public void Branching_history_reports_discard_and_retains_only_reachable_files()
    {
        var baseline = Snapshot("baseline.png");
        var first = Snapshot("first.png");
        var discarded = Snapshot("discarded.png");
        var branch = Snapshot("branch.png");
        var history = new FrameEditHistory();
        var discardCount = 0;
        history.BranchDiscarded += () => discardCount++;
        history.SetBaseline(baseline);
        history.Record("first", baseline, first);
        history.Record("discarded", first, discarded);
        Assert.True(history.TryUndo(out _, out _));

        history.Record("branch", first, branch);

        Assert.Equal(1, discardCount);
        Assert.DoesNotContain(Path.GetFullPath("discarded.png"), history.ReferencedFiles());
        Assert.Contains(Path.GetFullPath("branch.png"), history.ReferencedFiles());
    }

    [Fact]
    public void Branch_cleanup_io_failure_cannot_break_a_committed_history_branch()
    {
        var baseline = Snapshot("baseline.png");
        var discarded = Snapshot("discarded.png");
        var branch = Snapshot("branch.png");
        var history = new FrameEditHistory();
        history.SetBaseline(baseline);
        history.Record("discarded", baseline, discarded);
        Assert.True(history.TryUndo(out _, out _));
        history.BranchDiscarded += () => throw new IOException("cleanup unavailable");

        var exception = Record.Exception(() => history.Record("branch", baseline, branch));

        Assert.Null(exception);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.True(history.TryUndo(out var restored, out var description));
        Assert.Equal("branch", description);
        Assert.Equal(baseline, restored);
    }

    [Fact]
    public void History_bounds_retained_edits_and_reports_evicted_artifacts()
    {
        var baseline = Snapshot("baseline.png");
        var history = new FrameEditHistory();
        var discardedCount = 0;
        history.BranchDiscarded += () => discardedCount++;
        history.SetBaseline(baseline);

        var before = baseline;
        for (var index = 0; index < FrameEditHistory.MaximumEntries + 2; index++)
        {
            var after = Snapshot($"frame-{index}.png");
            history.Record($"edit-{index}", before, after);
            before = after;
        }

        var undoCount = 0;
        while (history.TryUndo(out _, out _))
            undoCount++;

        Assert.Equal(FrameEditHistory.MaximumEntries, undoCount);
        Assert.Equal(2, discardedCount);
        Assert.Contains(Path.GetFullPath("baseline.png"), history.ReferencedFiles());
        Assert.DoesNotContain(Path.GetFullPath("frame-0.png"), history.ReferencedFiles());
        Assert.Contains(Path.GetFullPath("frame-1.png"), history.ReferencedFiles());
    }

    [Theory]
    [InlineData("10", true)]
    [InlineData("800", true)]
    [InlineData("9", false)]
    [InlineData("801", false)]
    [InlineData("Fit", false)]
    public void Zoom_bounds_are_explicit(string value, bool expected) =>
        Assert.Equal(expected, ZoomLevel.TryParse(value, out _));

    [Fact]
    public void Inverse_deselect_and_go_to_edges_are_deterministic()
    {
        Assert.Equal([1, 3], TimelineSelection.Invert(4, [0, 2]));
        Assert.Equal([0, 1], TimelineSelection.Invert(2, []));
        Assert.Empty(TimelineSelection.Invert(0, [0]));
        Assert.True(TimelineSelection.TryResolveOneBased("1", 3, out var first));
        Assert.Equal(0, first);
        Assert.True(TimelineSelection.TryResolveOneBased("3", 3, out var last));
        Assert.Equal(2, last);
        Assert.False(TimelineSelection.TryResolveOneBased("0", 3, out _));
        Assert.False(TimelineSelection.TryResolveOneBased("4", 3, out _));
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    private string CreateFile(string name, string contents)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, contents);
        return path;
    }

    private static EditorSnapshot Snapshot(params string[] paths) =>
        new(paths.Select(path => new FrameState(path, 100)).ToArray(), paths.Length == 0 ? [] : [0]);
}
