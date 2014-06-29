using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// The processing info page.
    /// </summary>
    public partial class Processing : UserControl
    {
        #region Variables

        private int _max = 0;
        private string _fileName = "";

        #endregion

        public Processing()
        {
            //To localize this page too.
            if (CultureUtil.Lang.Length == 2)
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(CultureUtil.Lang);
            }

            InitializeComponent();
        }

        #region Setters

        /// <summary>
        /// Set the status.
        /// </summary>
        /// <param name="num">The actual status.</param>
        public void SetStatus(int num)
        {
            progressBarEncoding.Value = num + 1;
            lblValue.Text = (num + 1) + Resources.Title_Thread_out_of + _max;
        }

        /// <summary>
        /// The maximum value to the progress bar.
        /// </summary>
        /// <param name="max">The amount of frames in the process.</param>
        public void SetMaximumValue(int max)
        {
            _max = max + 1;
            progressBarEncoding.Maximum = _max;
        }

        /// <summary>
        /// Sets the Text of the page to a state that is not Encoding.
        /// </summary>
        /// <param name="text">The text to show as title of the page.</param>
        public void SetPreEncoding(string text)
        {
            lblProcessing.Text = text;
            lblValue.Visible = false;
            progressBarEncoding.Style = ProgressBarStyle.Marquee;
        }

        /// <summary>
        /// Sets the Text of the page to a state that represents "Encoding".
        /// </summary>
        /// <param name="text">The text to show as title of the page.</param>
        public void SetEncoding(string text)
        {
            lblProcessing.Text = text;
            lblValue.Visible = true;
            progressBarEncoding.Style = ProgressBarStyle.Continuous;
        }

        /// <summary>
        /// Sets the "Encoding Done" state.
        /// </summary>
        /// <param name="fileName">The name of the generated Gif file.</param>
        /// <param name="title">The title to show.</param>
        public void SetFinishedState(string fileName, string title)
        {
            _fileName = fileName;

            string size = "";
            try
            {
                var f = new FileInfo(fileName);
                size = BytesToString(f.Length);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while getting the file size.");
            }


            lblProcessing.Text = title;
            lblSize.Text = size;

            lblValue.Visible = false;
            linkClose.Visible = true;
            linkOpenFile.Visible = true;
        }

        #endregion

        #region Functions

        private string BytesToString(long byteCount)
        {
            string[] suf = {" B", " KB", " MB"}; //I hope no one make a gif with GB's of size. haha - Nicke

            if (byteCount == 0)
                return "0" + suf[0];

            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);

            return (Math.Sign(byteCount) * num) + suf[place];
        }

        #endregion

        #region Link Events

        private void linkOpenFile_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(_fileName);
            }
            catch (Exception exception)
            {
                LogWriter.Log(exception, "Missing File");
                MessageBox.Show(this, Resources.MsgBox_ErrorOpenning + exception.Message, "Missing File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkClose_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Dispose();
        }

        private void linkOpenFile_MouseHover(object sender, EventArgs e)
        {
            if (_fileName.Length > 1)
            {
                toolTip.Show(_fileName, linkOpenFile, (int)linkOpenFile.Width/2, linkOpenFile.Height, 3000);
            }
        }

        #endregion
    }
}
