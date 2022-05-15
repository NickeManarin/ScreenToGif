using System;
using System.IO;

namespace Translator.Util;

/// <summary>
/// Log Writer Class
/// </summary>
public static class LogWriter
{
    /// <summary>
    /// Write to Error Log (Text File).
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

            var documents = isFallback ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : ".";
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
                    writer.WriteLine($"▬ Message - {Environment.NewLine}\t{ex.Message}");
                    writer.WriteLine($"○ Type - {Environment.NewLine}\t{ex.GetType()}");
                    writer.WriteLine(FormattableString.Invariant($"♦ [Version] Date/Hour - {Environment.NewLine}\t[{App.Version}] {DateTime.Now}"));
                    writer.WriteLine($"▲ Source - {Environment.NewLine}\t{ex.Source}");
                    writer.WriteLine($"▼ TargetSite - {Environment.NewLine}\t{ex.TargetSite}");

                    var bad = ex as BadImageFormatException;

                    if (bad != null)
                        writer.WriteLine($"► Fuslog - {Environment.NewLine}\t{bad.FusionLog}");

                    if (additional != null)
                        writer.WriteLine($"◄ Additional - {Environment.NewLine}\t{additional}");

                    writer.WriteLine($"♠ StackTrace - {Environment.NewLine}{ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        writer.WriteLine();
                        writer.WriteLine($"▬▬ Message - {Environment.NewLine}\t{ex.InnerException.Message}");
                        writer.WriteLine($"○○ Type - {Environment.NewLine}\t{ex.InnerException.GetType()}");
                        writer.WriteLine($"▲▲ Source - {Environment.NewLine}\t{ex.InnerException.Source}");
                        writer.WriteLine($"▼▼ TargetSite - {Environment.NewLine}\t{ex.InnerException.TargetSite}");
                        writer.WriteLine($"♠♠ StackTrace - {Environment.NewLine}{ex.InnerException.StackTrace}");

                        if (ex.InnerException.InnerException != null)
                        {
                            writer.WriteLine();
                            writer.WriteLine($"▬▬▬ Message - {Environment.NewLine}\t{ex.InnerException.InnerException.Message}");
                            writer.WriteLine($"○○○ Type - {Environment.NewLine}\t{ex.InnerException.InnerException.GetType()}");
                            writer.WriteLine($"▲▲▲ Source - {Environment.NewLine}\t{ex.InnerException.InnerException.Source}");
                            writer.WriteLine($"▼▼▼ TargetSite - {Environment.NewLine}\t{ex.InnerException.InnerException.TargetSite}");
                            writer.WriteLine($"♠♠♠ StackTrace - {Environment.NewLine}\t{ex.InnerException.InnerException.StackTrace}");

                            if (ex.InnerException.InnerException.InnerException != null)
                            {
                                writer.WriteLine();
                                writer.WriteLine($"▬▬▬▬ Message - {Environment.NewLine}\t{ex.InnerException.InnerException.InnerException.Message}");
                                writer.WriteLine($"○○○○ Type - {Environment.NewLine}\t{ex.InnerException.InnerException.InnerException.GetType()}");
                                writer.WriteLine($"▲▲▲▲ Source - {Environment.NewLine}\t{ex.InnerException.InnerException.InnerException.Source}");
                                writer.WriteLine($"▼▼▼▼ TargetSite - {Environment.NewLine}\t{ex.InnerException.InnerException.InnerException.TargetSite}");
                                writer.WriteLine($"♠♠♠♠ StackTrace - {Environment.NewLine}\t{ex.InnerException.InnerException.InnerException.StackTrace}");
                            }
                        }
                    }

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
}