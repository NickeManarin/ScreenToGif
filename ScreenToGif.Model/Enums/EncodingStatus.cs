namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Encoding status.
/// </summary>
public enum EncodingStatus
{
    /// <summary>
    /// Processing encoding/uploading status.
    /// </summary>
    Processing,

    /// <summary>
    /// The Encoding was canceled. So apparently "cancelled" (with two L's) is also a valid grammar. Huh, that's strange.
    /// </summary>
    Canceled,

    /// <summary>
    /// An error happened with the encoding process.
    /// </summary>
    Error,

    /// <summary>
    /// Encoding done.
    /// </summary>
    Completed,

    /// <summary>
    /// File deleted or Moved.
    /// </summary>
    FileDeletedOrMoved
}