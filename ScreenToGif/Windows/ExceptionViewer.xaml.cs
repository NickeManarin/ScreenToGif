using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for ExceptionViewer.xaml
    /// </summary>
    public partial class ExceptionViewer : Window
    {
        #region Variables

        private readonly Exception _exception;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="ex">The Exception to show.</param>
        public ExceptionViewer(Exception ex)
        {
            InitializeComponent();

            _exception = ex;

            #region Shows Information

            TypeLabel.Content = ex.GetType().Name;
            MessageTextBox.Text = ex.Message;
            StackTextBox.Text = ex.StackTrace;
            SourceTextBox.Text = ex.Source;

            if (ex.TargetSite != null)
                SourceTextBox.Text += "." + ex.TargetSite.Name;

            if (ex.InnerException != null)
            {
                InnerButton.IsEnabled = true;
            }

            #endregion
        }

        private void InnerButton_Click(object sender, RoutedEventArgs e)
        {
            var errorViewer = new ExceptionViewer(_exception.InnerException);
            errorViewer.ShowDialog();

            GC.Collect(1);
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
