using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    public partial class GifSettings : UserControl
    {
        public GifSettings()
        {
            InitializeComponent();
        }

        private void GifSettings_Load(object sender, EventArgs e)
        {
            #region Compression

            trackBarQuality.Value = Properties.Settings.Default.STquality;
            labelQuality.Text = (-(trackBarQuality.Value - 20)).ToString();

            #endregion

            #region Gif Settings

            if (Settings.Default.STencodingCustom)
            {
                radioGif.Checked = true;
                trackBarQuality.Enabled = true;
            }
            else
            {
                radioPaint.Checked = true;
            }

            cbPaintTransparent.Checked = Settings.Default.STpaintTransparent;
            cbPaintTransparent.Enabled = pbTranspColor.Enabled = btnTranspColor.Enabled = radioGif.Checked;

            pbTranspColor.BackColor = Settings.Default.STtransparentColor;
            
            #endregion

            #region Gif Loop

            cbLoop.Checked = Properties.Settings.Default.STloop;

            cbRepeatForever.Enabled = Settings.Default.STloop;
            numRepeatCount.Enabled = Settings.Default.STloop;

            cbRepeatForever.Checked = Settings.Default.STrepeatForever;
            numRepeatCount.Enabled = cbRepeatForever.Checked;

            numRepeatCount.Value = Settings.Default.STrepeatCount;

            numRepeatCount.Enabled = !cbRepeatForever.Checked;

            #endregion
        }

        private void trackBarQuality_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.STquality = trackBarQuality.Value;
        }

        private void cbLoop_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.STloop = cbLoop.Checked;

            cbRepeatForever.Enabled = Settings.Default.STloop;

            if (!cbRepeatForever.Checked)
            {
                numRepeatCount.Enabled = Settings.Default.STloop;
                lblRepeatCount.Enabled = Settings.Default.STloop;
            }

        }

        private void trackBarQuality_Scroll(object sender, EventArgs e)
        {
            labelQuality.Text = (-(trackBarQuality.Value - 20)).ToString();

            if (trackBarQuality.Value >= 11)
            {
                labelQuality.ForeColor = Color.FromArgb(62, 91, 210);
            }
            else if (trackBarQuality.Value <= 9)
            {
                labelQuality.ForeColor = Color.DarkGoldenrod;
            }
            else if (trackBarQuality.Value == 10)
            {
                labelQuality.ForeColor = Color.FromArgb(0, 0, 0);
            }
            //Converts 1 to 20 and 20 to 1 (because the quality scroll is inverted, 1 better images, 20 worst)
            //But for the user, 20 is the best, so I need to invert the value.
        }

        private void radioGif_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.STencodingCustom = radioGif.Checked;
            gbQuality.Enabled = radioGif.Checked;

            cbPaintTransparent.Enabled = pbTranspColor.Enabled = btnTranspColor.Enabled = radioGif.Checked;
        }

        private void cbRepeatForever_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.STrepeatForever = cbRepeatForever.Checked;
            numRepeatCount.Enabled = !cbRepeatForever.Checked;
            lblRepeatCount.Enabled = !cbRepeatForever.Checked;
        }

        private void cbPaintTransparent_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.STpaintTransparent = cbPaintTransparent.Checked;

            pbTranspColor.Enabled = btnTranspColor.Enabled = cbPaintTransparent.Checked;
        }

        private void btnTranspColor_Click(object sender, EventArgs e)
        {
            colorDialog.SolidColorOnly = true;

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                pbTranspColor.BackColor = colorDialog.Color;

                Settings.Default.STtransparentColor = colorDialog.Color;
            }
        }
    }
}
