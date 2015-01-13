using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    public partial class Progress : Form
    {
        #region Variables

        private bool _starting = true;

        #endregion

        #region Initialization

        /// <summary>
        /// The Progress Indicator form.
        /// </summary>
        public Progress()
        {
            InitializeComponent();

            //TODO: Localization.
            #region Localization of Labels

            //this.Text = Resources.Con_CaptionOptions;
            //lblSizeType.Text = Resources.Label_FontSizeAs;
            //lblColor.Text = Resources.Label_Font;
            //lblBarThickness.Text = Resources.Label_FontSize;
            //lblPosition.Text = Resources.Label_VerticalAlign;
            //lblPercentageSize.Text = Resources.Label_AsImageHeight;

            #endregion
        }

        private void Progress_Load(object sender, EventArgs e)
        {
            rbPercentage.Checked = Settings.Default.progressThickAsPercentage;
            rbPoint.Checked = !rbPercentage.Checked;

            #region Thickness

            if (rbPercentage.Checked)
            {
                lblPercentageSize.Text = Resources.Label_AsImageHeight;

                numThick.Maximum = 100;

                numThick.Value = (decimal)Settings.Default.progressThickPercentage * 100;
            }
            else
            {
                lblPercentageSize.Text = Resources.Label_Points;

                numThick.Maximum = 100;

                float newValue = Settings.Default.progressThickness;

                if (newValue > 100)
                {
                    numThick.Value = numThick.Maximum;
                }
                else
                {
                    numThick.Value = (decimal)Settings.Default.progressThickness;
                }
            }

            #endregion

            #region Position

            if (Settings.Default.progressPosition == 'T')
            {
                rbTop.Checked = true;
            }
            else if (Settings.Default.progressPosition == 'B')
            {
                rbBottom.Checked = true;
            }
            else if (Settings.Default.progressPosition == 'L')
            {
                rbLeft.Checked = true;
            }
            else
            {
                rbRight.Checked = true;
            }

            #endregion

            rbUseHatch.Checked = Settings.Default.progressUseHatch;
            rbUseSolid.Checked = !rbUseHatch.Checked;

            cbHatchBrush.DataSource = Enum.GetValues(typeof(HatchStyle));
            cbHatchBrush.SelectedItem = Settings.Default.progressHatch;

            cbHatchBrush.Enabled = rbUseHatch.Checked;

            pbColor.BackColor = colorDialog.Color = Settings.Default.progressColor;

            _starting = false;
            Preview();
        }

        #endregion

        #region Events

        private void rbPercentage_CheckedChanged(object sender, EventArgs e)
        {
            if (rbPercentage.Checked)
            {
                lblPercentageSize.Text = Resources.Label_AsImageHeight;

                numThick.Maximum = 100;

                numThick.Value = (decimal)Settings.Default.progressThickPercentage * 100;
            }
            else
            {
                lblPercentageSize.Text = Resources.Label_Points;

                numThick.Maximum = 100;

                float newValue = Settings.Default.progressThickness;

                if (newValue > 100)
                {
                    numThick.Value = numThick.Maximum;
                }
                else
                {
                    numThick.Value = (decimal)Settings.Default.progressThickness;
                }
            }

            Preview();
        }

        private void rbUseSolid_CheckedChanged(object sender, EventArgs e)
        {
            cbHatchBrush.Enabled = rbUseHatch.Checked;

            Preview();
        }

        private void pbColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pbColor.BackColor = colorDialog.Color;

                Preview();
            }
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            Preview();
        }

        #endregion

        private void btnOk_Click(object sender, EventArgs e)
        {
            #region Settings

            Settings.Default.progressThickAsPercentage = rbPercentage.Checked;

            if (rbPercentage.Checked)
            {
                Settings.Default.progressThickPercentage = (float)(numThick.Value / 100);
            }
            else
            {
                Settings.Default.progressThickness = (float)numThick.Value;
            }

            #region Position

            if (rbTop.Checked)
            {
                Settings.Default.progressPosition = 'T';
            }
            else if (rbBottom.Checked)
            {
                Settings.Default.progressPosition = 'B';
            }
            else if (rbLeft.Checked)
            {
                Settings.Default.progressPosition = 'L';
            }
            else
            {
                Settings.Default.progressPosition = 'R';
            }

            #endregion

            Settings.Default.progressColor = colorDialog.Color;
            Settings.Default.progressUseHatch = rbUseHatch.Checked;
            Settings.Default.progressHatch = (HatchStyle)cbHatchBrush.SelectedItem;

            #endregion

            Settings.Default.Save();

            this.DialogResult = DialogResult.OK;
        }

        private void Preview()
        {
            if (_starting) return;

            var image = new Bitmap(pbExample.Size.Width, pbExample.Size.Height);

            using (Graphics imgGr = Graphics.FromImage(image))
            {
                const float actualValue = 50;
                const float maxValue = 100;

                Brush fillBrush = null;

                #region Pen

                var thickness = (float) numThick.Value;

                if (rbUseHatch.Checked)
                {
                    fillBrush = new HatchBrush((HatchStyle) cbHatchBrush.SelectedItem, colorDialog.Color, Color.Transparent);
                }
                else
                {
                    fillBrush = new SolidBrush(colorDialog.Color);
                }

                #endregion

                #region Position

                var rectangle = new Rectangle();
                
                if (rbTop.Checked)
                {
                    // 100 * 50 / 100
                    float width = ((100 * actualValue) / maxValue) / 100;
                    rectangle = new Rectangle(0, 0, (int)(pbExample.Size.Width * width), (int) thickness);
                }
                else if (rbBottom.Checked)
                {
                    float width = ((100 * actualValue) / maxValue) / 100;
                    rectangle = new Rectangle(0, (int)(pbExample.Size.Height - thickness), (int)(pbExample.Size.Width * width), (int)thickness);
                }
                else if (rbLeft.Checked)
                {
                    float height = ((100 * actualValue) / maxValue) / 100;
                    rectangle = new Rectangle(0, 0, (int)thickness, (int)(pbExample.Size.Height * height));
                }
                else
                {
                    float height = ((100 * actualValue) / maxValue) / 100;
                    rectangle = new Rectangle((int)(pbExample.Size.Width - thickness), 0, (int)thickness, (int)(pbExample.Size.Height * height));
                }

                imgGr.FillRectangle(fillBrush, rectangle);

                #endregion

                pbExample.Image = image;

                GC.Collect();
            }
        }
    }
}
