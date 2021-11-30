namespace ScreenToGif.Util;

public static class OperationalSystemHelper
{
    public static bool IsWin8OrHigher()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version >= new Version(6, 2, 9200, 0);
    }
}