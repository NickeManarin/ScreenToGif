using System.Windows;
using System.Windows.Input;

namespace ScreenToGif.Windows.Other;

public partial class GoTo
{
    #region Properties

    public int Maximum { get; set; }

    public int Selected { get; set; }

    #endregion

    public GoTo(int maximum)
    {
        InitializeComponent();

        Maximum = maximum;
    }

    #region Events

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        GoToLabel.Content = string.Format(GoToLabel.Content.ToString(), Maximum);
        NumberIntegerUpDown.Maximum = Maximum;

        NumberIntegerUpDown.Focus();
        NumberIntegerUpDown.SelectAll();
    }

    private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        DialogResult = false;
    }
        
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Selected = NumberIntegerUpDown.Value;
        DialogResult = true;
    }

    private void IntegerUpDown_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
            OkButton_Click(null, null);
    }

    #endregion
}