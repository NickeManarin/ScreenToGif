using Avalonia;
using System.Buffers;
using System.Runtime.InteropServices;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// Grabs desktop rectangles in-process through libX11, compositing the pointer through libXfixes.
/// Per ADR 20260921: <c>XGetImage</c> keeps the Windows timing contract at a per-frame cost that
/// leaves headroom at 15 fps, where a per-frame FFmpeg subprocess does not.
/// </summary>
/// <param name="displayName">
/// The X display to grab, or null for the one <c>DISPLAY</c> names. Tests pass it explicitly
/// because setting the variable from managed code does not reach the C library's environment.
/// </param>
public sealed class X11ScreenSource(string? displayName = null) : IScreenSource
{
    private const int ZPixmap = 2;
    private const int LsbFirst = 0;

    private IntPtr _display;
    private bool _disposed;
    private bool? _cursorAvailable;

    /// <summary>
    /// True when this session can be captured at all. A Wayland session reports a platform handle
    /// descriptor other than <c>XID</c>, and has no X11 view of the desktop to grab.
    /// </summary>
    public static bool IsSupportedSession(string? platformHandleDescriptor) =>
        OperatingSystem.IsLinux() && string.Equals(platformHandleDescriptor, "XID", StringComparison.Ordinal);

    public CapturedFrame Capture(PixelRect region, bool includeCursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (region.Width <= 0 || region.Height <= 0)
            throw new ScreenCaptureException(
                $"The capture region is empty ({region.Width}x{region.Height}); resize the recorder frame and try again.");

        var display = EnsureDisplay();
        var imagePointer = XGetImage(display, XDefaultRootWindow(display), region.X, region.Y,
            (uint)region.Width, (uint)region.Height, AllPlanes, ZPixmap);

        if (imagePointer == IntPtr.Zero)
            throw new ScreenCaptureException(
                "The X server refused to read the screen region. It may lie partly outside the desktop.");

        try
        {
            var image = Marshal.PtrToStructure<XImage>(imagePointer);
            EnsureSupportedFormat(image);

            var frame = CopyToBgra(image, region);
            if (includeCursor)
                CompositeCursor(display, frame, region);
            return frame;
        }
        finally
        {
            DestroyImage(imagePointer);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_display == IntPtr.Zero)
            return;

        XCloseDisplay(_display);
        _display = IntPtr.Zero;
    }

    private IntPtr EnsureDisplay()
    {
        if (_display != IntPtr.Zero)
            return _display;

        var name = IntPtr.Zero;
        try
        {
            if (displayName is not null)
                name = Marshal.StringToHGlobalAnsi(displayName);
            _display = XOpenDisplay(name);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            throw new ScreenCaptureException(
                "The X11 client library could not be loaded, so the screen cannot be recorded.", ex);
        }
        finally
        {
            if (name != IntPtr.Zero)
                Marshal.FreeHGlobal(name);
        }

        if (_display == IntPtr.Zero)
            throw new ScreenCaptureException(
                "No X display could be opened. Recording needs an X11 session; check the DISPLAY variable.");

        return _display;
    }

    private static void EnsureSupportedFormat(XImage image)
    {
        // ASM-2: the server is expected to serve 24- or 32-bit ZPixmap in BGRX byte order.
        var supported = image.BitsPerPixel == 32 &&
                        image.Depth is 24 or 32 &&
                        image.ByteOrder == LsbFirst &&
                        image.RedMask == 0x00FF0000 &&
                        image.GreenMask == 0x0000FF00 &&
                        image.BlueMask == 0x000000FF;

        if (!supported)
            throw new ScreenCaptureException(
                $"This X server serves an unsupported image format (depth {image.Depth}, {image.BitsPerPixel} bits per pixel). " +
                "Recording needs a 24- or 32-bit true-colour display.");
    }

    private static CapturedFrame CopyToBgra(XImage image, PixelRect region)
    {
        var stride = region.Width * 4;
        var buffer = ArrayPool<byte>.Shared.Rent(stride * region.Height);
        var frame = new CapturedFrame(new PixelSize(region.Width, region.Height), stride, buffer);

        try
        {
            for (var y = 0; y < region.Height; y++)
            {
                var source = image.Data + y * image.BytesPerLine;
                Marshal.Copy(source, buffer, y * stride, stride);

                // Depth 24 leaves the fourth byte undefined; the editor's PNGs are opaque.
                for (var x = 3; x < stride; x += 4)
                    buffer[y * stride + x] = 0xFF;
            }
        }
        catch
        {
            frame.Dispose();
            throw;
        }

        return frame;
    }

    private void CompositeCursor(IntPtr display, CapturedFrame frame, PixelRect region)
    {
        _cursorAvailable ??= TryQueryCursorExtension(display);
        if (_cursorAvailable != true)
            return;

        var cursorPointer = XFixesGetCursorImage(display);
        if (cursorPointer == IntPtr.Zero)
            return;

        try
        {
            var cursor = Marshal.PtrToStructure<XFixesCursorImage>(cursorPointer);
            if (cursor.Width == 0 || cursor.Height == 0 || cursor.Pixels == IntPtr.Zero)
                return;

            BlendCursor(cursor, frame, region);
        }
        finally
        {
            XFree(cursorPointer);
        }
    }

    private static void BlendCursor(XFixesCursorImage cursor, CapturedFrame frame, PixelRect region)
    {
        var count = cursor.Width * cursor.Height;
        var pixels = ArrayPool<uint>.Shared.Rent(count);
        try
        {
            // Each cursor pixel occupies one C `unsigned long`, premultiplied ARGB in the low 32
            // bits. Unpacking here is what lets the blending below be plain arithmetic over a span.
            for (var i = 0; i < count; i++)
                pixels[i] = (uint)Marshal.ReadIntPtr(cursor.Pixels, i * IntPtr.Size).ToInt64();

            BlendCursor(
                pixels.AsSpan(0, count), cursor.Width, cursor.Height,
                cursor.X - cursor.XHot - region.X, cursor.Y - cursor.YHot - region.Y,
                frame);
        }
        finally
        {
            ArrayPool<uint>.Shared.Return(pixels);
        }
    }

    /// <summary>
    /// Draws a premultiplied-ARGB cursor image over <paramref name="frame"/> at
    /// (<paramref name="originX"/>, <paramref name="originY"/>), which is the cursor's top-left
    /// corner in the frame's own coordinates, hotspot already subtracted. Whatever falls outside
    /// the frame is clipped. Internal so the clipping and blending can be tested without an X
    /// server, since they are what fail at the edges and the pointer usually is near one.
    /// </summary>
    internal static void BlendCursor(
        ReadOnlySpan<uint> cursorPixels, int cursorWidth, int cursorHeight,
        int originX, int originY, CapturedFrame frame)
    {
        var destination = frame.Buffer;

        for (var y = 0; y < cursorHeight; y++)
        {
            var targetY = originY + y;
            if (targetY < 0 || targetY >= frame.Size.Height)
                continue;

            for (var x = 0; x < cursorWidth; x++)
            {
                var targetX = originX + x;
                if (targetX < 0 || targetX >= frame.Size.Width)
                    continue;

                var packed = cursorPixels[y * cursorWidth + x];
                var alpha = (byte)(packed >> 24);
                if (alpha == 0)
                    continue;

                var offset = targetY * frame.Stride + targetX * 4;
                var inverse = 255 - alpha;
                destination[offset + 0] = OverPremultiplied((byte)packed, destination[offset + 0], inverse);
                destination[offset + 1] = OverPremultiplied((byte)(packed >> 8), destination[offset + 1], inverse);
                destination[offset + 2] = OverPremultiplied((byte)(packed >> 16), destination[offset + 2], inverse);
                destination[offset + 3] = 0xFF;
            }
        }
    }

    private static byte OverPremultiplied(byte source, byte destination, int inverseAlpha) =>
        (byte)Math.Min(255, source + (destination * inverseAlpha + 127) / 255);

    private static bool TryQueryCursorExtension(IntPtr display)
    {
        try
        {
            return XFixesQueryExtension(display, out _, out _) != 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return false;
        }
    }

    private static unsafe void DestroyImage(IntPtr imagePointer)
    {
        // XDestroyImage is a C macro over the image's own function table, not an exported symbol.
        var destroy = (delegate* unmanaged[Cdecl]<IntPtr, int>)Marshal.ReadIntPtr(imagePointer, DestroyImageOffset);
        if (destroy is not null)
            destroy(imagePointer);
    }

    private static readonly nuint AllPlanes = unchecked((nuint)(nint)(-1));

    private static readonly int DestroyImageOffset =
        (int)Marshal.OffsetOf<XImage>(nameof(XImage.DestroyImage));

    [StructLayout(LayoutKind.Sequential)]
    private struct XImage
    {
        public int Width;
        public int Height;
        public int XOffset;
        public int Format;
        public IntPtr Data;
        public int ByteOrder;
        public int BitmapUnit;
        public int BitmapBitOrder;
        public int BitmapPad;
        public int Depth;
        public int BytesPerLine;
        public int BitsPerPixel;
        public nuint RedMask;
        public nuint GreenMask;
        public nuint BlueMask;
        public IntPtr ObData;
        public IntPtr CreateImage;
        public IntPtr DestroyImage;
        public IntPtr GetPixel;
        public IntPtr PutPixel;
        public IntPtr SubImage;
        public IntPtr AddPixel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XFixesCursorImage
    {
        public short X;
        public short Y;
        public ushort Width;
        public ushort Height;
        public ushort XHot;
        public ushort YHot;
        public nuint CursorSerial;
        public IntPtr Pixels;
        public nuint Atom;
        public IntPtr Name;
    }

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern nuint XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr XGetImage(IntPtr display, nuint drawable, int x, int y, uint width, uint height,
        nuint planeMask, int format);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XFree(IntPtr data);

    [DllImport("libXfixes.so.3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XFixesQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [DllImport("libXfixes.so.3", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr XFixesGetCursorImage(IntPtr display);
}
