using System;

namespace ScreenToGif.Util
{
    internal static class Global
    {
        private static string _assemblyShortName;


        /// <summary>
        /// Helper method for generating a "pack://" URI for a given relative file based on the
        /// assembly that this class is in.
        /// </summary>
        public static Uri MakePackUri(string relativeFile)
        {
            string uriString = "pack://application:,,,/" + AssemblyShortName + ";component/" + relativeFile;
            return new Uri(uriString);
        }

        private static string AssemblyShortName
        {
            get
            {
                if (_assemblyShortName != null)
                    return _assemblyShortName;

                var a = typeof(Global).Assembly;

                //Pull out the short name.
                _assemblyShortName = a.ToString().Split(',')[0];

                return _assemblyShortName;
            }
        }

        public static DateTime StartupDateTime { get; set; }

        /// <summary>
        /// When it's true, the global shortcuts won't work.
        /// </summary>
        public static bool IgnoreHotKeys { get; set; }

        /// <summary>
        /// When it's true, the hotfix with the bug is installed.
        /// https://github.com/dotnet/announcements/issues/53
        /// </summary>
        public static bool IsHotFix4055002Installed { get; set; }
    }
}