using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Holds information about the current arguments of the running instance.
    /// </summary>
    public static class Argument
    {
        #region Properties

        /// <summary>
        /// The path of the files passed as arguments to this executable.
        /// Only files that exists are not ignored.
        /// </summary>
        public static List<string> FileNames { get; set; } = new List<string>();

        /// <summary>
        /// True if this instance should not try to display anything, besides the download window.
        /// </summary>
        public static bool IsInDownloadMode { get; set; }

        /// <summary>
        /// The type of download that should happen (Gifski, FFmpeg, SharpDX).
        /// </summary>
        public static string DownloadMode { get; set; }

        /// <summary>
        /// The output path of the download.
        /// </summary>
        public static string DownloadPath { get; set; }

        #endregion

        public static void Prepare(string[] args)
        {
            FileNames.Clear();

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "/lang":
                    case "-lang":
                    {
                        //Changes the language of the app, example: -lang pt
                        if (args.Length > i + 1)
                        {
                            try
                            {
                                //Fail silently if the language is not properly set.
                                UserSettings.All.LanguageCode = new CultureInfo(args[i + 1]).ThreeLetterISOLanguageName;
                                i++;
                            }
                            catch (Exception e)
                            {
                                LogWriter.Log(e, $"The language code {args[i + 1]} was not recognized.");
                            }
                        }

                        break;
                    }

                    case "-d":
                    case "/d":
                    case "-download":
                    case "/download":
                    {
                        if (args.Length > i + 2)
                        {
                            IsInDownloadMode = true;
                            i++;

                            DownloadMode = args[i++];
                            DownloadPath = args[i++];
                        }
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
                        var path = args[i].Trim('"').Trim('\'');

                        //Anything else is treated as file to be imported.
                        if (File.Exists(path))
                            FileNames.Add(path);

                        break;
                    }
                }
            }
        }
    }
}