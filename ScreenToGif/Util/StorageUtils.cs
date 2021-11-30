using System;
using System.IO;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Util;

internal static class StorageUtils
{
    internal static void PurgeCache()
    {
        if (UserSettings.All.AskDeleteCacheWhenClosing && !CacheDialog.Ask(false, out _))
            return;

        try
        {
            var cache = Other.AdjustPath(UserSettings.All.TemporaryFolderResolved);

            Directory.Delete(cache, true);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Purging cache");
        }
    }
}