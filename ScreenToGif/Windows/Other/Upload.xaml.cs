using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.UserControls;
using ScreenToGif.Util;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.ViewModel.UploadPresets.Gfycat;
using ScreenToGif.ViewModel.UploadPresets.Imgur;
using ScreenToGif.ViewModel.UploadPresets.Yandex;

namespace ScreenToGif.Windows.Other;

public partial class Upload : Window
{
    public UploadPreset CurrentPreset { get; set; }

    public bool IsEditing { get; set; }

    public ExportFormats? Type { get; set; }

    public Upload()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (IsEditing)
        {
            MainBorder.Background = TryFindResource("Vector.Pen") as Brush;
            TypeTextBlock.Text = LocalizationHelper.Get("S.Edit");
            TypeComboBox.SelectedIndex = (int)(CurrentPreset?.Type ?? UploadDestinations.NotDefined);
            TypeComboBox.IsEnabled = false;
            EnabledCheckBox.Visibility = Visibility.Visible;

            TypeComboBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
        else
        {
            //If the file type is set, only display services that supports upload for that.
            if (Type.HasValue)
            {
                FilterRun.Text = $"\n(*.{Type.ToString().ToLower()})";

                if (!ImgurPresetItem.AllowedTypes.Contains(Type.Value))
                    TypeComboBox.Items.Remove(ImgurPresetItem);

                if (!GfycatPresetItem.AllowedTypes.Contains(Type.Value))
                    TypeComboBox.Items.Remove(GfycatPresetItem);
            }

            TypeComboBox.Focus();
        }
    }

    private void TypeComboBox_Selected(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (TypeComboBox.SelectedIndex < 1)
        {
            MainPresenter.Content = null;
            return;
        }

        if (!IsEditing)
        {
            //Create a new model.
            switch ((UploadDestinations)TypeComboBox.SelectedIndex)
            {
                case UploadDestinations.Imgur:
                    CurrentPreset = new ImgurPreset();
                    break;
                case UploadDestinations.Gfycat:
                    CurrentPreset = new GfycatPreset();
                    break;
                case UploadDestinations.Yandex:
                    CurrentPreset = new YandexPreset();
                    break;
            }
        }

        switch ((UploadDestinations)TypeComboBox.SelectedIndex)
        {
            case UploadDestinations.Imgur:
                MainPresenter.Content = new ImgurPanel { DataContext = CurrentPreset };
                break;
            case UploadDestinations.Gfycat:
                MainPresenter.Content = new GfycatPanel { DataContext = CurrentPreset };
                break;
            case UploadDestinations.Yandex:
                MainPresenter.Content = new YandexPanel { DataContext = CurrentPreset };
                break;
        }
    }

    private void Ok_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && TypeComboBox.SelectedIndex > 0;
    }

    private async void Ok_Executed(object sender, RoutedEventArgs e)
    {
        OkButton.Focus();

        if (MainPresenter.Content is not IPanel panel || !await panel.IsValid())
            return;

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}