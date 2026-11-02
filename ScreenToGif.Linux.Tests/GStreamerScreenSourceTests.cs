using Avalonia;
using System.Runtime.InteropServices;
using ScreenToGif.Linux.Services.Capture;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class GStreamerGlFactAttribute : FactAttribute
{
    public GStreamerGlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENTOGIF_GSTREAMER_GL_SMOKE") != "1")
            Skip = "Opt in with GStreamer OpenGL plugins and a working graphics context.";
    }
}

public sealed class GStreamerScreenSourceTests
{
    [GStreamerGlFact]
    public void Gpu_download_preserves_crop_orientation_stride_and_pixels()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=BGRA,width=8,height=4,framerate=10/1\"" +
            " ! taginject tags=\"image-orientation=rotate-90\" ! glupload ! glcolorconvert ! gldownload ! video/x-raw,format=BGRA",
            initialize: pipeline => NativeFixture.PushGradient(pipeline, padded: true));
        using var frame = source.Capture(new PixelRect(source.Size), false);
        Assert.Equal(new PixelSize(2, 4), frame.Size);
        Assert.Equal(new byte[]
        {
            30, 60, 128, 255, 30, 30, 128, 255,
            45, 60, 128, 255, 45, 30, 128, 255,
            60, 60, 128, 255, 60, 30, 128, 255,
            75, 60, 128, 255, 75, 30, 128, 255
        }, frame.Pixels.ToArray());
    }

    [Theory]
    [InlineData("rotate-0", "none")]
    [InlineData("rotate-90", "clockwise")]
    [InlineData("rotate-180", "rotate-180")]
    [InlineData("rotate-270", "counterclockwise")]
    [InlineData("flip-rotate-0", "horizontal-flip")]
    [InlineData("flip-rotate-90", "upper-right-diagonal")]
    [InlineData("flip-rotate-180", "vertical-flip")]
    [InlineData("flip-rotate-270", "upper-left-diagonal")]
    public void Every_orientation_matches_the_native_reference_filter(string tag, string method)
    {
        const string input = "videotestsrc num-buffers=1 pattern=smpte75 ! video/x-raw,format=BGRA,width=16,height=8,framerate=10/1";
        using var reference = GStreamerScreenSource.OpenTestSource(input + " ! videoflip method=" + method);
        using var expected = reference.Capture(new PixelRect(reference.Size), false);
        using var source = GStreamerScreenSource.OpenTestSource(input + " ! taginject tags=\"image-orientation=" + tag + "\"");
        using var actual = source.Capture(new PixelRect(source.Size), false);
        Assert.Equal(expected.Size, actual.Size);
        Assert.Equal(expected.Pixels.ToArray(), actual.Pixels.ToArray());
    }

    [Fact]
    public void Rgba_sources_are_normalized_to_the_existing_bgra_contract()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=RGBA,width=8,height=4,framerate=10/1\"",
            initialize: NativeFixture.PushCroppedGradient);
        using var frame = source.Capture(new PixelRect(source.Size), false);
        Assert.Equal(new PixelSize(4, 2), frame.Size);
        Assert.Equal(new byte[] { 128, 30, 30, 255 }, frame.Pixels[..4].ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Video_metadata_maps_padded_rows_and_negative_stride(bool negativeStride)
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=BGRA,width=8,height=4,framerate=10/1\"",
            initialize: pipeline => NativeFixture.PushGradient(pipeline, padded: true, negativeStride: negativeStride));
        using var frame = source.Capture(new PixelRect(source.Size), false);
        for (var row = 0; row < 2; row++)
        for (var column = 0; column < 4; column++)
            Assert.Equal(new byte[] { (byte)((column + 2) * 15), (byte)((row + 1) * 30), 128, 255 },
                frame.Pixels.Slice((row * 4 + column) * 4, 4).ToArray());
    }

    [Fact]
    public void An_invalid_crop_fails_before_adopting_a_source()
    {
        var error = Assert.Throws<ScreenCaptureException>(() => GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=BGRA,width=8,height=4,framerate=10/1\"",
            initialize: pipeline => NativeFixture.PushGradient(pipeline, invalidCrop: true)));
        Assert.Contains("invalid crop", error.Message);
    }

    [Fact]
    public void An_incomplete_native_buffer_is_rejected_before_copying_pixels()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=BGRA,width=8,height=4,framerate=10/1\"",
            initialize: pipeline => NativeFixture.PushGradient(pipeline, incomplete: true));
        var error = Assert.Throws<ScreenCaptureException>(() => source.Capture(new PixelRect(source.Size), false));
        Assert.Contains("incomplete pixel buffer", error.Message);
    }

    [Fact]
    public void Latest_frame_replaces_old_buffers_and_disposal_releases_the_final_native_reference()
    {
        var releases = 0;
        NativeFixture.BufferFreed freed = (_, _) => Interlocked.Increment(ref releases);
        var pipeline = IntPtr.Zero;
        var source = GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=BGRA,width=8,height=4,framerate=10/1\"",
            initialize: graph =>
            {
                pipeline = graph;
                NativeFixture.PushGradient(graph, freed: freed, end: false);
            });
        try
        {
            using var frame = source.Capture(new PixelRect(source.Size), false);
            var copied = frame.Pixels.ToArray();
            Assert.Equal(0, Volatile.Read(ref releases));
            NativeFixture.PushGradient(pipeline, freed: freed, end: false, timestamp: 100_000_000);
            NativeFixture.PushGradient(pipeline, freed: freed, timestamp: 200_000_000);
            Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref releases) == 2, TimeSpan.FromSeconds(5)),
                "Old native buffers must be released even when the recorder is not pulling frames.");
            using var latest = source.Capture(new PixelRect(source.Size), false);
            Assert.Equal(200, latest.PresentationMilliseconds);
            source.Dispose();
            source.Dispose();
            Assert.Equal(3, Volatile.Read(ref releases));
            Assert.Equal(copied, frame.Pixels.ToArray());
        }
        finally { source.Dispose(); GC.KeepAlive(freed); }
    }

    [Fact]
    public void Native_crop_metadata_excludes_padding_from_the_selected_source()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=BGRA,width=8,height=4,framerate=10/1\"",
            initialize: NativeFixture.PushCroppedGradient);
        Assert.Equal(new PixelSize(4, 2), source.Size);
        using var frame = source.Capture(new PixelRect(source.Size), false);
        for (var row = 0; row < 2; row++)
        for (var column = 0; column < 4; column++)
            Assert.Equal(new byte[] { (byte)((column + 2) * 15), (byte)((row + 1) * 30), 128, 255 },
                frame.Pixels.Slice((row * 4 + column) * 4, 4).ToArray());
    }

    [Fact]
    public void Native_crop_and_rotation_keep_the_same_visible_pixels()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "appsrc name=fixture is-live=true format=time caps=\"video/x-raw,format=BGRA,width=8,height=4,framerate=10/1\" ! taginject tags=\"image-orientation=rotate-90\"",
            initialize: NativeFixture.PushCroppedGradient);
        Assert.Equal(new PixelSize(2, 4), source.Size);
        using var frame = source.Capture(new PixelRect(source.Size), false);
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 2; column++)
            Assert.Equal(new byte[] { (byte)((row + 2) * 15), (byte)((2 - column) * 30), 128, 255 },
                frame.Pixels.Slice((row * 2 + column) * 4, 4).ToArray());
    }

    [Fact]
    public void Native_orientation_tags_are_applied_before_the_capture_size_is_locked()
    {
        const string input = "videotestsrc num-buffers=1 pattern=smpte ! video/x-raw,format=BGRA,width=16,height=8,framerate=10/1";
        using var normal = GStreamerScreenSource.OpenTestSource(input);
        using var original = normal.Capture(new PixelRect(0, 0, 16, 8), false);
        using var rotated = GStreamerScreenSource.OpenTestSource(input + " ! taginject tags=\"image-orientation=rotate-90\"");
        Assert.Equal(new PixelSize(8, 16), rotated.Size);
        using var actual = rotated.Capture(new PixelRect(rotated.Size), false);
        var expected = new byte[original.Pixels.Length];
        for (var row = 0; row < 8; row++)
        for (var column = 0; column < 16; column++)
            original.Pixels.Slice((row * 16 + column) * 4, 4)
                .CopyTo(expected.AsSpan((column * 8 + (7 - row)) * 4, 4));
        Assert.Equal(expected, actual.Pixels.ToArray());
    }

    [Fact]
    public async Task Canceled_first_frame_wait_cleans_up_and_allows_a_new_stream()
    {
        using var canceled = new CancellationTokenSource();
        var opening = Task.Run(() => GStreamerScreenSource.OpenTestSource(
            "videotestsrc is-live=true ! valve drop=true", canceled.Token));
        canceled.CancelAfter(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => opening.WaitAsync(TimeSpan.FromSeconds(10)));
        using var retry = GStreamerScreenSource.OpenTestSource(
            "videotestsrc is-live=true pattern=blue ! video/x-raw,width=32,height=24");
        using var frame = retry.Capture(new PixelRect(0, 0, 32, 24), false);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, frame.Pixels[..4].ToArray());
    }

    [Fact]
    public void Native_bgra_frames_keep_size_colour_and_stream_time()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "videotestsrc is-live=true pattern=red ! video/x-raw,width=65,height=37,framerate=10/1");
        using var first = source.Capture(new PixelRect(0, 0, 65, 37), false);
        Assert.Equal(new PixelSize(65, 37), first.Size);
        Assert.Equal(260, first.Stride);
        for (var offset = 0; offset < first.Pixels.Length; offset += 4)
            Assert.Equal(new byte[] { 0, 0, 255, 255 }, first.Pixels.Slice(offset, 4).ToArray());
        using var second = source.Capture(new PixelRect(0, 0, 65, 37), false);
        Assert.True(second.PresentationMilliseconds > first.PresentationMilliseconds);
        Assert.InRange(second.PresentationMilliseconds!.Value - first.PresentationMilliseconds!.Value, 99, 101);
    }

    [Fact]
    public void Cropped_native_video_respects_plane_offsets_and_rows()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "videotestsrc is-live=true pattern=red ! video/x-raw,format=BGRA,width=80,height=40 ! videocrop left=3 right=12 top=1 bottom=2");
        using var frame = source.Capture(new PixelRect(0, 0, 65, 37), false);
        Assert.Equal(new PixelSize(65, 37), frame.Size);
        for (var offset = 0; offset < frame.Pixels.Length; offset += 4)
            Assert.Equal(new byte[] { 0, 0, 255, 255 }, frame.Pixels.Slice(offset, 4).ToArray());
    }

    [Fact]
    public void Unsupported_region_and_invalid_pipeline_report_actionable_errors()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "videotestsrc is-live=true pattern=red ! video/x-raw,width=32,height=24");
        Assert.Contains("entire source", Assert.Throws<ScreenCaptureException>(() =>
            source.Capture(new PixelRect(1, 1, 16, 16), false)).Message);
        Assert.Throws<ScreenCaptureException>(() => GStreamerScreenSource.OpenTestSource("screentogif-missing-source"));
    }

    [Fact]
    public void Native_eos_is_an_error_and_disposal_is_repeatable()
    {
        var source = GStreamerScreenSource.OpenTestSource(
            "videotestsrc is-live=true num-buffers=1 pattern=green ! video/x-raw,width=32,height=24");
        using (source.Capture(new PixelRect(0, 0, 32, 24), false)) { }
        Assert.Throws<ScreenCaptureException>(() => source.Capture(new PixelRect(0, 0, 32, 24), false));
        source.Dispose();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.Capture(new PixelRect(0, 0, 32, 24), false));
    }

    [Fact]
    public void Changing_native_content_is_copied_before_the_sample_is_released()
    {
        using var source = GStreamerScreenSource.OpenTestSource(
            "videotestsrc is-live=true pattern=ball ! video/x-raw,width=96,height=64,framerate=10/1");
        using var first = source.Capture(new PixelRect(0, 0, 96, 64), false);
        var saved = first.Pixels.ToArray();
        using var second = source.Capture(new PixelRect(0, 0, 96, 64), false);
        Assert.False(saved.SequenceEqual(second.Pixels.ToArray()));
        Assert.Equal(saved, first.Pixels.ToArray());
    }
    private static class NativeFixture
    {
        internal static void PushCroppedGradient(IntPtr pipeline) => PushGradient(pipeline);

        internal static void PushGradient(IntPtr pipeline, bool padded = false, bool negativeStride = false,
            bool invalidCrop = false, bool incomplete = false, BufferFreed? freed = null, bool end = true, long timestamp = 0)
        {
            var source = gst_bin_get_by_name(pipeline, "fixture");
            var length = incomplete ? 16 : padded ? 168 : 128;
            var stride = negativeStride ? -40 : padded ? 40 : 32;
            var origin = negativeStride ? 128 : padded ? 8 : 0;
            var buffer = gst_buffer_new_allocate(IntPtr.Zero, (nuint)length, IntPtr.Zero);
            try
            {
                if (freed is not null) gst_mini_object_weak_ref(buffer, freed, IntPtr.Zero);
                if (padded)
                    Assert.NotEqual(IntPtr.Zero, gst_buffer_add_video_meta_full(buffer, 0, gst_video_format_from_string("BGRA"),
                        8, 4, 1, [(nuint)origin, 0, 0, 0], [stride, 0, 0, 0]));
                var pixels = new byte[length];
                for (var row = 0; row < 4; row++)
                for (var column = 0; column < 8; column++)
                {
                    var offset = origin + row * stride + column * 4;
                    if (offset + 4 > pixels.Length) continue;
                    pixels[offset] = (byte)(column * 15);
                    pixels[offset + 1] = (byte)(row * 30);
                    pixels[offset + 2] = 128;
                    pixels[offset + 3] = 255;
                }
                Assert.Equal((nuint)pixels.Length, gst_buffer_fill(buffer, 0, pixels, (nuint)pixels.Length));
                // GstBuffer's public 64-bit 1.x ABI: GstMiniObject64 + pool8 precede pts.
                Assert.Equal(8, IntPtr.Size);
                Marshal.WriteInt64(buffer, 72, timestamp);
                var crop = gst_buffer_add_meta(buffer, gst_video_crop_meta_get_info(), IntPtr.Zero);
                Assert.NotEqual(IntPtr.Zero, crop);
                // GstVideoCropMeta follows GstMeta (flags + aligned info pointer).
                var fields = Marshal.SizeOf<MetaHeader>();
                Marshal.WriteInt32(crop, fields, invalidCrop ? 7 : 2);
                Marshal.WriteInt32(crop, fields + 4, 1);
                Marshal.WriteInt32(crop, fields + 8, 4);
                Marshal.WriteInt32(crop, fields + 12, 2);
                var owned = buffer;
                buffer = IntPtr.Zero;
                Assert.Equal(0, gst_app_src_push_buffer(source, owned));
                if (end) Assert.Equal(0, gst_app_src_end_of_stream(source));
            }
            finally
            {
                if (buffer != IntPtr.Zero) gst_mini_object_unref(buffer);
                gst_object_unref(source);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void BufferFreed(IntPtr data, IntPtr buffer);
        [DllImport("libgstreamer-1.0.so.0")] private static extern void gst_mini_object_weak_ref(IntPtr buffer, BufferFreed notify, IntPtr data);
        [DllImport("libgstvideo-1.0.so.0")] private static extern int gst_video_format_from_string([MarshalAs(UnmanagedType.LPUTF8Str)] string format);
        [DllImport("libgstvideo-1.0.so.0")] private static extern IntPtr gst_buffer_add_video_meta_full(IntPtr buffer, uint flags, int format, uint width, uint height, uint planes, nuint[] offsets, int[] strides);
        [StructLayout(LayoutKind.Sequential)] private struct MetaHeader { internal uint Flags; internal IntPtr Info; }
        [DllImport("libgstreamer-1.0.so.0")] private static extern IntPtr gst_bin_get_by_name(IntPtr bin, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
        [DllImport("libgstreamer-1.0.so.0")] private static extern IntPtr gst_buffer_new_allocate(IntPtr allocator, nuint size, IntPtr options);
        [DllImport("libgstreamer-1.0.so.0")] private static extern nuint gst_buffer_fill(IntPtr buffer, nuint offset, byte[] pixels, nuint size);
        [DllImport("libgstreamer-1.0.so.0")] private static extern IntPtr gst_buffer_add_meta(IntPtr buffer, IntPtr info, IntPtr options);
        [DllImport("libgstvideo-1.0.so.0")] private static extern IntPtr gst_video_crop_meta_get_info();
        [DllImport("libgstapp-1.0.so.0")] private static extern int gst_app_src_push_buffer(IntPtr source, IntPtr buffer);
        [DllImport("libgstapp-1.0.so.0")] private static extern int gst_app_src_end_of_stream(IntPtr source);
        [DllImport("libgstreamer-1.0.so.0")] private static extern void gst_object_unref(IntPtr value);
        [DllImport("libgstreamer-1.0.so.0")] private static extern void gst_mini_object_unref(IntPtr value);
    }

}
