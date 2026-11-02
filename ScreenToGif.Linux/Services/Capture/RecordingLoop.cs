using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// Runs one <see cref="RecordingSession"/> on a dedicated thread, so grabbing and encoding never
/// touch the UI thread. Every command reaches the session on that same thread through the queue
/// below, which is what lets the session stay single-threaded and lock-free.
/// </summary>
public sealed class RecordingLoop : IAsyncDisposable
{
    private readonly RecordingSession _session;
    private readonly Action<RecordingStatus> _statusChanged;
    private readonly Action<RecordingErrorEventArgs> _failed;
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action<RecordingSession>> _commands = new();
    private readonly ManualResetEventSlim _wake = new(false);
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Thread _thread;
    private bool _disposed;

    /// <param name="statusChanged">Called on the recording thread whenever the session's reading changes.</param>
    /// <param name="failed">Called on the recording thread when the session reports a failure.</param>
    public RecordingLoop(
        RecordingSession session,
        Action<RecordingStatus> statusChanged,
        Action<RecordingErrorEventArgs> failed)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _statusChanged = statusChanged ?? throw new ArgumentNullException(nameof(statusChanged));
        _failed = failed ?? throw new ArgumentNullException(nameof(failed));

        _session.StateChanged += SessionStateChanged;
        _session.Error += SessionFailed;

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "ScreenToGif recording"
        };
        _thread.Start();
    }

    /// <summary>Queues work against the session and wakes the recording thread to run it.</summary>
    public void Post(Action<RecordingSession> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_disposed)
            return;

        _commands.Enqueue(command);
        _wake.Set();
    }

    /// <summary>Ends the recording on the recording thread and returns whatever it yielded.</summary>
    public Task<IReadOnlyList<EditorFrame>> StopAsync()
    {
        var completion = new TaskCompletionSource<IReadOnlyList<EditorFrame>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Post(session =>
        {
            try
            {
                completion.SetResult(session.StopAsync().GetAwaiter().GetResult());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        return completion.Task;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _cancellation.CancelAsync();
        _wake.Set();
        _thread.Join(TimeSpan.FromSeconds(10));

        _session.StateChanged -= SessionStateChanged;
        _session.Error -= SessionFailed;
        await _session.DisposeAsync();

        _cancellation.Dispose();
        _wake.Dispose();
    }

    private void Run()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            while (_commands.TryDequeue(out var command))
            {
                if (_cancellation.IsCancellationRequested)
                    return;

                RunGuarded(command);
            }

            _wake.Reset();

            // Anything queued between the drain and the reset would otherwise wait for the timeout.
            if (!_commands.IsEmpty)
                continue;

            var waitMilliseconds = Timeout.Infinite;
            RunGuarded(session => waitMilliseconds = session.Tick());

            try
            {
                _wake.Wait(waitMilliseconds, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void RunGuarded(Action<RecordingSession> work)
    {
        try
        {
            work(_session);
        }
        catch (ObjectDisposedException)
        {
            // The session is gone; the loop is on its way out.
        }
        catch (Exception exception)
        {
            _failed(new RecordingErrorEventArgs(
                $"The recording stopped unexpectedly: {exception.Message}", exception));
        }
    }

    private void SessionStateChanged(object? sender, EventArgs e) => _statusChanged(_session.Snapshot());

    private void SessionFailed(object? sender, RecordingErrorEventArgs e) => _failed(e);
}
