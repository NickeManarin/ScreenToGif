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
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "/lang":
                    case "-lang":
                    {
                        //Changes the language of the app, example: -lang pt
                        if (args.Length > i + 1)
                            UserSettings.All.LanguageCode = args[++i];

                        break;
                    }

                    case "-sm":
                    case "/sm":
                    case "-softmode":
                    case "/softmode":
                    {
                        //Forces using software mode.
                        UserSettings.All.DisableHardwareAcceleration = true;
                        break;
                    }

                    case "-hm":
                    case "/hm":
                    case "-hardmode":
                    case "/hardmode":
                    {
                        //Forces using hardware mode.
                        UserSettings.All.DisableHardwareAcceleration = false;
                        break;
                    }

                    default:
                    {
                        //Anything else is treated as file to be imported.
                        if (File.Exists(args[i]))
                            FileNames.Add(args[i]);

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Filenames arguments.
        /// </summary>
        public static List<string> FileNames { get; set; } = new List<string>();
    }
}