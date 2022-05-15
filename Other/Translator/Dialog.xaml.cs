using System;
using System.Windows;
using System.Windows.Controls;

namespace Translator;

/// <summary>
/// Interaction logic for Dialog.xaml
/// </summary>
public partial class Dialog : Window
{
    /// <summary>
    /// Default constructor.
    /// </summary>
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
                return (Canvas)FindResource("Vector.Error");
            case Icons.Info:
                return (Canvas)FindResource("Vector.Info");
            case Icons.Success:
                return (Canvas)FindResource("Vector.Success");
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

        InstructionLabel.Content = instruction;
        ObservationTextBlock.Text = observation;
        Title = title;
    }

    private void PrepareOkCancel(string title, string instruction, string observation, Icons icon)
    {
        YesButton.Visibility = Visibility.Collapsed;
        NoButton.Visibility = Visibility.Collapsed;

        CancelButton.Focus();

        IconViewbox.Child = GetIcon(icon);

        InstructionLabel.Content = instruction;
        ObservationTextBlock.Text = observation;
        Title = title;
    }

    private void PrepareAsk(string title, string instruction, string observation, Icons icon)
    {
        CancelButton.Visibility = Visibility.Collapsed;
        OkButton.Visibility = Visibility.Collapsed;

        NoButton.Focus();

        IconViewbox.Child = GetIcon(icon);

        InstructionLabel.Content = instruction;
        ObservationTextBlock.Text = observation;
        Title = title;
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
    /// <param name="icon">The image of the dialog.</param>
    /// <returns>True if Yes</returns>
    public static bool Ask(string title, string instruction, string observation, Icons icon = Icons.Question)
    {
        var dialog = new Dialog();
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