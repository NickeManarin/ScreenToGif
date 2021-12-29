using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Util;
using ScreenToGif.ViewModel.UploadPresets.Imgur;

namespace ScreenToGif.Windows.Other;

public partial class PickAlbumDialog : Window
{
    private List<ImgurAlbum> AlbumList { get; set; }

    public PickAlbumDialog()
    {
        InitializeComponent();
    }

    #region Methods

    private void PrepareOk(List<ImgurAlbum> list)
    {
        AlbumList = list;

        CancelButton.Visibility = Visibility.Collapsed;
        OkButton.Focus();
    }

    private void PrepareOkCancel(List<ImgurAlbum> list)
    {
        AlbumList = list;

        CancelButton.Focus();
    }

    /// <summary>
    /// Handle all pressed keys that get sent to this Window
    /// </summary>
    private void DialogKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.NumPad1:
            case Key.D1:
            case Key.Y:
                DialogResult = true; //[Y]/[1] will answer 'Yes'.
                break;
            case Key.NumPad2:
            case Key.D2:
            case Key.Escape:
            case Key.N:
                DialogResult = false; //[ESC]/[2]/[N] will answer 'No'.
                break;
        }
    }

    /// <summary>
    /// Shows a Ok dialog.
    /// </summary>
    /// <returns>True if Ok</returns>
    public static string Ok(List<ImgurAlbum> list)
    {
        var dialog = new PickAlbumDialog();
        dialog.PrepareOk(list);
        var result = dialog.ShowDialog();

        if (!result.HasValue || !result.Value)
            return null;

        var item = dialog.MainDataGrid.SelectedItem as ImgurAlbum;
        return item?.Id;
    }

    /// <summary>
    /// Shows a Ok/Cancel dialog.
    /// </summary>
    /// <returns>True if Ok</returns>
    public static string OkCancel(List<ImgurAlbum> list)
    {
        var dialog = new PickAlbumDialog();
        dialog.PrepareOkCancel(list);
        var result = dialog.ShowDialog();

        if (!result.HasValue || !result.Value)
            return null;

        var item = dialog.MainDataGrid.SelectedItem as ImgurAlbum;
        return item?.Id;
    }

    #endregion

    #region Events

    private void Dialog_Loaded(object sender, RoutedEventArgs e)
    {
        var remove = AlbumList?.FirstOrDefault(f => string.IsNullOrWhiteSpace(f.Id) || f.Id == "♥♦♣♠");

        if (remove != null)
            AlbumList.Remove(remove);

        MainDataGrid.ItemsSource = AlbumList;

        MainDataGrid.Focus();

        if (MainDataGrid.Items.Count > 0)
        {
            MainDataGrid.SelectedIndex = 0;
            MainDataGrid.FocusOnFirstCell();
        }
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (MainDataGrid.SelectedItem != null)
            DialogResult = true;
    }

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            if (MainDataGrid.SelectedItem != null)
                DialogResult = true;

            e.Handled = true;
        }
    }

    private void TrueActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainDataGrid.SelectedItem == null)
            return;

        DialogResult = true;
    }

    private void FalseActionButton_Click(object sender, RoutedEventArgs e)
    {
        MainDataGrid.SelectedItem = null;
        DialogResult = false;
    }

    #endregion
}