using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Creates a Title Frame
    /// </summary>
    public partial class TitleFrameSettings : Form
    {
        #region Variables

        private Color _colorBackground;
        private Font _font;
        private string _content;
        private Color _colorForeground;
        private bool _blured;

        #endregion

        #region Getter/Setter

        /// <summary>
        /// The Background Color.
        /// </summary>
        public Color ColorBackground
        {
            get { return _colorBackground; }
            set { _colorBackground = value; }
        }

        /// <summary>
        /// The selected Font of the text.
        /// </summary>
        public Font FontTitle
        {
            get { return _font; }
            set { _font = value; }
        }

        /// <summary>
        /// The Text typed by the user.
        /// </summary>
        public string Content
        {
            get { return _content; }
            set { _content = value; }
        }

        /// <summary>
        /// The Foreground color of the text.
        /// </summary>
        public Color ColorForeground
        {
            get { return _colorForeground; }
            set { _colorForeground = value; }
        }

        /// <summary>
        /// True if background should be a blured copy of the next frame. (Warning, if there is no next frame!)
        /// </summary>
        public bool Blured
        {
            get { return _blured; }
            set { _blured = value; }
        }

        #endregion

        /// <summary>
        /// Creates a Title Frame.
        /// </summary>
        public TitleFrameSettings(Bitmap bitmap = null)
        {
            InitializeComponent();

            #region Loads the fonts

            IList<string> fontNames = FontFamily.Families.Select(f => f.Name).ToList();
            cbFonts.DataSource = fontNames;

            #endregion

            //Gets the last used values.
            fontDialog.Font = Settings.Default.fontTitleFrame;
            foreColorDialog.Color = fontDialog.Color = Settings.Default.forecolorTitleFrame;
            backColorDialog.Color = Settings.Default.backcolorTitleFrame;

            #region Updates the UI

            FontUpdate();

            pbBackground.BackColor = backColorDialog.Color;
            
            lblExample.BackColor = backColorDialog.Color;
            lblExample.ForeColor = fontDialog.Color;
            lblExample.Font = fontDialog.Font;
            tbTitle.Font = fontDialog.Font;

            #endregion

            #region Sets the current default values

            this.FontTitle = fontDialog.Font;
            this.ColorBackground = backColorDialog.Color;
            this.ColorForeground = fontDialog.Color;

            #endregion

            //Sets the example text and gives selction to it.
            tbTitle.Text = Resources.Label_TitleContent;
            tbTitle.Select();

            EventRegister(true);

            #region Localize Labels

            lblContent.Text = Resources.Label_Content;
            gbBackground.Text = Resources.Label_Background;
            lblExample.Text = Resources.Label_Example;
            this.Text = Resources.Title_TitleFrame;

            #endregion
        }

        #region Events

        #region Background

        private void btnBackColor_Click(object sender, EventArgs e)
        {
            if (backColorDialog.ShowDialog(this) == DialogResult.OK)
            {
                Settings.Default.backcolorTitleFrame = backColorDialog.Color;

                this.ColorBackground = backColorDialog.Color;
                pbBackground.BackColor = backColorDialog.Color;

                lblExample.BackColor = backColorDialog.Color;
            }
        }

        private void pbBackground_MouseHover(object sender, EventArgs e)
        {
            tooltip.Show(pbBackground.BackColor.Name, pbBackground, 2000); 
        }

        private void rbSolidColor_CheckedChanged(object sender, EventArgs e)
        {
            pbBackground.Enabled = rbSolidColor.Checked;
            btnBackColor.Enabled = rbSolidColor.Checked;

            lblExample.Visible = rbSolidColor.Checked;

            Blured = rbBlured.Checked;
        }

        #endregion

        #region Font

        private void btnSelectFont_Click(object sender, EventArgs e)
        {
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                EventRegister(false);

                cbFonts.SelectedItem = fontDialog.Font.Name;
                numSize.Value = (decimal) fontDialog.Font.SizeInPoints;
                tbTitle.Font = fontDialog.Font;
                pbForeColor.BackColor = fontDialog.Color;

                lblExample.ForeColor = fontDialog.Color;
                lblExample.Font = fontDialog.Font;

                btnBold.Checked = fontDialog.Font.Bold;
                btnItalics.Checked = fontDialog.Font.Italic;
                btnUnderline.Checked = fontDialog.Font.Underline;

                EventRegister(true);
            }
        }

        private void pbForeColor_MouseHover(object sender, EventArgs e)
        {
            tooltip.Show(pbForeColor.BackColor.Name, pbForeColor, 2000);
        }

        private void pbForeColor_Click(object sender, EventArgs e)
        {
            if (foreColorDialog.ShowDialog() == DialogResult.OK)
            {
                pbForeColor.BackColor = fontDialog.Color = foreColorDialog.Color;

                ValueChanged(null, null);
            }
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            #region Update the UI

            #region FontStyle

            var fontStyle = new FontStyle();

            if (btnBold.Checked)
                fontStyle |= FontStyle.Bold;

            if (btnItalics.Checked)
                fontStyle |= FontStyle.Italic;

            if (btnUnderline.Checked)
                fontStyle |= FontStyle.Underline;

            #endregion

            tbTitle.Font = fontDialog.Font = new Font(cbFonts.SelectedItem.ToString(),
                (float)numSize.Value, fontStyle);

            lblExample.ForeColor = fontDialog.Color;
            lblExample.Font = fontDialog.Font;

            #endregion

            #region Update the Properties

            cbFonts.SelectedItem = fontDialog.Font.Name;
            numSize.Value = (decimal)fontDialog.Font.SizeInPoints;
            pbForeColor.BackColor = fontDialog.Color;
            pbBackground.BackColor = backColorDialog.Color;

            #endregion
        }

        #endregion

        private void TitleFrameSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            #region Persist Properties

            this.Content = tbTitle.Text;
            this.ColorBackground = backColorDialog.Color;
            this.ForeColor = foreColorDialog.Color;
            this.FontTitle = fontDialog.Font;

            #endregion

            #region Save Settings

            Settings.Default.fontTitleFrame = fontDialog.Font;
            Settings.Default.forecolorTitleFrame = fontDialog.Color;
            Settings.Default.backcolorTitleFrame = backColorDialog.Color;
            Settings.Default.Save();

            #endregion
        }

        #endregion

        private void FontUpdate()
        {
            cbFonts.SelectedItem = fontDialog.Font.Name;
            numSize.Value = (decimal)fontDialog.Font.SizeInPoints;
            pbForeColor.BackColor = foreColorDialog.Color = fontDialog.Color;

            btnBold.Checked = fontDialog.Font.Bold;
            btnItalics.Checked = fontDialog.Font.Italic;
            btnUnderline.Checked = fontDialog.Font.Underline;
        }

        private void EventRegister(bool register)
        {
            #region Event Register

            if (register)
            {
                cbFonts.SelectedValueChanged += ValueChanged;
                numSize.ValueChanged += ValueChanged;
                btnBold.CheckedChanged += ValueChanged;
                btnItalics.CheckedChanged += ValueChanged;
                btnUnderline.CheckedChanged += ValueChanged;
            }
            else
            {
                cbFonts.SelectedValueChanged -= ValueChanged;
                numSize.ValueChanged -= ValueChanged;
                btnBold.CheckedChanged -= ValueChanged;
                btnItalics.CheckedChanged -= ValueChanged;
                btnUnderline.CheckedChanged -= ValueChanged;
            }

            #endregion
        }
    }
}
