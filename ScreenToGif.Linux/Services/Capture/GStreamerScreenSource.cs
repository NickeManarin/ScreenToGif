using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>Consumes only the authorized PipeWire remote, retaining one immutable native frame.</summary>
internal sealed class GStreamerScreenSource : IScreenSource, IRecordingClock
{
    private readonly object _gate = new();
    private readonly Native.ProbeCallback _probeCallback;
    private IntPtr _pipeline, _sink, _bus, _clock, _pad, _segmentEvent, _segment;
    private nuint _probeId;
    private ulong _baseTime;
    private NativeFrame? _latest;
    private ScreenCaptureException? _failure;
    private long _version, _delivered;
    private Orientation _orientation;
    private bool _ended;
    private int _disposed;

    private GStreamerScreenSource(string source, CancellationToken cancellationToken, Action<IntPtr>? initialize = null)
    {
        _probeCallback = Observe;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Native.gst_init_check(IntPtr.Zero, IntPtr.Zero, out var error))
                throw Failure("GStreamer could not initialize", error);
            // Keep crop, orientation and stream time together at the final RGB boundary.
            // The GPU round trip's metadata is checked by the native GL fixture.
            _pipeline = Native.gst_parse_launch(source +
                " ! capsfilter caps=\"video/x-raw,format=(string){BGRA,BGRx,RGBA,RGBx}\" ! fakesink name=frames sync=false async=false enable-last-sample=false", out error);
            if (error != IntPtr.Zero || _pipeline == IntPtr.Zero)
                throw Failure("The screen stream could not be opened. Install GStreamer 1.24 or later with its PipeWire, OpenGL and core plugins", error);
            _sink = Native.gst_bin_get_by_name(_pipeline, "frames");
            _bus = Native.gst_element_get_bus(_pipeline);
            _pad = _sink == IntPtr.Zero ? IntPtr.Zero : Native.gst_element_get_static_pad(_sink, "sink");
            if (_sink == IntPtr.Zero || _bus == IntPtr.Zero || _pad == IntPtr.Zero)
                throw new ScreenCaptureException("The screen stream could not start. Check the GStreamer PipeWire plugins.");
            _probeId = Native.gst_pad_add_probe(_pad, 16 | 64, _probeCallback, IntPtr.Zero, IntPtr.Zero);
            if (_probeId == 0)
                throw new ScreenCaptureException("The screen stream could not be observed.");
            initialize?.Invoke(_pipeline);
            if (Native.gst_element_set_state(_pipeline, 4) == 0)
                throw new ScreenCaptureException("The screen stream could not start. Check the GStreamer PipeWire plugins.");
            var timeout = Stopwatch.StartNew();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CheckFailure();
                lock (_gate)
                {
                    if (_latest is { } frame) { Size = OutputSize(frame); break; }
                    if (_ended) throw Ended();
                    if (timeout.Elapsed >= TimeSpan.FromSeconds(10))
                        throw new ScreenCaptureException("The selected source did not send a frame. Select a visible window or monitor and try again.");
                    Monitor.Wait(_gate, 100);
                }
            }
            _clock = Native.gst_element_get_clock(_pipeline);
            if (_clock == IntPtr.Zero)
                throw new ScreenCaptureException("The screen stream has no running clock. Select the source again.");
            _baseTime = Native.gst_element_get_base_time(_pipeline);
        }
        catch (Exception exception)
        {
            Dispose();
            if (exception is DllNotFoundException or EntryPointNotFoundException)
                throw new ScreenCaptureException("Wayland recording needs GStreamer 1.0, its video library and PipeWire plugin. Install the documented runtime packages and try again.", exception);
            throw;
        }
    }

    internal static GStreamerScreenSource OpenPipeWire(int remoteFd, uint nodeId, ulong? serial, int intervalMilliseconds, CancellationToken cancellationToken)
    {
        try
        {
            // GPU rendering avoids Mutter's fractional-scale memory-copy crop mismatch.
            // BGRA conversion prevents retaining a passthrough import with cached CPU pixels.
            return new GStreamerScreenSource($"pipewiresrc fd={remoteFd} " +
                (serial is { } value ? $"target-object={value}" : $"path={nodeId}") +
                $" always-copy=false max-buffers=3 keepalive-time={Math.Clamp(intervalMilliseconds, 1, 100)}" +
                " ! video/x-raw(memory:DMABuf),format=DMA_DRM ! glupload ! glcolorconvert ! gldownload ! video/x-raw,format=BGRA", cancellationToken);
        }
        catch (ScreenCaptureException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ScreenCaptureException("The selected source could not provide GPU frames. Wayland recording needs GStreamer 1.24 or later with the PipeWire and OpenGL plugins and a compatible graphics driver. Choose another source or use an X11 session.", exception);
        }
    }

    internal static GStreamerScreenSource OpenTestSource(string pipeline, CancellationToken cancellationToken = default,
        Action<IntPtr>? initialize = null) => new(pipeline, cancellationToken, initialize);

    internal PixelSize Size { get; }

    public long ElapsedMilliseconds
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            var time = Native.gst_clock_get_time(_clock);
            return time >= _baseTime ? checked((long)((time - _baseTime) / 1_000_000)) : 0;
        }
    }

    public CapturedFrame Capture(PixelRect region, bool includeCursor)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (region != new PixelRect(Size))
            throw new ScreenCaptureException("Wayland records the entire source selected in the sharing dialog. Choose another source to change its size.");
        var wait = Stopwatch.StartNew();
        NativeFrame snapshot;
        while (true)
        {
            CheckFailure();
            lock (_gate)
            {
                if (_latest is { } frame && frame.Version != _delivered)
                {
                    snapshot = frame with { Buffer = Native.gst_mini_object_ref(frame.Buffer) };
                    _delivered = frame.Version;
                    break;
                }
                if (_ended) throw Ended();
                if (wait.ElapsedMilliseconds >= 250)
                    throw new ScreenCaptureException("The selected source stopped sending frames. Stop to keep the recording, then select the source again.");
                Monitor.Wait(_gate, 25);
            }
        }
        try
        {
            if (OutputSize(snapshot) != Size)
                throw new ScreenCaptureException("The selected source changed pixel size. Stop to keep the recording, then start a new recording at the new size.");
            return ReadFrame(snapshot);
        }
        finally { Native.gst_mini_object_unref(snapshot.Buffer); }
    }

    private int Observe(IntPtr pad, IntPtr information, IntPtr userData)
    {
        if (Volatile.Read(ref _disposed) != 0) return 1;
        try
        {
            var probe = Marshal.PtrToStructure<Native.ProbeInfo>(information);
            if ((probe.Type & 64) != 0) ObserveEvent(probe.Data);
            if ((probe.Type & 16) != 0) ObserveBuffer(pad, probe.Data);
        }
        catch (Exception exception)
        {
            lock (_gate)
            {
                _failure = exception as ScreenCaptureException ?? new ScreenCaptureException(
                    "The screen stream could not be read. Stop to keep the recording, then select the source again.", exception);
                Monitor.PulseAll(_gate);
            }
        }
        return 1; // GST_PAD_PROBE_OK. Never let managed exceptions cross the native callback.
    }

    private void ObserveEvent(IntPtr message)
    {
        var type = (uint)Marshal.ReadInt32(message, Marshal.SizeOf<Native.MiniObject>());
        switch (Marshal.PtrToStringUTF8(Native.gst_event_type_get_name(type)))
        {
            case "segment":
                var owned = Native.gst_mini_object_ref(message);
                Native.gst_event_parse_segment(owned, out var segment);
                if (_segmentEvent != IntPtr.Zero) Native.gst_mini_object_unref(_segmentEvent);
                _segmentEvent = owned;
                _segment = segment;
                break;
            case "tag":
                Native.gst_event_parse_tag(message, out var tags);
                if (Native.gst_tag_list_get_string(tags, "image-orientation", out var value))
                {
                    try
                    {
                        _orientation = Marshal.PtrToStringUTF8(value) switch
                        {
                            "rotate-0" => Orientation.Normal,
                            "rotate-90" => Orientation.Clockwise,
                            "rotate-180" => Orientation.HalfTurn,
                            "rotate-270" => Orientation.Counterclockwise,
                            "flip-rotate-0" => Orientation.Flip,
                            "flip-rotate-90" => Orientation.FlipClockwise,
                            "flip-rotate-180" => Orientation.FlipHalfTurn,
                            "flip-rotate-270" => Orientation.FlipCounterclockwise,
                            _ => throw new ScreenCaptureException("The screen stream provided an unsupported orientation.")
                        };
                    }
                    finally { Native.g_free(value); }
                }
                break;
            case "eos":
                lock (_gate) { _ended = true; Monitor.PulseAll(_gate); }
                break;
        }
    }

    private void ObserveBuffer(IntPtr pad, IntPtr buffer)
    {
        var caps = Native.gst_pad_get_current_caps(pad);
        if (caps == IntPtr.Zero || buffer == IntPtr.Zero || _segment == IntPtr.Zero)
            throw new ScreenCaptureException("The screen stream is missing its format or timestamps.");
        try
        {
            var structure = Native.gst_caps_get_structure(caps, 0);
            if (!Native.gst_structure_get_int(structure, "width", out var width) ||
                !Native.gst_structure_get_int(structure, "height", out var height) ||
                width <= 0 || height <= 0 || width > EditorResourceLimits.MaximumMediaDimension ||
                height > EditorResourceLimits.MaximumMediaDimension)
                throw new ScreenCaptureException("The selected source has an unsupported pixel size. Select a smaller source.");
            var format = Marshal.PtrToStringUTF8(Native.gst_structure_get_string(structure, "format"));
            if (format is not ("BGRA" or "BGRx" or "RGBA" or "RGBx"))
                throw new ScreenCaptureException("The selected source does not provide a supported RGB pixel format.");
            var pts = Marshal.PtrToStructure<Native.BufferHeader>(buffer).Pts;
            var running = Native.gst_segment_to_running_time(_segment, 3, pts);
            if (pts == ulong.MaxValue || running == ulong.MaxValue)
                throw new ScreenCaptureException("The screen stream has no usable frame timestamps. Select the source again.");
            var crop = VisibleRegion(buffer, width, height);
            var owned = Native.gst_mini_object_ref(buffer);
            NativeFrame? previous = null;
            var retained = false;
            lock (_gate)
            {
                if (Volatile.Read(ref _disposed) == 0)
                {
                    previous = _latest;
                    _latest = new NativeFrame(owned, width, height, crop, format is "RGBA" or "RGBx",
                        _orientation, checked((long)(running / 1_000_000)), ++_version);
                    retained = true;
                    Monitor.PulseAll(_gate);
                }
            }
            if (!retained) Native.gst_mini_object_unref(owned);
            if (previous is not null) Native.gst_mini_object_unref(previous.Buffer);
        }
        finally { Native.gst_mini_object_unref(caps); }
    }

    private static PixelSize OutputSize(NativeFrame frame) =>
        frame.Orientation is Orientation.Clockwise or Orientation.Counterclockwise or
            Orientation.FlipClockwise or Orientation.FlipCounterclockwise
            ? new PixelSize(frame.Crop.Height, frame.Crop.Width) : frame.Crop.Size;

    private static CapturedFrame ReadFrame(NativeFrame native)
    {
        var size = OutputSize(native);
        var stride = checked(size.Width * 4);
        var pixels = ArrayPool<byte>.Shared.Rent(checked(stride * size.Height));
        try
        {
            CopyPixels(native, pixels, size);
            return new CapturedFrame(size, stride, pixels, native.PresentationMilliseconds);
        }
        catch { ArrayPool<byte>.Shared.Return(pixels); throw; }
    }

    private static PixelRect VisibleRegion(IntPtr buffer, int width, int height)
    {
        var pointer = Native.gst_buffer_get_meta(buffer, Native.gst_video_crop_meta_api_get_type());
        if (pointer == IntPtr.Zero) return new PixelRect(0, 0, width, height);
        var crop = Marshal.PtrToStructure<Native.VideoCropMeta>(pointer);
        var videoPointer = Native.gst_buffer_get_video_meta(buffer);
        if (videoPointer != IntPtr.Zero)
        {
            var video = Marshal.PtrToStructure<Native.VideoMetaHeader>(videoPointer);
            if (video.Width == 0 || video.Height == 0 || video.Width > EditorResourceLimits.MaximumMediaDimension ||
                video.Height > EditorResourceLimits.MaximumMediaDimension)
                throw new ScreenCaptureException("The screen stream provided an unsupported image buffer size.");
            width = (int)video.Width;
            height = (int)video.Height;
        }
        if (crop.Width == 0 || crop.Height == 0 || crop.Width > EditorResourceLimits.MaximumMediaDimension ||
            crop.Height > EditorResourceLimits.MaximumMediaDimension || (ulong)crop.X + crop.Width > (ulong)width ||
            (ulong)crop.Y + crop.Height > (ulong)height)
            throw new ScreenCaptureException("The screen stream provided an invalid crop rectangle. Stop to keep the recording, then choose the source again.");
        return new PixelRect((int)crop.X, (int)crop.Y, (int)crop.Width, (int)crop.Height);
    }

    private static unsafe void CopyPixels(NativeFrame frame, byte[] pixels, PixelSize size)
    {
        var meta = Native.gst_buffer_get_video_meta(frame.Buffer);
        var info = new Native.MapInfo();
        var data = IntPtr.Zero;
        var stride = checked(frame.Width * 4);
        var mapped = meta != IntPtr.Zero
            ? Native.gst_video_meta_map(meta, 0, out info, out data, out stride, 1)
            : Native.gst_buffer_map(frame.Buffer, out info, 1);
        if (!mapped) throw new ScreenCaptureException("The selected source's pixels could not be read. Stop to keep the recorded frames.");
        if (meta == IntPtr.Zero) data = info.Data;
        try
        {
            var crop = frame.Crop;
            var first = (long)crop.Y * stride + crop.X * 4L;
            var last = ((long)crop.Y + crop.Height - 1) * stride + crop.X * 4L;
            var planeOffset = data.ToInt64() - info.Data.ToInt64();
            var firstByte = planeOffset + Math.Min(first, last);
            var endByte = planeOffset + Math.Max(first, last) + crop.Width * 4L;
            if (Math.Abs((long)stride) < Math.Max(frame.Width, crop.Right) * 4L || data == IntPtr.Zero ||
                info.Data == IntPtr.Zero || firstByte < 0 || endByte < 0 || (ulong)endByte > (ulong)info.Size)
                throw new ScreenCaptureException("The screen stream provided an incomplete pixel buffer.");
            var w = crop.Width;
            var h = crop.Height;
            for (var row = 0; row < size.Height; row++)
            {
                if (frame.Orientation == Orientation.Normal && !frame.SwapRedBlue)
                {
                    var target = pixels.AsSpan(row * size.Width * 4, size.Width * 4);
                    new ReadOnlySpan<byte>((void*)(data.ToInt64() + ((long)row + crop.Y) * stride + crop.X * 4L), target.Length).CopyTo(target);
                    for (var alpha = 3; alpha < target.Length; alpha += 4) target[alpha] = 255;
                    continue;
                }
                for (var column = 0; column < size.Width; column++)
                {
                    var (x, y) = frame.Orientation switch
                    {
                        Orientation.Normal => (column, row),
                        Orientation.Clockwise => (row, h - 1 - column),
                        Orientation.HalfTurn => (w - 1 - column, h - 1 - row),
                        Orientation.Counterclockwise => (w - 1 - row, column),
                        Orientation.Flip => (w - 1 - column, row),
                        Orientation.FlipClockwise => (w - 1 - row, h - 1 - column),
                        Orientation.FlipHalfTurn => (column, h - 1 - row),
                        Orientation.FlipCounterclockwise => (row, column),
                        _ => throw new ScreenCaptureException("The screen stream provided an unsupported orientation.")
                    };
                    var pixel = (byte*)(data.ToInt64() + ((long)y + crop.Y) * stride + ((long)x + crop.X) * 4);
                    var output = (row * size.Width + column) * 4;
                    pixels[output] = pixel[frame.SwapRedBlue ? 2 : 0];
                    pixels[output + 1] = pixel[1];
                    pixels[output + 2] = pixel[frame.SwapRedBlue ? 0 : 2];
                    pixels[output + 3] = 255;
                }
            }
        }
        finally
        {
            if (meta != IntPtr.Zero) Native.gst_video_meta_unmap(meta, 0, ref info);
            else Native.gst_buffer_unmap(frame.Buffer, ref info);
        }
    }

    private void CheckFailure()
    {
        var message = Native.gst_bus_pop_filtered(_bus, 2);
        if (message != IntPtr.Zero)
        {
            try
            {
                Native.gst_message_parse_error(message, out var error, out var debug);
                if (debug != IntPtr.Zero) Native.g_free(debug);
                lock (_gate) _failure = Failure("Screen sharing failed. Stop to keep the recording, then select the source again", error);
            }
            finally { Native.gst_mini_object_unref(message); }
        }
        lock (_gate) if (_failure is { } failure) throw failure;
    }

    private static ScreenCaptureException Ended() => new("Screen sharing ended. Stop to keep the recording, then select the source again.");

    private static ScreenCaptureException Failure(string message, IntPtr error)
    {
        if (error == IntPtr.Zero) return new ScreenCaptureException(message + ".");
        try { return new ScreenCaptureException(message + ": " + Marshal.PtrToStringUTF8(Marshal.PtrToStructure<Native.GError>(error).Message)); }
        finally { Native.g_error_free(error); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        NativeFrame? latest;
        lock (_gate)
        {
            latest = _latest;
            _latest = null;
            _ended = true;
            Monitor.PulseAll(_gate);
        }
        if (latest is not null) Native.gst_mini_object_unref(latest.Buffer);
        if (_pipeline != IntPtr.Zero) Native.gst_element_set_state(_pipeline, 1);
        if (_probeId != 0) Native.gst_pad_remove_probe(_pad, _probeId);
        if (_segmentEvent != IntPtr.Zero) Native.gst_mini_object_unref(_segmentEvent);
        if (_pad != IntPtr.Zero) Native.gst_object_unref(_pad);
        if (_clock != IntPtr.Zero) Native.gst_object_unref(_clock);
        if (_sink != IntPtr.Zero) Native.gst_object_unref(_sink);
        if (_bus != IntPtr.Zero) Native.gst_object_unref(_bus);
        if (_pipeline != IntPtr.Zero) Native.gst_object_unref(_pipeline);
        _pipeline = _sink = _bus = _clock = _pad = _segmentEvent = _segment = IntPtr.Zero;
    }

    private enum Orientation { Normal, Clockwise, HalfTurn, Counterclockwise, Flip, FlipClockwise, FlipHalfTurn, FlipCounterclockwise }
    private sealed record NativeFrame(IntPtr Buffer, int Width, int Height, PixelRect Crop, bool SwapRedBlue,
        Orientation Orientation, long PresentationMilliseconds, long Version);
    private static class Native
    {
        private const string Gst = "libgstreamer-1.0.so.0";
        private const string Video = "libgstvideo-1.0.so.0";
        // GStreamer's stable 1.x public ABI: gstminiobject.h, gstbuffer.h and gstmemory.h.
        [StructLayout(LayoutKind.Sequential)] internal struct MiniObject
        {
            internal nuint Type;
            internal int RefCount, LockState;
            internal uint Flags;
            internal IntPtr Copy, Dispose, Free;
            internal uint PrivateUInt;
            internal IntPtr PrivatePointer;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct BufferHeader
        {
            internal MiniObject MiniObject;
            internal IntPtr Pool;
            internal ulong Pts, Dts, Duration, Offset, OffsetEnd;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct MetaHeader
        {
            internal uint Flags;
            internal IntPtr Info;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct VideoCropMeta
        {
            internal MetaHeader Meta;
            internal uint X, Y, Width, Height;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct VideoMetaHeader
        {
            internal MetaHeader Meta;
            internal IntPtr Buffer;
            internal uint Flags;
            internal int Format, Id;
            internal uint Width, Height, Planes;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct MapInfo
        {
            internal IntPtr Memory;
            internal int Flags;
            internal IntPtr Data;
            internal nuint Size, MaxSize;
            internal IntPtr User0, User1, User2, User3, Reserved0, Reserved1, Reserved2, Reserved3;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct GError { internal uint Domain; internal int Code; internal IntPtr Message; }
        [StructLayout(LayoutKind.Sequential)] internal struct ProbeInfo
        {
            internal uint Type;
            internal nuint Id;
            internal IntPtr Data;
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int ProbeCallback(IntPtr pad, IntPtr info, IntPtr userData);
        [DllImport(Gst)] internal static extern IntPtr gst_element_get_static_pad(IntPtr element, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
        [DllImport(Gst)] internal static extern nuint gst_pad_add_probe(IntPtr pad, uint mask, ProbeCallback callback, IntPtr userData, IntPtr destroy);
        [DllImport(Gst)] internal static extern void gst_pad_remove_probe(IntPtr pad, nuint id);
        [DllImport(Gst)] internal static extern IntPtr gst_pad_get_current_caps(IntPtr pad);
        [DllImport(Gst)] internal static extern IntPtr gst_mini_object_ref(IntPtr value);
        [DllImport(Gst)] internal static extern IntPtr gst_event_type_get_name(uint type);
        [DllImport(Gst)] internal static extern void gst_event_parse_segment(IntPtr value, out IntPtr segment);
        [DllImport(Gst)] internal static extern void gst_event_parse_tag(IntPtr value, out IntPtr tags);
        [DllImport(Gst)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool gst_tag_list_get_string(IntPtr tags, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, out IntPtr value);
        [DllImport(Gst)] internal static extern IntPtr gst_structure_get_string(IntPtr structure, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
        [DllImport(Gst)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool gst_init_check(IntPtr argc, IntPtr argv, out IntPtr error);
        [DllImport(Gst)] internal static extern IntPtr gst_parse_launch([MarshalAs(UnmanagedType.LPUTF8Str)] string description, out IntPtr error);
        [DllImport(Gst)] internal static extern IntPtr gst_bin_get_by_name(IntPtr bin, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
        [DllImport(Gst)] internal static extern int gst_element_set_state(IntPtr element, int state);
        [DllImport(Gst)] internal static extern IntPtr gst_element_get_bus(IntPtr element);
        [DllImport(Gst)] internal static extern IntPtr gst_element_get_clock(IntPtr element);
        [DllImport(Gst)] internal static extern ulong gst_clock_get_time(IntPtr clock);
        [DllImport(Gst)] internal static extern ulong gst_element_get_base_time(IntPtr element);
        [DllImport(Gst)] internal static extern void gst_object_unref(IntPtr obj);
        [DllImport(Gst)] internal static extern void gst_mini_object_unref(IntPtr obj);
        [DllImport(Gst)] internal static extern ulong gst_segment_to_running_time(IntPtr segment, int format, ulong position);
        [DllImport(Gst)] internal static extern IntPtr gst_caps_get_structure(IntPtr caps, uint index);
        [DllImport(Gst)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool gst_structure_get_int(IntPtr structure, [MarshalAs(UnmanagedType.LPUTF8Str)] string field, out int value);
        [DllImport(Gst)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool gst_buffer_map(IntPtr buffer, out MapInfo info, int flags);
        [DllImport(Gst)] internal static extern void gst_buffer_unmap(IntPtr buffer, ref MapInfo info);
        [DllImport(Video)] internal static extern IntPtr gst_buffer_get_video_meta(IntPtr buffer);
        [DllImport(Gst)] internal static extern IntPtr gst_buffer_get_meta(IntPtr buffer, nuint api);
        [DllImport(Video)] internal static extern nuint gst_video_crop_meta_api_get_type();
        [DllImport(Video)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool gst_video_meta_map(IntPtr meta, uint plane, out MapInfo info, out IntPtr data, out int stride, int flags);
        [DllImport(Video)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool gst_video_meta_unmap(IntPtr meta, uint plane, ref MapInfo info);
        [DllImport(Gst)] internal static extern IntPtr gst_bus_pop_filtered(IntPtr bus, uint types);
        [DllImport(Gst)] internal static extern void gst_message_parse_error(IntPtr message, out IntPtr error, out IntPtr debug);
        [DllImport("libglib-2.0.so.0")] internal static extern void g_error_free(IntPtr error);
        [DllImport("libglib-2.0.so.0")] internal static extern void g_free(IntPtr memory);
    }
}
