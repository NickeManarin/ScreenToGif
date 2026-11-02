using Avalonia;
using ScreenToGif.Linux.Services.Capture;
using System.Buffers;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Pins the pointer compositing every default recording runs through. The clipping arithmetic is
/// what fails at a frame's edges, and the pointer is usually near one, so each case here puts the
/// cursor somewhere the bounds checks have to catch it.
/// </summary>
public sealed class CursorCompositingTests
{
    private const byte Background = 0x40;

    [Fact]
    public void An_opaque_cursor_inside_the_frame_replaces_exactly_its_own_pixels()
    {
        using var frame = SolidFrame(4, 4);

        // One opaque red pixel, premultiplied, at (1, 2).
        X11ScreenSource.BlendCursor([0xFFFF0000], cursorWidth: 1, cursorHeight: 1, originX: 1, originY: 2, frame);

        Assert.Equal([0x00, 0x00, 0xFF, 0xFF], PixelAt(frame, 1, 2));
        Assert.Equal([Background, Background, Background, 0xFF], PixelAt(frame, 0, 2));
        Assert.Equal([Background, Background, Background, 0xFF], PixelAt(frame, 1, 1));
    }

    [Fact]
    public void A_fully_transparent_cursor_pixel_leaves_the_background_alone()
    {
        using var frame = SolidFrame(2, 2);

        X11ScreenSource.BlendCursor([0x00FFFFFF], cursorWidth: 1, cursorHeight: 1, originX: 0, originY: 0, frame);

        Assert.Equal([Background, Background, Background, 0xFF], PixelAt(frame, 0, 0));
    }

    [Fact]
    public void A_half_transparent_cursor_pixel_is_composited_over_the_background()
    {
        using var frame = SolidFrame(2, 2);

        // Premultiplied white at half alpha: each colour channel already carries the 0x80 weight.
        X11ScreenSource.BlendCursor([0x80808080], cursorWidth: 1, cursorHeight: 1, originX: 0, originY: 0, frame);

        // 0x80 + (0x40 * 127 + 127) / 255 = 128 + 32 = 160.
        Assert.Equal([160, 160, 160, 0xFF], PixelAt(frame, 0, 0));
    }

    [Fact]
    public void A_cursor_straddling_the_top_left_corner_draws_only_the_part_inside_the_frame()
    {
        using var frame = SolidFrame(3, 3);

        // A 2x2 opaque green cursor whose top-left sits one pixel outside both edges, so only its
        // bottom-right quarter lands, at (0, 0).
        X11ScreenSource.BlendCursor(
            [0xFF00FF00, 0xFF00FF00, 0xFF00FF00, 0xFF00FF00],
            cursorWidth: 2, cursorHeight: 2, originX: -1, originY: -1, frame);

        Assert.Equal([0x00, 0xFF, 0x00, 0xFF], PixelAt(frame, 0, 0));
        Assert.Equal([Background, Background, Background, 0xFF], PixelAt(frame, 1, 0));
        Assert.Equal([Background, Background, Background, 0xFF], PixelAt(frame, 0, 1));
    }

    [Fact]
    public void A_cursor_straddling_the_bottom_right_corner_draws_only_the_part_inside_the_frame()
    {
        using var frame = SolidFrame(3, 3);

        X11ScreenSource.BlendCursor(
            [0xFF00FF00, 0xFF00FF00, 0xFF00FF00, 0xFF00FF00],
            cursorWidth: 2, cursorHeight: 2, originX: 2, originY: 2, frame);

        Assert.Equal([0x00, 0xFF, 0x00, 0xFF], PixelAt(frame, 2, 2));
        Assert.Equal([Background, Background, Background, 0xFF], PixelAt(frame, 1, 2));
    }

    [Fact]
    public void A_cursor_entirely_outside_the_frame_changes_nothing()
    {
        using var frame = SolidFrame(3, 3);
        var before = frame.Pixels.ToArray();

        foreach (var (originX, originY) in new[] { (-5, 0), (0, -5), (9, 0), (0, 9) })
            X11ScreenSource.BlendCursor([0xFFFF0000], cursorWidth: 1, cursorHeight: 1, originX, originY, frame);

        Assert.Equal(before, frame.Pixels.ToArray());
    }

    private static CapturedFrame SolidFrame(int width, int height)
    {
        var size = new PixelSize(width, height);
        var stride = width * 4;
        var pixels = ArrayPool<byte>.Shared.Rent(stride * height);
        var frame = new CapturedFrame(size, stride, pixels);
        frame.Buffer.Fill(Background);

        for (var i = 3; i < stride * height; i += 4)
            frame.Buffer[i] = 0xFF;

        return frame;
    }

    private static byte[] PixelAt(CapturedFrame frame, int x, int y) =>
        frame.Pixels.Slice(y * frame.Stride + x * 4, 4).ToArray();
}
