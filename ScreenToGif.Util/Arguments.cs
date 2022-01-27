using System.Globalization;
using System.IO;
using System.Windows;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Util;

/// <summary>
/// Holds information about the current arguments of the running instance.
/// </summary>
public static class Arguments
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

    /// <summary>
    /// True if this instance should not try to display anything, besides trying to save the settings to disk.
    /// </summary>
    public static bool IsInSettingsMode { get; set; }

    /// <summary>
    /// Ignores the single instance setting to continue opening a new instance of the app.
    /// </summary>
    public static bool NewInstance { get; set; }

    /// <summary>
    /// Opens a window.
    /// </summary>
    public static bool Open { get; set; }

    /// <summary>
    /// The window to open with the -open command.
    /// </summary>
    public static int WindownToOpen { get; set; }

    /// <summary>
    /// The capture region.
    /// </summary>
    public static Rect Region { get; set; } = Rect.Empty;

    /// <summary>
    /// The capture frequency multiplier.
    /// </summary>
    public static int Frequency { get; set; }

    /// <summary>
    /// The capture frequency type.
    /// </summary>
    public static CaptureFrequencies? FrequencyType { get; set; }

    /// <summary>
    /// The capture limit.
    /// </summary>
    public static TimeSpan Limit { get; set; }

    /// <summary>
    /// True if the recorder should start capture right away.
    /// </summary>
    public static bool StartCapture { get; set; }

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

                case "-settings":
                {
                    //Enables the mode which will try to save the settings using administrative privileges.
                    IsInSettingsMode = true;
                    break;
                }

                case "-n":
                case "/n":
                case "/new":
                case "-new":
                {
                    NewInstance = true;
                    break;
                }

                case "-o":
                case "/o":
                case "/open":
                case "-open":
                {
                    if (args.Length <= i + 1)
                        return;

                    //-open screen-recorder(webcam-recorder/board-recorder/editor/options/startup/minimized)
                    Open = true;

                    #region Get window to open

                    var window = args[++i];

                    switch (window)
                    {
                        case "m":
                        case "min":
                        case "minimized":
                            WindownToOpen = -1;
                            break;

                        case "up":
                        case "start":
                        case "startup":
                            WindownToOpen = 0;
                            break;

                        case "s":
                        case "screen":
                        case "screen-recorder":
                            WindownToOpen = 1;
                            break;

                        case "w":
                        case "webcam":
                        case "webcam-recorder":
                            WindownToOpen = 2;
                            break;

                        case "b":
                        case "board":
                        case "board-recorder":
                            WindownToOpen = 3;
                            break;

                        case "e":
                        case "editor":
                            WindownToOpen = 4;
                            break;

                        case "o":
                        case "options":
                            WindownToOpen = 5;
                            break;

                        default:
                            Open = false;
                            break;
                    }

                    #endregion

                    break;
                }

                case "-r":
                case "/r":
                case "/region":
                case "-region":
                {
                    try
                    {
                        //-region/-r 100,50,500,200
                        if (args.Length > i + 1)
                            Region = Rect.Parse(args[++i]);
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Not possible to parse the capture rectangle from arguments", args[i++]);
                    }

                    break;
                }

                case "-f":
                case "/f":
                case "/framerate":
                case "-framerate":
                case "/frequency":
                case "-frequency":
                {
                    if (args.Length <= i + 1)
                        return;

                    //-framerate/-f (60fps/60fpm/60fph/manual/interaction)
                    ParseFramerate(args[++i].Trim());
                    break;
                }

                case "-l":
                case "/l":
                case "/limit":
                case "-limit":
                {
                    //-limit/-l 01:30
                    if (args.Length <= i + 1)
                        return;

                    if (TimeSpan.TryParse(args[++i].Trim(), CultureInfo.InvariantCulture, out var time))
                        Limit = time;

                    break;
                }

                case "-c":
                case "/c":
                case "/capture":
                case "-capture":
                {
                    StartCapture = true;
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

    public static void ClearAutomationArgs()
    {
        Open = false;
        WindownToOpen = 0;
        Region = Rect.Empty;
        Frequency = 0;
        FrequencyType = null;
        Limit = TimeSpan.Zero;
        StartCapture = false;
    }


    private static void ParseFramerate(string frequency)
    {
        if (frequency.ToLowerInvariant().EndsWith("fps"))
        {
            ParseFramerate(frequency, CaptureFrequencies.PerSecond);
            return;
        }

        if (frequency.ToLowerInvariant().EndsWith("fpm"))
        {
            ParseFramerate(frequency, CaptureFrequencies.PerMinute);
            return;
        }

        if (frequency.ToLowerInvariant().EndsWith("fph"))
        {
            ParseFramerate(frequency, CaptureFrequencies.PerHour);
            return;
        }

        if (frequency.ToLowerInvariant().Equals("manual"))
        {
            FrequencyType = CaptureFrequencies.Manual;
            return;
        }

        if (frequency.ToLowerInvariant().Equals("interaction"))
            FrequencyType = CaptureFrequencies.Interaction;
    }

    private static void ParseFramerate(string frequency, CaptureFrequencies type)
    {
        if (!int.TryParse(frequency.Substring(0, frequency.Length - 3), out var time))
        {
            LogWriter.Log("Not possible to parse the framerate from the argument", frequency);
            return;
        }

        if (time < 1)
            time = 1;
        else if (time > 60)
            time = 60;

        FrequencyType = type;
        Frequency = time;
    }
}