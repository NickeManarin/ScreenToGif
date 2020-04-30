using System;
using System.Windows;

namespace PositioningTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            UpdateText();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateText();
        }

        private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            TopLeftTextBlock.Text = $"{Top} • {Left}";
            TopRightTextBlock.Text = $"{Top} • {Left + Width}";

            SizeTextBlock.Text = $"{Width} x {Height}";

            BottomLeftTextBlock.Text = $"{Top + Height} • {Left}";
            BottomRightTextBlock.Text = $"{Top + Height} • {Left + Width}";
        }
    }
}