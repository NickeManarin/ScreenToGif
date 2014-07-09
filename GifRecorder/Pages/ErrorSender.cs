using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Error sender form.
    /// </summary>
    public partial class ErrorSender : Form
    {
        /// <summary>
        /// Default Constructor of the Sender utility.
        /// </summary>
        public ErrorSender()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Overload of the constructor. Sets the type of email to send.
        /// </summary>
        /// <param name="type">The type of email: 1 = error, 2 = suggestion</param>
        public ErrorSender(int type)
        {
            InitializeComponent();

            if (type == 1)
            {
                this.Text = "Report an Error";

                cbError.Checked = true;
                cbSuggestion.Checked = false;
            }
            else
            {
                this.Text = "Send us, your ideas!";

                cbError.Checked = false;
                cbSuggestion.Checked = true;
            }
        }

        /// <summary>
        /// If there is no checkBox checked, check the other one.
        /// </summary>
        private void cbError_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbSuggestion.Checked && !cbError.Checked)
            {
                cbSuggestion.Checked = true;
            }
        }

        /// <summary>
        /// If there is no checkBox checked, check the other one.
        /// </summary>
        private void cbSuggestion_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbSuggestion.Checked && !cbError.Checked)
            {
                cbError.Checked = true;
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            //Password and Email needed, but how we are going to do? ask for it? or use a dummy email?
            //How we could store our password?
        }
    }
}
