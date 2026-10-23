using Avalonia;
using Avalonia.Media.Imaging;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using Xunit;
using System.Runtime.InteropServices;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Records a real X server with the real capture path and reads the resulting PNGs back, so the
/// interop, the pixel format, and the file layout are proven rather than assumed.
/// </summary>
public sealed class X11RecordingIntegrationTests
{
    private const byte SolidRed = 0x3C;
    private const byte SolidGreen = 0x7A;
    private const byte SolidBlue = 0xCF;

    private static readonly PixelRect Region = new(40, 30, 64, 48);

    [Fact]
    public async Task A_real_recording_is_pixel_exact_and_correctly_sized()
    {
        using var server = XvfbSession.Start(320, 240);
        using var source = new X11ScreenSource(server.Display);

        // Open the X connection before xsetroot runs. Xvfb resets the server when its last client
        // disconnects, which would discard the root colour the moment xsetroot exits.
        source.Capture(Region, includeCursor: false).Dispose();
        server.Run("xsetroot", $"-solid #{SolidRed:X2}{SolidGreen:X2}{SolidBlue:X2}");

        using var workspace = EditorWorkspace.Create();
        var batchPath = workspace.CreateBatch(EditorArtifactKind.Recordings);
        var writer = new PngFrameWriter(batchPath);
        var clock = new FakeRecordingClock();
        var session = new RecordingSession(source, writer, clock,
            new RecordingSettings { FramesPerSecond = 10, ShowCursor = false }, () => Region);

        await using (session)
        {
            session.Record();
            session.Tick();
            for (var frame = 1; frame < 4; frame++)
            {
                clock.Advance(100);
                session.Tick();
            }

            var frames = await session.StopAsync();

            Assert.Equal(4, frames.Count);
            foreach (var frame in frames)
            {
                Assert.StartsWith(batchPath + Path.DirectorySeparatorChar, frame.FilePath, StringComparison.Ordinal);
                Assert.True(File.Exists(frame.FilePath), $"'{frame.FilePath}' was not written.");
                AssertSolidColour(frame.FilePath);
            }
        }
    }

    [Fact]
    public void A_real_X11_cursor_appears_only_when_requested()
    {
        using var server = XvfbSession.Start(320, 240);
        using var window = new CursorWindow(server.Display);
        using var source = new X11ScreenSource(server.Display);
        source.Capture(Region, false).Dispose();
        server.Run("xdotool", "mousemove --sync 80 60");
        using var hidden = source.Capture(Region, false);
        using var shown = source.Capture(Region, true);
        Assert.False(hidden.Pixels.SequenceEqual(shown.Pixels), "The real XFixes cursor must change captured pixels.");
        for (var offset = 0; offset < hidden.Pixels.Length; offset += 4)
            Assert.Equal(new byte[] { 0, 0, 255, 255 }, hidden.Pixels.Slice(offset, 4).ToArray());
    }

    [Fact]
    public void An_unreachable_display_reports_a_cause_rather_than_crashing()
    {
        using var source = new X11ScreenSource(":8123");

        var failure = Assert.Throws<ScreenCaptureException>(() => source.Capture(Region, includeCursor: false));

        Assert.Contains("X display", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_empty_region_is_refused_before_the_server_is_asked()
    {
        using var source = new X11ScreenSource(":8123");

        var failure = Assert.Throws<ScreenCaptureException>(
            () => source.Capture(new PixelRect(0, 0, 0, 0), includeCursor: false));

        Assert.Contains("empty", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("XID", "x11", true)]
    [InlineData("XID", null, true)]
    [InlineData("XID", "wayland", false)]
    [InlineData("XID", "Wayland", false)]
    [InlineData("Wayland", "wayland", false)]
    [InlineData(null, "x11", false)]
    public void Only_an_x11_session_reports_itself_capturable(string? handleDescriptor, string? sessionType, bool expected) =>
        Assert.Equal(expected, X11ScreenSource.IsSupportedSession(handleDescriptor, sessionType));

    private static void AssertSolidColour(string pngPath)
    {
        AvaloniaImaging.EnsureRenderInterface();

        using var bitmap = new Bitmap(pngPath);
        Assert.Equal(new PixelSize(Region.Width, Region.Height), bitmap.PixelSize);

        var stride = bitmap.PixelSize.Width * 4;
        var pixels = new byte[stride * bitmap.PixelSize.Height];
        unsafe
        {
            fixed (byte* buffer = pixels)
                bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), (IntPtr)buffer, pixels.Length, stride);
        }

        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            var pixel = (Blue: pixels[offset], Green: pixels[offset + 1], Red: pixels[offset + 2],
                Alpha: pixels[offset + 3]);
            Assert.Equal((SolidBlue, SolidGreen, SolidRed, (byte)0xFF), pixel);
        }
    }
    private sealed class CursorWindow : IDisposable
    {
        private readonly IntPtr _display;
        internal CursorWindow(string displayName)
        {
            _display = XOpenDisplay(displayName);
            Assert.NotEqual(IntPtr.Zero, _display);
            var window = XCreateSimpleWindow(_display, XDefaultRootWindow(_display), 0, 0, 320, 240, 0, 0, 0xFF0000);
            XDefineCursor(_display, window, XCreateFontCursor(_display, 34));
            XMapWindow(_display, window);
            XSync(_display, false);
        }
        public void Dispose() => XCloseDisplay(_display);
        [DllImport("libX11.so.6")] private static extern IntPtr XOpenDisplay([MarshalAs(UnmanagedType.LPUTF8Str)] string name);
        [DllImport("libX11.so.6")] private static extern int XCloseDisplay(IntPtr display);
        [DllImport("libX11.so.6")] private static extern nuint XDefaultRootWindow(IntPtr display);
        [DllImport("libX11.so.6")] private static extern nuint XCreateSimpleWindow(IntPtr display, nuint parent, int x, int y, uint width, uint height, uint border, nuint borderPixel, nuint background);
        [DllImport("libX11.so.6")] private static extern nuint XCreateFontCursor(IntPtr display, uint shape);
        [DllImport("libX11.so.6")] private static extern int XDefineCursor(IntPtr display, nuint window, nuint cursor);
        [DllImport("libX11.so.6")] private static extern int XMapWindow(IntPtr display, nuint window);
        [DllImport("libX11.so.6")] private static extern int XSync(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool discard);
    }

}
