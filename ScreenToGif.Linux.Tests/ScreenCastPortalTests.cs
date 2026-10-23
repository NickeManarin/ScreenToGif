using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using ScreenToGif.Linux.Services.Capture;
using Tmds.DBus.Protocol;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class ScreenCastPortalTests
{
    [Fact]
    public async Task Persistent_sessions_register_the_app_and_rotate_the_single_use_token()
    {
        await using var portal = await TestPortal.StartAsync();
        portal.ReplacementToken = "next-opaque-token";
        using var session = await ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None,
            portal.Address, rememberSource: true, restoreToken: "previous-opaque-token");
        Assert.Equal("screentogif", portal.RegisteredAppId);
        Assert.True(portal.RegisteredBeforePortalCalls, "The host registry requires registration before other portal calls.");
        Assert.Equal(2u, portal.PersistMode);
        Assert.Equal("previous-opaque-token", portal.ReceivedRestoreToken);
        Assert.Equal("next-opaque-token", session.RestoreToken);
    }

    [Theory]
    [InlineData("org.freedesktop.DBus.Error.UnknownInterface")]
    [InlineData("org.freedesktop.DBus.Error.UnknownMethod")]
    [InlineData("org.freedesktop.portal.Error.InvalidArgument")]
    [InlineData("org.freedesktop.portal.Error.NotAllowed")]
    public async Task Unavailable_host_registration_preserves_ordinary_source_selection(string error)
    {
        await using var portal = await TestPortal.StartAsync();
        portal.RegistrationErrorName = error;
        portal.Version = 3;
        using var session = await ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None,
            portal.Address, rememberSource: true);
        Assert.Equal(42u, session.NodeId);
        Assert.Null(portal.PersistMode);
        Assert.Null(portal.ReceivedRestoreToken);
    }

    [Fact]
    public async Task Unexpected_registration_failure_releases_the_connection()
    {
        await using var portal = await TestPortal.StartAsync();
        portal.RegistrationErrorName = "org.freedesktop.portal.Error.Failed";
        await Assert.ThrowsAsync<ScreenCaptureException>(() => ScreenCastPortalSession.OpenAsync(
            "", false, CancellationToken.None, portal.Address, rememberSource: true));
        await portal.AssertClientGoneAsync();
    }

    [Theory]
    [InlineData(false, 5u)]
    [InlineData(true, 3u)]
    public async Task Disabled_or_unsupported_persistence_never_sends_a_restore_token(bool remember, uint version)
    {
        await using var portal = await TestPortal.StartAsync();
        portal.Version = version;
        portal.ReplacementToken = "unexpected-token";
        using var session = await ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None,
            portal.Address, rememberSource: remember, restoreToken: "must-not-be-sent");
        Assert.Null(portal.PersistMode);
        Assert.Null(portal.ReceivedRestoreToken);
        Assert.Null(session.RestoreToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_during_registration_rejects_late_success_and_failure(bool fail)
    {
        await using var portal = await TestPortal.StartAsync();
        portal.HoldStage = "Register";
        using var canceled = new CancellationTokenSource();
        var pending = ScreenCastPortalSession.OpenAsync("", false, canceled.Token, portal.Address, rememberSource: true);
        await portal.Reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
        portal.FailHeldStage = fail;
        portal.Release.TrySetResult();
        await portal.LateReplySent.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await portal.AssertClientGoneAsync();
    }

    [Theory]
    [InlineData("x11", "XID", false)]
    [InlineData("wayland", "XID", true)]
    [InlineData("Wayland", "XID", true)]
    [InlineData(null, "Wayland", true)]
    public void Session_capability_never_mistakes_XWayland_for_X11(string? sessionType, string descriptor, bool expected) =>
        Assert.Equal(expected, WaylandScreenSource.IsWaylandSession(sessionType, descriptor));

    [Theory]
    [InlineData(false, 1u)]
    [InlineData(true, 2u)]
    public async Task A_session_parses_streams_and_releases_its_connection_and_remote(bool cursor, uint expectedMode)
    {
        await using var portal = await TestPortal.StartAsync();
        using var session = await ScreenCastPortalSession.OpenAsync("x11:1234", cursor, CancellationToken.None, portal.Address);
        Assert.Equal(42u, session.NodeId);
        Assert.Equal(123456ul, session.Serial);
        Assert.Equal(expectedMode, portal.CursorMode);
        Assert.Equal("x11:1234", portal.Parent);
        Assert.True(File.Exists($"/proc/self/fd/{session.RemoteFd}"));
        Assert.False(session.IsClosed);
        session.Dispose();
        session.Dispose();
        await portal.AssertClientGoneAsync();
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public async Task Consent_cancel_and_decline_allow_a_new_session(uint response)
    {
        await using var portal = await TestPortal.StartAsync();
        portal.ResponseCode = response;
        var failed = ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None, portal.Address);
        if (response == 1) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => failed);
        else await Assert.ThrowsAsync<ScreenCaptureException>(() => failed);
        await portal.AssertClientGoneAsync();
        portal.ResponseCode = 0;
        using var retry = await ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None, portal.Address);
        Assert.Equal(42u, retry.NodeId);
    }

    [Theory]
    [InlineData("GetAll", false)]
    [InlineData("GetAll", true)]
    [InlineData("CreateSession", false)]
    [InlineData("CreateSession", true)]
    [InlineData("SelectSources", false)]
    [InlineData("SelectSources", true)]
    [InlineData("Start", false)]
    [InlineData("Start", true)]
    [InlineData("OpenPipeWireRemote", false)]
    [InlineData("OpenPipeWireRemote", true)]
    public async Task Closing_during_each_preparation_boundary_rejects_late_completion(string stage, bool lateFailure)
    {
        await using var portal = await TestPortal.StartAsync();
        portal.HoldStage = stage;
        using var canceled = new CancellationTokenSource();
        var pending = ScreenCastPortalSession.OpenAsync("", false, canceled.Token, portal.Address);
        await portal.Reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
        portal.FailHeldStage = lateFailure;
        portal.Release.TrySetResult();
        await portal.LateReplySent.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await portal.AssertClientGoneAsync();
        portal.HoldStage = null;
        portal.FailHeldStage = false;
        using var retry = await ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None, portal.Address);
        Assert.Equal(42u, retry.NodeId);
    }

    [Fact]
    public async Task Cancellation_before_connect_does_not_open_a_session()
    {
        await using var portal = await TestPortal.StartAsync();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ScreenCastPortalSession.OpenAsync("", false, canceled.Token, portal.Address));
        Assert.Equal(0, portal.RequestCount);
    }

    [Fact]
    public async Task Portal_restart_while_waiting_for_consent_fails_and_allows_retry()
    {
        await using var portal = await TestPortal.StartAsync();
        portal.DropStartResponse = true;
        var pending = ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None, portal.Address);
        await portal.Reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await portal.LoseNameAsync();
        await Assert.ThrowsAsync<ScreenCaptureException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
        await portal.AssertClientGoneAsync();
        portal.DropStartResponse = false;
        await portal.OwnNameAsync();
        using var retry = await ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None, portal.Address);
        Assert.Equal(42u, retry.NodeId);
    }

    [Fact]
    public async Task Session_closed_signals_are_seen_even_after_start()
    {
        await using var portal = await TestPortal.StartAsync();
        using var session = await ScreenCastPortalSession.OpenAsync("", false, CancellationToken.None, portal.Address);
        portal.EndSession();
        var timeout = Stopwatch.StartNew();
        while (!session.IsClosed && timeout.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(10);
        Assert.True(session.IsClosed);
    }

    [Fact]
    public async Task Missing_cursor_capability_is_actionable_and_does_not_start_a_session()
    {
        await using var portal = await TestPortal.StartAsync();
        portal.CursorModes = 1;
        var failure = await Assert.ThrowsAsync<ScreenCaptureException>(() =>
            ScreenCastPortalSession.OpenAsync("", true, CancellationToken.None, portal.Address));
        Assert.Contains("Turn off Show cursor", failure.Message);
        Assert.Equal(0, portal.RequestCount);
    }

    [Fact]
    public async Task Metadata_only_cursor_mode_does_not_suggest_an_unsupported_setting()
    {
        await using var portal = await TestPortal.StartAsync();
        portal.CursorModes = 4;
        var failure = await Assert.ThrowsAsync<ScreenCaptureException>(() =>
            ScreenCastPortalSession.OpenAsync("", true, CancellationToken.None, portal.Address));
        Assert.Contains("cursor metadata mode is not supported", failure.Message);
        Assert.Equal(0, portal.RequestCount);
    }

    private sealed class TestPortal : IPathMethodHandler, IAsyncDisposable
    {
        private readonly Process _daemon;
        private readonly DBusConnection _service;
        private string? _client;
        private string? _session;
        internal string Address { get; }
        public string Path => "/org/freedesktop/portal/desktop";
        public bool HandlesChildPaths => true;
        internal string? HoldStage { get; set; }
        internal bool FailHeldStage { get; set; }
        internal bool DropStartResponse { get; set; }
        internal uint ResponseCode { get; set; }
        internal uint CursorModes { get; set; } = 3;
        internal uint Version { get; set; } = 5;
        internal string? RegisteredAppId { get; private set; }
        internal string? RegistrationErrorName { get; set; }
        internal bool RegisteredBeforePortalCalls { get; private set; }
        private bool _sawPortalCall;
        internal uint? PersistMode { get; private set; }
        internal string? ReceivedRestoreToken { get; private set; }
        internal string? ReplacementToken { get; set; }
        internal uint CursorMode { get; private set; }
        internal string? Parent { get; private set; }
        internal int RequestCount { get; private set; }
        internal TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource LateReplySent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private TestPortal(Process daemon, string address)
        {
            _daemon = daemon;
            Address = address;
            _service = new DBusConnection(address);
        }

        internal static async Task<TestPortal> StartAsync()
        {
            var daemon = Process.Start(new ProcessStartInfo("dbus-daemon", "--session --nofork --print-address=1")
                { RedirectStandardOutput = true, RedirectStandardError = true })!;
            TestPortal? portal = null;
            try
            {
                var address = await daemon.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
                if (string.IsNullOrEmpty(address)) throw new IOException("A private dbus-daemon could not start.");
                portal = new TestPortal(daemon, address);
                await portal._service.ConnectAsync();
                portal._service.AddMethodHandler(portal);
                await portal._service.RequestNameAsync("org.freedesktop.portal.Desktop");
                return portal;
            }
            catch
            {
                if (portal is not null) await portal.DisposeAsync();
                else { if (!daemon.HasExited) daemon.Kill(true); daemon.Dispose(); }
                throw;
            }
        }

        public async ValueTask HandleMethodAsync(MethodContext context)
        {
            var request = Parse(context);
            if (request.Method == HoldStage)
            {
                Reached.TrySetResult();
                await Release.Task;
            }
            if (request.Method == HoldStage && FailHeldStage)
                context.ReplyError("org.freedesktop.portal.Error.Failed", "Controlled late failure");
            else Reply(context, request);
            if (request.Method == HoldStage) LateReplySent.TrySetResult();
        }

        private ParsedRequest Parse(MethodContext context)
        {
            _client = context.Request.SenderAsString;
            var method = context.Request.MemberAsString!;
            var reader = context.Request.GetBodyReader();
            if (method != "Register") _sawPortalCall = true;
            if (method == "GetAll") return new(method, null, new());
            if (method == "Register")
            {
                RegisteredBeforePortalCalls = !_sawPortalCall;
                RegisteredAppId = reader.ReadString();
                return new(method, _client, reader.ReadDictionaryOfStringToVariantValue());
            }
            if (method != "CreateSession") reader.ReadObjectPath();
            if (method == "Start") Parent = reader.ReadString();
            var options = reader.ReadDictionaryOfStringToVariantValue();
            if (method == "SelectSources")
            {
                CursorMode = options["cursor_mode"].GetUInt32();
                PersistMode = options.TryGetValue("persist_mode", out var mode) ? mode.GetUInt32() : null;
                ReceivedRestoreToken = options.TryGetValue("restore_token", out var token) ? token.GetString() : null;
                Assert.Equal(3u, options["types"].GetUInt32());
                Assert.False(options["multiple"].GetBool());
            }
            if (method == "CreateSession")
            {
                var owner = _client![1..].Replace('.', '_');
                _session = $"{Path}/session/{owner}/{options["session_handle_token"].GetString()}";
            }
            return new(method, _client, options);
        }

        private void Reply(MethodContext context, ParsedRequest request)
        {
            if (request.Method == "Register")
            {
                if (RegistrationErrorName is { } error)
                {
                    context.ReplyError(error, "Controlled registration error");
                    return;
                }
                using var registered = context.CreateReplyWriter("");
                context.Reply(registered.CreateMessage());
                return;
            }
            if (request.Method == "GetAll")
            {
                using var properties = context.CreateReplyWriter("a{sv}");
                properties.WriteDictionary(new Dictionary<string, VariantValue>
                    { ["AvailableSourceTypes"] = 3u, ["AvailableCursorModes"] = CursorModes, ["version"] = Version });
                context.Reply(properties.CreateMessage());
                return;
            }
            if (request.Method == "OpenPipeWireRemote")
            {
                using var fd = File.OpenHandle("/dev/null");
                using var reply = context.CreateReplyWriter("h");
                reply.WriteHandle(fd);
                context.Reply(reply.CreateMessage());
                return;
            }
            RequestCount++;
            var owner = request.Client![1..].Replace('.', '_');
            var path = $"{Path}/request/{owner}/{request.Options["handle_token"].GetString()}";
            var values = new Dictionary<string, VariantValue>();
            if (request.Method == "CreateSession") values["session_handle"] = _session!;
            if (request.Method == "Start") values["streams"] = new Tmds.DBus.Protocol.Array<Struct<uint, Dict<string, VariantValue>>>
            {
                new(42u, new Dict<string, VariantValue> { ["pipewire-serial"] = 123456ul,
                    ["size"] = new Struct<int, int>(64, 48) })
            };
            if (request.Method == "Start" && ReplacementToken is { } replacement)
                values["restore_token"] = replacement;
            // Signal before method reply deliberately exercises the subscribe-before-call race.
            using var signal = _service.GetMessageWriter();
            signal.WriteSignalHeader(request.Client, path, "org.freedesktop.portal.Request", "Response", "ua{sv}");
            signal.WriteUInt32(request.Method == "Start" ? ResponseCode : 0u);
            signal.WriteDictionary(values);
            if (request.Method != "Start" || !DropStartResponse)
                _service.TrySendMessage(signal.CreateMessage());
            using var result = context.CreateReplyWriter("o");
            result.WriteObjectPath(path);
            context.Reply(result.CreateMessage());
            if (request.Method == "Start" && DropStartResponse) Reached.TrySetResult();
        }

        internal Task LoseNameAsync() => _service.ReleaseNameAsync("org.freedesktop.portal.Desktop");
        internal Task OwnNameAsync() => _service.RequestNameAsync("org.freedesktop.portal.Desktop");

        internal void EndSession()
        {
            using var writer = _service.GetMessageWriter();
            writer.WriteSignalHeader(_client, _session!, "org.freedesktop.portal.Session", "Closed", "a{sv}");
            writer.WriteDictionary(new Dictionary<string, VariantValue>());
            _service.TrySendMessage(writer.CreateMessage());
        }

        internal async Task AssertClientGoneAsync()
        {
            Assert.NotNull(_client);
            var deadline = Stopwatch.StartNew();
            while (await _service.CallMethodAsync(HasOwnerMessage(_client!), static (message, _) => message.GetBodyReader().ReadBool()))
            {
                Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(10), "The session's dedicated bus connection survived disposal.");
                await Task.Delay(10);
            }
        }

        private MessageBuffer HasOwnerMessage(string name)
        {
            using var writer = _service.GetMessageWriter();
            writer.WriteMethodCallHeader("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", "s");
            writer.WriteString(name);
            return writer.CreateMessage();
        }

        public async ValueTask DisposeAsync()
        {
            Release.TrySetResult();
            _service.Dispose();
            if (!_daemon.HasExited) _daemon.Kill(entireProcessTree: true);
            await _daemon.WaitForExitAsync();
            _daemon.Dispose();
        }

        private sealed record ParsedRequest(string Method, string? Client, Dictionary<string, VariantValue> Options);
    }
}
