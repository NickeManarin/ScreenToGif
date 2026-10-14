using Avalonia;
using Avalonia.Media.Imaging;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using Xunit;

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
    [InlineData("XID", true)]
    [InlineData("Wayland", false)]
    [InlineData(null, false)]
    public void Only_an_x11_session_reports_itself_capturable(string? handleDescriptor, bool expected) =>
        Assert.Equal(expected, X11ScreenSource.IsSupportedSession(handleDescriptor));

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
}
