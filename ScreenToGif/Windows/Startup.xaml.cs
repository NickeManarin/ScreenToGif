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
using ScreenToGif.Util.Enum;

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

        //TODO: Revise all the flux logic.
        //User can use the program in various ways.
        //Startup > Recording > Edit
        //Startup > Recording > Exit
        //Startup > Recording > Startup
        //Recording > Startup
        //Recording > Edit
        //Recording > Exit
        //TODO: Edit flux...

        #region Button Events

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            var recorder = new Recorder();
            this.Hide();

            var result = recorder.ShowDialog();

            //TODO: Change to the Stop/Back button.
            //If FrameCount == 0, StopButton became the BackButton.
            //Else, Became the Stop button and return something different.

            if (result.HasValue && result.Value)
            {
                Environment.Exit(0);
            }
            else if (result.HasValue && !result.Value)
            {
                if (recorder._exit == ExitAction.Edit)
                {
                    var editor = new Editor();
                    GenericShowDialog(editor);
                    return;
                }

               this.Show();
            }
        }

        private void WebcamButton_Click(object sender, RoutedEventArgs e)
        {
            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //TODO: Send the list of frames.
                var editor = new Editor();
                webcam.Close();
                GenericShowDialog(editor);

                return;
            }

            webcam.Close();
        }

        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new Editor();
            GenericShowDialog(editor);
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

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new Options();
            options.ShowDialog();
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
    }
}
