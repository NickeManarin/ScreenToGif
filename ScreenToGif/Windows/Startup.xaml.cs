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
using Microsoft.Win32;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Startup.xaml
    /// </summary>
    public partial class Startup : Window
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Startup()
        {
            InitializeComponent();
        }

        #region Button Events

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            var recorder = new Recorder();
            this.Hide();

            var result = recorder.ShowDialog();

            if (result.HasValue && result.Value)
            {
                var editor = new Editor();
                GenericShowDialog(editor);
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.AddExtension = true;
            ofd.CheckFileExists = true;
            ofd.Filter = "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif";
            ofd.Title = "Open one image to insert";

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                var editor = new Editor();
                GenericShowDialog(editor);
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var create = new Create();
            var result = create.ShowDialog();

            if (result.HasValue && result.Value)
            {
                var editor = new Editor((int)create.WidthValue, (int)create.HeightValue, create.BrushValue);
                create.Close();
                GenericShowDialog(editor);

                return;
            }

            create.Close();
        }

        #endregion

        #region Functions

        private void GenericShowDialog(Window window)
        {
            this.Hide();
            window.ShowDialog();
            this.Close();
        }

        #endregion

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new Options();
            options.ShowDialog();
        }
    }
}
