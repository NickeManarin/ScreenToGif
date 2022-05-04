using System.Windows;

namespace ScreenToGif.Windows.Other;

public partial class TextDialog : Window
{
    public string Command { get; set; }
    public string Output { get; set; }

    public TextDialog()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CommandTextBox.Text = Command;
        OutputTextBox.Text = Output;

        CommandTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}