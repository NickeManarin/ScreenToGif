using System;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other;

public partial class CommandPreviewer : Window
{
    public string Parameters { get; set; }
        
    public string Extension { get; set; }


    public CommandPreviewer()
    {
        InitializeComponent();
    }


    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ExtensionTextBlock.Text = Extension;
        ParametersTextBox.Text = "ffmpeg\n" + (Parameters ?? "").Replace("{I}", $"-safe 0 -f concat -i \"[{LocalizationHelper.Get("S.CommandPreviewer.Input")}]\"")
            .Replace("{O}", $"-y \"[{LocalizationHelper.Get("S.CommandPreviewer.Output")}]\"");

        if (ParametersTextBox.Text.Contains("-pass 2"))
        {
            ParametersTextBox.Text = ParametersTextBox.Text.Replace("-pass 2 ", $"-pass 1 -passlogfile -y \"[{LocalizationHelper.Get("S.CommandPreviewer.Output")}]\" ") + 
                                     Environment.NewLine + Environment.NewLine +
                                     ParametersTextBox.Text.Replace("-pass 2", $"-pass 2 -passlogfile -y \"[{LocalizationHelper.Get("S.CommandPreviewer.Output")}]\" ");
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}