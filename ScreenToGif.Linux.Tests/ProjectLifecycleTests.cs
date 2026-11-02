using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using System.IO.Compression;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class ProjectLifecycleTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-project-tests-{Guid.NewGuid():N}");
    private readonly FfmpegTool _ffmpeg = new();

    public ProjectLifecycleTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Initial_import_becomes_the_reset_baseline_but_remains_unsaved()
    {
        var history = new FrameEditHistory();
        var empty = new EditorSnapshot([], []);
        var current = empty;
        var coordinator = new EditorMutationCoordinator(history, () => current, () => { });
        history.SetBaseline(empty);
        coordinator.MarkClean();
        Assert.True(coordinator.IsPristineEmptySession(empty));

        var imported = new EditorSnapshot([new FrameState("imported.png", 100)], [0]);
        current = imported;
        coordinator.FinalizeImport(empty);

        Assert.True(coordinator.HasUnsavedChanges);
        Assert.False(history.CanUndo);
        Assert.False(history.TryGetResetTarget(imported, out _));

        var edited = new EditorSnapshot([new FrameState("edited.png", 100)], [0]);
        current = edited;
        coordinator.Commit("Edit", imported);

        Assert.True(history.TryGetResetTarget(edited, out var reset));
        Assert.Equal(imported, reset);
    }

    [Fact]
    public void Import_into_an_emptied_project_preserves_the_existing_history_and_reset_baseline()
    {
        var history = new FrameEditHistory();
        var imported = new EditorSnapshot([new FrameState("imported.png", 100)], [0]);
        var empty = new EditorSnapshot([], []);
        var current = imported;
        var coordinator = new EditorMutationCoordinator(history, () => current, () => { });
        history.SetBaseline(imported);
        coordinator.MarkClean();

        current = empty;
        coordinator.Commit("Delete frames", imported);

        var inserted = new EditorSnapshot([new FrameState("later.png", 80)], [0]);
        current = inserted;
        coordinator.FinalizeImport(empty);

        Assert.True(history.CanUndo);
        Assert.True(coordinator.HasUnsavedChanges);
        Assert.True(history.TryUndo(out var afterUndoImport, out var importDescription));
        Assert.Equal("Insert media", importDescription);
        Assert.Equal(empty, afterUndoImport);
        Assert.True(history.TryUndo(out var afterUndoDelete, out _));
        Assert.Equal(imported, afterUndoDelete);
        Assert.True(history.TryGetResetTarget(inserted, out var reset));
        Assert.Equal(imported, reset);
    }

    [Fact]
    public void Import_baseline_policy_rejects_each_non_pristine_session_signal()
    {
        var empty = new EditorSnapshot([], []);
        var nonEmpty = new EditorSnapshot([new FrameState("frame.png", 100)], [0]);
        var cleanHistory = new FrameEditHistory();
        var cleanCurrent = empty;
        var cleanCoordinator = new EditorMutationCoordinator(cleanHistory, () => cleanCurrent, () => { });
        cleanHistory.SetBaseline(empty);
        cleanCoordinator.MarkClean();
        Assert.False(cleanCoordinator.IsPristineEmptySession(nonEmpty));

        cleanCurrent = nonEmpty;
        cleanCoordinator.Refresh();
        Assert.False(cleanCoordinator.IsPristineEmptySession(empty));

        var history = new FrameEditHistory();
        var current = empty;
        var coordinator = new EditorMutationCoordinator(history, () => current, () => { });
        history.SetBaseline(empty);
        coordinator.MarkClean();
        var selectionOnly = new EditorSnapshot([], [0]);
        history.Record("Selection", empty, selectionOnly);
        Assert.False(coordinator.IsPristineEmptySession(empty));

        Assert.True(history.TryUndo(out _, out _));
        Assert.False(coordinator.IsPristineEmptySession(empty));
    }

    [Fact]
    public async Task Blank_project_has_explicit_dimensions_count_delay_and_unique_files()
    {
        var loaded = await new BlankProjectFactory(_ffmpeg).CreateAsync(14, 10, 3, 125);
        try
        {
            Assert.Equal(3, loaded.Frames.Count);
            Assert.Equal([125, 125, 125], loaded.Frames.Select(frame => frame.DelayMs));
            Assert.Equal(3, loaded.Frames.Select(frame => frame.FilePath).Distinct().Count());
            Assert.All(loaded.Frames, frame => Assert.True(File.Exists(frame.FilePath)));
            Assert.All(loaded.Frames, frame => Assert.Equal((14, 10), GetSize(frame.FilePath)));
        }
        finally
        {
            loaded.Dispose();
        }
    }

    [Fact]
    public async Task Blank_project_rejects_unbounded_dimensions_counts_and_aggregate_pixels()
    {
        var factory = new BlankProjectFactory(_ffmpeg);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => factory.CreateAsync(8193, 10, 1, 100));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => factory.CreateAsync(10, 10, 1001, 100));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => factory.CreateAsync(1000, 1000, 251, 100));
    }

    [Fact]
    public async Task Blank_project_preserves_requested_odd_dimensions()
    {
        var blank = await new BlankProjectFactory(_ffmpeg).CreateAsync(31, 23, 1, 100);
        try
        {
            Assert.Equal((31, 23), GetSize(blank.Frames.Single().FilePath));
        }
        finally
        {
            blank.Dispose();
        }
    }

    [Fact]
    public async Task Recent_projects_are_most_recent_first_bounded_and_prune_missing_entries()
    {
        var store = new RecentProjectStore(Path.Combine(_root, "recents.json"), capacity: 2);
        var first = FileOf("first.stg-linux", "one");
        var second = FileOf("second.stg-linux", "two");
        var third = FileOf("third.stg-linux", "three");

        await store.AddAsync(first);
        await store.AddAsync(second);
        await store.AddAsync(third);
        Assert.Equal([Path.GetFullPath(third), Path.GetFullPath(second)], await store.GetExistingAsync());

        File.Delete(third);
        Assert.Equal([Path.GetFullPath(second)], await store.GetExistingAsync());
    }

    [Fact]
    public async Task Recent_project_best_effort_add_reports_storage_failure_without_throwing()
    {
        var blockingFile = FileOf("not-a-directory", "blocked");
        var store = new RecentProjectStore(Path.Combine(blockingFile, "recents.json"));

        Assert.False(await store.TryAddAsync(Path.Combine(_root, "project.stg-linux")));
    }

    [Fact]
    public async Task Failed_project_save_preserves_existing_archive_and_removes_temporary_file()
    {
        var archive = FileOf("project.stg-linux", "existing-project");
        var missing = new EditorFrame(Path.Combine(_root, "missing.png"), 100);

        await Assert.ThrowsAnyAsync<Exception>(() => ProjectArchive.SaveAsync(archive, [missing]));

        Assert.Equal("existing-project", File.ReadAllText(archive));
        Assert.Empty(Directory.EnumerateFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task Project_save_rejects_a_frame_above_the_loader_limit_before_replacing_the_destination()
    {
        var archive = FileOf("bounded-project.stg-linux", "existing-project");
        var oversizedFrame = Path.Combine(_root, "oversized.png");
        await using (var stream = new FileStream(oversizedFrame, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            stream.SetLength(300_000_001);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProjectArchive.SaveAsync(archive, [new EditorFrame(oversizedFrame, 100)]));

        Assert.Contains("too large to save", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("existing-project", File.ReadAllText(archive));
        Assert.Empty(Directory.EnumerateFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task Saved_blank_project_round_trips_and_can_seed_recents()
    {
        var blank = await new BlankProjectFactory(_ffmpeg).CreateAsync(8, 6, 2, 90);
        var archive = Path.Combine(_root, "round-trip.stg-linux");
        try
        {
            await ProjectArchive.SaveAsync(archive, blank.Frames);
            var loaded = await ProjectArchive.LoadAsync(archive);
            try
            {
                Assert.Equal([90, 90], loaded.Frames.Select(frame => frame.DelayMs));
                Assert.All(loaded.Frames, frame => Assert.Equal((8, 6), GetSize(frame.FilePath)));
            }
            finally
            {
                loaded.Dispose();
            }
        }
        finally
        {
            blank.Dispose();
        }
    }

    [Fact]
    public void Workspace_scavenger_removes_dead_owners_and_preserves_live_ones()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), "screentogif-linux");
        var dead = Path.Combine(workspaceRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dead);
        File.WriteAllText(Path.Combine(dead, ".owner"), $"{int.MaxValue}|0");
        var live = ProjectArchive.CreateWorkspace();
        try
        {
            ProjectArchive.ScavengeStaleWorkspaces();
            Assert.False(Directory.Exists(dead));
            Assert.True(Directory.Exists(live.RootPath));
        }
        finally
        {
            live.Dispose();
            if (Directory.Exists(dead))
                Directory.Delete(dead, recursive: true);
        }
    }

    [Fact]
    public void Workspace_owns_all_artifact_batches_and_prunes_unreachable_imports_and_edits()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "owned-workspace"));
        var referencedImport = FileOf(workspace.CreateBatch(EditorArtifactKind.Imports), "keep.png", "keep");
        var discardedImport = FileOf(workspace.CreateBatch(EditorArtifactKind.Imports), "discard.png", "discard");
        var discardedEdit = FileOf(workspace.CreateBatch(EditorArtifactKind.Edits), "discard.png", "discard");

        workspace.PruneUnreachable([referencedImport]);

        Assert.True(File.Exists(referencedImport));
        Assert.False(File.Exists(discardedImport));
        Assert.False(File.Exists(discardedEdit));
    }

    [Fact]
    public void Active_project_replacement_deletes_the_old_workspace_and_adopts_the_candidate()
    {
        var previous = EditorWorkspace.Create(Path.Combine(_root, "previous-workspace"));
        var previousRoot = previous.RootPath;
        FileOf(previous.CreateBatch(EditorArtifactKind.Frames), "old.png", "old");
        var candidateWorkspace = EditorWorkspace.Create(Path.Combine(_root, "candidate-workspace"));
        var candidateFrame = new EditorFrame(
            FileOf(candidateWorkspace.CreateBatch(EditorArtifactKind.Frames), "new.png", "new"), 125);
        var candidate = new LoadedProject(candidateWorkspace, [candidateFrame]);
        var active = previous;
        IReadOnlyList<EditorFrame> activeFrames = [];

        ProjectWorkspaceLifecycle.Replace(active, candidate, replacement =>
        {
            active = replacement.Workspace;
            activeFrames = replacement.Frames;
        });

        Assert.False(Directory.Exists(previousRoot));
        Assert.True(Directory.Exists(active.RootPath));
        Assert.Same(candidateFrame, Assert.Single(activeFrames));
        active.Dispose();
    }

    [Fact]
    public void Failed_project_adoption_preserves_the_old_workspace_and_cleans_the_candidate()
    {
        using var previous = EditorWorkspace.Create(Path.Combine(_root, "preserved-workspace"));
        var candidateWorkspace = EditorWorkspace.Create(Path.Combine(_root, "failed-candidate-workspace"));
        var candidateRoot = candidateWorkspace.RootPath;
        var candidate = new LoadedProject(candidateWorkspace, []);

        Assert.Throws<InvalidOperationException>(() => ProjectWorkspaceLifecycle.Replace(
            previous, candidate, _ => throw new InvalidOperationException("simulated adoption failure")));

        Assert.True(Directory.Exists(previous.RootPath));
        Assert.False(Directory.Exists(candidateRoot));
    }

    [Fact]
    public async Task Project_load_rejects_unsafe_missing_and_malformed_content_without_leaking_workspace()
    {
        var cases = new (string Name, Action<ZipArchive> Write, string Message)[]
        {
            ("unsafe", archive => WriteEntry(archive, "../escaped.txt", "nope"), "unsafe path"),
            ("reserved-owner", archive => WriteEntry(archive, ".owner", $"{int.MaxValue}|0"), "reserved workspace path"),
            ("unsupported", archive => WriteEntry(archive, "notes.txt", "nope"), "unsupported archive path"),
            ("duplicate", archive =>
            {
                WriteEntry(archive, "frames/duplicate.png", "first");
                WriteEntry(archive, "frames/duplicate.png", "second");
            }, "duplicate archive path"),
            ("oversized-manifest", archive => WriteEntry(archive, "project.json", new string('x', 1_048_577)), "too large"),
            ("missing-manifest", archive => WriteEntry(archive, "frames/000000.png", "pixels"), "missing project.json"),
            ("malformed", archive => WriteEntry(archive, "project.json", "{not-json"), "not valid JSON"),
            ("missing-frame", archive => WriteEntry(archive, "project.json", "{\"Version\":1,\"Frames\":[{\"Path\":\"frames/missing.png\",\"DelayMs\":100}]}"), "missing frame"),
            ("unknown-version", archive => WriteEntry(archive, "project.json", "{\"Version\":99,\"Frames\":[]}"), "version 99"),
            ("null-frame", archive => WriteEntry(archive, "project.json", "{\"Version\":1,\"Frames\":[null]}"), "invalid frame entry"),
            ("null-path", archive => WriteEntry(archive, "project.json", "{\"Version\":1,\"Frames\":[{\"Path\":null,\"DelayMs\":100}]}"), "invalid frame entry"),
            ("invalid-delay", archive => WriteEntry(archive, "project.json", "{\"Version\":1,\"Frames\":[{\"Path\":\"frames/000000.png\",\"DelayMs\":0}]}"), "invalid delay")
        };

        foreach (var item in cases)
        {
            var archivePath = Path.Combine(_root, $"{item.Name}.stg-linux");
            var marker = $"{item.Name}-{Guid.NewGuid():N}.png";
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                WriteEntry(archive, $"frames/{marker}", "cleanup-marker");
                item.Write(archive);
            }

            var error = await Assert.ThrowsAsync<InvalidDataException>(() => ProjectArchive.LoadAsync(archivePath));

            Assert.Contains(item.Message, error.Message, StringComparison.OrdinalIgnoreCase);
            var workspaceRoot = Path.Combine(Path.GetTempPath(), "screentogif-linux");
            Assert.Empty(Directory.Exists(workspaceRoot)
                ? Directory.EnumerateFiles(workspaceRoot, marker, SearchOption.AllDirectories)
                : []);
        }

        Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), "screentogif-linux", "escaped.txt")));
    }

    [Fact]
    public async Task Project_load_rejects_an_excessive_archive_entry_count()
    {
        var archivePath = Path.Combine(_root, "too-many-entries.stg-linux");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            for (var index = 0; index < 10_003; index++)
                archive.CreateEntry($"frames/{index:00000}.png");
        }

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => ProjectArchive.LoadAsync(archivePath));

        Assert.Contains("too many archive entries", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Project_load_rejects_a_manifest_above_the_shared_frame_limit()
    {
        var archivePath = Path.Combine(_root, "too-many-frames.stg-linux");
        var frames = Enumerable.Range(0, 10_001)
            .Select(index => new { Path = $"frames/{index:00000}.png", DelayMs = 100 })
            .ToArray();
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "project.json", System.Text.Json.JsonSerializer.Serialize(new { Version = 1, Frames = frames }));
            foreach (var frame in frames)
                archive.CreateEntry(frame.Path);
        }

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => ProjectArchive.LoadAsync(archivePath));

        Assert.Contains("more than 10000 frames", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Project_load_rejects_excessive_decoded_dimensions_and_cleans_the_workspace()
    {
        var archivePath = Path.Combine(_root, "excessive-dimensions.stg-linux");
        var marker = $"oversized-{Guid.NewGuid():N}.png";
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "project.json",
                $"{{\"Version\":1,\"Frames\":[{{\"Path\":\"frames/{marker}\",\"DelayMs\":100}}]}}");
            WriteEntry(archive, $"frames/{marker}", PngHeader(8193, 1));
        }

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => ProjectArchive.LoadAsync(archivePath));

        Assert.Contains("dimensions must be between", error.Message, StringComparison.OrdinalIgnoreCase);
        var workspaceRoot = Path.Combine(Path.GetTempPath(), "screentogif-linux");
        Assert.Empty(Directory.Exists(workspaceRoot)
            ? Directory.EnumerateFiles(workspaceRoot, marker, SearchOption.AllDirectories)
            : []);
    }

    [Fact]
    public async Task Project_load_rejects_excessive_aggregate_decoded_pixels()
    {
        var archivePath = Path.Combine(_root, "excessive-decoded-pixels.stg-linux");
        var frames = Enumerable.Range(0, 16)
            .Select(index => new { Path = $"frames/{index:000000}.png", DelayMs = 100 })
            .ToArray();
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "project.json",
                System.Text.Json.JsonSerializer.Serialize(new { Version = 1, Frames = frames }));
            foreach (var frame in frames)
                WriteEntry(archive, frame.Path, PngHeader(8192, 8192));
        }

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => ProjectArchive.LoadAsync(archivePath));

        Assert.Contains("decoded-pixel limit", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Canceling_a_large_project_save_preserves_the_destination_and_removes_staging()
    {
        var archivePath = FileOf("cancel-save.stg-linux", "existing-project");
        var largeFrame = Path.Combine(_root, "large-frame.png");
        await using (var stream = new FileStream(largeFrame, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await stream.WriteAsync(PngHeader(1, 1));
            stream.SetLength(250_000_000);
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ProjectArchive.SaveAsync(archivePath, [new EditorFrame(largeFrame, 100)], cancellation.Token));

        Assert.Equal("existing-project", File.ReadAllText(archivePath));
        Assert.Empty(Directory.EnumerateFiles(_root, "*.tmp"));
    }

    [Fact]
    public void Proposed_timeline_storage_is_bounded_before_archive_creation()
    {
        var frame = Path.Combine(_root, "repeatable-large-frame.png");
        using (var stream = new FileStream(frame, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            stream.SetLength(299_000_000);

        var error = Assert.Throws<InvalidOperationException>(() =>
            EditorProjectBudget.EnsureStorageBudget(Enumerable.Repeat(frame, 15), "Creating a yoyo"));

        Assert.Contains("project storage limit", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Proposed_timeline_aggregate_pixels_are_bounded_across_separate_ingress_batches()
    {
        var paths = Enumerable.Range(0, 16).Select(index =>
        {
            var path = Path.Combine(_root, $"large-dimensions-{index}.png");
            File.WriteAllBytes(path, PngHeader(8192, 8192));
            return path;
        }).ToArray();

        var error = Assert.Throws<InvalidOperationException>(() =>
            EditorProjectBudget.EnsureTimelineBudget(paths, "Adding another import"));

        Assert.Contains("decoded-pixel limit", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private (int Width, int Height) GetSize(string path)
    {
        var result = _ffmpeg.RunFfprobeCheckedAsync(
            ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "csv=s=x:p=0", path]).GetAwaiter().GetResult();
        var parts = result.StandardOutput.Trim().Split('x').Select(int.Parse).ToArray();
        return (parts[0], parts[1]);
    }

    private string FileOf(string name, string content)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static string FileOf(string directory, string name, string content)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open());
        writer.Write(content);
    }

    private static void WriteEntry(ZipArchive archive, string path, byte[] content)
    {
        using var stream = archive.CreateEntry(path).Open();
        stream.Write(content);
    }

    private static byte[] PngHeader(int width, int height)
    {
        var header = new byte[24];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(header, 0);
        "IHDR"u8.CopyTo(header.AsSpan(12));
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16, 4), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(20, 4), height);
        return header;
    }
}
