using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Domain.Models.Upload.Gfycat;
using ScreenToGif.ViewModel.UploadPresets.Gfycat;

namespace ScreenToGif.Windows.Other;

public partial class UploadDetailsDialog : Window
{
    public UploadDetailsDialog()
    {
        InitializeComponent();
    }

    #region Methods

    private void PrepareOk(GfycatPreset preset)
    {
        TitleTextBox.Text = preset.DefaultTitle;
        DescriptionTextBox.Text = preset.DefaultDescription;
        TagsTextBox.Text = preset.DefaultTags;
        IsPrivateCheckBox.IsChecked = preset.DefaultIsPrivate;

        CancelButton.Visibility = Visibility.Collapsed;
        AcceptButton.Focus();
    }

    private void PrepareOkCancel(GfycatPreset preset)
    {
        TitleTextBox.Text = preset.DefaultTitle;
        DescriptionTextBox.Text = preset.DefaultDescription;
        TagsTextBox.Text = preset.DefaultTags;
        IsPrivateCheckBox.IsChecked = preset.DefaultIsPrivate;

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
    public static GfycatCreateRequest Ok(GfycatPreset preset)
    {
        var dialog = new UploadDetailsDialog();
        dialog.PrepareOk(preset);
        var result = dialog.ShowDialog();

        if (!result.HasValue || !result.Value)
            return null;

        var tags = dialog.TagsTextBox.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        tags = tags.Length > 0 ? tags.Select(s => s.Trim()).ToArray() : null;

        return new GfycatCreateRequest
        {
            Tile = dialog.TitleTextBox.Text,
            Description = dialog.DescriptionTextBox.Text,
            Tags = tags,
            IsPrivate = dialog.IsPrivateCheckBox.IsChecked == true
        };
    }

    /// <summary>
    /// Shows a Ok/Cancel dialog.
    /// </summary>
    /// <returns>True if Ok</returns>
    public static GfycatCreateRequest OkCancel(GfycatPreset preset)
    {
        var dialog = new UploadDetailsDialog();
        dialog.PrepareOkCancel(preset);
        var result = dialog.ShowDialog();

        if (!result.HasValue || !result.Value)
            return null;

        var tags = dialog.TagsTextBox.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        tags = tags.Length > 0 ? tags.Select(s => s.Trim()).ToArray() : null;

        return new GfycatCreateRequest
        {
            Tile = dialog.TitleTextBox.Text,
            Description = dialog.DescriptionTextBox.Text,
            Tags = tags,
            IsPrivate = dialog.IsPrivateCheckBox.IsChecked == true
        };
    }

    #endregion

    #region Events

    private void Dialog_Loaded(object sender, RoutedEventArgs e)
    {
        TitleTextBox.Focus();
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    #endregion
}