using System;
using System.Windows;
using ScreenToGif.Util.Enum;

namespace ScreenToGif.Windows.Other
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
            Hide();

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

                Show();

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
