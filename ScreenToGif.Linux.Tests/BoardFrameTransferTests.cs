using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class BoardFrameTransferTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-board-transfer-{Guid.NewGuid():N}");

    private EditorWorkspace CreateWorkspace(string name) => EditorWorkspace.Create(Path.Combine(_root, name));

    private static EditorFrame[] WriteFrames(EditorWorkspace workspace, params int[] delays)
    {
        var batch = workspace.CreateBatch(EditorArtifactKind.Frames);
        return delays.Select((delay, index) =>
        {
            var path = Path.Combine(batch, $"{index:000000}.png");
            File.WriteAllText(path, $"frame-{index}");
            return new EditorFrame(path, delay);
        }).ToArray();
    }

    [Fact]
    public void CopiedFramesLiveUnderTheEditorWorkspaceAndSurviveTheBoardWorkspaceBeingDeleted()
    {
        using var editor = CreateWorkspace("editor");
        var board = CreateWorkspace("board");
        var recorded = WriteFrames(board, 100, 120, 140);

        var copied = BoardFrameTransfer.CopyInto(editor, recorded);
        board.Dispose();

        Assert.Equal(3, copied.Count);
        Assert.All(copied, frame => Assert.StartsWith(editor.RootPath, frame.FilePath, StringComparison.Ordinal));
        Assert.All(copied, frame => Assert.True(File.Exists(frame.FilePath)));
        Assert.Equal([100, 120, 140], copied.Select(frame => frame.DelayMs));
        Assert.Equal(["frame-0", "frame-1", "frame-2"], copied.Select(frame => File.ReadAllText(frame.FilePath)));
    }

    [Fact]
    public void CopyingKeepsTheRecordedOrder()
    {
        using var editor = CreateWorkspace("editor");
        using var board = CreateWorkspace("board");
        var recorded = WriteFrames(board, 10, 20, 30, 40, 50);

        var copied = BoardFrameTransfer.CopyInto(editor, recorded);

        Assert.Equal(
            copied.Select(frame => Path.GetFileName(frame.FilePath)),
            copied.Select(frame => Path.GetFileName(frame.FilePath)).Order(StringComparer.Ordinal));
        Assert.Equal([10, 20, 30, 40, 50], copied.Select(frame => frame.DelayMs));
    }

    [Fact]
    public void AnEmptyRecordingCopiesNothing()
    {
        using var editor = CreateWorkspace("editor");

        Assert.Empty(BoardFrameTransfer.CopyInto(editor, []));
    }

    [Fact]
    public void AMissingSourceFrameFailsTheTransferInsteadOfInsertingHalfOfIt()
    {
        using var editor = CreateWorkspace("editor");
        using var board = CreateWorkspace("board");
        var recorded = WriteFrames(board, 100, 100);
        File.Delete(recorded[1].FilePath);

        Assert.Throws<FileNotFoundException>(() => BoardFrameTransfer.CopyInto(editor, recorded));
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(editor.RootPath, "imports")));
    }

    [Fact]
    public void CancellationStopsTheTransferAndLeavesNoFrameToInsert()
    {
        using var editor = CreateWorkspace("editor");
        using var board = CreateWorkspace("board");
        var recorded = WriteFrames(board, 100, 100, 100);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => BoardFrameTransfer.CopyInto(editor, recorded, cancellation.Token));
        Assert.False(Directory.Exists(Path.Combine(editor.RootPath, "imports")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
