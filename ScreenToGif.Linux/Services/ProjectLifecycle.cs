using ScreenToGif.Linux.Models;
using System.Text.Json;

namespace ScreenToGif.Linux.Services;

public static class ProjectWorkspaceLifecycle
{
    public static void Replace(
        EditorWorkspace currentWorkspace,
        LoadedProject candidate,
        Action<EditorProjectContent> adopt)
    {
        var replacement = candidate.TransferOwnership();
        try
        {
            adopt(replacement);
        }
        catch
        {
            foreach (var frame in replacement.Frames)
                frame.Dispose();
            replacement.Workspace.Dispose();
            throw;
        }

        currentWorkspace.Dispose();
    }
}

public sealed class BlankProjectFactory(IFfmpegTool ffmpeg)
{
    private const int MaximumDimension = 8192;
    private const int MaximumFrameCount = 1000;
    private const long MaximumTotalPixels = 250_000_000;

    public async Task<LoadedProject> CreateAsync(int width, int height, int frameCount, int delayMs, CancellationToken cancellationToken = default)
    {
        if (width is < 1 or > MaximumDimension || height is < 1 or > MaximumDimension)
            throw new ArgumentOutOfRangeException(nameof(width), $"Blank project dimensions must be between 1 and {MaximumDimension} pixels.");
        if (frameCount is < 1 or > MaximumFrameCount)
            throw new ArgumentOutOfRangeException(nameof(frameCount), $"Blank project frame count must be between 1 and {MaximumFrameCount}.");
        if ((long)width * height * frameCount > MaximumTotalPixels)
            throw new ArgumentOutOfRangeException(nameof(frameCount), "Blank project size is too large; reduce its dimensions or frame count.");
        if (delayMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(delayMs), "Frame delay must be positive.");

        var workspace = ProjectArchive.CreateWorkspace();
        var framesPath = workspace.CreateBatch(EditorArtifactKind.Frames);

        try
        {
            var first = Path.Combine(framesPath, "000000.png");
            await ffmpeg.RunFfmpegCheckedAsync(
            [
                "-y", "-hide_banner", "-loglevel", "error",
                "-f", "lavfi", "-i", $"color=black:size={width}x{height}:rate=1,format=rgba",
                "-frames:v", "1", first
            ], cancellationToken);
            if (!File.Exists(first))
                throw new IOException("FFmpeg did not create the blank frame.");

            for (var index = 1; index < frameCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Copy(first, Path.Combine(framesPath, $"{index:000000}.png"));
            }

            var frames = Enumerable.Range(0, frameCount)
                .Select(index => new EditorFrame(Path.Combine(framesPath, $"{index:000000}.png"), delayMs))
                .ToArray();
            return new LoadedProject(workspace, frames);
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }
}

public sealed class RecentProjectStore
{
    private readonly string _path;
    private readonly int _capacity;

    public RecentProjectStore(string? path = null, int capacity = 12)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenToGif", "Linux", "recent-projects.json");
        _capacity = Math.Max(1, capacity);
    }

    public async Task AddAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var current = (await ReadRawAsync(cancellationToken))
            .Where(path => !string.Equals(path, fullPath, StringComparison.Ordinal))
            .Prepend(fullPath)
            .Take(_capacity)
            .ToArray();
        await WriteAsync(current, cancellationToken);
    }

    public async Task<bool> TryAddAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAsync(projectPath, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetExistingAsync(CancellationToken cancellationToken = default)
    {
        var raw = await ReadRawAsync(cancellationToken);
        var existing = raw.Where(File.Exists).Distinct(StringComparer.Ordinal).Take(_capacity).ToArray();
        if (!raw.SequenceEqual(existing))
            await WriteAsync(existing, cancellationToken);
        return existing;
    }

    private async Task<string[]> ReadRawAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(_path, cancellationToken)) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task WriteAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(paths), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
