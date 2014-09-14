using System;
using System.Drawing;
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

        private Color _foregroundColor;
        private string _content;
        private Font _font;
        private bool _isLegacy;

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

            //If this page was called by the Legacy form.
            _isLegacy = isLegacy;

            fontDialog.Font = Settings.Default.fontInsertText;
            fontDialog.Color = Settings.Default.forecolorInsertText;

            //Get the current default values.
            _font = fontDialog.Font = tbContent.Font;
            _foregroundColor = fontDialog.Color = tbContent.ForeColor;

            //Show the current Font.
            lblFont.Text = fontDialog.Font.Name + "; " + fontDialog.Font.SizeInPoints + "pt";
            pbForeColor.BackColor = fontDialog.Color;

            #region Localize Labels

            lblFontTitle.Text = Resources.Label_Font;
            lblContent.Text = Resources.Label_Content;
            this.Text = Resources.Title_InsertText;

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

            //Check [txtContent] value
            _content = tbContent.Text.Trim();

            if (String.IsNullOrEmpty(_content))
            {
                txtContentErrorProvider.SetError(tbContent,
                   "You need to type something here"); //TODO: Localize
                tbContent.Focus();
            }
            else
            {
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
      
                insertTextMethod(_content);

                #region Save The Used Font and Color

                Settings.Default.fontInsertText = _font;
                Settings.Default.forecolorInsertText = _foregroundColor;
                Settings.Default.Save();

                #endregion

                _okClicked = true;
                this.Close();
            }
        }

        private void btnSelectFont_Click(object sender, EventArgs e)
        {
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                //Get selected font and color
                _font = tbContent.Font = fontDialog.Font;
                _foregroundColor = fontDialog.Color; //tbContent.ForeColor =

                //Display font information with sample
                lblFont.Text = _font.Name + "; " + _font.Size + "pt";
                pbForeColor.BackColor = _foregroundColor;
            }

            tbContent.Focus();
        }

        private void pbForeColor_MouseHover(object sender, EventArgs e)
        {
            if (pbForeColor.BackColor.IsNamedColor)
            {
                tooltip.Show(pbForeColor.BackColor.Name, pbForeColor, 2000);
            }
        }
    }
}