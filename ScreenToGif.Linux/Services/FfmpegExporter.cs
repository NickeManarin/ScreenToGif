using ScreenToGif.Linux.Models;
using System.Globalization;
using System.Text;

namespace ScreenToGif.Linux.Services;

public sealed class FfmpegExporter
{
    private readonly FfmpegTool _ffmpeg;

    public FfmpegExporter(FfmpegTool ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public async Task ExportAsync(IEnumerable<EditorFrame> sourceFrames, string outputPath, CancellationToken cancellationToken = default)
    {
        var frames = sourceFrames.ToArray();

        if (frames.Length == 0)
            throw new InvalidOperationException("There are no frames to export.");

        foreach (var frame in frames)
        {
            if (!File.Exists(frame.FilePath))
                throw new FileNotFoundException("A frame file is missing.", frame.FilePath);
        }

        var listPath = Path.Combine(Path.GetTempPath(), $"screentogif-linux-{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(listPath, BuildConcatList(frames), cancellationToken);

            var arguments = new List<string>
            {
                "-y", "-hide_banner", "-loglevel", "error",
                "-f", "concat",
                "-safe", "0",
                "-i", listPath,
                "-fps_mode", "vfr",
                "-an"
            };

            AddCodecArguments(arguments, outputPath);
            arguments.Add(outputPath);

            await _ffmpeg.RunFfmpegCheckedAsync(arguments, cancellationToken);
        }
        finally
        {
            try
            {
                File.Delete(listPath);
            }
            catch (IOException)
            {
                // The export result is more important than cleanup of this temporary list.
            }
        }
    }

    private static string BuildConcatList(IEnumerable<EditorFrame> frames)
    {
        var builder = new StringBuilder();
        var frameList = frames.ToArray();

        foreach (var frame in frameList)
        {
            builder.Append("file ").AppendLine(QuoteConcatPath(frame.FilePath));
            builder.Append("duration ").AppendLine((Math.Max(1, frame.DelayMs) / 1000d).ToString("0.######", CultureInfo.InvariantCulture));
        }

        // The concat demuxer uses the final file as the final duration marker.
        builder.Append("file ").AppendLine(QuoteConcatPath(frameList[^1].FilePath));
        return builder.ToString();
    }

    private static string QuoteConcatPath(string path) =>
        $"'{Path.GetFullPath(path).Replace("'", "'\\''", StringComparison.Ordinal)}'";

    private static void AddCodecArguments(ICollection<string> arguments, string outputPath)
    {
        switch (Path.GetExtension(outputPath).ToLowerInvariant())
        {
            case ".gif":
                arguments.Add("-loop");
                arguments.Add("0");
                break;
            case ".mp4":
                arguments.Add("-c:v");
                arguments.Add("libx264");
                arguments.Add("-pix_fmt");
                arguments.Add("yuv420p");
                arguments.Add("-movflags");
                arguments.Add("+faststart");
                break;
            case ".webm":
                arguments.Add("-c:v");
                arguments.Add("libvpx-vp9");
                arguments.Add("-pix_fmt");
                arguments.Add("yuv420p");
                break;
            case ".apng":
                arguments.Add("-plays");
                arguments.Add("0");
                break;
            default:
                throw new NotSupportedException("The Linux editor currently exports GIF, APNG, MP4, and WebM.");
        }
    }
}
