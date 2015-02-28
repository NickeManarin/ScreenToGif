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
using System.Windows.Navigation;
using System.Windows.Shapes;
using HookTest.Util;

namespace HookTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UserActivityHook _hook;

        public MainWindow()
        {
            InitializeComponent();

            _hook = new UserActivityHook(true, true);
            _hook.OnMouseActivity += _hook_OnMouseActivity;
            _hook.KeyDown += _hook_KeyDown;
        }

        void _hook_KeyDown(object sender, CustomKeyEventArgs e)
        {
            KeyboardTextBox.Text += String.Format(">> {0} - {1}{2}", e.Key, DateTime.Now.ToString("HH:mm:ss"), Environment.NewLine);
            MouseTextBox.ScrollToLine(MouseTextBox.LineCount - 1);
        }

        void _hook_OnMouseActivity(object sender, CustomMouseEventArgs e)
        {
            MouseTextBox.Text += String.Format(">> {0} - {1}{2}", e.Button, DateTime.Now.ToString("HH:mm:ss"), Environment.NewLine);
            MouseTextBox.ScrollToLine(MouseTextBox.LineCount - 1);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _hook.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _hook.Stop();
        }
    }
}
