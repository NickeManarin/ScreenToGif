using System.ComponentModel;
using ScreenToGif.Properties;

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

        public static int TopOffset => Settings.Default.RecorderThinMode ? 6 : 34;

        public static int LeftOffset => Settings.Default.RecorderThinMode ? 6 : 9;

        public static int RightOffset => Settings.Default.RecorderThinMode ? 6 : 9;

        public const int BottomOffset = 35;

        public static int HorizontalOffset => LeftOffset + RightOffset;

        public static int VerticalOffset => TopOffset + BottomOffset;

        #endregion

        #region Recorder

        public static int TopBoardOffset => Settings.Default.RecorderThinMode ? 34 : 63;

        public static int LeftBoardOffset => Settings.Default.RecorderThinMode ? 4 : 8;

        public static int RightBoardOffset => Settings.Default.RecorderThinMode ? 4 : 8;

        public const int BottomBoardOffset = 34;

        public static int HorizontalBoardOffset => LeftBoardOffset + RightBoardOffset;

        public static int VerticalBoardOffset => TopBoardOffset + BottomBoardOffset;

        #endregion
    }
}
