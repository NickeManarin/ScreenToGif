using Avalonia;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>Owns portal consent and the native consumer for exactly one selected source.</summary>
internal sealed class WaylandScreenSource : IScreenSource, IRecordingClock
{
    private readonly ScreenCastPortalSession _portal;
    private readonly GStreamerScreenSource _stream;
    private bool _disposed;

    private WaylandScreenSource(ScreenCastPortalSession portal, GStreamerScreenSource stream)
    {
        _portal = portal;
        _stream = stream;
    }

    internal static bool IsWaylandSession(string? sessionType, string? handleDescriptor) =>
        string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(handleDescriptor, "Wayland", StringComparison.OrdinalIgnoreCase);

    internal static async Task<WaylandScreenSource> OpenAsync(string parentWindow, RecordingSettings settings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var remember = WaylandSourceReusePolicy.IsAllowed;
        var tokens = new ScreenCastRestoreTokenStore();
        var restoreToken = remember ? tokens.Take() : null;
        var portal = await ScreenCastPortalSession.OpenAsync(parentWindow, settings.ShowCursor, cancellationToken,
            rememberSource: remember, restoreToken: restoreToken);
        GStreamerScreenSource? stream = null;
        try
        {
            stream = await Task.Run(() => GStreamerScreenSource.OpenPipeWire(portal.RemoteFd, portal.NodeId,
                portal.Serial, settings.Mode == RecorderCaptureMode.Manual ? 100 : settings.CaptureIntervalMilliseconds, cancellationToken), CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
            if (portal.IsClosed)
                throw new ScreenCaptureException("Screen sharing ended before recording started. Choose the source again.");
            if (remember && WaylandSourceReusePolicy.IsAllowed) tokens.Save(portal.RestoreToken);
            return new WaylandScreenSource(portal, stream);
        }
        catch
        {
            stream?.Dispose();
            portal.Dispose();
            throw;
        }
    }

    internal PixelSize Size => _stream.Size;
    public long ElapsedMilliseconds => _stream.ElapsedMilliseconds;

    public CapturedFrame Capture(PixelRect region, bool includeCursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_portal.IsClosed)
            throw new ScreenCaptureException("Screen sharing ended. Stop to keep the recording, then choose a source again.");
        return _stream.Capture(region, includeCursor);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _stream.Dispose(); }
        finally { _portal.Dispose(); }
    }
}
