using System;
using System.IO;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows.Other;
using System.Linq;

namespace ScreenToGif.Util;

internal static class StorageUtils
{
    internal static void PurgeCache()
    {
        if (UserSettings.All.AskDeleteCacheWhenClosing && !CacheDialog.Ask(false, out _))
            return;

        try
        {
            var cache = PathHelper.AdjustPath(UserSettings.All.TemporaryFolderResolved);
            var path = Path.Combine(cache, "ScreenToGif");

            Directory.Delete(path, true);

            //The user-defined cache directory may contain user data. It should only be removed if it is empty.
            if (!Directory.EnumerateFileSystemEntries(cache).Any())
                Directory.Delete(cache);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Purging cache");
        }
    }
}