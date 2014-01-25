using System;
using System.IO;
using System.Windows.Forms;

namespace ScreenToGif.Util
{
    public static class LogWriter
    {
        // default constructor
        //public LogWriter()
        //{

        //}

        //public LogWriter(string msg, string stkTrace, string title)
        //{
        //    WriteToErrorLog(msg, stkTrace, title);
        //}

        public static void Log(Exception ex, string message = "")
        {
            WriteToErrorLog(ex.Message, ex.StackTrace, message);
        }

        /// <summary>
        /// Write to Error Log (Text File)
        /// </summary>
        /// <param name="msg">The message text 
        /// to write</param>
        /// <param name="stkTrace">The stack trace 
        /// for the exception</param>
        /// <param name="title">The name of the error</param>
        public static void WriteToErrorLog(string msg, string stkTrace, string title)
        {



            // check and make the directory if necessary; 
            // this is set to look in the application
            // folder, you may wish to place the error 
            // log in another location depending upon the
            // the user's role and write access to different 
            // areas of the file system

            if (!System.IO.Directory.Exists(Application.StartupPath + "\\Logs\\"))
            {
                System.IO.Directory.CreateDirectory(Application.StartupPath + "\\Logs\\");

            }

            string DateAppendage = DateTime.Now.Day.ToString() + "_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year.ToString();

            // check the file, create it if necessary - do not 
            // write the message in this pass,
            FileStream fs = new FileStream(Application.StartupPath + "\\Logs\\log_" + DateAppendage + ".txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            StreamWriter s = new StreamWriter(fs);
            s.Close();
            fs.Close();
            fs.Dispose();

            // re-open the log file and log the message
            FileStream fs1 = new FileStream(Application.StartupPath + "\\Logs\\log_" + DateAppendage + ".txt", FileMode.Append, FileAccess.Write);

            StreamWriter s1 = new StreamWriter(fs1);

            s1.Write("Título: " + title + Environment.NewLine);
            s1.Write("Mensagem: " + msg + Environment.NewLine);
            s1.Write("StackTrace: " + stkTrace + Environment.NewLine);
            s1.Write("Data/Hora: " + DateTime.Now.ToString() + Environment.NewLine);
            s1.Write("==============================" + Environment.NewLine);

            s1.Close();
            fs1.Close();
            fs1.Dispose();

        }

    }

}
