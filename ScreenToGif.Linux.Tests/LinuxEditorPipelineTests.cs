using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using System.Buffers.Binary;
using System.Text.Json;
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
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "workspace"));
        var importer = new MediaImporter(_ffmpeg);

        var firstBatch = await importer.ImportAsync([source], workspace);
        var secondBatch = await importer.ImportAsync([source], workspace);

        Assert.Equal(5, firstBatch.Count);
        Assert.Equal(firstBatch.Count, secondBatch.Count);
        Assert.All(firstBatch.Concat(secondBatch), frame => Assert.True(File.Exists(frame.FilePath)));
        Assert.Equal(firstBatch.Count + secondBatch.Count, firstBatch.Concat(secondBatch).Select(frame => frame.FilePath).Distinct().Count());
        Assert.All(firstBatch, frame => Assert.Equal(200, frame.DelayMs));
        var sourceDigests = await FrameDigestsAsync(source);
        Assert.Equal(sourceDigests, await FrameDigestsAsync(firstBatch.Select(frame => frame.FilePath)));
        Assert.Equal(sourceDigests, await FrameDigestsAsync(secondBatch.Select(frame => frame.FilePath)));

        DisposeFrames(firstBatch.Concat(secondBatch));
    }

    [Fact]
    public async Task Imports_variable_frame_durations_without_flattening_them_to_stream_rate()
    {
        var source = await CreateVariableGifAsync();
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "variable-workspace"));

        var frames = await new MediaImporter(_ffmpeg).ImportAsync([source], workspace);

        Assert.Equal([40, 160, 80, 40], frames.Select(frame => frame.DelayMs));
    }

    [Fact]
    public async Task Imports_vfr_timestamp_deltas_when_packet_durations_are_constant()
    {
        var source = await CreateVariableVideoAsync();
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "vfr-workspace"));
        var probe = await _ffmpeg.RunFfprobeCheckedAsync(
            ["-v", "error", "-select_streams", "v:0", "-show_entries", "frame=best_effort_timestamp_time,duration_time", "-of", "json", source]);
        using var document = JsonDocument.Parse(probe.StandardOutput);
        var timing = document.RootElement.GetProperty("frames").EnumerateArray().ToArray();
        Assert.Equal(["0.000000", "0.040000", "0.200000", "0.280000"],
            timing.Select(frame => frame.GetProperty("best_effort_timestamp_time").GetString()));
        Assert.All(timing, frame => Assert.Equal("0.040000", frame.GetProperty("duration_time").GetString()));

        var frames = await new MediaImporter(_ffmpeg).ImportAsync([source], workspace);

        Assert.Equal([40, 160, 80, 40], frames.Select(frame => frame.DelayMs));
    }

    [Fact]
    public async Task Importer_interface_exposes_probe_fallback_missing_output_and_cleanup_paths()
    {
        var source = FileOf("fake.gif", "media");
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "fake-import"));
        var successful = new FakeImportTool("5/1\n", outputCount: 3);
        var frames = await new MediaImporter(successful).ImportAsync([source], workspace);
        Assert.Equal(3, frames.Count);
        Assert.All(frames, frame => Assert.Equal(200, frame.DelayMs));
        Assert.Equal(2, successful.ProbeCalls);

        using var fallbackWorkspace = EditorWorkspace.Create(Path.Combine(_root, "fallback-import"));
        var fallback = await new MediaImporter(new FakeImportTool("malformed\n", outputCount: 1))
            .ImportAsync([source], fallbackWorkspace);
        Assert.Equal(100, fallback.Single().DelayMs);

        using var missingWorkspace = EditorWorkspace.Create(Path.Combine(_root, "missing-import"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaImporter(new FakeImportTool("5/1\n", outputCount: 0)).ImportAsync([source], missingWorkspace));
        Assert.Contains("did not produce frames", error.Message);
        Assert.False(Directory.Exists(Path.Combine(missingWorkspace.RootPath, "imports")) &&
                     Directory.EnumerateFiles(Path.Combine(missingWorkspace.RootPath, "imports"), "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Import_rejects_an_unbounded_frame_workload_before_generation()
    {
        var source = FileOf("too-long.gif", "media");
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "oversized-import"));
        var ffmpeg = new FakeImportTool("30/1", outputCount: 0, declaredFrameCount: 10_001);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaImporter(ffmpeg).ImportAsync([source], workspace));

        Assert.Contains("10000-frame project limit", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ffmpeg.GenerationCalls);
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "imports")) &&
                     Directory.EnumerateFiles(Path.Combine(workspace.RootPath, "imports"), "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Import_rejects_frames_that_exceed_the_remaining_project_capacity_before_generation()
    {
        var source = FileOf("two-more.gif", "media");
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "remaining-capacity-import"));
        var ffmpeg = new FakeImportTool("30/1", outputCount: 2, declaredFrameCount: 2);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaImporter(ffmpeg).ImportAsync([source], workspace, existingFrameCount: 9_999));

        Assert.Contains("10000-frame project limit", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ffmpeg.GenerationCalls);
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "imports")) &&
                     Directory.EnumerateFiles(Path.Combine(workspace.RootPath, "imports"), "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Import_preflights_the_complete_multi_source_batch_before_generating_frames()
    {
        var first = FileOf("first.png", "media-one");
        var second = FileOf("second.png", "media-two");
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "multi-source-capacity-import"));
        var ffmpeg = new FakeImportTool("30/1", outputCount: 1);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaImporter(ffmpeg).ImportAsync([first, second], workspace, existingFrameCount: 9_999));

        Assert.Contains("10000-frame project limit", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, ffmpeg.ProbeCalls);
        Assert.Equal(0, ffmpeg.GenerationCalls);
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "imports")) &&
                     Directory.EnumerateFiles(Path.Combine(workspace.RootPath, "imports"), "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Empty_import_is_artifact_free_and_observes_precancellation()
    {
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "empty-import"));
        var importer = new MediaImporter(new FakeImportTool("30/1", outputCount: 0));

        Assert.Empty(await importer.ImportAsync([], workspace));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            importer.ImportAsync([], workspace, cancellation.Token));

        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "imports")));
    }

    [Fact]
    public async Task Import_reconciles_actual_generated_pixels_when_probe_count_is_too_small()
    {
        var source = FileOf("underreported.gif", "media");
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "actual-pixel-import"));
        var ffmpeg = new FakeImportTool(
            "30/1", outputCount: 16, declaredFrameCount: 1, width: 8_192, height: 8_192);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaImporter(ffmpeg).ImportAsync([source], workspace));

        Assert.Contains("decoded-pixel limit", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ffmpeg.GenerationCalls);
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "imports")) &&
                     Directory.EnumerateFiles(Path.Combine(workspace.RootPath, "imports"), "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Canceled_import_after_an_earlier_source_removes_the_partial_batch()
    {
        var first = FileOf("first.gif", "media-one");
        var second = FileOf("second.gif", "media-two");
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "canceled-import"));
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new MediaImporter(new CancelingImportTool(cancellation))
                .ImportAsync([first, second], workspace, cancellation.Token));

        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "imports")) &&
                     Directory.EnumerateFiles(Path.Combine(workspace.RootPath, "imports"), "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task ProjectArchiveRoundTripsFrameOrderAndDelays()
    {
        var source = await CreateAnimatedGifAsync();
        using var workspace = EditorWorkspace.Create(Path.Combine(_root, "roundtrip-workspace"));
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
        loaded.Dispose();
    }

    [Fact]
    public async Task ExportsEditedFramesToGifMp4AndWebm()
    {
        var frames = new[]
        {
            new EditorFrame(await CreateColorFrameAsync("red", "export-red.png"), 40),
            new EditorFrame(await CreateColorFrameAsync("white", "export-white.png"), 160),
            new EditorFrame(await CreateColorFrameAsync("blue", "export-blue.png"), 80)
        };
        var expectedLumas = await AverageLumasAsync(frames.Select(frame => frame.FilePath));

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
            // Each frame starts where the delays before it end, and the last frame lasts its own
            // delay. An animated image holds exactly the project's frames; a video ends on a copy
            // of the last frame, since that copy is what gives the last frame its duration.
            var isVideo = extension is "mp4" or "webm";
            var packets = await ReadPacketTimingAsync(output);
            Assert.Equal(frames.Length + (isVideo ? 1 : 0), packets.Count);
            var expectedTimestamps = new[] { 0d, 0.04d, 0.20d };
            for (var index = 0; index < expectedTimestamps.Length; index++)
                Assert.InRange(Math.Abs(packets[index].Pts - expectedTimestamps[index]), 0, 0.005);
            var lastFrameDuration = isVideo ? packets[3].Pts - packets[2].Pts : packets[2].Duration;
            Assert.InRange(Math.Abs(lastFrameDuration - 0.08d), 0, 0.005);
            var actualLumas = await AverageLumasAsync(output);
            Assert.Equal(packets.Count, actualLumas.Count);
            for (var index = 0; index < expectedLumas.Count; index++)
                Assert.InRange(Math.Abs(expectedLumas[index] - actualLumas[index]), 0, 3);
            if (isVideo)
                Assert.InRange(Math.Abs(expectedLumas[2] - actualLumas[3]), 0, 3);
        }

        DisposeFrames(frames);
    }

    [Fact]
    public async Task A_last_frame_held_past_the_gif_limit_exports_at_the_limit()
    {
        var frames = new[]
        {
            new EditorFrame(await CreateColorFrameAsync("red", "held-red.png"), 40),
            new EditorFrame(await CreateColorFrameAsync("blue", "held-blue.png"), 700_000)
        };
        var output = Path.Combine(_root, "held.gif");

        await new FfmpegExporter(_ffmpeg).ExportAsync(frames, output);

        var packets = await ReadPacketTimingAsync(output);
        Assert.Equal(2, packets.Count);
        Assert.InRange(Math.Abs(packets[1].Duration - 655.35d), 0, 0.005);
        DisposeFrames(frames);
    }

    [Fact]
    public async Task Canceled_export_preserves_existing_destination_and_removes_staging_file()
    {
        var source = FileOf("source.png", "source");
        var output = FileOf("existing.gif", "original");
        var exporter = new FfmpegExporter(new CancelingExportTool());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            exporter.ExportAsync([new EditorFrame(source, 100)], output));

        Assert.Equal("original", await File.ReadAllTextAsync(output));
        Assert.Empty(Directory.EnumerateFiles(_root, ".existing.*.tmp.gif"));
    }

    [Fact]
    public async Task Export_normalizes_mixed_and_odd_frames_to_an_explicit_codec_safe_canvas()
    {
        var frames = new[]
        {
            new EditorFrame(await CreateColorFrameAsync("red", "mixed-red.png", "31x23"), 40),
            new EditorFrame(await CreateColorFrameAsync("white", "mixed-white.png", "40x20"), 40)
        };
        var exporter = new FfmpegExporter(_ffmpeg);

        foreach (var extension in new[] { "gif", "apng", "mp4", "webm" })
        {
            var output = Path.Combine(_root, $"mixed.{extension}");
            await exporter.ExportAsync(frames, output);
            Assert.Equal(extension is "mp4" or "webm" ? (40, 24) : (40, 23), await ReadSizeAsync(output));
        }
    }

    [Fact]
    public async Task Gif_export_rejects_delays_below_its_ten_millisecond_contract()
    {
        var source = await CreateColorFrameAsync("red", "too-fast.png");
        var output = Path.Combine(_root, "too-fast.gif");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FfmpegExporter(_ffmpeg).ExportAsync([new EditorFrame(source, 9)], output));

        Assert.Contains("at least 10 ms", error.Message);
        Assert.False(File.Exists(output));
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

    private async Task<string> CreateVariableGifAsync()
    {
        var red = await CreateColorFrameAsync("red", "variable-red.png");
        var white = await CreateColorFrameAsync("white", "variable-white.png");
        var blue = await CreateColorFrameAsync("blue", "variable-blue.png");
        var list = Path.Combine(_root, "variable.txt");
        await File.WriteAllTextAsync(list,
            $"file '{red}'\nduration 0.04\nfile '{white}'\nduration 0.16\nfile '{blue}'\nduration 0.08\nfile '{blue}'\n");
        var output = Path.Combine(_root, "variable.gif");
        await _ffmpeg.RunFfmpegCheckedAsync(
            ["-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", list, "-fps_mode", "vfr", output]);
        return output;
    }

    private async Task<string> CreateVariableVideoAsync()
    {
        var red = await CreateColorFrameAsync("red", "vfr-red.png");
        var white = await CreateColorFrameAsync("white", "vfr-white.png");
        var blue = await CreateColorFrameAsync("blue", "vfr-blue.png");
        var list = Path.Combine(_root, "vfr.txt");
        await File.WriteAllTextAsync(list,
            $"file '{red}'\nduration 0.04\nfile '{white}'\nduration 0.16\nfile '{blue}'\nduration 0.08\nfile '{blue}'\n");
        var output = Path.Combine(_root, "variable.mkv");
        await _ffmpeg.RunFfmpegCheckedAsync(
            ["-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", list, "-fps_mode", "vfr", "-c:v", "ffv1", output]);
        return output;
    }

    private async Task<string> CreateColorFrameAsync(string color, string name, string size = "32x24")
    {
        var path = Path.Combine(_root, name);
        await _ffmpeg.RunFfmpegCheckedAsync(
            ["-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", $"color={color}:size={size},format=rgba", "-frames:v", "1", path]);
        return path;
    }

    private async Task<(int Width, int Height)> ReadSizeAsync(string path)
    {
        var result = await _ffmpeg.RunFfprobeCheckedAsync(
            ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "csv=s=x:p=0", path]);
        var parts = result.StandardOutput.Trim().Split('x').Select(int.Parse).ToArray();
        return (parts[0], parts[1]);
    }

    private string FileOf(string name, string content)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        return path;
    }

    private async Task<string[]> FrameDigestsAsync(string path)
    {
        var result = await _ffmpeg.RunFfmpegAsync(
            ["-hide_banner", "-loglevel", "error", "-i", path, "-pix_fmt", "rgba", "-f", "framemd5", "-"]);
        Assert.Equal(0, result.ExitCode);
        return result.StandardOutput.Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Select(line => line[(line.LastIndexOf(',') + 1)..].Trim())
            .ToArray();
    }

    private async Task<string[]> FrameDigestsAsync(IEnumerable<string> paths)
    {
        var digests = new List<string>();
        foreach (var path in paths)
            digests.AddRange(await FrameDigestsAsync(path));
        return digests.ToArray();
    }

    private async Task<IReadOnlyList<(double Pts, double Duration)>> ReadPacketTimingAsync(string path)
    {
        var result = await _ffmpeg.RunFfprobeCheckedAsync(
        [
            "-v", "error", "-select_streams", "v:0", "-show_packets",
            "-show_entries", "packet=pts_time,duration_time", "-of", "json", path
        ]);
        using var document = JsonDocument.Parse(result.StandardOutput);
        return document.RootElement.GetProperty("packets").EnumerateArray()
            .Select(packet => (
                ParseTime(packet.GetProperty("pts_time")),
                ParseTime(packet.GetProperty("duration_time"))))
            .ToArray();

        static double ParseTime(JsonElement element) =>
            double.Parse(element.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyList<double>> AverageLumasAsync(string path)
    {
        var result = await _ffmpeg.RunFfmpegAsync(
            ["-hide_banner", "-loglevel", "error", "-i", path, "-vf", "signalstats,metadata=print:file=-", "-f", "null", "-"]);
        Assert.Equal(0, result.ExitCode);
        return result.StandardOutput.Split('\n')
            .Where(line => line.StartsWith("lavfi.signalstats.YAVG=", StringComparison.Ordinal))
            .Select(line => double.Parse(line[(line.IndexOf('=') + 1)..], System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
    }

    private async Task<IReadOnlyList<double>> AverageLumasAsync(IEnumerable<string> paths)
    {
        var values = new List<double>();
        foreach (var path in paths)
            values.AddRange(await AverageLumasAsync(path));
        return values;
    }

    private sealed class CancelingExportTool : IFfmpegTool
    {
        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));

        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));

        public async Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(arguments.Last(), "partial", CancellationToken.None);
            throw new OperationCanceledException();
        }

        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessResult(0, "{\"streams\":[{\"width\":2,\"height\":2,\"pix_fmt\":\"rgba\"}]}", string.Empty));
    }

    private sealed class FakeImportTool(
        string probeOutput,
        int outputCount,
        int? declaredFrameCount = null,
        int width = 32,
        int height = 24) : IFfmpegTool
    {
        public int ProbeCalls { get; private set; }
        public int GenerationCalls { get; private set; }

        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            ProbeCalls++;
            var output = arguments.Contains("frame=best_effort_timestamp_time,pts_time,duration_time,pkt_duration_time")
                ? "{\"frames\":[]}"
                : JsonSerializer.Serialize(new
                {
                    streams = new[]
                    {
                        new
                        {
                            width,
                            height,
                            nb_read_frames = (declaredFrameCount ?? Math.Max(1, outputCount)).ToString(),
                            avg_frame_rate = probeOutput.Trim(),
                            r_frame_rate = probeOutput.Trim()
                        }
                    }
                });
            return Task.FromResult(new ProcessResult(0, output, string.Empty));
        }

        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            GenerationCalls++;
            var output = arguments.Last();
            for (var index = 1; index <= outputCount; index++)
                WritePngHeader(
                    output.Replace("%06d", index.ToString("000000"), StringComparison.Ordinal),
                    width,
                    height);
            return Task.CompletedTask;
        }

        private static void WritePngHeader(string path, int width, int height)
        {
            var header = new byte[24];
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(header, 0);
            "IHDR"u8.CopyTo(header.AsSpan(12, 4));
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16, 4), width);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(20, 4), height);
            File.WriteAllBytes(path, header);
        }
    }

    private sealed class CancelingImportTool(CancellationTokenSource cancellation) : IFfmpegTool
    {
        private int _generationCalls;

        public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProcessResult(0,
                arguments.Contains("frame=best_effort_timestamp_time,pts_time,duration_time,pkt_duration_time")
                    ? "{\"frames\":[]}"
                    : "{\"streams\":[{\"width\":32,\"height\":24,\"nb_read_frames\":\"1\",\"avg_frame_rate\":\"5/1\"}]}",
                string.Empty));
        public Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            var output = arguments.Last();
            var path = output.Replace("%06d", "000001", StringComparison.Ordinal);
            var header = new byte[24];
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(header, 0);
            "IHDR"u8.CopyTo(header.AsSpan(12, 4));
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16, 4), 32);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(20, 4), 24);
            File.WriteAllBytes(path, header);
            if (_generationCalls++ == 0)
                return Task.CompletedTask;
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private static void DisposeFrames(IEnumerable<EditorFrame> frames)
    {
        foreach (var frame in frames)
            frame.Dispose();
    }
}
