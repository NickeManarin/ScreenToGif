using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        public Brush BrushValue { get; set; }

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

            BrushValue = ((Border)((StackPanel)selected).Children[0]).Background;

            this.DialogResult = true;
            this.Close();
        }

        #region Input Events

        private void Text_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (String.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (IsTextDisallowed(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (String.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        private void PastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                var text = (String)e.DataObject.GetData(typeof(String));

                if (IsTextDisallowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool IsTextDisallowed(string text)
        {
            var regex = new Regex("[^0-9]+");
            return regex.IsMatch(text);
        }

        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox == null) return;

            if (textBox.Text == String.Empty)
            {
                textBox.Text = "50";
            }
        }

        #endregion
    }
}
