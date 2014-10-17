using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Caption Option.
    /// </summary>
    public partial class CaptionOptions : Form
    {
        #region Variables

        private readonly int _imageHeight;
        private bool _starting = true;

        #endregion

        /// <summary>
        /// The Caption Options form.
        /// </summary>
        /// <param name="height">The image height</param>
        public CaptionOptions(int height)
        {
            InitializeComponent();

            _imageHeight = height;

            #region Localization of Labels

            this.Text = Resources.Con_CaptionOptions;
            lblSizeType.Text = Resources.Label_FontSizeAs;
            lblFontTitle.Text = Resources.Label_Font;
            lblFontSize2.Text = Resources.Label_FontSize;
            lblVertical.Text = Resources.Label_VerticalAlign;
            lblPercentageSize.Text = Resources.Label_AsImageHeight;
            lblHorizontal.Text = Resources.Label_HorizontalAlign;
            lblThick.Text = Resources.Label_Thickness;
            lblPointsDesc.Text = Resources.Label_Points;

            #endregion
        }

        private void CaptionOptions_Load(object sender, EventArgs e)
        {
            cbHatchBrush.DataSource = Enum.GetValues(typeof(HatchStyle));
            cbHatchBrush.SelectedItem = Settings.Default.captionHatch;

            rbPercentage.Checked = Settings.Default.fontSizeAsPercentage;
            rbPoint.Checked = !rbPercentage.Checked;
            rbPercentage_CheckedChanged(null, null);

            #region Font

            fontDialog.Font = Settings.Default.fontCaption;
            pbFontColor.BackColor = colorDialog.Color = fontDialog.Color = Settings.Default.fontCaptionColor;

            lblFont.Text = fontDialog.Font.Name + "; ";
            if (!rbPercentage.Checked)
            {
                lblFont.Text += fontDialog.Font.Size + "pt";
            }

            if (fontDialog.Font.Bold)
            {
                lblFont.Text += " - " + Resources.Label_Bold;
            }

            #endregion

            numFontSizePercentage.Value = (decimal) (Settings.Default.fontCaptionPercentage * 100);

            #region Vertical Alignment

            if (Settings.Default.captionVerticalAlign == StringAlignment.Near)
            {
                rbTop.Checked = true;
            }
            else if (Settings.Default.captionVerticalAlign == StringAlignment.Center)
            {
                rbVerticalCenter.Checked = true;
            }
            else
            {
                rbBottom.Checked = true;
            }

            #endregion

            #region Horizontal Alignment

            if (Settings.Default.captionHorizontalAlign == StringAlignment.Near)
            {
                rbLeft.Checked = true;
            }
            else if (Settings.Default.captionHorizontalAlign == StringAlignment.Center)
            {
                rbHorizontalCenter.Checked = true;
            }
            else
            {
                rbRight.Checked = true;
            }

            #endregion

            pbHatchColor.BackColor = colorDialogHatch.Color = Settings.Default.captionHatchColor;
            cbUseHatch.Checked = Settings.Default.captionUseHatch;

            cbUseOutline.Checked = Settings.Default.captionUseOutline;
            pbOutlineColor.BackColor = colorDialogOutline.Color = Settings.Default.captionOutlineColor;
            numThick.Value = (decimal) Settings.Default.captionOutlineThick;

            _starting = false;
            Preview();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            #region Settings

            Settings.Default.fontSizeAsPercentage = rbPercentage.Checked;
            Settings.Default.fontCaption = fontDialog.Font;
            Settings.Default.fontCaptionColor = colorDialog.Color;

            Settings.Default.fontCaptionPercentage = (float) (numFontSizePercentage.Value / 100);

            #region Vertical Alignment

            if (rbTop.Checked)
            {
                Settings.Default.captionVerticalAlign = StringAlignment.Near;
            }
            else if (rbVerticalCenter.Checked)
            {
                Settings.Default.captionVerticalAlign = StringAlignment.Center;
            }
            else
            {
                Settings.Default.captionVerticalAlign = StringAlignment.Far;
            }

            #endregion

            #region Horizontal Alignment

            //This is inverted in right to left languages. 
            //Left is Far because it's the starting point of that kind of languages.
            if (rbLeft.Checked)
            {
                Settings.Default.captionHorizontalAlign = StringAlignment.Near;
            }
            else if (rbHorizontalCenter.Checked)
            {
                Settings.Default.captionHorizontalAlign = StringAlignment.Center;
            }
            else
            {
                Settings.Default.captionHorizontalAlign = StringAlignment.Far;
            }

            #endregion

            Settings.Default.captionHatchColor = colorDialogHatch.Color;
            Settings.Default.captionUseHatch = cbUseHatch.Checked;
            Settings.Default.captionHatch = (HatchStyle)cbHatchBrush.SelectedItem;

            Settings.Default.captionUseOutline = cbUseOutline.Checked;
            Settings.Default.captionOutlineColor = colorDialogOutline.Color;
            Settings.Default.captionOutlineThick = (float)numThick.Value;

            #endregion

            Settings.Default.Save();

            this.DialogResult = DialogResult.OK;
        }

        #region Events

        private void rbPercentage_CheckedChanged(object sender, EventArgs e)
        {
            lblFont.Text = fontDialog.Font.Name + "; ";

            if (!rbPercentage.Checked)
            {
                lblFont.Text += fontDialog.Font.Size + "pt";
                lblPercentageSize.Text = Resources.Label_Points;
                numFontSizePercentage.Value = (decimal)fontDialog.Font.Size;
            }
            else
            {
                lblPercentageSize.Text = Resources.Label_AsImageHeight;
                numFontSizePercentage.Value = (decimal) (Settings.Default.fontCaptionPercentage * 100);
            }

            if (fontDialog.Font.Bold)
            {
                lblFont.Text += " - " + Resources.Label_Bold;
            }

            Preview();
        }

        private void numFontSizePercentage_ValueChanged(object sender, EventArgs e)
        {
            if (!rbPercentage.Checked)
            {
                fontDialog.Font = new Font(fontDialog.Font.FontFamily, (float)numFontSizePercentage.Value, fontDialog.Font.Style);
                lblFont.Text = fontDialog.Font.Name + "; " + fontDialog.Font.Size + "pt";
            }
            
            Preview();
        }

        private void pbFontColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pbFontColor.BackColor = fontDialog.Color = colorDialog.Color;

                Preview();
            }
        }

        private void lblFont_Click(object sender, EventArgs e)
        {
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                lblFont.Text = fontDialog.Font.Name + "; " + fontDialog.Font.Size + "pt";
                if (fontDialog.Font.Bold)
                {
                    lblFont.Text += " - " + Resources.Label_Bold;
                }

                pbFontColor.BackColor = fontDialog.Color;

                Preview();
            }
        }

        private void cbUseOutline_CheckedChanged(object sender, EventArgs e)
        {
            flowOutline.Enabled = cbUseOutline.Checked;

            Preview();
        }

        private void pbOutlineColor_Click(object sender, EventArgs e)
        {
            if (colorDialogOutline.ShowDialog() == DialogResult.OK)
            {
                pbOutlineColor.BackColor = colorDialogOutline.Color;

                Preview();
            }
        }

        private void pbHatchColor_Click(object sender, EventArgs e)
        {
            if (colorDialogHatch.ShowDialog() == DialogResult.OK)
            {
                pbHatchColor.BackColor = colorDialogHatch.Color;

                Preview();
            }
        }

        private void cbUseHatch_CheckedChanged(object sender, EventArgs e)
        {
            cbHatchBrush.Enabled = cbUseHatch.Checked;
            pbHatchColor.Enabled = cbUseHatch.Checked;

            Preview();
        }

        private void preview_ValueChanged(object sender, EventArgs e)
        {
            Preview();
        }

        #endregion

        /// <summary>
        /// Renders how the caption will look like.
        /// </summary>
        public void Preview()
        {
            if (_starting) return;

            var image = new Bitmap(pbExample.Size.Width, pbExample.Size.Height);

            using (Graphics imgGr = Graphics.FromImage(image))
            {
                var graphPath = new GraphicsPath();

                imgGr.CompositingQuality = CompositingQuality.HighQuality;
                imgGr.CompositingMode = CompositingMode.SourceCopy;

                var fSt = (int) fontDialog.Font.Style;
                var fF = fontDialog.Font.FontFamily;

                StringFormat sFr = StringFormat.GenericDefault;

                #region Vertical Alignment

                if (rbTop.Checked)
                {
                    sFr.LineAlignment = StringAlignment.Near;
                }
                else if (rbVerticalCenter.Checked)
                {
                    sFr.LineAlignment = StringAlignment.Center;
                }
                else
                {
                    sFr.LineAlignment = StringAlignment.Far;
                }

                #endregion

                #region Horizontal Alignment

                if (rbLeft.Checked)
                {
                    sFr.Alignment = StringAlignment.Near;
                }
                else if (rbHorizontalCenter.Checked)
                {
                    sFr.Alignment = StringAlignment.Center;
                }
                else
                {
                    sFr.Alignment = StringAlignment.Far;
                }

                #endregion

                #region Draw the string using a specific sizing type. Percentage of the height or Points

                if (rbPercentage.Checked)
                {
                    graphPath.AddString(Resources.Label_Example, fF, fSt, (_imageHeight * (float)(numFontSizePercentage.Value/100)), new Rectangle(new Point(0, 0), image.Size), sFr);
                }
                else
                {
                    graphPath.AddString(Resources.Label_Example, fF, fSt, (fontDialog.Font.Size), new Rectangle(new Point(0, 0), image.Size), sFr);
                }

                #endregion

                #region Draw the path to the surface

                if (cbUseOutline.Checked)
                {
                    imgGr.DrawPath(new Pen(colorDialogOutline.Color, (float)numThick.Value), graphPath);
                }
                else
                {
                    imgGr.DrawPath(new Pen(Color.Transparent), graphPath);
                }

                #endregion

                #region Fill the path with a solid color or a hatch brush

                if (cbUseHatch.Checked)
                {
                    imgGr.FillPath(new HatchBrush((HatchStyle)cbHatchBrush.SelectedItem, colorDialogHatch.Color, colorDialog.Color), graphPath); 
                }
                else
                {
                    imgGr.FillPath(new SolidBrush(colorDialog.Color), graphPath);
                }

                #endregion

                pbExample.Image = image;

                GC.Collect();
            }
        }

        private void CaptionOptions_ResizeEnd(object sender, EventArgs e)
        {
            Preview();
        }
    }
}
