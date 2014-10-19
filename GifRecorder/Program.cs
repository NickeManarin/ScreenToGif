using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ScreenToGif.Pages;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            #region Error Handlers

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            #endregion

            #region Arguments

            if (args.Length > 0)
            {
                if (args[0].EndsWith(".gif"))
                {
                    if (File.Exists(args[0]))
                    {
                        ArgumentUtil.FileName = args[0];
                    }
                }
                else if (args[0].Equals("/lang"))
                {
                    if (args.Length > 1)
                    {
                        Settings.Default.language = args[1];
                    }
                }
            }

            #endregion

            #region Upgrade Application Settings

            //See http://stackoverflow.com/questions/534261/how-do-you-keep-user-config-settings-across-different-assembly-versions-in-net
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            #endregion

            #region Language

            if (!Settings.Default.language.Equals("detect"))
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.language);
                    CultureUtil.Lang = Settings.Default.language;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wrong language arguments.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    LogWriter.Log(ex, "Erro while trying to set the language.");
                }
            }

            #endregion

            #region Test Scenario

            //Application.Run(new TestForm());
            //return;

            #endregion

            #region Modern or Legacy Forms

            if (!Settings.Default.modernStyle) 
            {
                Application.Run(new Legacy());
            }
            else
            {
                Application.Run(new Modern());
            }

            #endregion

            #region Info About the colors

            /*
                <-------- CLICK-THRU COLORS -------->
                Black
                DarkGray
                DarkGreen
                DarkMagenta
                DimGray
                ForestGreen
                Fuchsia
                Gainsboro
                Gray
                Green
                Honeydew
                LightGray
                LightGreen
                Lime
                LimeGreen
                Magenta
                PaleGreen
                Plum
                Purple
                Silver
                Thistle
                Violet
                White
                WhiteSmoke
                */

            #endregion
        }

        #region Controle de Exceções

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogWriter.Log(e.Exception, "Thread Exception");

            var errorViewer = new ErrorViewer(e.Exception);
            errorViewer.ShowDialog();

            Environment.Exit(1);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            LogWriter.Log(ex, "Unhandled Exception");

            var errorViewer = new ErrorViewer(ex);
            errorViewer.ShowDialog();
            
            Environment.Exit(2);
        }

        #endregion
    }
}
