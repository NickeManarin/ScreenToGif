using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
