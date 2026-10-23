using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>A dedicated bus connection owns one sharing session and its remote FD.</summary>
internal sealed class ScreenCastPortalSession : IDisposable
{
    private const string Destination = "org.freedesktop.portal.Desktop";
    private const string Desktop = "/org/freedesktop/portal/desktop";
    private const string ScreenCast = "org.freedesktop.portal.ScreenCast";
    private readonly DBusConnection _connection;
    private IDisposable? _closedWatch;
    private SafeFileHandle? _remote;
    private string? _sessionPath;
    private int _disposed;
    private int _closed;

    private ScreenCastPortalSession(string busAddress) => _connection = new DBusConnection(busAddress);

    internal uint NodeId { get; private set; }
    internal ulong? Serial { get; private set; }
    internal string? RestoreToken { get; private set; }
    internal int RemoteFd => checked((int)(_remote ?? throw new ObjectDisposedException(nameof(ScreenCastPortalSession))).DangerousGetHandle());
    internal bool IsClosed => Volatile.Read(ref _closed) != 0;

    internal static async Task<ScreenCastPortalSession> OpenAsync(string parentWindow, bool showCursor,
        CancellationToken cancellationToken, string? busAddress = null, bool rememberSource = false,
        string? restoreToken = null)
    {
        var address = busAddress ?? DBusAddress.Session;
        if (string.IsNullOrEmpty(address))
            throw new ScreenCaptureException("The desktop session bus is unavailable. Sign in to a desktop session with a ScreenCast portal.");
        var session = new ScreenCastPortalSession(address);
        // Disconnecting also makes the portal close all this client's pending requests/sessions.
        using var canceled = cancellationToken.Register(session.CancelConnection);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await session._connection.ConnectAsync().ConfigureAwait(false);
            if (rememberSource)
            {
                try { await session._connection.CallMethodAsync(session.RegisterMessage()).ConfigureAwait(false); }
                catch (DBusErrorReplyException exception) when (exception.ErrorName is
                    "org.freedesktop.DBus.Error.UnknownInterface" or "org.freedesktop.DBus.Error.UnknownMethod" or
                    "org.freedesktop.portal.Error.InvalidArgument" or "org.freedesktop.portal.Error.NotAllowed")
                { } // Older portals and launchers may supply their own identity instead.
            }
            var properties = await session._connection.CallMethodAsync(
                session.GetPropertiesMessage(), static (message, _) =>
                    message.GetBodyReader().ReadDictionaryOfStringToVariantValue()).ConfigureAwait(false);
            var types = GetUInt(properties, "AvailableSourceTypes") & 3u;
            var modes = GetUInt(properties, "AvailableCursorModes");
            var cursor = showCursor ? 2u : 1u;
            if (types == 0)
                throw new ScreenCaptureException("The desktop portal offers no monitor or window source. Check your desktop's ScreenCast portal backend.");
            if ((modes & 3u) == 0)
                throw new ScreenCaptureException("This desktop does not provide hidden or embedded cursor frames. Its cursor metadata mode is not supported by this Recorder.");
            if ((modes & cursor) == 0)
                throw new ScreenCaptureException(showCursor
                    ? "This desktop cannot embed the cursor in shared frames. Turn off Show cursor in Recorder options and try again."
                    : "This desktop cannot hide the cursor in shared frames. Turn on Show cursor in Recorder options and try again.");

            var token = "stg_" + Guid.NewGuid().ToString("N");
            var owner = session._connection.UniqueName![1..].Replace('.', '_');
            session._sessionPath = $"/org/freedesktop/portal/desktop/session/{owner}/{token}";
            session._closedWatch = await session._connection.AddMatchAsync(
                new MatchRule { Type = MessageType.Signal, Sender = Destination,
                    Path = session._sessionPath, Interface = "org.freedesktop.portal.Session", Member = "Closed" },
                _ => Interlocked.Exchange(ref session._closed, 1), emitOnCapturedContext: false,
                flags: ObserverFlags.EmitAll).ConfigureAwait(false);

            var created = await session.RequestAsync("CreateSession", null, null,
                new() { ["session_handle_token"] = token }).ConfigureAwait(false);
            if (!created.TryGetValue("session_handle", out var handle) ||
                handle.GetString() != session._sessionPath)
                throw new ScreenCaptureException("The desktop portal returned an unexpected sharing session. Try again after restarting the portal.");
            var selection = new Dictionary<string, VariantValue>
                { ["types"] = types, ["multiple"] = false, ["cursor_mode"] = cursor };
            var persistent = rememberSource && GetUInt(properties, "version") >= 4;
            if (persistent)
            {
                selection["persist_mode"] = 2u;
                if (!string.IsNullOrWhiteSpace(restoreToken)) selection["restore_token"] = restoreToken;
            }
            await session.RequestAsync("SelectSources", session._sessionPath, null, selection).ConfigureAwait(false);
            var started = await session.RequestAsync("Start", session._sessionPath, parentWindow, new()).ConfigureAwait(false);
            if (persistent && started.TryGetValue("restore_token", out var replacement))
                session.RestoreToken = replacement.GetString();
            if (!started.TryGetValue("streams", out var streams) || streams.Count != 1)
                throw new ScreenCaptureException("Select exactly one monitor or window to record.");
            var stream = streams.GetItem(0);
            session.NodeId = stream.GetItem(0).GetUInt32();
            var streamProperties = stream.GetItem(1).GetDictionary<string, VariantValue>();
            if (streamProperties.TryGetValue("pipewire-serial", out var serial))
                session.Serial = serial.GetUInt64();
            // Consume the FD-valued reply even when cancellation races it; never abandon this task.
            session._remote = await session._connection.CallMethodAsync(session.OpenRemoteMessage(),
                static (message, _) => message.GetBodyReader().ReadHandle<SafeFileHandle>()).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (session.IsClosed)
                throw new ScreenCaptureException("Screen sharing ended before recording started. Select the source again.");
            return session;
        }
        catch (Exception exception)
        {
            session.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
            if (exception is ScreenCaptureException or OperationCanceledException) throw;
            throw new ScreenCaptureException("The desktop could not start screen sharing. Check that xdg-desktop-portal and your desktop's ScreenCast backend are running, then try again.", exception);
        }
    }

    private static uint GetUInt(Dictionary<string, VariantValue> properties, string key) =>
        properties.TryGetValue(key, out var value) ? value.GetUInt32() : 0;

    private MessageBuffer GetPropertiesMessage()
    {
        using var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(Destination, Desktop, "org.freedesktop.DBus.Properties", "GetAll", "s");
        writer.WriteString(ScreenCast);
        return writer.CreateMessage();
    }

    private MessageBuffer RegisterMessage()
    {
        using var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(Destination, Desktop, "org.freedesktop.host.portal.Registry", "Register", "sa{sv}");
        writer.WriteString("screentogif");
        writer.WriteDictionary(new Dictionary<string, VariantValue>());
        return writer.CreateMessage();
    }

    private MessageBuffer OpenRemoteMessage()
    {
        using var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(Destination, Desktop, ScreenCast, "OpenPipeWireRemote", "oa{sv}");
        writer.WriteObjectPath(_sessionPath!);
        writer.WriteDictionary(new Dictionary<string, VariantValue>());
        return writer.CreateMessage();
    }

    private async Task<Dictionary<string, VariantValue>> RequestAsync(string method, string? session,
        string? parent, Dictionary<string, VariantValue> options)
    {
        var token = "stg_" + Guid.NewGuid().ToString("N");
        options["handle_token"] = token;
        var owner = _connection.UniqueName![1..].Replace('.', '_');
        var path = $"/org/freedesktop/portal/desktop/request/{owner}/{token}";
        var response = new TaskCompletionSource<PortalResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watch = await _connection.AddMatchAsync(
            new MatchRule { Type = MessageType.Signal, Sender = Destination, Path = path,
                Interface = "org.freedesktop.portal.Request", Member = "Response" },
            static (message, _) =>
            {
                var reader = message.GetBodyReader();
                return new PortalResponse(reader.ReadUInt32(), reader.ReadDictionaryOfStringToVariantValue());
            }, notification =>
            {
                if (notification.HasValue) response.TrySetResult(notification.Value);
                else response.TrySetException(notification.Exception ?? new IOException("The desktop portal disconnected."));
            }, emitOnCapturedContext: false, flags: ObserverFlags.EmitAll).ConfigureAwait(false);
        var actualPath = await _connection.CallMethodAsync(RequestMessage(method, session, parent, options),
            static (message, _) => message.GetBodyReader().ReadObjectPathAsString()).ConfigureAwait(false);
        if (actualPath != path)
            throw new ScreenCaptureException("The desktop portal does not support predictable sharing requests. Update the portal and try again.");
        var result = await response.Task.ConfigureAwait(false);
        if (result.Code == 1)
            throw new OperationCanceledException("Screen sharing was canceled. Press Record to choose a source again.");
        if (result.Code != 0)
            throw new ScreenCaptureException("Screen sharing was declined or could not start. Press Record to choose a source again.");
        return result.Values;
    }

    private MessageBuffer RequestMessage(string method, string? session, string? parent,
        Dictionary<string, VariantValue> options)
    {
        using var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(Destination, Desktop, ScreenCast, method,
            session is null ? "a{sv}" : parent is null ? "oa{sv}" : "osa{sv}");
        if (session is not null) writer.WriteObjectPath(session);
        if (parent is not null) writer.WriteString(parent);
        writer.WriteDictionary(options);
        return writer.CreateMessage();
    }

    private void CancelConnection()
    {
        Interlocked.Exchange(ref _closed, 1);
        _connection.Dispose();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Interlocked.Exchange(ref _closed, 1);
        // The dedicated connection disappearing closes the session even if CreateSession or an FD
        // reply is in flight. No shared Avalonia D-Bus connection is affected.
        _connection.Dispose();
        _closedWatch?.Dispose();
        _remote?.Dispose();
    }

    private sealed record PortalResponse(uint Code, Dictionary<string, VariantValue> Values);
}
