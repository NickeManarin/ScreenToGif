using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class EditSequenceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-edit-tests-{Guid.NewGuid():N}");
    private readonly FfmpegTool _ffmpeg = new();

    public EditSequenceTests() => Directory.CreateDirectory(_root);

    [Theory]
    [InlineData(0, 10_000)]
    [InlineData(9_999, 1)]
    [InlineData(10_000, 0)]
    public void Shared_frame_capacity_accepts_only_projects_at_or_below_the_save_limit(int current, int additional) =>
        EditorResourceLimits.EnsureCanInsertFrames(current, additional, "Editing");

    [Theory]
    [InlineData(0, 10_001)]
    [InlineData(9_999, 2)]
    [InlineData(10_000, 1)]
    public void Shared_frame_capacity_rejects_unsaveable_insertions(int current, int additional)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            EditorResourceLimits.EnsureCanInsertFrames(current, additional, "Editing"));

        Assert.Contains("10000-frame project limit", error.Message);
    }

    [Fact]
    public void Delete_before_after_and_discontiguous_delete_preserve_unaffected_identity()
    {
        var frames = States("a", "b", "c", "d", "e");
        Assert.Equal(["a", "c", "e"], Paths(FrameSequenceOperations.Delete(frames, [1, 3])));
        Assert.Equal(["c", "d", "e"], Paths(FrameSequenceOperations.DeleteBefore(frames, 2)));
        Assert.Equal(["a", "b", "c"], Paths(FrameSequenceOperations.DeleteAfter(frames, 2)));
        Assert.Equal(Paths(frames), Paths(FrameSequenceOperations.Delete(frames, [])));
    }

    [Fact]
    public void Reverse_yoyo_reduce_and_move_have_exact_boundary_behavior()
    {
        var frames = States("a", "b", "c", "d", "e");
        Assert.Equal(["a", "d", "c", "b", "e"], Paths(FrameSequenceOperations.Reverse(frames, [1, 2, 3])));
        Assert.Equal(["a", "b", "c", "d", "c", "b", "e"], Paths(FrameSequenceOperations.Yoyo(frames, [0, 1, 2, 3])));
        Assert.Equal(["a", "c", "e"], Paths(FrameSequenceOperations.Reduce(frames, [0, 1, 2, 3, 4], 2)));
        Assert.Equal(["b", "c", "a", "d", "e"], Paths(FrameSequenceOperations.Move(frames, [1, 2], -1)));
        Assert.Equal(["a", "d", "b", "c", "e"], Paths(FrameSequenceOperations.Move(frames, [1, 2], 1)));
        Assert.Equal([0, 1], FrameSequenceOperations.MoveSelection(5, [1, 2], -1));
        Assert.Equal([2, 3], FrameSequenceOperations.MoveSelection(5, [1, 2], 1));
        Assert.Equal(Paths(frames), Paths(FrameSequenceOperations.Move(frames, [0], -1)));
        Assert.Equal(Paths(frames), Paths(FrameSequenceOperations.Yoyo(frames, [0])));
    }

    [Fact]
    public void Every_sequence_and_delay_operation_records_one_exact_undo_redo_state()
    {
        var frames = States("a", "b", "c", "d", "e");
        AssertReversible("Delete", frames, [1, 3], FrameSequenceOperations.Delete(frames, [1, 3]), [], ["a", "c", "e"]);
        AssertReversible("Delete before", frames, [2], FrameSequenceOperations.DeleteBefore(frames, 2), [0], ["c", "d", "e"]);
        AssertReversible("Delete after", frames, [2], FrameSequenceOperations.DeleteAfter(frames, 2), [2], ["a", "b", "c"]);
        AssertReversible("Reduce", frames, [0, 1, 2, 3, 4], FrameSequenceOperations.Reduce(frames, [0, 1, 2, 3, 4], 2), [], ["a", "c", "e"]);
        AssertReversible("Reverse", frames, [1, 3], FrameSequenceOperations.Reverse(frames, [1, 3]), [1, 3], ["a", "d", "c", "b", "e"]);
        AssertReversible("Yoyo", frames, [0, 1, 2, 3, 4], FrameSequenceOperations.Yoyo(frames, [0, 1, 2, 3, 4]), [5, 6, 7], ["a", "b", "c", "d", "e", "d", "c", "b"]);
        AssertReversible("Move left", frames, [1, 3], FrameSequenceOperations.Move(frames, [1, 3], -1), [0, 2], ["b", "a", "d", "c", "e"]);
        AssertReversible("Move right", frames, [1, 3], FrameSequenceOperations.Move(frames, [1, 3], 1), [2, 4], ["a", "c", "b", "e", "d"]);
        AssertReversible("Apply selected delay", frames, [0, 2], FrameSequenceOperations.ApplyDelay(frames, [0, 2], 250), [0, 2], ["a", "b", "c", "d", "e"], [250, 100, 250, 100, 100]);
        AssertReversible("Apply all delays", frames, [1], FrameSequenceOperations.ApplyDelay(frames, Enumerable.Range(0, frames.Length), 300), [1], ["a", "b", "c", "d", "e"], [300, 300, 300, 300, 300]);
    }

    [Fact]
    public void Every_sequence_operation_has_exact_empty_single_boundary_discontiguous_and_repeated_edges()
    {
        var empty = States();
        Assert.Empty(FrameSequenceOperations.Delete(empty, []));
        Assert.Empty(FrameSequenceOperations.DeleteBefore(empty, 0));
        Assert.Empty(FrameSequenceOperations.DeleteAfter(empty, 0));
        Assert.Empty(FrameSequenceOperations.Reduce(empty, [], 2));
        Assert.Empty(FrameSequenceOperations.Reverse(empty, []));
        Assert.Empty(FrameSequenceOperations.Yoyo(empty, []));
        Assert.Empty(FrameSequenceOperations.Move(empty, [], -1));
        Assert.Empty(FrameSequenceOperations.ApplyDelay(empty, [], 200));

        var single = States("a");
        Assert.Empty(FrameSequenceOperations.Delete(single, [0]));
        Assert.Equal(["a"], Paths(FrameSequenceOperations.DeleteBefore(single, 0)));
        Assert.Equal(["a"], Paths(FrameSequenceOperations.DeleteAfter(single, 0)));
        Assert.Equal(["a"], Paths(FrameSequenceOperations.Reduce(single, [0], 2)));
        Assert.Equal(["a"], Paths(FrameSequenceOperations.Reverse(single, [0])));
        Assert.Equal(["a"], Paths(FrameSequenceOperations.Yoyo(single, [0])));
        Assert.Equal(["a"], Paths(FrameSequenceOperations.Move(single, [0], 1)));
        Assert.Equal([200], FrameSequenceOperations.ApplyDelay(single, [0], 200).Select(frame => frame.DelayMs));

        var boundary = States("a", "b", "c");
        Assert.Equal(["b"], Paths(FrameSequenceOperations.Delete(boundary, [0, 2])));
        Assert.Equal(["a", "b", "c"], Paths(FrameSequenceOperations.DeleteBefore(boundary, 0)));
        Assert.Equal(["a", "b", "c"], Paths(FrameSequenceOperations.DeleteAfter(boundary, 2)));
        Assert.Equal(["a", "b"], Paths(FrameSequenceOperations.Reduce(boundary, [0, 2], 2)));
        Assert.Equal(["c", "b", "a"], Paths(FrameSequenceOperations.Reverse(boundary, [0, 2])));
        Assert.Equal(["a", "b", "c"], Paths(FrameSequenceOperations.Yoyo(boundary, [0, 2])));
        Assert.Equal(["a", "c", "b"], Paths(FrameSequenceOperations.Move(boundary, [0, 2], -1)));
        Assert.Equal(["b", "a", "c"], Paths(FrameSequenceOperations.Move(boundary, [0, 2], 1)));
        Assert.Equal([200, 100, 200], FrameSequenceOperations.ApplyDelay(boundary, [0, 2], 200).Select(frame => frame.DelayMs));

        var discontiguous = States("a", "b", "c", "d", "e");
        Assert.Equal(["a", "c", "e"], Paths(FrameSequenceOperations.Delete(discontiguous, [1, 3])));
        Assert.Equal(["b", "c", "d", "e"], Paths(FrameSequenceOperations.DeleteBefore(discontiguous, 1)));
        Assert.Equal(["a", "b", "c", "d"], Paths(FrameSequenceOperations.DeleteAfter(discontiguous, 3)));
        Assert.Equal(["a", "b", "c", "e"], Paths(FrameSequenceOperations.Reduce(discontiguous, [1, 3], 2)));
        Assert.Equal(["a", "d", "c", "b", "e"], Paths(FrameSequenceOperations.Reverse(discontiguous, [1, 3])));
        Assert.Equal(["a", "b", "c", "d", "e"], Paths(FrameSequenceOperations.Yoyo(discontiguous, [1, 3])));
        Assert.Equal(["b", "a", "d", "c", "e"], Paths(FrameSequenceOperations.Move(discontiguous, [1, 3], -1)));
        Assert.Equal(["a", "c", "b", "e", "d"], Paths(FrameSequenceOperations.Move(discontiguous, [1, 3], 1)));
        Assert.Equal([100, 200, 100, 200, 100], FrameSequenceOperations.ApplyDelay(discontiguous, [1, 3], 200).Select(frame => frame.DelayMs));

        var repeated = States("a", "a", "b");
        Assert.Empty(FrameSequenceOperations.Delete(repeated, [0, 1, 2]));
        Assert.Equal(["a", "a", "b"], Paths(FrameSequenceOperations.DeleteBefore(repeated, 0)));
        Assert.Equal(["a", "a", "b"], Paths(FrameSequenceOperations.DeleteAfter(repeated, 2)));
        Assert.Equal(["a", "b"], Paths(FrameSequenceOperations.Reduce(repeated, [0, 1, 2], 2)));
        Assert.Equal(["b", "a", "a"], Paths(FrameSequenceOperations.Reverse(repeated, [0, 1, 2])));
        Assert.Equal(["a", "a", "b", "a"], Paths(FrameSequenceOperations.Yoyo(repeated, [0, 1, 2])));
        Assert.Equal(["a", "a", "b"], Paths(FrameSequenceOperations.Move(repeated, [0, 1, 2], -1)));
        Assert.Equal([200, 200, 200], FrameSequenceOperations.ApplyDelay(repeated, [0, 1, 2], 200).Select(frame => frame.DelayMs));
    }

    [Fact]
    public void Boundary_delete_no_ops_preserve_selection_and_create_no_history_entry()
    {
        var frames = States("a", "b", "c");
        var selection = new[] { 0, 2 };
        var before = new EditorSnapshot(frames, selection);

        foreach (var unchanged in new[]
                 {
                     FrameSequenceOperations.DeleteBefore(frames, selection[0]),
                     FrameSequenceOperations.DeleteAfter(frames, selection[^1])
                 })
        {
            var history = new FrameEditHistory();
            history.SetBaseline(before);
            history.Record("Boundary no-op", before, new EditorSnapshot(unchanged, selection));
            Assert.False(history.CanUndo);
            Assert.Equal(["a", "b", "c"], Paths(unchanged));
        }
    }

    [Fact]
    public void Delay_operations_change_only_the_scope_and_are_snapshot_reversible()
    {
        var frames = States("a", "b", "c");
        var changed = FrameSequenceOperations.ApplyDelay(frames, [0, 2], 250);
        Assert.Equal([250, 100, 250], changed.Select(frame => frame.DelayMs));

        var before = new EditorSnapshot(frames, [0, 2]);
        var after = new EditorSnapshot(changed, [0, 2]);
        var history = new FrameEditHistory();
        history.SetBaseline(before);
        history.Record("Delay", before, after);
        Assert.True(history.TryUndo(out var undone, out _));
        Assert.Equal([100, 100, 100], undone.Frames.Select(frame => frame.DelayMs));
        Assert.True(history.TryRedo(out var redone, out _));
        Assert.Equal([250, 100, 250], redone.Frames.Select(frame => frame.DelayMs));
    }

    [Fact]
    public async Task Duplicate_detection_is_consecutive_pixel_scoped_and_handles_repeated_content()
    {
        var sameA = await ColorFrameAsync("same-a.png", "red", 9);
        var sameB = await ColorFrameAsync("same-b.png", "red", 0);
        var different = await ColorFrameAsync("different.png", "blue", 6);
        var sameC = await ColorFrameAsync("same-c.png", "red", 3);
        var states = new[]
        {
            new FrameState(sameA, 50), new FrameState(sameB, 60),
            new FrameState(different, 70), new FrameState(sameC, 80)
        };

        Assert.False((await File.ReadAllBytesAsync(sameA)).SequenceEqual(await File.ReadAllBytesAsync(sameB)));
        var duplicates = await FrameSequenceOperations.FindConsecutiveDuplicatesAsync(states, [0, 1, 2, 3], _ffmpeg);
        Assert.Equal([1], duplicates);
        AssertReversible("Remove duplicates", states, [0, 1, 2, 3],
            FrameSequenceOperations.Delete(states, duplicates), [],
            [sameA, different, sameC], [50, 70, 80]);
        Assert.Empty(await FrameSequenceOperations.FindConsecutiveDuplicatesAsync(states, [1, 3], _ffmpeg));
    }

    [Fact]
    public async Task Duplicate_detection_canonicalizes_rgb_and_rgba_pixels()
    {
        var rgb = await ColorFrameAsync("rgb.png", "red", 3, "rgb24");
        var rgba = await ColorFrameAsync("rgba.png", "red", 3, "rgba");

        Assert.Equal([1], await FrameSequenceOperations.FindConsecutiveDuplicatesAsync(
            [new FrameState(rgb, 50), new FrameState(rgba, 60)], [0, 1], _ffmpeg));
    }

    [Fact]
    public void Empty_single_and_invalid_inputs_are_safe()
    {
        Assert.Empty(FrameSequenceOperations.Reverse([], []));
        Assert.Equal(["a"], Paths(FrameSequenceOperations.Reduce(States("a"), [0], 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => FrameSequenceOperations.Reduce(States("a", "b"), [0, 1], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => FrameSequenceOperations.ApplyDelay(States("a"), [0], 0));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private FrameState[] States(params string[] names) => names.Select(name => new FrameState(name, 100)).ToArray();
    private static string[] Paths(IEnumerable<FrameState> frames) => frames.Select(frame => frame.FilePath).ToArray();

    private static void AssertReversible(
        string description,
        IReadOnlyList<FrameState> beforeFrames,
        IReadOnlyList<int> beforeSelection,
        IReadOnlyList<FrameState> afterFrames,
        IReadOnlyList<int> afterSelection,
        IReadOnlyList<string> expectedPaths,
        IReadOnlyList<int>? expectedDelays = null)
    {
        Assert.Equal(expectedPaths, Paths(afterFrames));
        Assert.Equal(expectedDelays ?? Enumerable.Repeat(100, afterFrames.Count), afterFrames.Select(frame => frame.DelayMs));
        var before = new EditorSnapshot(beforeFrames, beforeSelection);
        var after = new EditorSnapshot(afterFrames, afterSelection);
        var history = new FrameEditHistory();
        history.SetBaseline(before);
        history.Record(description, before, after);
        Assert.True(history.TryUndo(out var undone, out var undoDescription));
        Assert.Equal(description, undoDescription);
        Assert.Equal(before, undone);
        Assert.True(history.TryRedo(out var redone, out var redoDescription));
        Assert.Equal(description, redoDescription);
        Assert.Equal(after, redone);
    }

    private async Task<string> ColorFrameAsync(string name, string color, int compression, string? pixelFormat = null)
    {
        var path = Path.Combine(_root, name);
        var arguments = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", $"color={color}:size=8x6",
            "-frames:v", "1", "-compression_level", compression.ToString()
        };
        if (pixelFormat is not null)
        {
            arguments.Add("-pix_fmt");
            arguments.Add(pixelFormat);
        }

        arguments.Add(path);
        await _ffmpeg.RunFfmpegCheckedAsync(arguments);
        return path;
    }
}
