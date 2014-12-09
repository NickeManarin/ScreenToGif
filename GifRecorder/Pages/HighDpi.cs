using System;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// High DPI Warning form.
    /// </summary>
    public partial class HighDpi : Form
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public HighDpi()
        {
            InitializeComponent();
        }

        private void cbIgnore_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.showHighDpiWarn = cbIgnore.Checked;
            Settings.Default.Save();
        }
    }
}
