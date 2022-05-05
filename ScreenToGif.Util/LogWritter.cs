using System.IO;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Util;

/// <summary>
/// Basic log writer that stores messages on a file on disk.
/// </summary>
public static class LogWriter
{
    private static void WriteDetails(TextWriter writer, Exception ex, int level)
    {
        writer.WriteLine(new string('▬', level) + $" Message - {Environment.NewLine}\t{ex.Message}");
        writer.WriteLine(new string('○', level) + $" Type - {Environment.NewLine}\t{ex.GetType()}");
        writer.WriteLine(new string('▲', level) + $" Source - {Environment.NewLine}\t{ex.Source}");
        writer.WriteLine(new string('▼', level) + $" TargetSite - {Environment.NewLine}\t{ex.TargetSite}");

        if (ex is BadImageFormatException bad)
        {
            writer.WriteLine(new string('☼', level) + $" Filename - {Environment.NewLine}\t{bad.FileName}");
            writer.WriteLine(new string('►', level) + $" Fuslog - {Environment.NewLine}\t{bad.FusionLog}");
        }
        else if (ex is ArgumentException arg)
        {
            writer.WriteLine(new string('☼', level) + $" ParamName - {Environment.NewLine}\t{arg.ParamName}");
        }
        
        if (ex.HelpLink != null)
            writer.WriteLine(new string('◘', level) + $" Other - {Environment.NewLine}\t{ex.HelpLink}");

        writer.WriteLine(new string('♠', level) + $" StackTrace - {Environment.NewLine}{ex.StackTrace}");

        if (ex.InnerException == null || level >= 6)
            return;

        writer.WriteLine();
        WriteDetails(writer, ex.InnerException, level + 1);
    }

    /// <summary>
    /// Writes the exception details to the error log on disk.
    /// </summary>
    /// <param name="ex">The Exception to write.</param>
    /// <param name="title">The name of the error</param>
    /// <param name="additional">Additional information.</param>
    /// <param name="isFallback">Fallbacks to the Documents folder.</param>
    public static void Log(Exception ex, string title, object additional = null, bool isFallback = false)
    {
        try
        {
            #region Output folder

            var documents = isFallback || string.IsNullOrWhiteSpace(UserSettings.All.LogsFolder) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : UserSettings.All.LogsFolder;
            var folder = Path.Combine(documents, "ScreenToGif", "Logs");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            #endregion

            #region Creates the file

            var date = Path.Combine(folder, DateTime.Now.ToString("yy_MM_dd") + ".txt");
            var dateTime = Path.Combine(folder, DateTime.Now.ToString("yy_MM_dd hh_mm_ss_fff") + ".txt");

            FileStream fs = null;
            var inUse = false;

            try
            {
                fs = new FileStream(date, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch (Exception)
            {
                inUse = true;
                fs = new FileStream(dateTime, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }

            fs.Dispose();

            #endregion

            #region Append the exception information

            using (var fileStream = new FileStream(inUse ? dateTime : date, FileMode.Append, FileAccess.Write))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.WriteLine($"► Title - {Environment.NewLine}\t{title}");
                    writer.WriteLine(FormattableString.Invariant($"♦ [Version] Date/Hour - {Environment.NewLine}\t[{UserSettings.All?.VersionText}] {DateTime.Now}"));

                    if (additional != null)
                        writer.WriteLine($"◄ Additional - {Environment.NewLine}\t{additional}");

                    WriteDetails(writer, ex, 1);

                    writer.WriteLine();
                    writer.WriteLine("----------------------------------");
                    writer.WriteLine();
                }
            }

            #endregion
        }
        catch (Exception)
        {
            //One last trial.
            if (!isFallback)
                Log(ex, title, additional, true);
        }
    }

    /// <summary>
    /// Writes the details to the error log on disk.
    /// </summary>
    /// <param name="title">The name of the error</param>
    /// <param name="additional">Additional information.</param>
    /// <param name="secondAdditional">Additional information.</param>
    /// <param name="isFallback">Fallbacks to the Documents folder.</param>
    public static void Log(string title, object additional = null, object secondAdditional = null, bool isFallback = false)
    {
        try
        {
            #region Output folder

            var documents = isFallback || string.IsNullOrWhiteSpace(UserSettings.All.LogsFolder) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : UserSettings.All.LogsFolder;
            var folder = Path.Combine(documents, "ScreenToGif", "Logs");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            #endregion

            #region Creates the file

            var date = Path.Combine(folder, DateTime.Now.ToString("yy_MM_dd") + ".txt");
            var dateTime = Path.Combine(folder, DateTime.Now.ToString("yy_MM_dd hh_mm_ss_fff") + ".txt");

            FileStream fs = null;
            var inUse = false;

            try
            {
                fs = new FileStream(date, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch (Exception)
            {
                inUse = true;
                fs = new FileStream(dateTime, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }

            fs.Dispose();

            #endregion

            #region Append the exception information

            using (var fileStream = new FileStream(inUse ? dateTime : date, FileMode.Append, FileAccess.Write))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.WriteLine($"► Title - {Environment.NewLine}\t{title}");
                    writer.WriteLine(FormattableString.Invariant($"♦ [Version] Date/Hour - {Environment.NewLine}\t[{UserSettings.All?.VersionText}] {DateTime.Now}"));

                    if (additional != null)
                        writer.WriteLine($"◄ Additional - {Environment.NewLine}\t{additional}");

                    if (secondAdditional != null)
                        writer.WriteLine($"◄ Second Additional - {Environment.NewLine}\t{secondAdditional}");

                    writer.WriteLine();
                    writer.WriteLine("----------------------------------");
                    writer.WriteLine();
                }
            }

            #endregion
        }
        catch (Exception)
        {
            //One last trial.
            if (!isFallback)
                Log(title, additional, secondAdditional, true);
        }
    }
}