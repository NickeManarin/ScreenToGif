using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Color filter form.
    /// </summary>
    public partial class ColorFilter : Form
    {
        #region Getters and Setters

        public int Red { get; private set; }

        public int Green { get; private set; }

        public int Blue { get; private set; }

        public int Alpha { get; private set; }

        #endregion

        /// <summary>
        /// Color filter instance.
        /// </summary>
        public ColorFilter()
        {
            InitializeComponent();

            Red = trackRed.Value = Settings.Default.redFilter;
            Green = trackGreen.Value = Settings.Default.greenFilter;
            Blue = trackBlue.Value = Settings.Default.blueFilter;
            Alpha = trackAlpha.Value = Settings.Default.alphaFilter;
        }

        private void UpdateColor()
        {
            pbColor.BackColor =
                Color.FromArgb(trackAlpha.Value, trackRed.Value, trackGreen.Value, trackBlue.Value);
        }

        private void trackRed_ValueChanged(object sender, EventArgs e)
        {
            UpdateColor();
            lblRed.Text = trackRed.Value.ToString();
            Red = trackRed.Value;
        }

        private void trackGreen_ValueChanged(object sender, EventArgs e)
        {
            UpdateColor();
            lblGreen.Text = trackGreen.Value.ToString();
            Green = trackGreen.Value;
        }

        private void trackBlue_ValueChanged(object sender, EventArgs e)
        {
            UpdateColor();
            lblBlue.Text = trackBlue.Value.ToString();
            Blue = trackBlue.Value;
        }

        private void trackAlpha_ValueChanged(object sender, EventArgs e)
        {
            UpdateColor();
            lblAlpha.Text = trackAlpha.Value.ToString();
            Alpha = trackAlpha.Value;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Settings.Default.redFilter = trackRed.Value;
            Settings.Default.greenFilter = trackGreen.Value;
            Settings.Default.blueFilter = trackBlue.Value;
            Settings.Default.alphaFilter = trackAlpha.Value;
            Settings.Default.Save();
        }

        private void pbColor_MouseHover(object sender, EventArgs e)
        {
            tooltip.Show(pbColor.BackColor.Name, pbColor, 2000);
        }
    }
}
