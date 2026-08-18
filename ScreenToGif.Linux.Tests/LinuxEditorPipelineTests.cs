using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class LinuxEditorPipelineTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"screentogif-linux-tests-{Guid.NewGuid():N}");
    private readonly FfmpegTool _ffmpeg = new();

    public LinuxEditorPipelineTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task ImportsAnimatedGifAsMultipleFramesAndKeepsSeparateBatches()
    {
        var source = await CreateAnimatedGifAsync();
        var workspace = Path.Combine(_root, "workspace");
        var importer = new MediaImporter(_ffmpeg);

        var firstBatch = await importer.ImportAsync([source], workspace);
        var secondBatch = await importer.ImportAsync([source], workspace);

        Assert.True(firstBatch.Count > 1, "Animated GIF input should become an editable frame sequence.");
        Assert.Equal(firstBatch.Count, secondBatch.Count);
        Assert.All(firstBatch.Concat(secondBatch), frame => Assert.True(File.Exists(frame.FilePath)));
        Assert.Equal(firstBatch.Count + secondBatch.Count, firstBatch.Concat(secondBatch).Select(frame => frame.FilePath).Distinct().Count());
        Assert.All(firstBatch, frame => Assert.InRange(frame.DelayMs, 1, 1000));

        DisposeFrames(firstBatch.Concat(secondBatch));
    }

    [Fact]
    public async Task ProjectArchiveRoundTripsFrameOrderAndDelays()
    {
        var source = await CreateAnimatedGifAsync();
        var workspace = Path.Combine(_root, "workspace");
        var frames = (await new MediaImporter(_ffmpeg).ImportAsync([source], workspace)).Take(3).ToArray();
        frames[0].DelayMs = 37;
        frames[1].DelayMs = 211;
        frames[2].DelayMs = 503;

        var archivePath = Path.Combine(_root, "round-trip.stg-linux");
        await ProjectArchive.SaveAsync(archivePath, frames);
        var loaded = await ProjectArchive.LoadAsync(archivePath);

        Assert.Equal(3, loaded.Frames.Count);
        Assert.Equal([37, 211, 503], loaded.Frames.Select(frame => frame.DelayMs));
        Assert.All(loaded.Frames, frame => Assert.True(File.Exists(frame.FilePath)));
        Assert.All(loaded.Frames, frame => Assert.Equal(".png", Path.GetExtension(frame.FilePath)));

        DisposeFrames(frames);
        DisposeFrames(loaded.Frames);
        ProjectArchive.TryDeleteWorkspace(loaded.WorkspacePath);
    }

    [Fact]
    public async Task ExportsEditedFramesToGifMp4AndWebm()
    {
        var source = await CreateAnimatedGifAsync();
        var workspace = Path.Combine(_root, "workspace");
        var frames = (await new MediaImporter(_ffmpeg).ImportAsync([source], workspace)).Take(3).ToArray();
        frames[0].DelayMs = 40;
        frames[1].DelayMs = 160;
        frames[2].DelayMs = 80;

        var exporter = new FfmpegExporter(_ffmpeg);

        foreach (var extension in new[] { "gif", "apng", "mp4", "webm" })
        {
            var output = Path.Combine(_root, $"export.{extension}");
            await exporter.ExportAsync(frames, output);

            Assert.True(new FileInfo(output).Length > 0, $"Expected a non-empty {extension} export.");
            var probe = await _ffmpeg.RunFfprobeCheckedAsync(
            [
                "-v", "error",
                "-show_entries", "format=format_name",
                "-of", "default=noprint_wrappers=1:nokey=1",
                output
            ]);
            Assert.False(string.IsNullOrWhiteSpace(probe.StandardOutput));
        }

        DisposeFrames(frames);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private async Task<string> CreateAnimatedGifAsync()
    {
        var source = Path.Combine(_root, "input.gif");

        await _ffmpeg.RunFfmpegCheckedAsync(
        [
            "-y", "-hide_banner", "-loglevel", "error",
            "-f", "lavfi",
            "-i", "testsrc2=size=32x24:rate=5:duration=1",
            "-frames:v", "5",
            source
        ]);

        return source;
    }

    private static void DisposeFrames(IEnumerable<EditorFrame> frames)
    {
        foreach (var frame in frames)
            frame.Dispose();
    }
}
