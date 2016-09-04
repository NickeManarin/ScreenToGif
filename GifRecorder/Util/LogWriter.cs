using System;
using System.IO;
using System.Windows.Forms;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Log Writer Class
    /// </summary>
    public static class LogWriter
    {
        /// <summary>
        /// Write to Error Log (Text File)
        /// </summary>
        /// <param name="msg">The message text 
        /// to write</param>
        /// <param name="stkTrace">The stack trace 
        /// for the exception</param>
        /// <param name="title">The name of the error</param>
        public static void Log(Exception ex, string title = "")
        {
            // check and make the directory if necessary; 
            // this is set to look in the application
            // folder, you may wish to place the error 
            // log in another location depending upon the
            // the user's role and write access to different 
            // areas of the file system

            if (!Directory.Exists(Application.StartupPath + "\\Logs\\"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\Logs\\");
            }

            var dateAppendage = DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Year;

            // check the file, create it if necessary - do not 
            // write the message in this pass,
            var fs = new FileStream(Application.StartupPath + "\\Logs\\log_" + dateAppendage + ".txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            var s = new StreamWriter(fs);
            s.Close();
            fs.Close();
            fs.Dispose();

            // re-open the log file and log the message
            var fs1 = new FileStream(Application.StartupPath + "\\Logs\\log_" + dateAppendage + ".txt", FileMode.Append, FileAccess.Write);

            var s1 = new StreamWriter(fs1);

            s1.Write("Title: " + title + Environment.NewLine);
            s1.Write("Message: " + ex.Message + Environment.NewLine);
            s1.Write("Type: " + ex.GetType() + Environment.NewLine);
            s1.Write("Source: "+ ex.Source + Environment.NewLine);
            s1.Write("TargetSite: " + ex.TargetSite + Environment.NewLine);
            s1.Write("StackTrace: " + ex.StackTrace + Environment.NewLine);

            if (ex.InnerException != null)
            {
                s1.Write(">> Message: " + ex.InnerException.Message + Environment.NewLine);
                s1.Write(">> Type: " + ex.InnerException.GetType() + Environment.NewLine);
                s1.Write(">> Source: " + ex.InnerException.Source + Environment.NewLine);
                s1.Write(">> TargetSite: " + ex.InnerException.TargetSite + Environment.NewLine);
                s1.Write(">> StackTrace: " + ex.InnerException.StackTrace + Environment.NewLine);
            }

            s1.Write("Date/Time: " + DateTime.Now + Environment.NewLine);
            s1.Write("==============================" + Environment.NewLine);

            s1.Close();
            fs1.Close();
            fs1.Dispose();
        }
    }
}
