namespace ScreenToGif.Util.Enum
{
    /// <summary>
    /// EncoderListBox Item Status.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Normal encoding status.
        /// </summary>
        Encoding,

        /// <summary>
        /// The Encoding was cancelled.
        /// </summary>
        Canceled,

        /// <summary>
        /// An error hapenned with the encoding process.
        /// </summary>
        Error,

        /// <summary>
        /// Encoding done.
        /// </summary>
        Completed,

        /// <summary>
        /// File deleted or Moved.
        /// </summary>
        FileDeletedOrMoved,
    }
}