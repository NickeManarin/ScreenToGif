using ScreenToGif.Linux.Models;
using System.IO.Compression;
using System.Text.Json;

namespace ScreenToGif.Linux.Services;

public sealed record LoadedProject(string WorkspacePath, IReadOnlyList<EditorFrame> Frames);

public static class ProjectArchive
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task SaveAsync(string archivePath, IEnumerable<EditorFrame> sourceFrames, CancellationToken cancellationToken = default)
    {
        var frames = sourceFrames.ToArray();

        if (frames.Length == 0)
            throw new InvalidOperationException("There are no frames to save.");

        await using var stream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        var manifest = new LinuxProjectManifest
        {
            Frames = frames.Select((frame, index) => new LinuxProjectFrame
            {
                Path = $"frames/{index:000000}.png",
                DelayMs = frame.DelayMs
            }).ToList()
        };

        var manifestEntry = archive.CreateEntry("project.json", CompressionLevel.Fastest);
        await using (var manifestStream = manifestEntry.Open())
            await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonOptions, cancellationToken);

        foreach (var (frame, manifestFrame) in frames.Zip(manifest.Frames))
        {
            cancellationToken.ThrowIfCancellationRequested();
            archive.CreateEntryFromFile(frame.FilePath, manifestFrame.Path, CompressionLevel.Fastest);
        }
    }

    public static async Task<LoadedProject> LoadAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("Project archive was not found.", archivePath);

        var workspacePath = CreateWorkspace();

        try
        {
            await ExtractAsync(archivePath, workspacePath, cancellationToken);

            var manifestPath = Path.Combine(workspacePath, "project.json");
            var manifest = JsonSerializer.Deserialize<LinuxProjectManifest>(await File.ReadAllTextAsync(manifestPath, cancellationToken), JsonOptions)
                ?? throw new InvalidDataException("The project manifest is empty.");

            var frames = manifest.Frames.Select(frame =>
            {
                var path = SafePath(workspacePath, frame.Path);

                if (!File.Exists(path))
                    throw new InvalidDataException($"The project is missing frame '{frame.Path}'.");

                return new EditorFrame(path, frame.DelayMs);
            }).ToArray();

            if (frames.Length == 0)
                throw new InvalidDataException("The project contains no frames.");

            return new LoadedProject(workspacePath, frames);
        }
        catch
        {
            TryDeleteWorkspace(workspacePath);
            throw;
        }
    }

    public static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "screentogif-linux", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static void TryDeleteWorkspace(string? workspacePath)
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

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = SafePath(workspacePath, entry.FullName);

            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(path);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var input = entry.Open();
            await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await input.CopyToAsync(output, cancellationToken);
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
        public int Version { get; set; } = 1;
        public List<LinuxProjectFrame> Frames { get; set; } = [];
    }

    private sealed class LinuxProjectFrame
    {
        public string Path { get; set; } = string.Empty;
        public int DelayMs { get; set; } = 100;
    }
}
