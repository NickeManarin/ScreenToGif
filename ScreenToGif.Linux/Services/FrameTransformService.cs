using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

public enum FrameTransformKind
{
    Resize,
    Crop,
    FlipHorizontal,
    FlipVertical,
    RotateClockwise,
    RotateCounterClockwise,
    Border,
    Shadow
}

public sealed record FrameTransformRequest(
    FrameTransformKind Kind,
    int Width = 0,
    int Height = 0,
    int X = 0,
    int Y = 0);

public sealed record TransformedFrame(EditorFrame Source, string FilePath);

public sealed class FrameTransformService(IFfmpegTool ffmpeg)
{
    public const int MaximumDimension = 8192;
    public const long MaximumOutputPixels = 16_777_216;

    public async Task<IReadOnlyList<TransformedFrame>> TransformAsync(
        IReadOnlyList<EditorFrame> frames,
        EditorWorkspace workspace,
        FrameTransformRequest request,
        CancellationToken cancellationToken = default)
    {
        if (frames.Count == 0)
            throw new InvalidOperationException("Select at least one frame first.");

        Validate(frames, request);
        var operationPath = workspace.CreateBatch(EditorArtifactKind.Edits);
        var transformed = new TransformedFrame[frames.Count];

        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, frames.Count),
                new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
                async (index, token) =>
            {
                var output = Path.Combine(operationPath, $"{index:000000}.png");
                await ffmpeg.RunFfmpegCheckedAsync(
                [
                    "-y", "-hide_banner", "-loglevel", "error",
                    "-i", frames[index].FilePath,
                    "-vf", Filter(request),
                    "-frames:v", "1",
                    output
                ], token);

                if (!File.Exists(output))
                    throw new IOException("FFmpeg did not produce the transformed frame.");

                transformed[index] = new TransformedFrame(frames[index], output);
            });

            return transformed;
        }
        catch
        {
            TryDelete(operationPath);
            throw;
        }
    }

    private static void Validate(IReadOnlyList<EditorFrame> frames, FrameTransformRequest request)
    {
        if (request.Kind is FrameTransformKind.Resize or FrameTransformKind.Crop &&
            (request.Width <= 0 || request.Height <= 0))
            throw new ArgumentOutOfRangeException(nameof(request), "Width and height must be positive whole numbers.");
        if (request.Kind is FrameTransformKind.Resize or FrameTransformKind.Crop)
            ValidateOutputSize(request.Width, request.Height);

        if (request.Kind == FrameTransformKind.Crop && (request.X < 0 || request.Y < 0))
            throw new ArgumentOutOfRangeException(nameof(request), "Crop coordinates cannot be negative.");
        if (request.Kind == FrameTransformKind.Border && request.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Border width must be positive.");
        if (request.Kind == FrameTransformKind.Border && request.Width > MaximumDimension / 2)
            throw new ArgumentOutOfRangeException(nameof(request), $"Border width must be at most {MaximumDimension / 2} pixels.");

        if (request.Kind is FrameTransformKind.Border or FrameTransformKind.Shadow)
        {
            var padding = request.Kind == FrameTransformKind.Border ? (long)request.Width * 2 : 16;
            foreach (var frame in frames)
            {
                var size = ReadPngSize(frame.FilePath);
                ValidateOutputSize(size.Width + padding, size.Height + padding);
            }
        }
    }

    private static void ValidateOutputSize(long width, long height)
    {
        if (width is < 1 or > MaximumDimension || height is < 1 or > MaximumDimension ||
            width * height > MaximumOutputPixels)
            throw new ArgumentOutOfRangeException(
                "request",
                $"Transformed frames must be at most {MaximumDimension} pixels per side and {MaximumOutputPixels:N0} pixels total.");
    }

    private static (int Width, int Height) ReadPngSize(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        stream.ReadExactly(header);
        if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            throw new InvalidDataException($"Frame '{Path.GetFileName(path)}' is not a PNG image.");
        return (
            System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[16..20]),
            System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[20..24]));
    }

    private static string Filter(FrameTransformRequest request) => request.Kind switch
    {
        FrameTransformKind.Resize => $"scale={request.Width}:{request.Height}:flags=lanczos",
        FrameTransformKind.Crop => $"crop={request.Width}:{request.Height}:{request.X}:{request.Y}",
        FrameTransformKind.FlipHorizontal => "hflip",
        FrameTransformKind.FlipVertical => "vflip",
        FrameTransformKind.RotateClockwise => "transpose=clock",
        FrameTransformKind.RotateCounterClockwise => "transpose=cclock",
        FrameTransformKind.Border => $"pad=iw+{request.Width * 2}:ih+{request.Width * 2}:{request.Width}:{request.Width}:color=black",
        FrameTransformKind.Shadow => "pad=iw+16:ih+16:0:0:color=#555555",
        _ => throw new ArgumentOutOfRangeException(nameof(request))
    };

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
