using System;
using ScreenToGif.Model;

namespace ScreenToGif.Util
{
    internal static class Global
    {
        internal static DateTime StartupDateTime { get; set; }

        /// <summary>
        /// When it's true, the global shortcuts won't work.
        /// </summary>
        internal static bool IgnoreHotKeys { get; set; }

        /// <summary>
        /// When it's true, the hotfix with the bug is installed.
        /// https://github.com/dotnet/announcements/issues/53
        /// </summary>
        internal static bool IsHotFix4055002Installed { get; set; }

        /// <summary>
        /// When it's true, the app is currently deleting old projects.
        /// </summary>
        internal static bool IsCurrentlyDeletingFiles { get; set; }

        /// <summary>
        /// The available space on the disk that currently holds the data, as percentage.
        /// </summary>
        internal static double AvailableDiskSpacePercentage { get; set; }

        /// <summary>
        /// The available space on the disk that currently holds the data.
        /// </summary>
        internal static double AvailableDiskSpace { get; set; }

        /// <summary>
        /// Holds the details of the latest update available.
        /// </summary>
        internal static UpdateAvailable UpdateAvailable { get; set; }
    }
}