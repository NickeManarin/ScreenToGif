using System;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Error Viewer Form.
    /// </summary>
    public partial class ExceptionViewer : Form
    {
        #region Variables

        private readonly Exception _exception;

        #endregion

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="ex">The Exception to show.</param>
        public ExceptionViewer(Exception ex)
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

            btnClose.Select();

            #endregion

            #region Localize Labels

            this.Text = Resources.TitleExceptionViewer;

            #endregion
        }

        private void btnInnerException_Click(object sender, EventArgs e)
        {
            var errorViewer = new ExceptionViewer(_exception.InnerException);
            errorViewer.ShowDialog();

            errorViewer.Dispose();
            GC.Collect(1);
        }
    }
}
