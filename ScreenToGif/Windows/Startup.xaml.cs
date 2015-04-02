using System;
using System.Windows;
using System.Windows.Input;
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
        //Startup > Recording > Edit - Done
        //Startup > Recording > Exit - Done
        //Startup > Recording > Startup - Done
        //Recording > Startup - Done
        //Recording > Edit - Done
        //Recording > Exit - Done
        //Edit - Done
        //TODO: Edit flux...

        #region Button Events

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            var recorder = new Recorder();
            this.Hide();

            var result = recorder.ShowDialog();

            if (result.HasValue && result.Value)
            {
                #region If Close

                Environment.Exit(0);

                #endregion
            }
            else if (result.HasValue)
            {
                #region If Backbutton or Stop Clicked

                if (recorder.ExitArg == ExitAction.Recorded)
                {
                    var editor = new Editor {ListFrames = recorder.ListFrames};
                    GenericShowDialog(editor);
                    return;
                }

                this.Show();

                #endregion
            }
        }

        private void WebcamButton_Click(object sender, RoutedEventArgs e)
        {
            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            if (result.HasValue && result.Value)
            {
                #region If Close

                Environment.Exit(0);

                #endregion
            }
            else if (result.HasValue)
            {
                #region If Backbutton or Stop Clicked

                if (webcam.ExitArg == ExitAction.Recorded)
                {
                    var editor = new Editor { ListFrames = webcam.ListFrames };
                    GenericShowDialog(editor);
                    return;
                }

                this.Show();

                #endregion
            }
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
