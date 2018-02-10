using System.Collections.Generic;
using System.IO;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Holds information about the current arguments of the running instance.
    /// </summary>
    public static class Argument
    {
        public static void Prepare(string[] args)
        {
            if (args[0].Equals("/lang"))
            {
                if (args.Length > 1)
                    UserSettings.All.LanguageCode = args[1];
            }

            //Check each arg to know if it's a file.
            foreach (var arg in args)
            {
                if (File.Exists(arg))
                    FileNames.Add(arg);
            }
        }

        /// <summary>
        /// Filenames arguments.
        /// </summary>
        public static List<string> FileNames { get; set; } = new List<string>();
    }
}