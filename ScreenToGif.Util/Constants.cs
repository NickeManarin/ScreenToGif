using ScreenToGif.Util.Settings;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util;

public static class Constants
{
    /// <summary>
    /// Delimiter chars.
    /// </summary>
    public static readonly char[] DelimiterChars = { '\\', '/' };

    public const int BufferSize = 0x2000;

    public const int LargeBufferSize = BufferSize * 1024;

    #region Recorder

    public static int TopOffset => UserSettings.All.RecorderThinMode ? 4 : 33;

    public static int LeftOffset => UserSettings.All.RecorderThinMode ? 4 : 8;

    public static int RightOffset => UserSettings.All.RecorderThinMode ? 4 : 8;

    public const int BottomOffset = 33;

    public static int HorizontalOffset => LeftOffset + RightOffset;

    public static int VerticalOffset => TopOffset + BottomOffset;

    #endregion

    #region Board Recorder

    public static int TopBoardOffset => UserSettings.All.RecorderThinMode ? 34 : 65;

    public static int LeftBoardOffset => UserSettings.All.RecorderThinMode ? 4 : 8;

    public static int RightBoardOffset => UserSettings.All.RecorderThinMode ? 4 : 8;

    public const int BottomBoardOffset = 32;

    public static int HorizontalBoardOffset => LeftBoardOffset + RightBoardOffset;

    public static int VerticalBoardOffset => TopBoardOffset + BottomBoardOffset;

    #endregion

    #region Colors

    public static Color VeryLightEven = Color.FromArgb(255, 245, 245, 245);
    public static Color VeryLightOdd = Color.FromArgb(255, 240, 240, 240);

    public static Color LightEven = Color.FromArgb(255, 255, 255, 255);
    public static Color LightOdd = Color.FromArgb(255, 211, 211, 211);

    public static Color MediumEven = Color.FromArgb(255, 153, 153, 153);
    public static Color MediumOdd = Color.FromArgb(255, 102, 102, 102);

    public static Color DarkEven = Color.FromArgb(255, 45, 45, 45);
    public static Color DarkOdd = Color.FromArgb(255, 50, 50, 50);

    #endregion
}