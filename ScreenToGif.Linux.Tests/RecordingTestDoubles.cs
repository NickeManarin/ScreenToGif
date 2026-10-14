using Avalonia;
using ScreenToGif.Linux.Services.Capture;
using System.Buffers;

namespace ScreenToGif.Linux.Tests;

/// <summary>A clock the test moves by hand, so pacing can be driven in irregular steps.</summary>
internal sealed class FakeRecordingClock : IRecordingClock
{
    public long ElapsedMilliseconds { get; private set; }

    public void Advance(long milliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(milliseconds);
        ElapsedMilliseconds += milliseconds;
    }
}

/// <summary>A screen source that hands out solid frames, and can be told to start failing.</summary>
internal sealed class FakeScreenSource(PixelSize size) : IScreenSource
{
    private readonly List<PixelRect> _requests = [];

    public IReadOnlyList<PixelRect> Requests => _requests;

    public int CaptureCount { get; private set; }

    public bool LastRequestedCursor { get; private set; }

    public bool IsDisposed { get; private set; }

    /// <summary>When set, the next and every later capture throws this message.</summary>
    public string? FailWith { get; set; }

    public CapturedFrame Capture(PixelRect region, bool includeCursor)
    {
        _requests.Add(region);
        LastRequestedCursor = includeCursor;

        if (FailWith is { } message)
            throw new ScreenCaptureException(message);

        CaptureCount++;
        var stride = size.Width * 4;
        var pixels = ArrayPool<byte>.Shared.Rent(stride * size.Height);
        Array.Fill(pixels, (byte)0x7F, 0, stride * size.Height);
        return new CapturedFrame(size, stride, pixels);
    }

    public void Dispose() => IsDisposed = true;
}

/// <summary>A frame writer that records what it was handed without touching the disk.</summary>
internal sealed class FakeFrameWriter : IFrameWriter
{
    private readonly List<string> _paths = [];

    public IReadOnlyList<string> Paths => _paths;

    public int DiscardCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public ScreenCaptureException? Failure { get; set; }

    /// <summary>Settable, so a test can put the session in front of an encoder falling behind.</summary>
    public long PendingBytes { get; set; }

    public void Write(CapturedFrame frame)
    {
        using (frame)
            _paths.Add($"frame-{_paths.Count:000000}.png");
    }

    public Task<IReadOnlyList<string>> CompleteAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(_paths.ToArray());

    public void Discard()
    {
        DiscardCount++;
        _paths.Clear();
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
