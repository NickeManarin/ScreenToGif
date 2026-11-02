namespace ScreenToGif.Linux.Services;

public enum EditorOperationStatus
{
    Completed,
    Busy,
    Canceled,
    Failed
}

public sealed record EditorOperationResult(
    EditorOperationStatus Status,
    Exception? Error = null,
    bool CloseRequested = false);

/// <summary>
/// Serializes long-running editor commands and owns their cancellation/close handoff.
/// The window supplies only the UI state transitions around the operation.
/// </summary>
public sealed class EditorOperationCoordinator
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cancellation;
    private bool _closeRequested;

    public bool IsRunning => _cancellation is not null;

    public bool RequestCancellation()
    {
        if (_cancellation is not { IsCancellationRequested: false } cancellation)
            return false;
        cancellation.Cancel();
        return true;
    }

    public bool RequestClose()
    {
        if (_cancellation is null)
            return false;
        _closeRequested = true;
        RequestCancellation();
        return true;
    }

    public async Task<EditorOperationResult> RunAsync(
        Func<CancellationToken, Task> operation,
        Action onStarted,
        Action onFinished)
    {
        if (!_gate.Wait(0))
            return new EditorOperationResult(EditorOperationStatus.Busy);

        _cancellation = new CancellationTokenSource();
        EditorOperationStatus status;
        Exception? error = null;
        try
        {
            onStarted();
            await operation(_cancellation.Token);
            status = EditorOperationStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            status = EditorOperationStatus.Canceled;
        }
        catch (Exception ex)
        {
            status = EditorOperationStatus.Failed;
            error = ex;
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            _gate.Release();
            onFinished();
        }

        var closeRequested = _closeRequested;
        _closeRequested = false;
        return new EditorOperationResult(status, error, closeRequested);
    }
}
