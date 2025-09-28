namespace ScreenToGif.Domain.Enums;

public enum GifskiErrorCodes
{
    /// <summary>
    /// Alright.
    /// </summary>
    Ok = 0,

    /// <summary>
    /// One of input arguments was NULL.
    /// </summary>
    NullArgument = 1,

    /// <summary>
    /// A one-time function was called twice, or functions were called in wrong order.
    /// </summary>
    InvalidState = 2,

    /// <summary>
    /// Internal error related to palette quantization.
    /// </summary>
    QuantizationError = 4,

    /// <summary>
    /// Internal error related to gif composing.
    /// </summary>
    GifError = 5,

    /// <summary>
    /// Internal error related to multithreading.
    /// </summary>
    ThreadLost = 6,

    /// <summary>
    /// I/O error: file or directory not found.
    /// </summary>
    NotFound = 7,

    /// <summary>
    /// I/O error: permission denied.
    /// </summary>
    PermissionDenied = 8,

    /// <summary>
    /// I/O error: File already exists.
    /// </summary>
    AlreadyExists = 9,

    /// <summary>
    /// Misc I/O error.
    /// </summary>
    InvalidInput = 10,

    /// <summary>
    /// Misc I/O error.
    /// </summary>
    TimedOut = 11,

    /// <summary>
    /// Misc I/O error.
    /// </summary>
    WriteZero = 12,

    /// <summary>
    /// Misc I/O error.
    /// </summary>
    Interrupted = 13,

    /// <summary>
    /// Misc I/O error.
    /// </summary>
    UnexpectedEof = 14,

    /// <summary>
    /// Should not happen, file a bug.
    /// </summary>
    OtherError = 15
}