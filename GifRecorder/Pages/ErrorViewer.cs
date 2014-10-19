using System;
using System.Windows.Forms;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Error Viewer Form.
    /// </summary>
    public partial class ErrorViewer : Form
    {
        #region Variables

        private readonly Exception _exception;

        #endregion

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="ex">The Exception to show.</param>
        public ErrorViewer(Exception ex)
        {
            InitializeComponent();

            _exception = ex;

            #region Shows Information

            lblTitle.Text = ex.GetType().Name;
            tbMessage.Text = ex.Message;
            tbError.Text = ex.StackTrace;
            tbSource.Text = ex.Source;
            
            if (ex.TargetSite != null)
                tbSource.Text += "." + ex.TargetSite.Name;

            if (ex.InnerException != null)
            {
                btnInnerException.Enabled = true;
            }

            btnOk.Focus();

            #endregion
        }

        private void btnInnerException_Click(object sender, EventArgs e)
        {
            var errorViewer = new ErrorViewer(_exception.InnerException);
            errorViewer.ShowDialog();

            errorViewer.Dispose();
            GC.Collect(1);
        }
    }
}
