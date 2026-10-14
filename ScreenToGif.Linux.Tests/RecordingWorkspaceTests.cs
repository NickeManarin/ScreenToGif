using Avalonia;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Covers what a recording leaves on disk: that its frames form a project the editor accepts, and
/// that throwing it away reaches its own files and nothing else.
/// </summary>
public sealed class RecordingWorkspaceTests : IDisposable
{
    private static readonly PixelRect Region = new(0, 0, 24, 16);

    private readonly string _scratch =
        Path.Combine(Path.GetTempPath(), $"screentogif-recording-tests-{Guid.NewGuid():N}");

    public RecordingWorkspaceTests() => Directory.CreateDirectory(_scratch);

    [Fact]
    public async Task Recorded_frames_round_trip_through_the_project_archive_unchanged()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "workspace"));
        var clock = new FakeRecordingClock();
        var session = NewSession(workspace, clock, new RecordingSettings { FramesPerSecond = 10 });

        session.Record();
        session.Tick();
        foreach (var gap in new[] { 120, 150, 110 })
        {
            clock.Advance(gap);
            session.Tick();
        }

        var recorded = await session.StopAsync();
        Assert.Equal(4, recorded.Count);

        var archivePath = Path.Combine(_scratch, "recording.stgl");
        await ProjectArchive.SaveAsync(archivePath, recorded);
        using var reloaded = await ProjectArchive.LoadAsync(archivePath);

        Assert.Equal(recorded.Count, reloaded.Frames.Count);
        Assert.Equal(
            recorded.Select(frame => frame.DelayMs),
            reloaded.Frames.Select(frame => frame.DelayMs));
        Assert.Equal([120, 150, 110], reloaded.Frames.Take(3).Select(frame => frame.DelayMs));

        foreach (var frame in reloaded.Frames)
        {
            var dimensions = EditorProjectBudget.ReadPngDimensions(frame.FilePath);
            Assert.Equal(new PngDimensions(Region.Width, Region.Height), dimensions);
        }

        await session.DisposeAsync();
    }

    [Fact]
    public async Task Discard_removes_only_the_recording_batch()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "workspace"));
        var sibling = workspace.CreateBatch(EditorArtifactKind.Frames);
        var siblingFile = Path.Combine(sibling, "keep-me.png");
        File.WriteAllText(siblingFile, "an unrelated editing artifact");
        var outsider = Path.Combine(_scratch, "outside-the-workspace.txt");
        File.WriteAllText(outsider, "must survive");

        var clock = new FakeRecordingClock();
        var session = NewSession(workspace, clock, new RecordingSettings { FramesPerSecond = 10 },
            out var batchPath);

        session.Record();
        session.Tick();
        clock.Advance(100);
        session.Tick();
        Assert.Equal(2, session.FrameCount);

        session.Discard();

        Assert.False(Directory.Exists(batchPath), "The recording's own batch directory should be gone.");
        Assert.True(Directory.Exists(workspace.RootPath), "The workspace itself is not the recording's to delete.");
        Assert.True(File.Exists(siblingFile), "A sibling batch must survive a discard.");
        Assert.True(File.Exists(outsider), "Nothing outside the workspace may be touched.");

        await session.DisposeAsync();
    }

    [Fact]
    public async Task Closing_a_session_that_was_never_stopped_discards_only_its_own_batch()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "workspace"));
        var sibling = workspace.CreateBatch(EditorArtifactKind.Imports);
        var siblingFile = Path.Combine(sibling, "imported.png");
        File.WriteAllText(siblingFile, "an unrelated import");
        var outsider = Path.Combine(_scratch, "outside-the-workspace.txt");
        File.WriteAllText(outsider, "must survive");

        var clock = new FakeRecordingClock();
        var session = NewSession(workspace, clock, new RecordingSettings(), out var batchPath);
        session.Record();
        session.Tick();

        await session.DisposeAsync();

        Assert.False(Directory.Exists(batchPath));
        Assert.True(Directory.Exists(workspace.RootPath));
        Assert.True(File.Exists(siblingFile));
        Assert.True(File.Exists(outsider));
    }

    [Fact]
    public async Task Frames_handed_to_the_editor_survive_closing_the_recorder()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "workspace"));
        var clock = new FakeRecordingClock();
        var session = NewSession(workspace, clock, new RecordingSettings());

        session.Record();
        session.Tick();
        var frames = await session.StopAsync();
        Assert.Single(frames);

        await session.DisposeAsync();

        Assert.All(frames, frame => Assert.True(File.Exists(frame.FilePath),
            $"'{frame.FilePath}' belongs to the editor now and must not be deleted."));
    }

    [Fact]
    public void Pruning_treats_recordings_like_every_other_artifact_kind()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "workspace"));
        var batch = workspace.CreateBatch(EditorArtifactKind.Recordings);
        var referenced = Path.Combine(batch, "000000.png");
        var unreferenced = Path.Combine(batch, "000001.png");
        File.WriteAllText(referenced, "still on the timeline");
        File.WriteAllText(unreferenced, "undone and unreachable");

        workspace.PruneUnreachable([referenced]);

        Assert.True(File.Exists(referenced));
        Assert.False(File.Exists(unreferenced));
    }

    [Fact]
    public void An_abandoned_recording_workspace_is_scavenged_once_its_owner_is_gone()
    {
        string abandonedRoot;
        string recordedFrame;

        using (var workspace = EditorWorkspace.Create())
        {
            abandonedRoot = workspace.RootPath;
            recordedFrame = Path.Combine(workspace.CreateBatch(EditorArtifactKind.Recordings), "000000.png");
            File.WriteAllText(recordedFrame, "a frame from a recorder that never stopped");

            // Claim the workspace for a process that has long since exited.
            File.WriteAllText(Path.Combine(abandonedRoot, ".owner"), "1|1");
        }

        Directory.CreateDirectory(abandonedRoot);
        File.WriteAllText(Path.Combine(abandonedRoot, ".owner"), "1|1");
        Directory.CreateDirectory(Path.GetDirectoryName(recordedFrame)!);
        File.WriteAllText(recordedFrame, "a frame from a recorder that never stopped");

        using var live = EditorWorkspace.Create();
        var liveFrame = Path.Combine(live.CreateBatch(EditorArtifactKind.Recordings), "000000.png");
        File.WriteAllText(liveFrame, "a recording this process still owns");

        // A recording nobody handed over is the only copy there is, so the sweep leaves it for the
        // retention period the user set rather than taking it the moment the app next starts.
        ProjectArchive.ScavengeStaleWorkspaces();
        Assert.True(File.Exists(recordedFrame), "A recording abandoned moments ago must survive its retention period.");

        Directory.SetLastWriteTimeUtc(abandonedRoot, DateTime.UtcNow.AddDays(-30));
        ProjectArchive.ScavengeStaleWorkspaces();

        Assert.False(Directory.Exists(abandonedRoot), "An abandoned recording workspace should be scavenged once it is past retention.");
        Assert.True(File.Exists(liveFrame), "A workspace this process still owns must be left alone.");
    }

    [Fact]
    public void A_workspace_whose_owner_claim_cannot_be_read_yet_is_not_scavenged()
    {
        // Another instance writes its claim just after creating the directory, so a scavenger can
        // read it half-written. Reading that as "nobody owns this" would delete a live recording.
        var root = Path.Combine(Path.GetTempPath(), "screentogif-linux", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var recordedFrame = Path.Combine(root, "recordings", "batch", "000000.png");
        Directory.CreateDirectory(Path.GetDirectoryName(recordedFrame)!);
        File.WriteAllText(recordedFrame, "a frame of a recording that is still being set up");
        File.WriteAllText(Path.Combine(root, ".owner"), "4213");

        try
        {
            ProjectArchive.ScavengeStaleWorkspaces();

            Assert.True(File.Exists(recordedFrame),
                "A workspace with an unreadable owner claim must keep the retention grace a claimless one has.");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    public async Task A_workspace_root_that_cannot_be_written_surfaces_a_cause_rather_than_crashing()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "workspace"));
        var batchPath = workspace.CreateBatch(EditorArtifactKind.Recordings);
        var writer = new PngFrameWriter(batchPath);
        var clock = new FakeRecordingClock();
        var session = new RecordingSession(new FakeScreenSource(new PixelSize(Region.Width, Region.Height)),
            writer, clock, new RecordingSettings(), () => Region);

        var errors = new List<string>();
        session.Error += (_, args) => errors.Add(args.Message);

        session.Record();
        session.Tick();

        // Encoding runs off the capture loop, so the first frame has to land before the directory
        // is closed to writing; the point of the test is that frames already on disk are kept.
        Assert.True(SpinWait.SpinUntil(() => Directory.GetFiles(batchPath).Length == 1, TimeSpan.FromSeconds(10)),
            "The first frame was never encoded.");

        // A read-only batch directory is the shape a full or permission-denied workspace takes.
        File.SetUnixFileMode(batchPath, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            clock.Advance(100);
            session.Tick();

            Assert.True(SpinWait.SpinUntil(() => writer.Failure is not null, TimeSpan.FromSeconds(10)),
                "The writer never reported the failed write.");

            clock.Advance(100);
            session.Tick();

            Assert.Equal(RecordingStage.Paused, session.Stage);
            var message = Assert.Single(errors);
            Assert.DoesNotContain("Exception", message, StringComparison.Ordinal);
            Assert.Contains("not writable", message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(batchPath, message, StringComparison.Ordinal);

            // The frames encoded before the batch went unwritable are the recording the user is
            // trying to save, so stopping hands them over rather than taking the failure as a loss.
            var frames = await session.StopAsync();
            Assert.NotEmpty(frames);
            Assert.All(frames, frame => Assert.True(File.Exists(frame.FilePath),
                $"'{frame.FilePath}' was encoded before the failure and must still be handed over."));
        }
        finally
        {
            File.SetUnixFileMode(batchPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        await session.DisposeAsync();
    }

    [Fact]
    public async Task Closing_the_recorder_deletes_its_own_workspace_and_nothing_beside_it()
    {
        // The recorder's own teardown disposes the whole workspace, not just the batch, so the
        // bound this asserts is the one the Discard and close-with-discard buttons actually reach.
        var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "recording-workspace"));
        var neighbour = EditorWorkspace.Create(Path.Combine(_scratch, "another-workspace"));
        var neighbourFile = Path.Combine(neighbour.CreateBatch(EditorArtifactKind.Frames), "keep-me.png");
        File.WriteAllText(neighbourFile, "another project's frames");
        var outsider = Path.Combine(_scratch, "outside-every-workspace.txt");
        File.WriteAllText(outsider, "must survive");

        var clock = new FakeRecordingClock();
        var session = NewSession(workspace, clock, new RecordingSettings(), out var batchPath);
        session.Record();
        session.Tick();
        clock.Advance(100);
        session.Tick();

        var recordingRoot = workspace.RootPath;

        // Exactly what RecorderWindow.TearDownRecordingAsync does, for a Discard and for a close.
        session.Discard();
        await session.DisposeAsync();
        workspace.Dispose();

        Assert.False(Directory.Exists(batchPath));
        Assert.False(Directory.Exists(recordingRoot), "The recorder owns its workspace and takes it with it.");
        Assert.True(File.Exists(neighbourFile), "Another workspace must survive the recorder's teardown.");
        Assert.True(File.Exists(outsider), "Nothing outside the workspace may be touched.");

        neighbour.Dispose();
    }

    /// <summary>
    /// The pending-byte gauge is what the session's encoder-backlog pause reads, and nothing
    /// re-derives it: an add without its matching subtract pauses a recording that is not behind at
    /// all, and a subtract too many stops the pause ever firing. Each of the three sites that move
    /// the counter is exercised here, and each ends at zero.
    /// </summary>
    [Fact]
    public async Task Every_frame_a_writer_takes_leaves_its_pending_bytes_behind()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_scratch, "workspace"));

        var completed = new PngFrameWriter(workspace.CreateBatch(EditorArtifactKind.Recordings));
        await using (completed)
        {
            foreach (var _ in Enumerable.Range(0, 3))
                completed.Write(NewFrame());

            Assert.Equal(3, (await completed.CompleteAsync()).Count);
            Assert.Equal(0, completed.PendingBytes);
        }

        // Discard drains whatever the encode loop never reached, which is the second of the two
        // subtract paths.
        var discarded = new PngFrameWriter(workspace.CreateBatch(EditorArtifactKind.Recordings));
        await using (discarded)
        {
            foreach (var _ in Enumerable.Range(0, 3))
                discarded.Write(NewFrame());

            discarded.Discard();
            Assert.Equal(0, discarded.PendingBytes);

            // A frame handed to a discarded writer is dropped rather than queued, so it must not
            // add bytes that nothing will ever take away.
            discarded.Write(NewFrame());
            Assert.Equal(0, discarded.PendingBytes);
        }
    }

    private static CapturedFrame NewFrame()
    {
        var stride = Region.Width * 4;
        var pixels = System.Buffers.ArrayPool<byte>.Shared.Rent(stride * Region.Height);
        return new CapturedFrame(new PixelSize(Region.Width, Region.Height), stride, pixels);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_scratch))
                Directory.Delete(_scratch, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Test scratch space is best effort.
        }
    }

    private RecordingSession NewSession(EditorWorkspace workspace, IRecordingClock clock, RecordingSettings settings) =>
        NewSession(workspace, clock, settings, out _);

    private RecordingSession NewSession(EditorWorkspace workspace, IRecordingClock clock, RecordingSettings settings,
        out string batchPath)
    {
        batchPath = workspace.CreateBatch(EditorArtifactKind.Recordings);
        return new RecordingSession(
            new FakeScreenSource(new PixelSize(Region.Width, Region.Height)),
            new PngFrameWriter(batchPath),
            clock,
            settings,
            () => Region);
    }
}
