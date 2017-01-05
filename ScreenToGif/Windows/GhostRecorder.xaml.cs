using System.Windows;
using System.Windows.Input;
using ScreenToGif.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for GhostRecorder.xaml
    /// </summary>
    public partial class GhostRecorder : GhostWindow
    {
        public GhostRecorder()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var monitor = Monitor.AllMonitors;

            Left = 0;
            Top = 0;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
