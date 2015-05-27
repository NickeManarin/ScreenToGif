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
using ScreenToGif.Util.Writers;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for MiniPath.xaml
    /// </summary>
    public partial class MiniPath : Window
    {
        public MiniPath()
        {
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //ExamplePath.Data = Geometry.Parse(InputTextBox.Text);
            }
            catch (Exception ex)
            {
                //LogWriter.Log(ex, "Geometry Parse error", InputTextBox.Text);
            }
        }
    }
}
