using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Windows.Other
{
    public partial class ErrorDialog : Window
    {
        public Exception Exception { get; set; }

        public ErrorDialog()
        {
            InitializeComponent();
        }

        #region Methods

        private Canvas GetIcon(Icons icon)
        {
            switch (icon)
            {
                case Icons.Error:
                    return (Canvas)FindResource("Vector.Cancel.Round");
                case Icons.Info:
                    return (Canvas)FindResource("Vector.Info");
                case Icons.Success:
                    return (Canvas)FindResource("Vector.Ok.Round");
                case Icons.Warning:
                    return (Canvas)FindResource("Vector.Warning");
                case Icons.Question:
                    return (Canvas)FindResource("Vector.Question");

                default:
                    return (Canvas)FindResource("Vector.Info");
            }
        }

        private void PrepareOk(string title, string instruction, string observation, Icons icon)
        {
            CancelButton.Visibility = Visibility.Collapsed;
            YesButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;
            DetailsButton.Visibility = Exception != null ? Visibility.Visible : Visibility.Collapsed;

            OkButton.Focus();

            IconViewbox.Child = GetIcon(icon);

            InstructionLabel.Content = instruction;
            DetailsRun.Text = observation;
            Title = title;
        }

        private void PrepareOkCancel(string title, string instruction, string observation, Icons icon)
        {
            YesButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;
            DetailsButton.Visibility = Exception != null ? Visibility.Visible : Visibility.Collapsed;

            CancelButton.Focus();

            IconViewbox.Child = GetIcon(icon);

            InstructionLabel.Content = instruction;
            DetailsRun.Text = observation;
            Title = title;
        }

        private void PrepareAsk(string title, string instruction, string observation, Icons icon)
        {
            CancelButton.Visibility = Visibility.Collapsed;
            OkButton.Visibility = Visibility.Collapsed;
            DetailsButton.Visibility = Exception != null ? Visibility.Visible : Visibility.Collapsed;

            NoButton.Focus();

            IconViewbox.Child = GetIcon(icon);

            InstructionLabel.Content = instruction;
            DetailsRun.Text = observation;
            Title = title;
        }

        /// <summary>
        /// Shows a Ok dialog.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="instruction">The main instruction.</param>
        /// <param name="observation">A complementar observation.</param>
        /// <param name="exception">The exception of the error.</param>
        /// <param name="icon">The image of the dialog.</param>
        /// <returns>True if Ok</returns>
        public static bool Ok(string title, string instruction, string observation, Exception exception, Icons icon = Icons.Error)
        {
            var dialog = new ErrorDialog { Exception = exception };
            dialog.PrepareOk(title, instruction, observation.Replace(@"\n", Environment.NewLine).Replace(@"\r", ""), icon);
            var result = dialog.ShowDialog();

            return result.HasValue && result.Value;
        }

        /// <summary>
        /// Shows a Ok/Cancel dialog.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="instruction">The main instruction.</param>
        /// <param name="observation">A complementar observation.</param>
        /// <param name="exception">The exception of the error.</param>
        /// <param name="icon">The image of the dialog.</param>
        /// <returns>True if Ok</returns>
        public static bool OkCancel(string title, string instruction, string observation, Exception exception, Icons icon = Icons.Error)
        {
            var dialog = new ErrorDialog { Exception = exception };
            dialog.PrepareOkCancel(title, instruction, observation.Replace(@"\n", Environment.NewLine).Replace(@"\r", ""), icon);
            var result = dialog.ShowDialog();

            return result.HasValue && result.Value;
        }

        /// <summary>
        /// Shows a Yes/No dialog.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="instruction">The main instruction.</param>
        /// <param name="observation">A complementar observation.</param>
        /// <param name="exception">The exception of the error.</param>
        /// <param name="icon">The image of the dialog.</param>
        /// <returns>True if Yes</returns>
        public static bool Ask(string title, string instruction, string observation, Exception exception, Icons icon = Icons.Question)
        {
            var dialog = new ErrorDialog { Exception = exception };
            dialog.PrepareAsk(title, instruction, observation.Replace(@"\n", Environment.NewLine).Replace(@"\r", ""), icon);
            var result = dialog.ShowDialog();

            return result.HasValue && result.Value;
        }

        #endregion

        #region Events

        private void FalseActionButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TrueActionButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Exception == null)
                return;

            var viewer = new ExceptionViewer(Exception);
            viewer.ShowDialog();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var feedback = new Feedback { Topmost = true };

            if (feedback.ShowDialog() != true)
                return;

            if (App.MainViewModel != null)
                await Task.Factory.StartNew(App.MainViewModel.SendFeedback, TaskCreationOptions.LongRunning);
        }

        #endregion

        /// <summary>
        /// Dialog Icons.
        /// </summary>
        public enum Icons
        {
            /// <summary>
            /// Information. Blue.
            /// </summary>
            Info,

            /// <summary>
            /// Warning, yellow.
            /// </summary>
            Warning,

            /// <summary>
            /// Error, red.
            /// </summary>
            Error,

            /// <summary>
            /// Success, green.
            /// </summary>
            Success,

            /// <summary>
            /// A question mark, blue.
            /// </summary>
            Question,
        }
    }
}
