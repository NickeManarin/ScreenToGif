using System;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other;

public partial class FeedbackPreview : Window
{
    public string Html { get; set; }

    public FeedbackPreview()
    {
        InitializeComponent();
    }

    private void FeedbackPreview_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            MainBrowser.NavigateToString(Html);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Displaying the preview");

            Dialog.Ok("Feedback Preview", "It was not possible to display the content", ex.Message);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}