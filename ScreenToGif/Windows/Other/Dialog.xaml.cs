using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Dialog : Window
    {
        public Dialog()
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

            OkButton.Focus();

            IconViewbox.Child = GetIcon(icon);

            InstructionLabel.Text = instruction;
            ObservationTextBlock.Text = observation;
            Title = title;
        }

        private void PrepareOkCancel(string title, string instruction, string observation, Icons icon)
        {
            YesButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;

            CancelButton.Focus();

            IconViewbox.Child = GetIcon(icon);

            InstructionLabel.Text = instruction;
            ObservationTextBlock.Text = observation;
            Title = title;
        }

        private void PrepareAsk(string title, string instruction, string observation, bool yesAsDefault, Icons icon)
        {
            CancelButton.Visibility = Visibility.Collapsed;
            OkButton.Visibility = Visibility.Collapsed;

            if (yesAsDefault)
                YesButton.Focus();
            else
                NoButton.Focus();

            IconViewbox.Child = GetIcon(icon);

            InstructionLabel.Text = instruction;
            ObservationTextBlock.Text = observation;
            Title = title;
        }

        /// <summary>
        /// Handle all pressed keys that get sent to this Window
        /// </summary>
        private void Dialog_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Y:
                    DialogResult = true; //[Y] will answer 'Yes' to ask-dialog
                    break;
                case Key.Escape:
                case Key.N:
                    DialogResult = false; //[ESC] or [N] will answer 'No' to ask-dialog
                    break;
            }
        }

        /// <summary>
        /// Shows a Ok dialog.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="instruction">The main instruction.</param>
        /// <param name="observation">A complementar observation.</param>
        /// <param name="icon">The image of the dialog.</param>
        /// <returns>True if Ok</returns>
        public static bool Ok(string title, string instruction, string observation, Icons icon = Icons.Error)
        {
            var dialog = new Dialog();
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
        /// <param name="icon">The image of the dialog.</param>
        /// <returns>True if Ok</returns>
        public static bool OkCancel(string title, string instruction, string observation, Icons icon = Icons.Error)
        {
            var dialog = new Dialog();
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
        /// <param name="yesAsDefault">If true, the Yes button will receive the initial focus.</param>
        /// <param name="icon">The image of the dialog.</param>
        /// <returns>True if Yes</returns>
        public static bool Ask(string title, string instruction, string observation, bool yesAsDefault = true, Icons icon = Icons.Question)
        {
            var dialog = new Dialog();
            dialog.PrepareAsk(title, instruction, observation.Replace(@"\n", Environment.NewLine).Replace(@"\r", ""), yesAsDefault, icon);
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

        #endregion
    }
}