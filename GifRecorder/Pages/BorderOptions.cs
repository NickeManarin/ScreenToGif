using System;
using System.Drawing;
using System.Windows.Forms;
using ScreenToGif.Encoding;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Border Options form.
    /// </summary>
    public partial class BorderOptions : Form
    {
        /// <summary>
        /// Default constructor for this form.
        /// </summary>
        public BorderOptions()
        {
            InitializeComponent();

            pbOutlineColor.BackColor = colorDialog.Color = Settings.Default.borderColor;
            numThick.Value = (decimal) Settings.Default.borderThickness;
            lblColorName.Text = pbOutlineColor.BackColor.Name;

            //TODO: Localize...
            #region Localize Labels

            this.Text = "Border Options";
            lblColor.Text = "Color";
            lblThick.Text = Resources.Label_Thickness;
            lblPoints.Text = Resources.Label_Points;

            #endregion
        }

        #region Events

        private void numThick_ValueChanged(object sender, EventArgs e)
        {
            Preview();
        }

        private void pbOutlineColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pbOutlineColor.BackColor = colorDialog.Color;
                lblColorName.Text = pbOutlineColor.BackColor.Name;

                Preview();
            }
        }

        private void BorderOptions_Resize(object sender, EventArgs e)
        {
            Preview();
        }

        private void BorderOptions_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.borderColor = pbOutlineColor.BackColor;
            Settings.Default.borderThickness = (float) numThick.Value;
            Settings.Default.Save();
        }

        #endregion

        /// <summary>
        /// Shows the how the border will look like in the pictureBox.
        /// </summary>
        private void Preview()
        {
            var image = new Bitmap(pbExample.Size.Width - 1, pbExample.Size.Height - 1);

            image = ImageUtil.Border(image, (float)numThick.Value, pbOutlineColor.BackColor);

            pbExample.Image = image;

            GC.Collect();
        }
    }
}
