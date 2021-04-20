using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenToGif.Model.ExportPresets;
using ScreenToGif.Model.ExportPresets.AnimatedImage.Apng;
using ScreenToGif.Model.ExportPresets.AnimatedImage.Gif;
using ScreenToGif.Model.ExportPresets.AnimatedImage.Webp;
using ScreenToGif.Model.ExportPresets.Image;
using ScreenToGif.Model.ExportPresets.Other;
using ScreenToGif.Model.ExportPresets.Video.Avi;
using ScreenToGif.Model.ExportPresets.Video.Mkv;
using ScreenToGif.Model.ExportPresets.Video.Mov;
using ScreenToGif.Model.ExportPresets.Video.Mp4;
using ScreenToGif.Model.ExportPresets.Video.Webm;
using ScreenToGif.Settings;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Preset : Window
    {
        public ExportPreset Current { get; set; }
        
        public bool IsNew { get; set; }
        
        
        public Preset()
        {
            InitializeComponent();
        }

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Adjust to file type

            switch (Current.Type)
            {
                case Export.Gif:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = true;
                    EncoderSystemItem.IsEnabled = true;
                    break;
                case Export.Apng:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    break;
                case Export.Webp:
                case Export.Bpg:
                case Export.Avi:
                case Export.Mkv:
                case Export.Mov:
                case Export.Mp4:
                case Export.Webm:
                    EncoderScreenToGifItem.IsEnabled = false;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    break;

                case Export.Jpeg:
                case Export.Png:
                case Export.Bmp:
                case Export.Stg:
                case Export.Psd:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = false;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    break;
            }

            #endregion
            
            TitleTextBox.Focus();
            ExtensionTextBlock.Text = Current.Type.ToString();
            EncoderComboBox.SelectedValue = Current.Encoder;
            
            if (IsNew)
            {
                AutoSaveCheckBox.IsChecked = true;
                return;
            }

            //Edit.
            IconBorder.Background = TryFindResource("Vector.Pen") as Brush;
            ModeTextBlock.Text = LocalizationHelper.Get("S.Edit");
            EncoderComboBox.IsEnabled = false;
            TitleTextBox.Text = Current.Title ?? "";
            DescriptionTextBox.Text = Current.Description ?? "";
            AutoSaveCheckBox.IsChecked = Current.HasAutoSave;
            SaveInfoTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Preset.Info." + (AutoSaveCheckBox.IsChecked == true ? "Automatic" : "Manual"));

            //If it's a default preset, just let the user edit the "auto save" feature.
            if (string.IsNullOrWhiteSpace(Current.TitleKey))
                return;

            TitleTextBox.IsEnabled = false;
            DescriptionTextBox.IsEnabled = false;
            AutoSaveCheckBox.Focus();

            StatusBand.Info(LocalizationHelper.Get("S.Preset.Warning.Readonly"));
        }

        private void Ok_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = EncoderComboBox.SelectedItem != null && !TitleTextBox.IsNullOrWhiteSpace();
        }

        private void Ok_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            #region Validation

            if (TitleTextBox.IsNullOrWhiteSpace())
            {
                StatusBand.Warning(LocalizationHelper.Get("S.Preset.Warning.Name"));
                return;
            }
            
            var list = UserSettings.All.ExportPresets.OfType<ExportPreset>().ToList();

            //Don't let two preset of the same type to have the same name.
            if (list.Any(a => a.Type == Current.Type && a.Title != Current.Title && a.Title == TitleTextBox.Text.Trim()))
            {
                StatusBand.Warning(LocalizationHelper.Get("S.Preset.Warning.SameName"));
                return;
            }

            #endregion

            #region Build preset

            var encoder = EncoderComboBox.SelectedValue as EncoderType?;
            
            if (IsNew)
                switch (Current.Type)
                {
                    case Export.Gif:
                    {
                        switch (encoder)
                        {
                            case EncoderType.ScreenToGif:
                                var embeddedGifPreset = new EmbeddedGifPreset();
                                
                                Current.CopyPropertiesTo(embeddedGifPreset);
                                embeddedGifPreset.Encoder = EncoderType.ScreenToGif;
                                embeddedGifPreset.ImageId = "Vector.Logo";
                                Current = embeddedGifPreset;
                                break;
                            case EncoderType.FFmpeg:
                                var ffmpegGifPreset = new FfmpegGifPreset();
                                
                                Current.CopyPropertiesTo(ffmpegGifPreset);
                                ffmpegGifPreset.Encoder = EncoderType.FFmpeg;
                                ffmpegGifPreset.ImageId = "Vector.Ffmpeg";
                                Current = ffmpegGifPreset;
                                break;
                            case EncoderType.Gifski:
                                var gifskiGifPreset = new GifskiGifPreset();
                                
                                Current.CopyPropertiesTo(gifskiGifPreset);
                                gifskiGifPreset.Encoder = EncoderType.Gifski;
                                gifskiGifPreset.ImageId = "Vector.Gifski";
                                Current = gifskiGifPreset;
                                break;
                            case EncoderType.System:
                                var systemGifPreset = new SystemGifPreset();
                                
                                Current.CopyPropertiesTo(systemGifPreset);
                                systemGifPreset.Encoder = EncoderType.System;
                                systemGifPreset.ImageId = "Vector.Net";
                                Current = systemGifPreset;
                                break;
                        }
                            
                        break;
                    }
                    case Export.Apng:
                    {
                        switch (encoder)
                        {
                            case EncoderType.ScreenToGif:
                            {
                                var embeddedApngPreset = new EmbeddedApngPreset();

                                Current.CopyPropertiesTo(embeddedApngPreset);
                                embeddedApngPreset.Encoder = EncoderType.ScreenToGif;
                                embeddedApngPreset.ImageId = "Vector.Logo";
                                Current = embeddedApngPreset;
                                break;
                            }
                            case EncoderType.FFmpeg:
                            {
                                var ffmpegApngPreset = new FfmpegApngPreset();

                                Current.CopyPropertiesTo(ffmpegApngPreset);
                                ffmpegApngPreset.Encoder = EncoderType.FFmpeg;
                                ffmpegApngPreset.ImageId = "Vector.Ffmpeg";
                                Current = ffmpegApngPreset;
                                break;
                            }
                        }
                        
                        break;
                    }
                    case Export.Webp:
                    {
                        var ffmpegWebpPreset = new FfmpegWebpPreset();

                        Current.CopyPropertiesTo(ffmpegWebpPreset);
                        Current = ffmpegWebpPreset;
                        break;
                    }
                        
                    case Export.Avi:
                    {
                        var ffmpegAviPreset = new FfmpegAviPreset();

                        Current.CopyPropertiesTo(ffmpegAviPreset);
                        Current = ffmpegAviPreset;
                        break;
                    }
                    case Export.Mkv:
                    {
                        var ffmpegMkvPreset = new FfmpegMkvPreset();

                        Current.CopyPropertiesTo(ffmpegMkvPreset);
                        Current = ffmpegMkvPreset;
                        break;
                    }
                    case Export.Mov:
                    {
                        var ffmpegMovPreset = new FfmpegMovPreset();

                        Current.CopyPropertiesTo(ffmpegMovPreset);
                        Current = ffmpegMovPreset;
                        break;
                    }
                    case Export.Mp4:
                    {
                        var ffmpegMp4Preset = new FfmpegMp4Preset();

                        Current.CopyPropertiesTo(ffmpegMp4Preset);
                        Current = ffmpegMp4Preset;
                        break;
                    }
                    case Export.Webm:
                    {
                        var ffmpegWebmPreset = new FfmpegWebmPreset();

                        Current.CopyPropertiesTo(ffmpegWebmPreset);
                        Current = ffmpegWebmPreset;
                        break;
                    }
                    case Export.Jpeg:
                    {
                        var jpegPreset = new JpegPreset();

                        Current.CopyPropertiesTo(jpegPreset);
                        Current = jpegPreset;
                        break;
                    }
                    case Export.Png:
                    {
                        var pngPreset = new PngPreset();

                        Current.CopyPropertiesTo(pngPreset);
                        Current = pngPreset;
                        break;
                    }
                    case Export.Bmp:
                    {
                        var bmpPreset = new BmpPreset();

                        Current.CopyPropertiesTo(bmpPreset);
                        Current = bmpPreset;
                        break;
                    }
                    case Export.Stg:
                    {
                        var projectPreset = new StgPreset();

                        Current.CopyPropertiesTo(projectPreset);
                        Current = projectPreset;
                        break;
                    }
                    case Export.Psd:
                    {
                        var psdPreset = new PsdPreset();

                        Current.CopyPropertiesTo(psdPreset);
                        Current = psdPreset;
                        break;
                    }
                }

            Current.IsDefault = false;
            Current.Title = TitleTextBox.Text;
            Current.TitleKey = null;
            Current.Description = DescriptionTextBox.Text;
            Current.DescriptionKey = null;
            Current.HasAutoSave = AutoSaveCheckBox.IsChecked == true;
            Current.CreationDate = IsNew ? DateTime.UtcNow : Current.CreationDate;

            #endregion

            #region Update on list

            if (IsNew)
                list.Add(Current);
            
            //Persist the changes to the settings.
            UserSettings.All.ExportPresets = new ArrayList(list.ToArray());

            #endregion

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void AutoSaveCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SaveInfoTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Preset.Info." + (AutoSaveCheckBox.IsChecked == true ? "Automatic" : "Manual"));
        }
    }
}