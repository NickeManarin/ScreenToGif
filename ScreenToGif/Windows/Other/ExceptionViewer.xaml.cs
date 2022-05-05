using System;
using System.Windows;

namespace ScreenToGif.Windows.Other;

public partial class ExceptionViewer
{
    #region Variables

    private readonly Exception _exception;

    #endregion

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="ex">The Exception to show.</param>
    /// <param name="additional">Additional details.</param>
    public ExceptionViewer(Exception ex, string additional = null)
    {
        InitializeComponent();

        _exception = ex;

        #region Shows Information

        TypeLabel.Text = ex.GetType().Name;
        MessageTextBox.Text = ex.Message;
        StackTextBox.Text = (!string.IsNullOrWhiteSpace(additional) ? additional + Environment.NewLine : "") + ex.StackTrace;
        SourceTextBox.Text = ex.Source;

        if (ex.TargetSite != null)
            SourceTextBox.Text += "." + ex.TargetSite.Name;

        //If there's additional details.
        if (!string.IsNullOrEmpty(ex.HelpLink))
            StackTextBox.Text += Environment.NewLine + Environment.NewLine + ex.HelpLink;

        if (ex.InnerException != null)
            InnerButton.IsEnabled = true;

        #endregion
    }

    private void InnerButton_Click(object sender, RoutedEventArgs e)
    {
        var errorViewer = new ExceptionViewer(_exception.InnerException);
        errorViewer.ShowDialog();

        GC.Collect(1);
    }

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}