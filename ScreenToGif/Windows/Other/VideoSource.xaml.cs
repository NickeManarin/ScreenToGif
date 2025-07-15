using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ScreenToGif.Windows.Other;

public partial class VideoSource
{
    private readonly VideoSourceViewModel _viewModel;

    /// <summary>
    /// The path of the video file to be imported.
    /// </summary>
    public string VideoPath
    {
        get => _viewModel.VideoPath;
        set => _viewModel.VideoPath = value;
    }

    /// <summary>
    /// The path of the project where the imported frames will be stored after importing.
    /// </summary>
    public string RootFolder
    {
        get => _viewModel.RootFolder;
        set => _viewModel.RootFolder = value;
    }

    /// <summary>
    /// The imported frame list.
    /// </summary>
    public List<FrameInfo> Frames => _viewModel.Frames.Select(s => new FrameInfo(s.Path, s.Delay)).ToList();

    public VideoSource()
    {
        InitializeComponent();

        _viewModel = DataContext as VideoSourceViewModel;
    }

    //Events
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadSettings();

        _viewModel.ShowErrorRequested += (_, args) =>
        {
            if (!IsLoaded)
                return;

            switch (args)
            {
                case string error:
                    StatusBand.Error(error);
                    break;

                case Exception ex:
                    ErrorDialog.Ok(Title, LocalizationHelper.Get("S.ImportVideo.Error"), LocalizationHelper.Get("S.ImportVideo.Error.Detail"), ex);
                    break;
            }
        };

        _viewModel.ShowWarningRequested += (_, e) =>
        {
            if (!IsLoaded)
                return;

            StatusBand.Warning(e, () => App.MainViewModel.OpenOptions.Execute(Options.ExtrasIndex));
        };

        _viewModel.HideErrorRequested += (_, _) =>
        {
            if (!IsLoaded)
                return;

            StatusBand.Hide();
        };

        _viewModel.CloseRequested += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (IsLoaded)
                    DialogResult = true;
            });
        };

        await _viewModel.LoadPreview();
    }

    private async void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.FrameCount == 0)
        {
            StatusBand.Warning(LocalizationHelper.Get("S.ImportVideo.Nothing"));
            return;
        }

        MinHeight = Height;
        SizeToContent = SizeToContent.Manual;

        await _viewModel.Import();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Cancel();

        DialogResult = false;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.SaveSettings();
        _viewModel.RemoveImportFiles();
        _viewModel.Dispose();

        GC.Collect();
    }
}