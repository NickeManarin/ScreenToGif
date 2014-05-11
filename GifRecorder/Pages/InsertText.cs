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

        private delegate void InsertTextDelegate(String text, Font font, Color foreColor);

        #endregion Delagated Method

        #region Variables

        private bool _blured;
        private Color _colorBackground;
        private Color _colorForeground;
        private string _content;
        private Font _font;
        private bool _isLegacy;

        /// <summary>
        /// Used to not execute OnFormClosing function
        /// </summary>
        private bool _okClicked = false;

        #endregion Variables

        #region Getter/Setter

        /// <summary>
        /// True if background should be a blured copy of the next frame. (Warning, if there is no next frame!)
        /// </summary>
        public bool Blured
        {
            get { return _blured; }
            set { _blured = value; }
        }

        /// <summary>
        /// The Background Color.
        /// </summary>
        public Color ColorBackground
        {
            get { return _colorBackground; }
            set { _colorBackground = value; }
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
        /// The Text typed by the user.
        /// </summary>
        public string Content
        {
            get { return _content; }
            set { _content = value; }
        }

        /// <summary>
        /// The selected Font of the text.
        /// </summary>
        public Font FontTitle
        {
            get { return _font; }
            set { _font = value; }
        }

        #endregion Getter/Setter

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

            // Get the current default values.
            _font = fontDialog.Font = tbContent.Font;
            _colorForeground = fontDialog.Color = tbContent.ForeColor;

            // Show the current Font.
            lblFont.Text = fontDialog.Font.Name + "; " + fontDialog.Font.SizeInPoints + "pt";
            pbForeColor.BackColor = fontDialog.Color;
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

            if (MessageBox.Show(Resources.Msg_CancelConfirm, Resources.Title_InsertText,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                == DialogResult.Yes)
                // Free the resources and quit
                this.Dispose();
            else
                e.Cancel = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            ErrorProvider txtContentErrorProvider = new ErrorProvider();

            // Check [txtContent] value
            _content = tbContent.Text.Trim();
            if (String.IsNullOrEmpty(_content))
            {
                txtContentErrorProvider.SetError(tbContent,
                   "You need to type something here");
                tbContent.Focus();
            }
            else
            {
                // Insert content in owner form using delegate method
                InsertTextDelegate insertTextMethod;

                if (_isLegacy)
                {
                    insertTextMethod = ((Legacy)Owner).InsertText;
                }
                else
                {
                    insertTextMethod = ((Legacy)Owner).InsertText;
                }
      
                insertTextMethod(_content, _font, _colorForeground);

                _okClicked = true;
                this.Close();
            }
        }

        private void InsertText_Load(object sender, EventArgs e)
        {
            tbContent.Select();
        }

        private void btnSelectFont_Click(object sender, EventArgs e)
        {
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                // Get selected font and color
                _font = tbContent.Font = fontDialog.Font;
                _colorForeground = tbContent.ForeColor = fontDialog.Color;

                // Display font information with sample
                lblFont.Text = _font.Name + "; " + _font.Size + "pt";
                pbForeColor.BackColor = _colorForeground;
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