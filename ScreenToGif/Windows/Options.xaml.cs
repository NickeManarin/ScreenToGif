using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Options()
        {
            InitializeComponent();
        }

        #region About

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                //TODO:
            }
        }

        #endregion

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Save all settings.
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //TODO: Load all settings.
        }
    }
}
