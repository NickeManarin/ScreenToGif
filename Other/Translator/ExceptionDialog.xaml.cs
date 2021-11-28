using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using Translator.Util;

namespace Translator;

public partial class ExceptionDialog : Window
{
    #region Properties

    public bool BugWithHotFix4055002 { get; set; }
        
    public Exception Exception { get; set; }

    #endregion

    public ExceptionDialog(Exception exception)
    {
        InitializeComponent();

        Exception = exception;
    }

    #region Eventos

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (Exception == null)
            DetailsButton.IsEnabled = false;
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        var errorViewer = new ExceptionViewer(Exception);
        errorViewer.ShowDialog();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Métodos

    private void PrepareOk(string title, string instruction, string observation)
    {
        TypeTextBlock.Text = instruction;
        DetailsTextBlock.Inlines.Add(new Run("\t" + observation));
        Title = title ?? "ScreenToGif - Error";

        if (BugWithHotFix4055002)
        {
            DetailsTextBlock.Inlines.Add(new LineBreak());
            DetailsTextBlock.Inlines.Add(new LineBreak());
            DetailsTextBlock.Inlines.Add(new Run("\tThis was likely caused by a bug with an update for .Net Framework 4.7.1 (KB4055002, released in January 2018). This bug happens on machines with Windows 7 SP1 or Windows Server 2008 R2."));
            DetailsTextBlock.Inlines.Add(new LineBreak());
            DetailsTextBlock.Inlines.Add(new LineBreak());
            DetailsTextBlock.Inlines.Add(new Run("\t"));

            var hyper = new Hyperlink(new Run("Click here to open a page with some details on how to fix this issue.") {ToolTip = "https://github.com/dotnet/announcements/issues/53" });
            hyper.Click += HyperOnClick;
            DetailsTextBlock.Inlines.Add(hyper);
        }

        OkButton.Focus();
    }

    private void HyperOnClick(object sender, RoutedEventArgs routedEventArgs)
    {
        try
        {
            Process.Start("https://github.com/dotnet/announcements/issues/53");
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to open link");
        }
    }

    #endregion

    #region Static Methods

    public static bool Ok(Exception exception, string title, string instruction, string observation = "", bool bugWith4055002 = false)
    {
        var dialog = new ExceptionDialog(exception) { BugWithHotFix4055002 = bugWith4055002 };
        dialog.PrepareOk(title, instruction, observation);
        var result = dialog.ShowDialog();

        return result.HasValue && result.Value;
    }

    #endregion
}