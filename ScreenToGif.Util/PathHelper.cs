using ScreenToGif.Util.Settings;
using System.IO;
using System.Text.RegularExpressions;

namespace ScreenToGif.Util;

public static class PathHelper
{
    /// <summary>
    /// Puts the current date/time into filename, replacing the format typed in between two questions marks.
    /// Such as 'Animation ?dd-MM-yy?' -> 'Animation 21-04-21'
    /// Only some of the formats are available, since there's a file name limitation from Windows.
    /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings 
    /// </summary>
    /// <param name="name">The name of the file, with the date/time format.</param>
    /// <returns>The name with the date and time.</returns>
    public static string ReplaceRegexInName(string name)
    {
        //Less than 2 question marks? Then it's not valid.
        if (name.Split('?').Length - 1 < 2)
            return name;

        const string dateTimeFileNameRegEx = "[?]([ymdhsfzgkt]{1,6}[-_ ]{0,2}){1,10}[?]";

        if (!Regex.IsMatch(name, dateTimeFileNameRegEx, RegexOptions.IgnoreCase))
            return name;

        var match = Regex.Match(name, dateTimeFileNameRegEx, RegexOptions.IgnoreCase);
        var date = DateTime.Now.ToString(Regex.Replace(match.Value, "[?]", ""));

        return name.Replace(match.ToString(), date);
    }

    /// <summary>
    /// When dealing with relative paths, the app will fail to point to the right folder when starting it via the "Open with..." or automatic startup methods.
    /// </summary>
    public static string AdjustPath(string path)
    {
        //If the path is relative, File.Exists() was returning C:\\Windows\\System32\ffmpeg.exe when the app was launched from the "Open with" context menu.
        //So, in order to get the correct location, I need to combine the current base directory with the relative path.
        if (!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path))
        {
            var adjusted = path.StartsWith("." + Path.AltDirectorySeparatorChar) ? path.TrimStart('.', Path.AltDirectorySeparatorChar) :
                path.StartsWith("." + Path.DirectorySeparatorChar) ? path.TrimStart('.', Path.DirectorySeparatorChar) : path;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, adjusted);
        }

        return path;
    }

    public static bool IsFfmpegPresent(bool ignoreEnvironment = false, bool ignoreEmpty = false)
    {
        //If the path is relative, File.Exists() was returning C:\\Windows\\System32\ffmpeg.exe when the app was launched from the "Open with" context menu.
        //So, in order to get the correct location, I need to combine the current base directory with the relative path.
        var realPath = AdjustPath(UserSettings.All.FfmpegLocation);

        //File location already chosen or detected.
        if (!string.IsNullOrWhiteSpace(realPath) && File.Exists(realPath))
            return true;

        //The path was not selected, it may be located inside a common folder.
        if (!ignoreEmpty && string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation))
        {
            //Same path as application.
            if (File.Exists(AdjustPath("ffmpeg.exe")))
            {
                UserSettings.All.FfmpegLocation = "ffmpeg.exe";
                return true;
            }

            //Program Data folder.
            var expandedPath = Environment.ExpandEnvironmentVariables(@"%ProgramData%\ScreenToGif\ffmpeg.exe");

            if (File.Exists(expandedPath))
            {
                UserSettings.All.FfmpegLocation = expandedPath;
                return true;
            }
        }

        //If not found by direct/relative path, ignore the environment variables.
        if (ignoreEnvironment)
            return false;

        #region Check Environment Variables

        var variable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) + ";" +
                       Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);

        foreach (var path in variable.Split(';').Where(w => !string.IsNullOrWhiteSpace(w)))
        {
            try
            {
                if (!File.Exists(Path.Combine(path, "ffmpeg.exe")))
                    continue;
            }
            catch (Exception)
            {
                //LogWriter.Log(ex, "Checking the path variables", path);
                continue;
            }

            UserSettings.All.FfmpegLocation = Path.Combine(path, "ffmpeg.exe");
            return true;
        }

        #endregion

        return false;
    }

    public static bool IsGifskiPresent(bool ignoreEnvironment = false, bool ignoreEmpty = false)
    {
        //If the path is relative, File.Exists() was returning C:\\Windows\\System32\Gifski.dll when the app was launched from the "Open with" context menu.
        //So, in order to get the correct location, I need to combine the current base directory with the relative path.
        var realPath = AdjustPath(UserSettings.All.GifskiLocation);

        //File location already chosen or detected.
        if (!string.IsNullOrWhiteSpace(realPath) && File.Exists(realPath))
            return true;

        //The path was not selected, but the file exists inside the same folder.
        if (!ignoreEmpty && string.IsNullOrWhiteSpace(UserSettings.All.GifskiLocation) && File.Exists(AdjustPath("gifski.dll")))
        {
            UserSettings.All.GifskiLocation = "gifski.dll";
            return true;
        }

        //If not found by direct/relative path, ignore the environment variables.
        if (ignoreEnvironment)
            return false;

        #region Check Environment Variables

        var variable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) + ";" +
                       Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);

        foreach (var path in variable.Split(';').Where(w => !string.IsNullOrWhiteSpace(w)))
        {
            try
            {
                if (!File.Exists(Path.Combine(path, "gifski.dll")))
                    continue;
            }
            catch (Exception ex)
            {
                //LogWriter.Log(ex, "Checking the path variables", path);
                continue;
            }

            UserSettings.All.GifskiLocation = Path.Combine(path, "gifski.dll");
            return true;
        }

        #endregion

        return false;
    }
}