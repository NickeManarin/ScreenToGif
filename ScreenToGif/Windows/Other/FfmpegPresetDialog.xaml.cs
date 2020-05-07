using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class FfmpegPresetDialog : Window
    {
        public FfmpegPreset CurrentPreset { get; set; }
        public string Extension { get; set; }
        public bool IsEditing { get; set; }

        public FfmpegPresetDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ExtensionTextBlock.Text = CurrentPreset?.Extension ?? Extension;
            NameTextBox.Text = CurrentPreset?.Name ?? "";
            ParametersTextBox.Text = CurrentPreset?.Parameters ?? "";

            if (IsEditing)
            {
                ParametersTextBox.Focus();
                MainViewbox.Child = TryFindResource("Vector.Pen") as Canvas;
                ModeTextBlock.Text = LocalizationHelper.Get("S.Edit");
            }
            else
                NameTextBox.Focus();
        }

        private void Ok_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLoaded;
        }

        private void Ok_Executed(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.IsNullOrWhiteSpace())
            {
                StatusBand.Warning(LocalizationHelper.Get("S.FfmpegPreset.Warning.Name"));
                return;
            }

            if (ParametersTextBox.IsNullOrWhiteSpace())
            {
                StatusBand.Warning(LocalizationHelper.Get("S.FfmpegPreset.Warning.Parameter"));
                return;
            }

            //Get all presets, so we can persist later.
            var list = UserSettings.All.FfmpegPresets.Cast<FfmpegPreset>().ToList();

            if (!IsEditing && list.Any(a => a.ActualName == NameTextBox.Trim() && a.Extension == Extension))
            {
                StatusBand.Warning(LocalizationHelper.Get("S.FfmpegPreset.Warning.SameName"));
                return;
            }

            if (IsEditing)
                list.Remove(CurrentPreset);

            list.Add(new FfmpegPreset
            {
                Extension = CurrentPreset?.Extension ?? Extension,
                ActualName = NameTextBox.Text.Trim(),
                Parameters = ParametersTextBox.Text
            });

            foreach (var preset in list)
            {
                if (preset.Extension != (CurrentPreset?.Extension ?? Extension))
                    continue;

                preset.LastSelected = preset.Name == NameTextBox.Text.Trim();
            }

            //Persist the changes to the settings.
            UserSettings.All.FfmpegPresets = new ArrayList(list.ToArray());

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}