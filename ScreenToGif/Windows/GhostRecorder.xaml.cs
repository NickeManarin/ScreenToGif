using System.Windows;
using System.Windows.Input;
using ScreenToGif.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows
{
    public partial class GhostRecorder : GhostWindow
    {
        public GhostRecorder()
        {
            InitializeComponent();
        }

        //Load small window with a back button and a record button.

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var monitor = Monitor.AllMonitors;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!IsPickingRegion)
                    Close();

                IsPickingRegion = false;
            }
        }
    }
}
