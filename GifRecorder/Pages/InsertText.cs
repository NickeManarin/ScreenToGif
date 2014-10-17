using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Page that gives the user the ability to type a text to put in a frame.
    /// </summary>
    public partial class InsertText : Form
    {
        #region Delagated Method

        private delegate void InsertTextDelegate(String text);

        #endregion Delagated Method

        #region Variables

        /// <summary>
        /// True if owner in the Legacy form.
        /// </summary>
        private readonly bool _isLegacy;

        /// <summary>
        /// Used to not execute OnFormClosing function
        /// </summary>
        private bool _okClicked = false;

        /// <summary>
        /// Used to not execute OnFormClosing function
        /// </summary>
        private bool _cancelClicked;

        #endregion Variables

        /// <summary>
        /// Initialize the form
        /// </summary>
        /// <param name="isLegacy">True if the owner is the Legacy form.
        /// False if the owner is the Modern form.</param>
        public InsertText(bool isLegacy)
        {
            InitializeComponent();

            #region Loads the fonts

            IList<string> fontNames = FontFamily.Families.Select(f => f.Name).ToList();
            cbFonts.DataSource = fontNames;

            #endregion

            //If this page was called by the Legacy form.
            _isLegacy = isLegacy;

            fontDialog.Font = Settings.Default.fontInsertText;
            colorDialog.Color = fontDialog.Color = pbForeColor.BackColor =
                Settings.Default.forecolorInsertText;

            FontUpdate();

            #region Localize Labels

            btnMoreOptions.Text = "More Options"; //TODO: Localize.
            lblContent.Text = Resources.Label_Content;
            this.Text = Resources.Title_InsertText;

            #endregion

            #region Event Register

            cbFonts.SelectedValueChanged += ValueChanged;
            numSize.ValueChanged += ValueChanged;
            btnBold.CheckedChanged += ValueChanged;
            btnItalics.CheckedChanged += ValueChanged;
            btnUnderline.CheckedChanged += ValueChanged;

            #endregion
        }

        private void InsertText_Load(object sender, EventArgs e)
        {
            tbContent.Select();
        }

        /// <summary>
        /// Request user a confirmation before closing the form
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;
            if (_okClicked) return;
            if (_cancelClicked) return;

            if (MessageBox.Show(Resources.Msg_CancelConfirm, Resources.Title_InsertText,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                == DialogResult.Yes)
                //Free the resources and quit
                this.Dispose();
            else
                e.Cancel = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cancelClicked = true;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            var txtContentErrorProvider = new ErrorProvider();

            if (String.IsNullOrEmpty(tbContent.Text.Trim()))
            {
                txtContentErrorProvider.SetError(tbContent,
                   "You need to type something here"); //TODO: Localize
                tbContent.Focus();

                return;
            }

            #region Save The Used Font and Color

            InsertText_FormClosing(null, null);

            #endregion

            //Insert content in owner form using delegate method
            InsertTextDelegate insertTextMethod;

            if (_isLegacy)
            {
                insertTextMethod = ((Legacy)Owner).InsertText;
            }
            else
            {
                insertTextMethod = ((Modern)Owner).InsertText;
            }

            insertTextMethod(tbContent.Text.Trim());

            _okClicked = true;
            this.Close();
        }

        private void btnMoreOptions_Click(object sender, EventArgs e)
        {
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                EventRegister(false);

                FontUpdate();

                EventRegister(true);
            }

            tbContent.Focus();
        }

        private void pbForeColor_MouseHover(object sender, EventArgs e)
        {
            tooltip.Show(pbForeColor.BackColor.Name, pbForeColor, 2000);
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            #region FontStyle

            var fontStyle = new FontStyle();

            if (btnBold.Checked)
                fontStyle |= FontStyle.Bold;

            if (btnItalics.Checked)
                fontStyle |= FontStyle.Italic;

            if (btnUnderline.Checked)
                fontStyle |= FontStyle.Underline;

            #endregion

            tbContent.Font = fontDialog.Font = new Font(cbFonts.SelectedItem.ToString(),
                (float)numSize.Value, fontStyle);

            FontUpdate();
        }

        private void InsertText_FormClosing(object sender, FormClosingEventArgs e)
        {
            ValueChanged(null, null);

            #region Save Properties

            Settings.Default.fontInsertText = fontDialog.Font;
            Settings.Default.forecolorInsertText = fontDialog.Color;
            Settings.Default.Save();

            #endregion
        }

        private void pbForeColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pbForeColor.BackColor = fontDialog.Color = colorDialog.Color;

                ValueChanged(null, null);
            }
        }

        /// <summary>
        /// Updates the UI with the selected font information.
        /// </summary>
        private void FontUpdate()
        {
            cbFonts.SelectedItem = fontDialog.Font.Name;
            numSize.Value = (decimal)fontDialog.Font.SizeInPoints;
            tbContent.Font = fontDialog.Font;
            pbForeColor.BackColor = colorDialog.Color = fontDialog.Color;

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