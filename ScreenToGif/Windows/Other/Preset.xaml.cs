using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.ExportPresets;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Apng;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Webp;
using ScreenToGif.ViewModel.ExportPresets.Image;
using ScreenToGif.ViewModel.ExportPresets.Other;
using ScreenToGif.ViewModel.ExportPresets.Video.Avi;
using ScreenToGif.ViewModel.ExportPresets.Video.Mkv;
using ScreenToGif.ViewModel.ExportPresets.Video.Mov;
using ScreenToGif.ViewModel.ExportPresets.Video.Mp4;
using ScreenToGif.ViewModel.ExportPresets.Video.Webm;

namespace ScreenToGif.Windows.Other;

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
            case ExportFormats.Gif:
                EncoderScreenToGifItem.IsEnabled = true;
                EncoderFfmpegItem.IsEnabled = true;
                EncoderGifskiItem.IsEnabled = Environment.Is64BitProcess;
                EncoderSystemItem.IsEnabled = true;
                EncoderKGySoftItem.IsEnabled = true;
                break;
            case ExportFormats.Apng:
                EncoderScreenToGifItem.IsEnabled = true;
                EncoderFfmpegItem.IsEnabled = true;
                EncoderGifskiItem.IsEnabled = false;
                EncoderSystemItem.IsEnabled = false;
                EncoderKGySoftItem.IsEnabled = false;
                break;
            case ExportFormats.Webp:
            case ExportFormats.Bpg:
            case ExportFormats.Avi:
            case ExportFormats.Mkv:
            case ExportFormats.Mov:
            case ExportFormats.Mp4:
            case ExportFormats.Webm:
                EncoderScreenToGifItem.IsEnabled = false;
                EncoderFfmpegItem.IsEnabled = true;
                EncoderGifskiItem.IsEnabled = false;
                EncoderSystemItem.IsEnabled = false;
                EncoderKGySoftItem.IsEnabled = false;
                break;

            case ExportFormats.Jpeg:
            case ExportFormats.Png:
            case ExportFormats.Bmp:
            case ExportFormats.Stg:
            case ExportFormats.Psd:
                EncoderScreenToGifItem.IsEnabled = true;
                EncoderFfmpegItem.IsEnabled = false;
                EncoderGifskiItem.IsEnabled = false;
                EncoderSystemItem.IsEnabled = false;
                EncoderKGySoftItem.IsEnabled = false;
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

        var encoder = EncoderComboBox.SelectedValue as EncoderTypes?;
            
        if (IsNew)
            switch (Current.Type)
            {
                case ExportFormats.Gif:
                {
                    switch (encoder)
                    {
                        case EncoderTypes.ScreenToGif:
                            var embeddedGifPreset = new EmbeddedGifPreset();
                                
                            Current.CopyPropertiesTo(embeddedGifPreset);
                            embeddedGifPreset.Encoder = EncoderTypes.ScreenToGif;
                            embeddedGifPreset.ImageId = "Vector.Logo";
                            Current = embeddedGifPreset;
                            break;
                        case EncoderTypes.KGySoft:
                            var kgySoftGifPreset = new KGySoftGifPreset();

                            Current.CopyPropertiesTo(kgySoftGifPreset);
                            kgySoftGifPreset.Encoder = EncoderTypes.KGySoft;
                            kgySoftGifPreset.ImageId = "Vector.KGySoft";
                            Current = kgySoftGifPreset;
                            break;
                        case EncoderTypes.FFmpeg:
                            var ffmpegGifPreset = new FfmpegGifPreset();
                                
                            Current.CopyPropertiesTo(ffmpegGifPreset);
                            ffmpegGifPreset.Encoder = EncoderTypes.FFmpeg;
                            ffmpegGifPreset.ImageId = "Vector.Ffmpeg";
                            Current = ffmpegGifPreset;
                            break;
                        case EncoderTypes.Gifski:
                            var gifskiGifPreset = new GifskiGifPreset();
                                
                            Current.CopyPropertiesTo(gifskiGifPreset);
                            gifskiGifPreset.Encoder = EncoderTypes.Gifski;
                            gifskiGifPreset.ImageId = "Vector.Gifski";
                            Current = gifskiGifPreset;
                            break;
                        case EncoderTypes.System:
                            var systemGifPreset = new SystemGifPreset();
                                
                            Current.CopyPropertiesTo(systemGifPreset);
                            systemGifPreset.Encoder = EncoderTypes.System;
                            systemGifPreset.ImageId = "Vector.Net";
                            Current = systemGifPreset;
                            break;
                    }
                            
                    break;
                }
                case ExportFormats.Apng:
                {
                    switch (encoder)
                    {
                        case EncoderTypes.ScreenToGif:
                        {
                            var embeddedApngPreset = new EmbeddedApngPreset();

                            Current.CopyPropertiesTo(embeddedApngPreset);
                            embeddedApngPreset.Encoder = EncoderTypes.ScreenToGif;
                            embeddedApngPreset.ImageId = "Vector.Logo";
                            Current = embeddedApngPreset;
                            break;
                        }
                        case EncoderTypes.FFmpeg:
                        {
                            var ffmpegApngPreset = new FfmpegApngPreset();

                            Current.CopyPropertiesTo(ffmpegApngPreset);
                            ffmpegApngPreset.Encoder = EncoderTypes.FFmpeg;
                            ffmpegApngPreset.ImageId = "Vector.Ffmpeg";
                            Current = ffmpegApngPreset;
                            break;
                        }
                    }
                        
                    break;
                }
                case ExportFormats.Webp:
                {
                    var ffmpegWebpPreset = new FfmpegWebpPreset();

                    Current.CopyPropertiesTo(ffmpegWebpPreset);
                    Current = ffmpegWebpPreset;
                    break;
                }
                        
                case ExportFormats.Avi:
                {
                    var ffmpegAviPreset = new FfmpegAviPreset();

                    Current.CopyPropertiesTo(ffmpegAviPreset);
                    Current = ffmpegAviPreset;
                    break;
                }
                case ExportFormats.Mkv:
                {
                    var ffmpegMkvPreset = new FfmpegMkvPreset();

                    Current.CopyPropertiesTo(ffmpegMkvPreset);
                    Current = ffmpegMkvPreset;
                    break;
                }
                case ExportFormats.Mov:
                {
                    var ffmpegMovPreset = new FfmpegMovPreset();

                    Current.CopyPropertiesTo(ffmpegMovPreset);
                    Current = ffmpegMovPreset;
                    break;
                }
                case ExportFormats.Mp4:
                {
                    var ffmpegMp4Preset = new FfmpegMp4Preset();

                    Current.CopyPropertiesTo(ffmpegMp4Preset);
                    Current = ffmpegMp4Preset;
                    break;
                }
                case ExportFormats.Webm:
                {
                    var ffmpegWebmPreset = new FfmpegWebmPreset();

                    Current.CopyPropertiesTo(ffmpegWebmPreset);
                    Current = ffmpegWebmPreset;
                    break;
                }
                case ExportFormats.Jpeg:
                {
                    var jpegPreset = new JpegPreset();

                    Current.CopyPropertiesTo(jpegPreset);
                    Current = jpegPreset;
                    break;
                }
                case ExportFormats.Png:
                {
                    var pngPreset = new PngPreset();

                    Current.CopyPropertiesTo(pngPreset);
                    Current = pngPreset;
                    break;
                }
                case ExportFormats.Bmp:
                {
                    var bmpPreset = new BmpPreset();

                    Current.CopyPropertiesTo(bmpPreset);
                    Current = bmpPreset;
                    break;
                }
                case ExportFormats.Stg:
                {
                    var projectPreset = new StgPreset();

                    Current.CopyPropertiesTo(projectPreset);
                    Current = projectPreset;
                    break;
                }
                case ExportFormats.Psd:
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