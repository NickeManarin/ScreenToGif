using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.ViewModel.UploadPresets.History;

namespace ScreenToGif.Windows.Other;

public partial class UploadHistory : Window
{
    public UploadPreset CurrentPreset { get; set; }


    public UploadHistory()
    {
        InitializeComponent();
    }


    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        DataGrid.ItemsSource = CurrentPreset?.History;
        DataGrid.SelectedIndex = 0;

        DataGrid.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            if (!(DataGrid.SelectedItem is History selected))
                return;

            Delete(selected);
            e.Handled = true;
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FlowDocumentViewer.Document = (DataGrid.SelectedItem as History)?.Content;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not History history)
            return;

        Delete(history);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }


    private void Delete(History history)
    {
        if (!Dialog.Ask(Title, LocalizationHelper.Get("S.Options.Upload.History.Delete.Instruction"), LocalizationHelper.Get("S.Options.Upload.History.Delete.Message")))
            return;

        CurrentPreset?.History.Remove(history);
        DataGrid.ItemsSource = null;
        DataGrid.ItemsSource = CurrentPreset?.History;
        DataGrid.SelectedIndex = 0;
    }
}