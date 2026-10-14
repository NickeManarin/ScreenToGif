namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// Consumes captured frames and yields the files they were encoded into, in capture order.
/// The session hands every frame over and never touches its pixels again, so encoding can run
/// off the capture loop.
/// </summary>
public interface IFrameWriter : IAsyncDisposable
{
    /// <summary>
    /// The first failure the writer hit, or null. The session polls this to fault promptly, and
    /// shows its message to the user, so it must already read as plain words.
    /// </summary>
    ScreenCaptureException? Failure { get; }

    /// <summary>
    /// Bytes of frames handed over and not yet encoded. Encoding runs off the capture loop, so a
    /// capture rate the encoder cannot keep up with queues instead of slowing down. The session
    /// watches this so it can say so while the recording is still savable, rather than letting the
    /// backlog grow until the process is killed and the recording goes with it.
    /// </summary>
    long PendingBytes { get; }

    /// <summary>Takes ownership of <paramref name="frame"/> and disposes it once encoded.</summary>
    void Write(CapturedFrame frame);

    /// <summary>
    /// Finishes encoding everything already handed over and returns the file paths in order.
    /// A writer that failed part way returns the frames it did encode rather than throwing them
    /// away; <see cref="Failure"/> is where the caller learns that something went wrong.
    /// </summary>
    Task<IReadOnlyList<string>> CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops every frame written so far, deleting only this recording's own files.</summary>
    void Discard();
}
