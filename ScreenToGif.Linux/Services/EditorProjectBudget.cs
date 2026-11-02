using System.Buffers.Binary;

namespace ScreenToGif.Linux.Services;

public readonly record struct PngDimensions(int Width, int Height);

public static class EditorProjectBudget
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static PngDimensions ReadPngDimensions(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            stream.ReadExactly(header);
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException($"Frame '{Path.GetFileName(path)}' is not a valid PNG image.", ex);
        }

        if (!header[..8].SequenceEqual(PngSignature) || !header[12..16].SequenceEqual("IHDR"u8))
            throw new InvalidDataException($"Frame '{Path.GetFileName(path)}' is not a valid PNG image.");

        var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        if (width is < 1 or > EditorResourceLimits.MaximumMediaDimension ||
            height is < 1 or > EditorResourceLimits.MaximumMediaDimension)
            throw new InvalidDataException(
                $"Frame '{Path.GetFileName(path)}' dimensions must be between 1 and {EditorResourceLimits.MaximumMediaDimension} pixels.");
        return new PngDimensions(width, height);
    }

    public static void EnsureDecodedPixelBudget(
        IEnumerable<string> paths,
        string operation,
        CancellationToken cancellationToken = default)
    {
        var totalPixels = 0L;
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var size = ReadPngDimensions(path);
            var pixels = (long)size.Width * size.Height;
            if (pixels > EditorResourceLimits.MaximumImportedPixels - totalPixels)
                throw new InvalidOperationException(
                    $"{operation} exceeds the editor's decoded-pixel limit; reduce frame dimensions or frame count.");
            totalPixels += pixels;
        }
    }

    public static void EnsureStorageBudget(
        IEnumerable<string> paths,
        string operation,
        CancellationToken cancellationToken = default)
    {
        var totalBytes = 0L;
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var length = new FileInfo(path).Length;
            if (length > EditorResourceLimits.MaximumProjectFrameBytes)
                throw new InvalidOperationException(
                    $"{operation} contains a frame too large to save; reduce its dimensions or complexity.");
            if (length > EditorResourceLimits.MaximumProjectArchiveBytes -
                EditorResourceLimits.MaximumProjectManifestBytes - totalBytes)
                throw new InvalidOperationException(
                    $"{operation} would exceed the project storage limit; remove frames or reduce their dimensions.");
            totalBytes += length;
        }
    }

    public static void EnsureTimelineBudget(
        IEnumerable<string> paths,
        string operation,
        CancellationToken cancellationToken = default)
    {
        var materializedPaths = paths.ToArray();
        EnsureStorageBudget(materializedPaths, operation, cancellationToken);
        EnsureDecodedPixelBudget(materializedPaths, operation, cancellationToken);
    }

    public static Task EnsureTimelineBudgetAsync(
        IEnumerable<string> paths,
        string operation,
        CancellationToken cancellationToken = default)
    {
        var materializedPaths = paths.ToArray();
        return Task.Run(
            () => EnsureTimelineBudget(materializedPaths, operation, cancellationToken),
            cancellationToken);
    }
}
