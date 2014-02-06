using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// The processing info page.
    /// </summary>
    public partial class Processing : UserControl
    {
        private int _max = 0;
        /// <summary>
        /// Constructor.
        /// </summary>
        public Processing()
        {
            //If there is a language setted via command line.
            if (CultureUtil.Lang.Length == 2)
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(CultureUtil.Lang);
            }

            InitializeComponent();
        }

        /// <summary>
        /// Set the status.
        /// </summary>
        /// <param name="num">The actual status.</param>
        public void SetStatus(int num)
        {
            progressBarEncoding.Value = num;
            lblValue.Text = num + Resources.Title_Thread_out_of + _max;
        }

        /// <summary>
        /// The maximum value to the progress bar.
        /// </summary>
        /// <param name="max">The amount of frames in the process.</param>
        public void SetMaximumValue(int max)
        {
            _max = max;
            progressBarEncoding.Maximum = max;
        }
    }
}
