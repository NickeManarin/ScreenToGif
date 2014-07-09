using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
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

            #region Arguments

            if (args.Length > 1) //Only if there is 2 or more parameters
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("/lang"))
                    {
                        try
                        {
                            Thread.CurrentThread.CurrentUICulture =
                                new CultureInfo(args[1]);
                            CultureUtil.Lang = args[1];
                            //This is needed to use in another thread, example: the Processing page that is called from the worker thread.

                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            MessageBox.Show("Language argument missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                            LogWriter.Log(ex, "Error while setting language. Language missing");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Wrong language arguments.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                            LogWriter.Log(ex, "Error while setting language.");
                        }
                    }
                }
            }
            else
            {
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

            //Application.Run(new TestForm());
            //return;

            try
            {
                if (!Settings.Default.modernStyle) //If user wants to use the legacy or modern theme.
                {
                    Application.Run(new Legacy());
                }
                else
                {
                    Application.Run(new Modern());
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Generic Error");
                MessageBox.Show(ex.Message, "Generic Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

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
    }
}
