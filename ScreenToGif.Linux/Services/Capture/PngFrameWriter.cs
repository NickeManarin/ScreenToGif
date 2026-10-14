using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// Encodes captured frames to PNG on one background thread, into a single batch directory of an
/// <see cref="EditorWorkspace"/>. Per ADR 20260921 the encoder is Avalonia's, not FFmpeg's: capture
/// is a source rather than a frame mutation, so the editor's all-imaging-through-FFmpeg rule is
/// not at stake, and a per-frame subprocess would cost more than the grab itself.
/// </summary>
public sealed class PngFrameWriter : IFrameWriter
{
    private static readonly Vector Dpi = new(96, 96);

    private readonly string _batchPath;
    private readonly Channel<CapturedFrame> _queue;
    private readonly Task _encodeLoop;
    private readonly List<string> _written = [];
    private readonly Lock _writtenGate = new();
    private long _pendingBytes;
    private bool _discarded;
    private bool _disposed;

    public PngFrameWriter(string batchPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchPath);

        _batchPath = Path.GetFullPath(batchPath);
        _queue = Channel.CreateUnbounded<CapturedFrame>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _encodeLoop = Task.Factory.StartNew(EncodeLoopAsync, TaskCreationOptions.LongRunning).Unwrap();
    }

    public ScreenCaptureException? Failure { get; private set; }

    /// <inheritdoc />
    public long PendingBytes => Interlocked.Read(ref _pendingBytes);

    /// <summary>The directory this recording owns. Nothing outside it is ever deleted.</summary>
    public string BatchPath => _batchPath;

    public void Write(CapturedFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var bytes = (long)frame.Stride * frame.Size.Height;

        if (_disposed || _discarded || !_queue.Writer.TryWrite(frame))
        {
            frame.Dispose();
            return;
        }

        Interlocked.Add(ref _pendingBytes, bytes);
    }

    public async Task<IReadOnlyList<string>> CompleteAsync(CancellationToken cancellationToken = default)
    {
        _queue.Writer.TryComplete();
        await _encodeLoop.WaitAsync(cancellationToken);

        // A writer that failed part way still returns what it encoded, because those frames are the
        // recording the user is trying to save. The failure is on <see cref="Failure"/> for the
        // caller to report; throwing here would put the frames out of reach instead.
        lock (_writtenGate)
            return _written.ToArray();
    }

    public void Discard()
    {
        _discarded = true;
        _queue.Writer.TryComplete();

        try
        {
            _encodeLoop.Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException)
        {
            // A writer that already failed has nothing left to drain.
        }

        lock (_writtenGate)
            _written.Clear();

        DeleteBatchDirectory();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _queue.Writer.TryComplete();

        try
        {
            await _encodeLoop.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception) when (exception is TimeoutException or ScreenCaptureException or IOException)
        {
            // Disposal is best effort; CompleteAsync is where a failure is reported.
        }

        while (_queue.Reader.TryRead(out var pending))
        {
            Interlocked.Add(ref _pendingBytes, -((long)pending.Stride * pending.Size.Height));
            pending.Dispose();
        }
    }

    /// <summary>
    /// Deletes this recording's own batch directory and nothing else. INV-G7: the path is the one
    /// this writer created, never derived from a setting, a window position, or an empty root.
    /// </summary>
    private void DeleteBatchDirectory()
    {
        if (string.IsNullOrWhiteSpace(_batchPath) || !Directory.Exists(_batchPath))
            return;

        try
        {
            Directory.Delete(_batchPath, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Workspace cleanup is best effort; the workspace scavenger removes the remainder.
        }
    }

    private async Task EncodeLoopAsync()
    {
        var index = 0;
        await foreach (var frame in _queue.Reader.ReadAllAsync())
        {
            using (frame)
            {
                Interlocked.Add(ref _pendingBytes, -((long)frame.Stride * frame.Size.Height));

                if (Failure is not null || _discarded)
                    continue;

                try
                {
                    var path = Path.Combine(_batchPath, $"{index:000000}.png");
                    Encode(frame, path);
                    lock (_writtenGate)
                        _written.Add(path);
                    index++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                               or InvalidOperationException or NotSupportedException
                                               or ExternalException)
                {
                    Failure = new ScreenCaptureException(FailureMessage(ex), ex);
                }
            }
        }
    }

    private static void Encode(CapturedFrame frame, string path)
    {
        AvaloniaImaging.EnsureRenderInterface();

        using var bitmap = new WriteableBitmap(frame.Size, Dpi, PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using (var locked = bitmap.Lock())
        {
            var source = frame.Pixels;
            var rowBytes = frame.Stride;
            unsafe
            {
                for (var y = 0; y < frame.Size.Height; y++)
                {
                    var destination = new Span<byte>((byte*)locked.Address + y * locked.RowBytes, rowBytes);
                    source.Slice(y * rowBytes, rowBytes).CopyTo(destination);
                }
            }
        }

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        bitmap.Save(stream, new PngBitmapEncoderOptions());
    }

    private string FailureMessage(Exception failure) => failure switch
    {
        UnauthorizedAccessException =>
            $"Recording stopped because '{_batchPath}' is not writable.",
        IOException io when IsDiskFull(io) =>
            "Recording stopped because the disk holding the recording workspace is full.",
        IOException io =>
            $"Recording stopped because a frame could not be written: {io.Message}",
        _ =>
            $"Recording stopped because a frame could not be encoded: {failure.Message}"
    };

    private static bool IsDiskFull(IOException exception) =>
        exception.HResult is 0x27 or 0x70 || exception.Message.Contains("No space left", StringComparison.OrdinalIgnoreCase);
}
