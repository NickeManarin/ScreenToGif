using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using ScreenToGif.Controls;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Create.xaml
    /// </summary>
    public partial class Create : Window
    {
        #region Properties

        /// <summary>
        /// The Height of the image to be created.
        /// </summary>
        public int HeightValue { get; set; }

        /// <summary>
        /// The Width of the image to be created.
        /// </summary>
        public int WidthValue { get; set; }

        /// <summary>
        /// The Brush of the image to be created.
        /// </summary>
        public Color Color { get; set; }

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Create()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            int width, heigth;

            #region Validation

            if (!int.TryParse(WidthText.Text, out width))
            {
                //TODO: Message.
                return;
            }

            if (!int.TryParse(HeightText.Text, out heigth))
            {
                //TODO: Message.
                return;
            }

            var selected = BackCombo.SelectedItem;

            if (selected == null) return;

            #endregion

            HeightValue = heigth;
            WidthValue = width;

            var colorBrush = ((Border)((StackPanel)selected).Children[0]).Background as SolidColorBrush;
            
            if (colorBrush != null)
                Color = colorBrush.Color;

            this.DialogResult = true;
            this.Close();
        }

        #region Input Events

        private void SizeBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as NumericTextBox;

            if (textBox == null) return;

            textBox.Value = Convert.ToInt32(textBox.Text);
            textBox.Value = e.Delta > 0 ? textBox.Value + 10 : textBox.Value - 10;
        }

        private void WidthText_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckValues(sender);
        }

        private void WidthText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckValues(sender);
            }
        }

        private void CheckValues(object sender)
        {
            var textBox = sender as NumericTextBox;

            if (textBox == null) return;

            if (Convert.ToInt32(textBox.Text) > textBox.MaxValue)
            {
                textBox.Value = textBox.MaxValue;
            }

            if (Convert.ToInt32(textBox.Text) < textBox.MinValue)
            {
                textBox.Value = textBox.MinValue;
            }
        }

        #endregion
    }
}
