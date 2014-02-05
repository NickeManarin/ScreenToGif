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
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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
