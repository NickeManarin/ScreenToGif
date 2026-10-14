using ScreenToGif.Linux.Models;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace ScreenToGif.Linux.Services;

public sealed record EditorProjectContent(EditorWorkspace Workspace, IReadOnlyList<EditorFrame> Frames);

public sealed class LoadedProject(EditorWorkspace workspace, IReadOnlyList<EditorFrame> frames) : IDisposable
{
    private bool _transferred;

    public EditorWorkspace Workspace { get; } = workspace;
    public IReadOnlyList<EditorFrame> Frames { get; } = frames;

    public EditorProjectContent TransferOwnership()
    {
        ObjectDisposedException.ThrowIf(_transferred, this);
        _transferred = true;
        return new EditorProjectContent(Workspace, Frames);
    }

    public void Dispose()
    {
        if (_transferred)
            return;
        _transferred = true;
        foreach (var frame in Frames)
            frame.Dispose();
        Workspace.Dispose();
    }
}

public static class ProjectArchive
{
    private const string WorkspaceRootName = "screentogif-linux";
    private const string OwnerFileName = ".owner";
    private const int CurrentSchemaVersion = 1;
    private const int MaximumArchiveEntries = EditorResourceLimits.MaximumProjectFrames + 2;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task SaveAsync(string archivePath, IEnumerable<EditorFrame> sourceFrames, CancellationToken cancellationToken = default)
    {
        var frames = sourceFrames.ToArray();

        if (frames.Length == 0)
            throw new InvalidOperationException("There are no frames to save.");
        await ValidateFramesForSaveAsync(frames, cancellationToken);

        var fullPath = Path.GetFullPath(archivePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await SaveCoreAsync(temporaryPath, frames, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static async Task SaveCoreAsync(string archivePath, IReadOnlyList<EditorFrame> frames, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var archiveFrames = frames.Select((frame, index) => (
            Frame: frame,
            Path: $"frames/{index:000000}.png")).ToArray();

        var manifest = new LinuxProjectManifest
        {
            Frames = archiveFrames.Select(entry => (LinuxProjectFrame?)new LinuxProjectFrame
            {
                Path = entry.Path,
                DelayMs = entry.Frame.DelayMs
            }).ToList()
        };

        var manifestEntry = archive.CreateEntry("project.json", CompressionLevel.Fastest);
        await using (var manifestStream = manifestEntry.Open())
            await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonOptions, cancellationToken);

        foreach (var entry in archiveFrames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var archiveEntry = archive.CreateEntry(entry.Path, CompressionLevel.Fastest);
            await using var source = new FileStream(
                entry.Frame.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var destination = archiveEntry.Open();
            await source.CopyToAsync(destination, 81920, cancellationToken);
        }
    }

    private static async Task ValidateFramesForSaveAsync(
        IReadOnlyCollection<EditorFrame> frames,
        CancellationToken cancellationToken)
    {
        if (frames.Count > EditorResourceLimits.MaximumProjectFrames)
            throw new InvalidOperationException($"A project cannot contain more than {EditorResourceLimits.MaximumProjectFrames} frames.");

        await EditorProjectBudget.EnsureTimelineBudgetAsync(
            frames.Select(frame => frame.FilePath), "The project", cancellationToken);
    }

    public static async Task<LoadedProject> LoadAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Project archive was not found.", archivePath);

        var workspace = CreateWorkspace();

        try
        {
            await ExtractAsync(archivePath, workspace.RootPath, cancellationToken);

            var manifestPath = Path.Combine(workspace.RootPath, "project.json");
            if (!File.Exists(manifestPath))
                throw new InvalidDataException("The project archive is missing project.json.");
            LinuxProjectManifest manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<LinuxProjectManifest>(
                    await File.ReadAllTextAsync(manifestPath, cancellationToken), JsonOptions)
                    ?? throw new InvalidDataException("The project manifest is empty.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("The project manifest is not valid JSON.", ex);
            }

            if (manifest.Frames is null)
                throw new InvalidDataException("The project manifest does not contain a frame list.");
            if (manifest.Version != CurrentSchemaVersion)
                throw new InvalidDataException(
                    $"Project version {manifest.Version} is not supported; this editor supports version {CurrentSchemaVersion}.");
            if (manifest.Frames.Count > EditorResourceLimits.MaximumProjectFrames)
                throw new InvalidDataException($"The project contains more than {EditorResourceLimits.MaximumProjectFrames} frames.");

            var frames = manifest.Frames.Select(frame =>
            {
                if (frame is null || string.IsNullOrWhiteSpace(frame.Path))
                    throw new InvalidDataException("The project contains an invalid frame entry.");
                if (frame.DelayMs <= 0)
                    throw new InvalidDataException("The project contains a frame with an invalid delay.");
                var path = SafePath(workspace.RootPath, frame.Path);

                if (!File.Exists(path))
                    throw new InvalidDataException($"The project is missing frame '{frame.Path}'.");

                return new EditorFrame(path, frame.DelayMs);
            }).ToArray();

            if (frames.Length == 0)
                throw new InvalidDataException("The project contains no frames.");

            try
            {
                await Task.Run(
                    () => EditorProjectBudget.EnsureDecodedPixelBudget(
                        frames.Select(frame => frame.FilePath), "The project", cancellationToken),
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }

            return new LoadedProject(workspace, frames);
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    public static EditorWorkspace CreateWorkspace() => EditorWorkspace.Create();

    public static void ScavengeStaleWorkspaces(int retentionDays = 1)
    {
        try
        {
            var root = Path.Combine(Path.GetTempPath(), WorkspaceRootName);
            if (!Directory.Exists(root))
                return;
            foreach (var path in Directory.EnumerateDirectories(root))
            {
                var ownership = ReadOwnership(path);
                if (ownership == WorkspaceOwnership.Live)
                    continue;

                // Only a claim that names a process known to be gone justifies deleting on sight.
                // A claim that cannot be read yet is the common shape of a workspace being created
                // right now by another instance, so it keeps the retention grace a missing claim has.
                if (ownership == WorkspaceOwnership.Unknown && retentionDays > 0 &&
                    Directory.GetLastWriteTimeUtc(path) > DateTime.UtcNow.AddDays(-retentionDays))
                    continue;

                // A recording is the only artifact here the user cannot recreate from a file they
                // still hold, so a recorder that died before it could hand its frames over leaves
                // the one copy. It gets the same grace, whoever owned it, rather than being swept
                // away before the person who made it has had a chance to come back for it.
                if (retentionDays > 0 && HoldsRecordedFrames(path) &&
                    Directory.GetLastWriteTimeUtc(path) > DateTime.UtcNow.AddDays(-retentionDays))
                    continue;

                TryDeleteWorkspace(path);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static bool HoldsRecordedFrames(string workspacePath)
    {
        try
        {
            var recordings = Path.Combine(workspacePath, EditorWorkspace.FolderName(EditorArtifactKind.Recordings));
            return Directory.Exists(recordings) &&
                   Directory.EnumerateFiles(recordings, "*", SearchOption.AllDirectories).Any();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Unreadable is not the same as empty, so keep the workspace rather than risk the loss.
            return true;
        }
    }

    private enum WorkspaceOwnership
    {
        /// <summary>The claim names a process that is still running; the workspace is in use.</summary>
        Live,

        /// <summary>The claim names a process that has exited; the workspace was abandoned.</summary>
        Dead,

        /// <summary>There is no readable claim, so nothing is known about who owns this.</summary>
        Unknown
    }

    private static WorkspaceOwnership ReadOwnership(string workspacePath)
    {
        var ownerPath = Path.Combine(workspacePath, OwnerFileName);
        string contents;
        try
        {
            contents = File.ReadAllText(ownerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return WorkspaceOwnership.Unknown;
        }

        var parts = contents.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var processId) || !long.TryParse(parts[1], out var startTicks))
            return WorkspaceOwnership.Unknown;

        try
        {
            using var process = Process.GetProcessById(processId);
            return process.StartTime.ToUniversalTime().Ticks == startTicks && !process.HasExited
                ? WorkspaceOwnership.Live
                : WorkspaceOwnership.Dead;
        }
        catch (ArgumentException)
        {
            // No process carries that id any more.
            return WorkspaceOwnership.Dead;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return WorkspaceOwnership.Unknown;
        }
    }

    internal static void TryDeleteWorkspace(string? workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
            return;

        try
        {
            Directory.Delete(workspacePath, recursive: true);
        }
        catch (IOException)
        {
            // Temporary workspaces are best-effort cleanup only.
        }
        catch (UnauthorizedAccessException)
        {
            // Temporary workspaces are best-effort cleanup only.
        }
    }

    private static async Task ExtractAsync(string archivePath, string workspacePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        if (archive.Entries.Count > MaximumArchiveEntries)
            throw new InvalidDataException($"The project contains too many archive entries; the limit is {MaximumArchiveEntries}.");

        var destinations = new HashSet<string>(StringComparer.Ordinal);
        long declaredBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var path = ValidateArchiveEntry(entry, workspacePath);
            if (!destinations.Add(path))
                throw new InvalidDataException($"The project contains duplicate archive path '{entry.FullName}'.");
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                continue;
            var entryLimit = IsManifestPath(path, workspacePath)
                ? EditorResourceLimits.MaximumProjectManifestBytes
                : EditorResourceLimits.MaximumProjectFrameBytes;
            if (entry.Length > entryLimit)
                throw new InvalidDataException($"The project archive entry '{entry.FullName}' is too large.");
            if (entry.Length > EditorResourceLimits.MaximumProjectArchiveBytes - declaredBytes)
                throw new InvalidDataException("The project archive expands beyond the supported size limit.");
            declaredBytes += entry.Length;
        }

        long extractedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = ValidateArchiveEntry(entry, workspacePath);

            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(path);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var input = entry.Open();
            await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            var entryLimit = IsManifestPath(path, workspacePath)
                ? EditorResourceLimits.MaximumProjectManifestBytes
                : EditorResourceLimits.MaximumProjectFrameBytes;
            extractedBytes += await CopyBoundedAsync(
                input, output, entryLimit,
                EditorResourceLimits.MaximumProjectArchiveBytes - extractedBytes, cancellationToken);
        }
    }

    private static string ValidateArchiveEntry(ZipArchiveEntry entry, string workspacePath)
    {
        var path = SafePath(workspacePath, entry.FullName);
        if (string.Equals(path, Path.Combine(workspacePath, OwnerFileName), StringComparison.Ordinal))
            throw new InvalidDataException("The project contains a reserved workspace path.");

        var relativePath = Path.GetRelativePath(workspacePath, path).Replace(Path.DirectorySeparatorChar, '/');
        var isDirectory = entry.FullName.EndsWith("/", StringComparison.Ordinal);
        var isManifest = string.Equals(relativePath, "project.json", StringComparison.Ordinal);
        var isFramesPath = string.Equals(relativePath, "frames", StringComparison.Ordinal) ||
                           relativePath.StartsWith("frames/", StringComparison.Ordinal);
        var isFrame = !isDirectory && isFramesPath &&
                      string.Equals(Path.GetExtension(relativePath), ".png", StringComparison.OrdinalIgnoreCase);
        if (!(isManifest || isFrame || isDirectory && isFramesPath))
            throw new InvalidDataException($"The project contains unsupported archive path '{entry.FullName}'.");
        return path;
    }

    private static bool IsManifestPath(string path, string workspacePath) =>
        string.Equals(path, Path.Combine(workspacePath, "project.json"), StringComparison.Ordinal);

    private static async Task<long> CopyBoundedAsync(
        Stream input,
        Stream output,
        long entryLimit,
        long aggregateRemaining,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long written = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                return written;
            if (written + read > entryLimit || written + read > aggregateRemaining)
                throw new InvalidDataException("The project archive expands beyond the supported size limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            written += read;
        }
    }

    private static string SafePath(string workspacePath, string relativePath)
    {
        var root = Path.GetFullPath(workspacePath) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(workspacePath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!path.StartsWith(root, StringComparison.Ordinal))
            throw new InvalidDataException("The project contains an unsafe path.");

        return path;
    }

    private sealed class LinuxProjectManifest
    {
        public int Version { get; set; } = CurrentSchemaVersion;
        public List<LinuxProjectFrame?>? Frames { get; set; } = [];
    }

    private sealed class LinuxProjectFrame
    {
        public string? Path { get; set; }
        public int DelayMs { get; set; } = 100;
    }
}
