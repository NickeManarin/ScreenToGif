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
    /// 
    /// </summary>
    public partial class ErrorViewer : Form
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        public ErrorViewer(Exception ex)
        {
            InitializeComponent();

            lblTitle.Text = ex.Message;
            tbError.Text = ex.StackTrace;
        }
    }
}
