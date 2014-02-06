using System;
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
                            System.Threading.Thread.CurrentThread.CurrentUICulture =
                                new System.Globalization.CultureInfo(args[1]);
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

            #endregion

            if (Settings.Default.STmodernStyle)
            {
                try
                {
                    Application.Run(new Modern());
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Generic Error");
                    MessageBox.Show(ex.Message, "Generic Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                try
                {
                    Application.Run(new Legacy());
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Generic Error");
                    MessageBox.Show(ex.Message, "Generic Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
