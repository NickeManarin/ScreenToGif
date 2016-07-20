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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    /// <summary>
    /// Interaction logic for WindowTest.xaml
    /// </summary>
    public partial class WindowTest : Window
    {
        private IntPtr _handle;

        public WindowTest()
        {
            InitializeComponent();
        }

        private void WindowTest_OnLoaded(object sender, RoutedEventArgs e)
        {
            _handle = new WindowInteropHelper(this).Handle;
        }

        private void WindowTest_OnLocationChanged(object sender, EventArgs e)
        {
            Native.Rect rect;
            Native.GetWindowRect(_handle, out rect);

            LeftLabel2.Content = rect.Left;
            TopLabel2.Content = rect.Top;
            RightLabel2.Content = rect.Right - rect.Left;
        }
    }
}
