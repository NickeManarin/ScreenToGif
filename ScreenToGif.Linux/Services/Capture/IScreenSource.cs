using Avalonia;
using System.Buffers;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>Raised when a screen source cannot produce a frame. Its message is shown to the user verbatim.</summary>
public sealed class ScreenCaptureException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>
/// One captured region, in <see cref="Avalonia.Platform.PixelFormat.Bgra8888"/> with opaque alpha.
/// The pixel buffer is rented from <see cref="ArrayPool{T}"/>, so every frame must be disposed
/// once encoded; <see cref="Pixels"/> is longer than <see cref="Stride"/> times height.
/// </summary>
public sealed class CapturedFrame : IDisposable
{
    private byte[]? _pixels;

    public CapturedFrame(PixelSize size, int stride, byte[] pooledPixels)
    {
        if (size.Width <= 0 || size.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "A captured frame must have a positive size.");
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, size.Width * 4);
        ArgumentNullException.ThrowIfNull(pooledPixels);
        if (pooledPixels.Length < (long)stride * size.Height)
            throw new ArgumentException("The pixel buffer is shorter than the frame it describes.", nameof(pooledPixels));

        Size = size;
        Stride = stride;
        _pixels = pooledPixels;
    }

    public PixelSize Size { get; }

    public int Stride { get; }

    public ReadOnlySpan<byte> Pixels => Buffer;

    /// <summary>The same bytes as <see cref="Pixels"/>, writable, for the source that fills the frame.</summary>
    internal Span<byte> Buffer =>
        (_pixels ?? throw new ObjectDisposedException(nameof(CapturedFrame))).AsSpan(0, Stride * Size.Height);

    public void Dispose()
    {
        var pixels = _pixels;
        if (pixels is null)
            return;

        _pixels = null;
        ArrayPool<byte>.Shared.Return(pixels);
    }
}

/// <summary>Grabs rectangles of the desktop. Implementations are single-threaded.</summary>
public interface IScreenSource : IDisposable
{
    /// <summary>
    /// Grabs <paramref name="region"/> in physical desktop pixels, compositing the pointer when
    /// <paramref name="includeCursor"/> is set. Throws <see cref="ScreenCaptureException"/> when the
    /// region cannot be read.
    /// </summary>
    CapturedFrame Capture(PixelRect region, bool includeCursor);
}
