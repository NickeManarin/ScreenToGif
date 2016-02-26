using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScreenToGif.Properties;

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
                {
                    Settings.Default.Language = args[1];
                }
            }

            //Check each arg to know if it's a file.
            foreach (var arg in args)
            {
                if (File.Exists(arg))
                {
                    FileNames.Add(arg);
                }
            }
        }

        /// <summary>
        /// Filenames arguments.
        /// </summary>
        public static List<string> FileNames { get; set; } = new List<string>();
    }
}
