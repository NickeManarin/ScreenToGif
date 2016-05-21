namespace ScreenToGif.Util
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

        public const int TopOffset = 34;

        public const int LeftOffset = 9;

        public const int RightOffset = 9;

        public const int BottomOffset = 35;

        public const int HorizontalOffset = LeftOffset + RightOffset;

        public const int VerticalOffset = TopOffset + BottomOffset;
    }
}
