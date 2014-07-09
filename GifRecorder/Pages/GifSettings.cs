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
    /// <summary>
    /// Gif Settings page.
    /// </summary>
    public partial class GifSettings : UserControl
    {
        public GifSettings()
        {
            InitializeComponent();

            #region Localize Labels (thanks to the form designer that resets every label)

            lblSlow.Text = Resources.Label_Slow;
            lblFast.Text = Resources.Label_Fast;
            lblWorst.Text = Resources.Label_Worst;
            lblBetter.Text = Resources.Label_Better;
            lblRepeatCount.Text = Resources.Label_RepeatCount;
            gbLoop.Text = Resources.Label_Loop;
            gbQuality.Text = Resources.Label_Compression;
            gbGifSettings.Text = Resources.Label_GifSettings;

            #endregion
        }

        private void GifSettings_Load(object sender, EventArgs e)
        {
            #region Compression

            trackBarQuality.Value = Properties.Settings.Default.quality;
            labelQuality.Text = (-(trackBarQuality.Value - 20)).ToString();

            #endregion

            #region Gif Settings

            if (Settings.Default.encodingCustom)
            {
                radioGif.Checked = true;
                trackBarQuality.Enabled = true;
            }
            else
            {
                radioPaint.Checked = true;
            }

            cbPaintTransparent.Checked = Settings.Default.paintTransparent;
            cbPaintTransparent.Enabled = pbTranspColor.Enabled = btnTranspColor.Enabled = radioGif.Checked;

            pbTranspColor.BackColor = Settings.Default.transparentColor;
            
            #endregion

            #region Gif Loop

            cbLoop.Checked = Properties.Settings.Default.loop;

            cbRepeatForever.Enabled = Settings.Default.loop;
            numRepeatCount.Enabled = Settings.Default.loop;

            cbRepeatForever.Checked = Settings.Default.repeatForever;
            numRepeatCount.Enabled = cbRepeatForever.Checked;

            numRepeatCount.Value = Settings.Default.repeatCount;

            numRepeatCount.Enabled = !cbRepeatForever.Checked;

            #endregion
        }

        private void trackBarQuality_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.quality = trackBarQuality.Value;
        }

        private void cbLoop_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.loop = cbLoop.Checked;

            cbRepeatForever.Enabled = Settings.Default.loop;

            if (!cbRepeatForever.Checked)
            {
                numRepeatCount.Enabled = Settings.Default.loop;
                lblRepeatCount.Enabled = Settings.Default.loop;
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
            Settings.Default.encodingCustom = radioGif.Checked;
            gbQuality.Enabled = radioGif.Checked;

            cbPaintTransparent.Enabled = pbTranspColor.Enabled = btnTranspColor.Enabled = radioGif.Checked;
        }

        private void cbRepeatForever_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.repeatForever = cbRepeatForever.Checked;
            numRepeatCount.Enabled = !cbRepeatForever.Checked;
            lblRepeatCount.Enabled = !cbRepeatForever.Checked;
        }

        private void cbPaintTransparent_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.paintTransparent = cbPaintTransparent.Checked;

            pbTranspColor.Enabled = btnTranspColor.Enabled = cbPaintTransparent.Checked;
        }

        private void btnTranspColor_Click(object sender, EventArgs e)
        {
            colorDialog.SolidColorOnly = true;

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                pbTranspColor.BackColor = colorDialog.Color;

                Settings.Default.transparentColor = colorDialog.Color;
            }
        }
    }
}
