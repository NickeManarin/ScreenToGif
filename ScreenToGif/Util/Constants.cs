namespace ScreenToGif.Util
{
    public static class Constants
    {
        /// <summary>
        /// Delimiter chars.
        /// </summary>
        public static readonly char[] DelimiterChars = { '\\', '/' };

        public const int BufferSize = 0x2000;

        public const int LargeBufferSize = BufferSize * 1024;

        #region Recorder

        public static int TopOffset => UserSettings.All.RecorderThinMode ? 6 : 34;

        public static int LeftOffset => UserSettings.All.RecorderThinMode ? 6 : 9;

        public static int RightOffset => UserSettings.All.RecorderThinMode ? 6 : 9;

        public const int BottomOffset = 35;

        public static int HorizontalOffset => LeftOffset + RightOffset;

        public static int VerticalOffset => TopOffset + BottomOffset;

        #endregion

        #region Recorder

        public static int TopBoardOffset => UserSettings.All.RecorderThinMode ? 34 : 63;

        public static int LeftBoardOffset => UserSettings.All.RecorderThinMode ? 4 : 8;

        public static int RightBoardOffset => UserSettings.All.RecorderThinMode ? 4 : 8;

        public const int BottomBoardOffset = 34;

        public static int HorizontalBoardOffset => LeftBoardOffset + RightBoardOffset;

        public static int VerticalBoardOffset => TopBoardOffset + BottomBoardOffset;

        #endregion
    }
}
