namespace ScreenToGif.Encoding
{
    /// <summary>
    /// Constants for the .net Gif encoding process.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Delimiter chars.
        /// </summary>
        public static readonly char[] DelimiterChars = { '\\', '/' };

        public const int BufferSize = 0x2000;

        public const int LargeBufferSize = BufferSize * 1024;
    }
}
