using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.ExportPresets;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.Windows.Other;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Views.Settings;

public partial class UploadSettings : Page
{
    /// <summary>
    /// List of upload presets.
    /// </summary>
    private ObservableCollection<UploadPreset> _uploadList;

    public UploadSettings()
    {
        InitializeComponent();
    }

    private void UploadSettings_Loaded(object sender, RoutedEventArgs e)
    {
        var list = UserSettings.All.UploadPresets?.Cast<UploadPreset>().ToList() ?? [];

        UploadDataGrid.ItemsSource = _uploadList = new ObservableCollection<UploadPreset>(list);

        try
        {
            if (!string.IsNullOrWhiteSpace(UserSettings.All.ProxyPassword))
                ProxyPasswordBox.Password = WebHelper.Unprotect(UserSettings.All.ProxyPassword);
        }
        catch (Exception ex)
        {
            StatusBand.Warning("It was not possible to correctly load your proxy password. This usually happens when sharing the app settings with different computers.");
            LogWriter.Log(ex, "Unprotect data");
        }
    }
    
    private void AddUpload_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CloudGrid.IsVisible;
    }

    private void Upload_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CloudGrid.IsVisible && UploadDataGrid.SelectedIndex != -1;
    }

    private void AddUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var upload = new Upload();
        var result = upload.ShowDialog();

        if (result != true)
            return;

        _uploadList.Add(upload.CurrentPreset);

        UserSettings.All.UploadPresets = new ArrayList(_uploadList.ToArray());
    }

    private void EditUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var index = UploadDataGrid.SelectedIndex;
        var current = _uploadList[UploadDataGrid.SelectedIndex];
        var selected = current.ShallowCopy();

        var preset = new Upload { CurrentPreset = selected, IsEditing = true };
        var result = preset.ShowDialog();

        if (result != true)
            return;

        _uploadList[UploadDataGrid.SelectedIndex] = preset.CurrentPreset;
        UploadDataGrid.Items.Refresh();
        UploadDataGrid.SelectedIndex = index;

        //Update the upload preset in all export presets.
        if (current.Title != preset.CurrentPreset.Title)
        {
            foreach (var exportPreset in UserSettings.All.ExportPresets.OfType<ExportPreset>().Where(w => w.UploadService == current.Title))
                exportPreset.UploadService = preset.CurrentPreset.Title;
        }

        UserSettings.All.UploadPresets = new ArrayList(_uploadList.ToArray());
    }

    private void RemoveUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var index = UploadDataGrid.SelectedIndex;

        //Ask if the user really wants to remove the preset.
        if (index < 0 || !Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Title"), LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Instruction"),
                LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Message")))
            return;

        var selected = _uploadList[UploadDataGrid.SelectedIndex];
        _uploadList.RemoveAt(UploadDataGrid.SelectedIndex);

        //Automatically selects the closest item from the position of the one that was removed.
        UploadDataGrid.SelectedIndex = _uploadList.Count == 0 ? -1 : _uploadList.Count <= index ? _uploadList.Count - 1 : index;

        UserSettings.All.UploadPresets = new ArrayList(_uploadList.ToArray());

        //Remove the upload preset from all export presets.
        foreach (var exportPreset in UserSettings.All.ExportPresets.OfType<ExportPreset>().Where(w => w.UploadService == selected.Title))
            exportPreset.UploadService = null;
    }

    private void HistoryUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var history = new UploadHistory
        {
            CurrentPreset = _uploadList[UploadDataGrid.SelectedIndex]
        };
        history.ShowDialog();
    }

    private void UploadDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EditUploadCommandBinding.Command.CanExecute(sender))
            EditUploadCommandBinding.Command.Execute(sender);
    }

    private void UploadDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && EditUploadCommandBinding.Command.CanExecute(sender))
        {
            EditUploadCommandBinding.Command.Execute(sender);
            e.Handled = true;
        }

        if (e.Key == Key.Space)
        {
            if (UploadDataGrid.SelectedItem is not UploadPreset selected)
                return;

            selected.IsEnabled = !selected.IsEnabled;
            e.Handled = true;
        }
    }

    private void UploadSettings_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ProxyPasswordBox.Password))
            UserSettings.All.ProxyPassword = WebHelper.Protect(ProxyPasswordBox.Password);
    }
}
