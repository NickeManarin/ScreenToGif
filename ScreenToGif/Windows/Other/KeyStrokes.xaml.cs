using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other;

public partial class KeyStrokes : Window
{
    public ObservableCollection<FrameInfo> InternalList { get; set; }

    public KeyStrokes()
    {
        InitializeComponent();
    }

    private void KeyStrokes_Loaded(object sender, RoutedEventArgs e)
    {
        if (InternalList == null)
            return;

        KeysDataGrid.ItemsSource = InternalList;
        KeysDataGrid.FocusOnFirstCell();
    }

    private void RemoveButton_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Button button || KeysDataGrid.SelectedIndex == -1)
            return;

        if (button.DataContext is not SimpleKeyGesture context)
            return;

        InternalList[KeysDataGrid.SelectedIndex].KeyList.Remove(context);
        KeysDataGrid.Items.Refresh();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (KeysDataGrid.SelectedIndex == -1)
            return;

        var row = (DataGridRow)KeysDataGrid.ItemContainerGenerator.ContainerFromItem(KeysDataGrid.SelectedItem);

        if (row == null)
            return;

        //Getting the ContentPresenter of the row details
        var presenter = VisualHelper.GetVisualChild<DataGridDetailsPresenter>(row);

        if (presenter == null)
            return;

        //Finding Remove button from the DataTemplate that is set on that ContentPresenter
        var template = presenter.ContentTemplate;
        var box = (KeyBox)template.FindName("AddKeyBox", presenter);

        if (!box.MainKey.HasValue)
            return;

        InternalList[KeysDataGrid.SelectedIndex].KeyList.Add(new SimpleKeyGesture(box.MainKey.Value, box.ModifierKeys, !box.IsSingleLetterLowerCase));
        KeysDataGrid.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}