using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Startup : Window
    {
        public Startup()
        {
            InitializeComponent();
        }

        #region Events

        private void Startup_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Argument.FileNames.Any())
                Editor_Executed(sender, null);
        }

        private void Buttons_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Recorder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var recorder = new Recorder {Owner = this};
            Application.Current.MainWindow = recorder;

            Hide();

            var result = recorder.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // If Close
                Environment.Exit(0);
            }
            else if (result.HasValue)
            {
                #region If Backbutton or Stop Clicked

                if (recorder.ExitArg == ExitAction.Recorded)
                {
                    var editor = new Editor { ListFrames = recorder.ListFrames };
                    
                    GenericShowDialog(editor);
                    return;
                }

                Show();

                #endregion
            }
        }

        private void WebcamRecorder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var webcam = new Webcam {Owner = this};
            Application.Current.MainWindow = webcam;

            Hide();

            var result = webcam.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // If Close
                Environment.Exit(0);
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

                Show();

                #endregion
            }
        }

        private void Board_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var board = new Board {Owner = this};
            Application.Current.MainWindow = board;

            Hide();

            var result = board.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // If Close
                Environment.Exit(0);
            }
            else if (result.HasValue)
            {
                #region If Backbutton or Stop Clicked

                if (board.ExitArg == ExitAction.Recorded)
                {
                    var editor = new Editor { ListFrames = board.ListFrames };

                    GenericShowDialog(editor);
                    return;
                }

                Show();

                #endregion
            }
        }

        private void Editor_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var editor = new Editor();
            GenericShowDialog(editor);
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var options = new Options {Owner = this};
            options.ShowDialog();
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var test = new TestField();
            test.ShowDialog();
        }

        #endregion

        #region Methods

        private void GenericShowDialog(Window window)
        {
            Hide();

            window.Owner = this;
            Application.Current.MainWindow = window;

            window.ShowDialog();

            Close();
        }

        #endregion
    }
}
