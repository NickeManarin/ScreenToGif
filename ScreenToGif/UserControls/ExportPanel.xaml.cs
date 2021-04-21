using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Controls.Items;
using ScreenToGif.Interfaces;
using ScreenToGif.Model.Events;
using ScreenToGif.Model.ExportPresets;
using ScreenToGif.Model.ExportPresets.AnimatedImage.Apng;
using ScreenToGif.Model.ExportPresets.AnimatedImage.Gif;
using ScreenToGif.Model.ExportPresets.AnimatedImage.Webp;
using ScreenToGif.Model.ExportPresets.Image;
using ScreenToGif.Model.ExportPresets.Other;
using ScreenToGif.Model.ExportPresets.Video;
using ScreenToGif.Model.ExportPresets.Video.Avi;
using ScreenToGif.Model.ExportPresets.Video.Codecs;
using ScreenToGif.Model.ExportPresets.Video.Mkv;
using ScreenToGif.Model.ExportPresets.Video.Mov;
using ScreenToGif.Model.ExportPresets.Video.Mp4;
using ScreenToGif.Model.ExportPresets.Video.Webm;
using ScreenToGif.Model.UploadPresets;
using ScreenToGif.Settings;
using ScreenToGif.Util;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.UserControls
{
    public partial class ExportPanel : UserControl, IPanel
    {
        private readonly DispatcherTimer _searchTimer;

        #region Dependency Properties

        public static readonly DependencyProperty CurrentPresetProperty = DependencyProperty.Register(nameof(CurrentPreset), typeof(ExportPreset), typeof(ExportPanel), new PropertyMetadata(default(ExportPreset)));

        public static readonly DependencyProperty FrameCountProperty = DependencyProperty.Register(nameof(FrameCount), typeof(int), typeof(ExportPanel), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty SelectionCountProperty = DependencyProperty.Register(nameof(SelectionCount), typeof(int), typeof(ExportPanel), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty TotalTimeProperty = DependencyProperty.Register(nameof(TotalTime), typeof(TimeSpan), typeof(ExportPanel), new PropertyMetadata(default(TimeSpan)));


        public ExportPreset CurrentPreset
        {
            get => (ExportPreset)GetValue(CurrentPresetProperty);
            set => SetValue(CurrentPresetProperty, value);
        }

        public int FrameCount
        {
            get => (int)GetValue(FrameCountProperty);
            set => SetValue(FrameCountProperty, value);
        }

        public int SelectionCount
        {
            get => (int)GetValue(SelectionCountProperty);
            set => SetValue(SelectionCountProperty, value);
        }

        public TimeSpan TotalTime
        {
            get => (TimeSpan)GetValue(TotalTimeProperty);
            set => SetValue(TotalTimeProperty, value);
        }

        #endregion

        #region Custom Events

        public static readonly RoutedEvent SaveEvent = EventManager.RegisterRoutedEvent(nameof(Save), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExportPanel));

        public static readonly RoutedEvent ValidatedEvent = EventManager.RegisterRoutedEvent(nameof(Validated), RoutingStrategy.Bubble, typeof(ValidatedEventHandler), typeof(ExportPanel));

        public static readonly RoutedEvent ValidationRemovedEvent = EventManager.RegisterRoutedEvent(nameof(ValidationRemoved), RoutingStrategy.Bubble, typeof(ValidatedEventHandler), typeof(ExportPanel));

        /// <summary>
        /// Event raised when the save is triggered within this user control.
        /// </summary>
        public event RoutedEventHandler Save
        {
            add => AddHandler(SaveEvent, value);
            remove => RemoveHandler(SaveEvent, value);
        }

        /// <summary>
        /// Event raised when a warning is triggered within this user control.
        /// </summary>
        public event ValidatedEventHandler Validated
        {
            add => AddHandler(ValidatedEvent, value);
            remove => RemoveHandler(ValidatedEvent, value);
        }

        /// <summary>
        /// Event raised when a request for removal of warning is triggered within this user control.
        /// </summary>
        public event ValidatedEventHandler ValidationRemoved
        {
            add => AddHandler(ValidationRemovedEvent, value);
            remove => RemoveHandler(ValidationRemovedEvent, value);
        }


        public void RaiseSaveEvent()
        {
            if (SaveEvent == null)
                return;

            RaiseEvent(new RoutedEventArgs(SaveEvent));
        }

        public void RaiseValidatedEvent(ValidatedEventArgs args)
        {
            if (ValidatedEvent == null)
                return;

            args.RoutedEvent = ValidatedEvent;

            RaiseEvent(args);
        }

        public void RaiseValidatedEvent(string message, StatusReasons reason, Action action = null)
        {
            if (ValidatedEvent == null)
                return;

            RaiseEvent(new ValidatedEventArgs(ValidatedEvent, message, reason, action));
        }

        public void RaiseValidationRemovedEvent(StatusReasons reason)
        {
            if (ValidatedEvent == null)
                return;

            RaiseEvent(new ValidatedEventArgs(ValidationRemovedEvent, null, reason));
        }

        #endregion


        public ExportPanel()
        {
            InitializeComponent();

            #region Initialize timers

            _searchTimer = new DispatcherTimer(DispatcherPriority.Background);
            _searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            _searchTimer.Tick += SearchTimer_Tick;

            #endregion

            #region UWP restrictions

#if UWP

            CustomCommandsCheckBox.IsEnabled = false;
            CustomCommandsTextBox.IsEnabled = false;

#endif

            #endregion
        }


        #region Methods

        /// <summary>
        /// Adjusts the UI for the different types of file types, encoders and quantizers.
        /// </summary>
        /// <param name="type"></param>
        private void AdjustPresentation(Export type)
        {
            foreach (var item in EncoderComboBox.Items.OfType<GenericItem>())
                item.IsEnabled = false;

            //File types can have different properties that need to be displayed.
            switch (type)
            {
                case Export.Apng:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".apng", ".png" };
                    break;
                case Export.Gif:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = true;
                    EncoderSystemItem.IsEnabled = true;
                    ExtensionComboBox.ItemsSource = new List<string> { ".gif" };
                    break;
                case Export.Webp:
                    EncoderScreenToGifItem.IsEnabled = false;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".webp" };
                    break;


                case Export.Avi:
                    EncoderScreenToGifItem.IsEnabled = false;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".avi" };
                    break;
                case Export.Mkv:
                    EncoderScreenToGifItem.IsEnabled = false;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".mkv" };
                    break;
                case Export.Mov:
                    EncoderScreenToGifItem.IsEnabled = false;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".mov" };
                    break;
                case Export.Mp4:
                    EncoderScreenToGifItem.IsEnabled = false;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".mp4" };
                    break;
                case Export.Webm:
                    EncoderScreenToGifItem.IsEnabled = false;
                    EncoderFfmpegItem.IsEnabled = true;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".webm" };
                    break;


                case Export.Jpeg:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = false;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".jpg", ".jpeg", ".zip" };
                    break;
                case Export.Png:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = false;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".png", ".zip" };
                    break;
                case Export.Bmp:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = false;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".bmp", ".zip" };
                    break;


                case Export.Stg:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = false;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".stg", ".zip" };
                    break;
                case Export.Psd:
                    EncoderScreenToGifItem.IsEnabled = true;
                    EncoderFfmpegItem.IsEnabled = false;
                    EncoderGifskiItem.IsEnabled = false;
                    EncoderSystemItem.IsEnabled = false;
                    ExtensionComboBox.ItemsSource = new List<string> { ".psd" };
                    break;
            }

            SaveAsProjectTooCheckBox.Visibility = type != Export.Stg ? Visibility.Visible : Visibility.Collapsed;
            UploadFileCheckBox.Visibility = type != Export.Bmp && type != Export.Jpeg && type != Export.Png ? Visibility.Visible : Visibility.Collapsed;
            CopyFileCheckBox.Visibility = type != Export.Bmp && type != Export.Jpeg && type != Export.Png ? Visibility.Visible : Visibility.Collapsed;
            CustomCommandsCheckBox.Visibility = type != Export.Bmp && type != Export.Jpeg && type != Export.Png ? Visibility.Visible : Visibility.Collapsed;
            SaveFileCheckBox.IsEnabled = UploadFileCheckBox.Visibility == Visibility.Visible || CopyFileCheckBox.Visibility == Visibility.Visible;
        }

        private void LoadPresets(Export type, ExportPreset toLoad = null, bool firstLoad = false)
        {
            //Get all presets of given type. It's possible that there's none available.
            var list = UserSettings.All.ExportPresets?.OfType<ExportPreset>().Where(w => w.Type == type).ToList() ?? new List<ExportPreset>();

            //Get the missing default presets.
            GeneratePresets(type, list);

            //TODO: Check if default presets were recently updated and display an info about it.

            //Localize the default presets.
            foreach (var preset in list.Where(w => w.IsDefault))
            {
                preset.Title = LocalizationHelper.Get(preset.TitleKey).Replace("{0}", preset.DefaultExtension);
                preset.Description = LocalizationHelper.Get(preset.DescriptionKey);
                preset.OutputFolder = preset.OutputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                preset.OutputFilename = (preset.OutputFilenameKey ?? "").Length <= 0 || !string.IsNullOrWhiteSpace(preset.OutputFilename) ? preset.OutputFilename : LocalizationHelper.Get(preset.OutputFilenameKey);
            }

            //Persist the changes to the settings.
            PersistPresets(list, type);

            //Display the presets and select the default one, based on the selected file type.
            PresetComboBox.ItemsSource = null;
            PresetComboBox.ItemsSource = list.OrderBy(o => o.Encoder).ThenBy(t => t.Title).ToList();
            PresetComboBox.SelectedItem = null;
            PresetComboBox.SelectedItem = toLoad ?? list.FirstOrDefault(f => f.IsSelected) ?? list.FirstOrDefault();

            if (PresetComboBox.SelectedItem == null) //Why?
                PresetComboBox.SelectedItem = toLoad ?? list.FirstOrDefault(f => f.IsSelected) ?? list.FirstOrDefault();
        }

        private void LoadUploadPresets(ExportPreset preset, UploadPreset uploadPreset = null)
        {
            var type = (preset.Extension ?? preset.DefaultExtension) == ".zip" ? Export.Zip : preset.Type;
            var list = UserSettings.All.UploadPresets?.OfType<UploadPreset>().Where(w => w.AllowedTypes.Count == 0 || w.AllowedTypes.Contains(type)).ToList() ?? new List<UploadPreset>();

            //No need to adding grouping when there's no item to be displayed.
            if (list.Count == 0)
            {
                UploadPresetComboBox.ItemsSource = null;
                UploadPresetComboBox.ItemsSource = list;
                return;
            }

            //Groups by authentication mode.
            var lcv = new ListCollectionView(list.OrderBy(o => o.IsAnonymous).ThenBy(t => t.Title).ToList());
            lcv.GroupDescriptions?.Add(new PropertyGroupDescription("Mode"));

            var previous = preset.UploadService;

            UploadPresetComboBox.IsEnabled = true;
            UploadPresetComboBox.ItemsSource = lcv;

            if (uploadPreset != null && list.Contains(uploadPreset))
                preset.UploadService = uploadPreset.Title;
            else
                preset.UploadService = previous;
        }

        private IEnumerable<ExportPreset> GeneratePresets(Export type, ICollection<ExportPreset> presets)
        {
            switch (type)
            {
                //Animated images.
                case Export.Gif:
                {
                    AddDistinct(presets, EmbeddedGifPreset.Defaults);
                    AddDistinct(presets, FfmpegGifPreset.Defaults);
                    AddDistinct(presets, GifskiGifPreset.Defaults);
                    AddDistinct(presets, SystemGifPreset.Default);
                    break;
                }
                case Export.Apng:
                {
                    AddDistinct(presets, EmbeddedApngPreset.Default);
                    AddDistinct(presets, FfmpegApngPreset.Defaults);
                    break;
                }
                case Export.Webp:
                {
                    AddDistinct(presets, FfmpegWebpPreset.Defaults);
                    break;
                }

                //Videos.
                case Export.Avi:
                {
                    AddDistinct(presets, FfmpegAviPreset.Default);
                    break;
                }
                case Export.Mkv:
                {
                    AddDistinct(presets, FfmpegMkvPreset.Defaults);
                    break;
                }
                case Export.Mov:
                {
                    AddDistinct(presets, FfmpegMovPreset.Defaults);
                    break;
                }
                case Export.Mp4:
                {
                    AddDistinct(presets, FfmpegMp4Preset.Defaults);
                    break;
                }
                case Export.Webm:
                {
                    AddDistinct(presets, FfmpegWebmPreset.Defaults);
                    break;
                }

                //Images.
                case Export.Jpeg:
                {
                    AddDistinct(presets, JpegPreset.Default);
                    break;
                }
                case Export.Png:
                {
                    AddDistinct(presets, PngPreset.Default);
                    break;
                }
                case Export.Bmp:
                {
                    AddDistinct(presets, BmpPreset.Default);
                    break;
                }

                //Other.
                case Export.Stg:
                {
                    AddDistinct(presets, StgPreset.Default);
                    break;
                }
                case Export.Psd:
                {
                    AddDistinct(presets, PsdPreset.Default);
                    break;
                }
            }

            return presets;
        }

        private void AddDistinct(ICollection<ExportPreset> current, IEnumerable<IExportPreset> newList)
        {
            foreach (var preset in newList.Where(preset => current.Where(w => w.Type == preset.Type).All(a => a.TitleKey != preset.TitleKey)))
                current.Add((ExportPreset)preset);
        }

        private void AddDistinct(ICollection<ExportPreset> current, IExportPreset newPreset)
        {
            if (current.Where(w => w.Type == newPreset.Type).All(a => a.TitleKey != newPreset.TitleKey))
                current.Add((ExportPreset)newPreset);
        }

        private void SetPresetAsLastSelected(ExportPreset preset)
        {
            if (preset == null)
                return;

            //Get all presets of given type. It's possible that there's none available.
            var list = UserSettings.All.ExportPresets?.OfType<ExportPreset>().Where(w => w.Type == preset.Type).ToList() ?? new List<ExportPreset>();

            //Set the selected preset as the last selected one.
            foreach (var pre in list)
            {
                pre.IsSelected = (pre.Title ?? "").Equals(preset.Title ?? "");

                if (pre.Encoder == preset.Encoder)
                    pre.IsSelectedForEncoder = pre.IsSelected;
            }

            foreach (var pre in PresetComboBox.ItemsSource.OfType<ExportPreset>())
            {
                pre.IsSelected = (pre.Title ?? "").Equals(preset.Title ?? "");

                if (pre.Encoder == preset.Encoder)
                    pre.IsSelectedForEncoder = pre.IsSelected;
            }

            PersistPresets(list, preset.Type);
        }

        private static void PersistPresets(IEnumerable<ExportPreset> typeList, Export type)
        {
            var list = UserSettings.All.ExportPresets?.OfType<ExportPreset>().Where(w => w.Type != type).ToList() ?? new List<ExportPreset>();

            list.AddRange(typeList);

            UserSettings.All.ExportPresets = new ArrayList(list.ToArray());
        }

        private void AdjustCodecs(ExportPreset preset)
        {
            if (!(preset is VideoPreset videoPreset))
                return;

            FfmpegCodecComboBox.SelectionChanged -= FfmpegCodecComboBox_SelectionChanged;
            var codec = videoPreset.VideoCodec;

            switch (videoPreset.Type)
            {
                case Export.Avi:
                {
                    FfmpegCodecComboBox.ItemsSource = new List<VideoCodec>
                    {
                        new Mpeg2(),
                        new Mpeg4()
                    };

                    break;
                }
                case Export.Mkv:
                {
                    if (videoPreset.HardwareAcceleration == HardwareAcceleration.On)
                    {
                        FfmpegCodecComboBox.ItemsSource = new List<VideoCodec>
                        {
                            new X264(),
                            new H264Amf(),
                            new H264Nvenc(),
                            new H264Qsv(),
                            new X265(),
                            new HevcAmf(),
                            new HevcNvenc(),
                            new HevcQsv(),
                            new Vp8(),
                            new Vp9()
                        };
                    }
                    else
                    {
                        FfmpegCodecComboBox.ItemsSource = new List<VideoCodec>
                        {
                            new X264(),
                            new X265(),
                            new Vp8(),
                            new Vp9()
                        };
                    }

                    break;
                }
                case Export.Mov:
                case Export.Mp4:
                {
                    if (videoPreset.HardwareAcceleration == HardwareAcceleration.On)
                    {
                        FfmpegCodecComboBox.ItemsSource = new List<VideoCodec>
                        {
                            new X264(),
                            new H264Amf(),
                            new H264Nvenc(),
                            new H264Qsv(),
                            new X265(),
                            new HevcAmf(),
                            new HevcNvenc(),
                            new HevcQsv()
                        };
                    }
                    else
                    {
                        FfmpegCodecComboBox.ItemsSource = new List<VideoCodec>
                        {
                            new X264(),
                            new X265()
                        };
                    }

                    break;
                }
                case Export.Webm:
                {
                    FfmpegCodecComboBox.ItemsSource = new List<VideoCodec>
                    {
                        new Vp8(),
                        new Vp9()
                    };

                    break;
                }
            }

            videoPreset.VideoCodec = VideoCodecs.NotSelected;
            FfmpegCodecComboBox.SelectionChanged += FfmpegCodecComboBox_SelectionChanged;
            videoPreset.VideoCodec = codec;
        }

        private void ChangeFileNumber(int change)
        {
            //If there's no filename declared, show the default one.
            if (string.IsNullOrWhiteSpace(CurrentPreset.OutputFilename))
            {
                CurrentPreset.OutputFilename = LocalizationHelper.Get(CurrentPreset.OutputFilenameKey);
                return;
            }

            var index = CurrentPreset.OutputFilename.Length;
            int start = -1, end = -1;

            //Detects the last number in a string.
            foreach (var c in CurrentPreset.OutputFilename.Reverse())
            {
                if (char.IsNumber(c))
                {
                    if (end == -1)
                        end = index;

                    start = index - 1;
                }
                else if (start == index)
                    break;

                index--;
            }

            //If there's no number.
            if (end == -1)
            {
                CurrentPreset.OutputFilename += $" ({change})";
                return;
            }

            //If it's a negative number, include the signal.
            if (start > 0 && CurrentPreset.OutputFilename.Substring(start - 1, 1).Equals("-"))
                start--;

            //Cut, convert, merge.
            if (int.TryParse(CurrentPreset.OutputFilename.Substring(start, end - start), out var number))
            {
                var offset = start + number.ToString().Length;

                CurrentPreset.OutputFilename = CurrentPreset.OutputFilename.Substring(0, start) + (number + change) + CurrentPreset.OutputFilename.Substring(offset, CurrentPreset.OutputFilename.Length - end);
            }
        }

        private bool IsExpressionValid(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            //Divide by commas
            var blocks = expression.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                var subs = block.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                //Only one X - Y range per block.
                if (subs.Length > 2)
                    return false;

                //Only valid, within-range numerical values accepted.
                foreach (var sub in subs)
                {
                    if (!int.TryParse(sub, out var number))
                        return false;

                    if (number < 0 || number > FrameCount - 1)
                        return false;
                }
            }

            return true;
        }

        public void InitialFocus()
        {
            FilenameTextBox.Focus();
        }

        public async Task<bool> IsValid()
        {
            #region Validate preset specific properties

            var args = await CurrentPreset.IsValid();

            if (args != null)
            {
                if (args.Reason == StatusReasons.FileAlreadyExists)
                    FileExistsGrid.Visibility = Visibility.Visible;

                RaiseValidatedEvent(args);
                return false;
            }

            #endregion

            if (CurrentPreset.PickLocation)
            {
                if (CurrentPreset.ExportAsProjectToo)
                {
                    if (!CurrentPreset.OverwriteOnSave)
                    {
                        //Get the project extension in use.
                        var extension = UserSettings.All.ExportPresets.OfType<StgPreset>().OrderBy(o => o.IsSelectedForEncoder).Select(s => s.Extension ?? s.DefaultExtension).FirstOrDefault() ?? ".stg";

                        if (File.Exists(Path.Combine(CurrentPreset.OutputFolder, CurrentPreset.OutputFilename + extension)))
                        {
                            RaiseValidatedEvent("S.SaveAs.Warning.Overwrite.Project", StatusReasons.FileAlreadyExists);
                            return false;
                        }
                    }
                }
            }

            if (CurrentPreset.UploadFile)
            {
                var presetType = CurrentPreset.Extension == ".zip" ? Export.Zip : CurrentPreset.Type;
                var upload = UserSettings.All.UploadPresets.OfType<UploadPreset>().FirstOrDefault(f => (f.AllowedTypes.Count == 0 || f.AllowedTypes.Contains(presetType)) && f.Title == CurrentPreset.UploadService);

                args = await upload.IsValid();

                if (args != null)
                {
                    RaiseValidatedEvent(args);
                    return false;
                }
            }

            if (CurrentPreset.ExportPartially)
            {
                if (CurrentPreset.PartialExport == PartialExportType.Selection && SelectionCount < 1)
                {
                    RaiseValidatedEvent("S.SaveAs.Warning.Partial.NoSelection", StatusReasons.InvalidState);
                    return false;
                }

                if (CurrentPreset.PartialExport == PartialExportType.FrameExpression && !IsExpressionValid(CurrentPreset.PartialExportFrameExpression))
                {
                    RaiseValidatedEvent("S.SaveAs.Warning.Partial.InvalidExpression", StatusReasons.InvalidState);
                    return false;
                }
            }

            #region FFmpeg

            if (CurrentPreset.RequiresFfmpeg)
            {
                if (!Other.IsFfmpegPresent())
                {
                    RaiseValidatedEvent("S.Editor.Warning.Ffmpeg", StatusReasons.MissingFfmpeg, () => App.MainViewModel.OpenOptions.Execute(Options.ExtrasIndex));
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation) && UserSettings.All.FfmpegLocation.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                {
                    RaiseValidatedEvent("S.Options.Extras.FfmpegLocation.Invalid", StatusReasons.MissingFfmpeg, () => App.MainViewModel.OpenOptions.Execute(Options.ExtrasIndex));
                    return false;
                }

                if (!(CurrentPreset is IFfmpegPreset ffmpegPreset))
                    return false;

                if (ffmpegPreset.SettingsMode == VideoSettingsMode.Advanced)
                {
                    //Empty.
                    if (string.IsNullOrWhiteSpace(ffmpegPreset.Parameters))
                    {
                        RaiseValidatedEvent("S.SaveAs.Warning.Ffmpeg.Empty", StatusReasons.EmptyProperty);
                        return false;
                    }

                    //Missing special parameters.
                    if (!ffmpegPreset.Parameters.Contains("{I}") || !ffmpegPreset.Parameters.Contains("{O}"))
                    {
                        RaiseValidatedEvent("S.SaveAs.Warning.Ffmpeg.MissingPath", StatusReasons.InvalidState);
                        return false;
                    }
                }
            }

            #endregion

            #region Gifski

            if (CurrentPreset.RequiresGifski)
            {
                if (!Other.IsGifskiPresent())
                {
                    RaiseValidatedEvent("S.Editor.Warning.Gifski", StatusReasons.MissingGifski, () => App.MainViewModel.OpenOptions.Execute(Options.ExtrasIndex));
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(UserSettings.All.GifskiLocation) && UserSettings.All.GifskiLocation.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                {
                    RaiseValidatedEvent("S.Options.Extras.GifskiLocation.Invalid", StatusReasons.MissingGifski, () => App.MainViewModel.OpenOptions.Execute(Options.ExtrasIndex));
                    return false;
                }
            }

            #endregion

            return true;
        }

        public ExportPreset GetPreset()
        {
            return CurrentPreset ?? PresetComboBox.SelectedItem as ExportPreset;
        }

        #endregion

        #region Events

        private void Panel_Loaded(object sender, RoutedEventArgs e)
        {
            //If a default file type was not selected, it picks 'Gif' as default.
            if (!(TypeComboBox.SelectedValue is Export type))
                TypeComboBox.SelectedValue = type = Export.Gif;

            AdjustPresentation(type);
            LoadPresets(type, null, true);
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (!(TypeComboBox.SelectedValue is Export type))
                return;

            AdjustPresentation(type);
            LoadPresets(type);
        }


        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(PresetComboBox.SelectedItem is ExportPreset selected))
                return;

            //Hide all other grids.
            foreach (var grid in EncoderGrid.Children.OfType<Grid>())
                grid.Visibility = Visibility.Collapsed;

            SetPresetAsLastSelected(selected);

            //Set encoder.
            EncoderComboBox.SelectedValue = selected.Encoder;
            QuantizerComboBox.Visibility = UserSettings.All.SaveType == Export.Gif && selected.Encoder == EncoderType.ScreenToGif ? Visibility.Visible : Visibility.Collapsed;
            EncoderExpander.SetResourceReference(HeaderedContentControl.HeaderProperty, QuantizerComboBox.Visibility == Visibility.Visible ? "S.SaveAs.Encoder.Quantizer" : "S.SaveAs.Encoder");

            //Remove events prior to changing preset.
            FfmpegAccelerationComboBox.SelectionChanged -= FfmpegAccelerationComboBox_SelectionChanged;

            //Load video codecs.
            AdjustCodecs(selected);

            //Set the preset to the UI.
            CurrentPreset = selected.HasAutoSave ? selected : selected.ShallowCopy();

            //Adjust values for non-persistent properties.
            selected.PartialExportFrameStart = 0;
            selected.PartialExportFrameEnd = FrameCount;
            selected.PartialExportTimeStart = TimeSpan.Zero;
            selected.PartialExportTimeEnd = TotalTime;
            selected.PartialExportFrameExpression = $"0 - {FrameCount - 1}";

            //Select the upload preset.
            LoadUploadPresets(selected);

            if (string.IsNullOrWhiteSpace(selected.Extension))
                selected.Extension = selected.DefaultExtension;

            //Get the selected preset and display it's settings in the next expanders.
            switch (selected.Type)
            {
                //Animated images.
                case Export.Apng:
                {
                    if (!(selected is ApngPreset apngPreset))
                        break;

                    switch (apngPreset.Encoder)
                    {
                        case EncoderType.ScreenToGif:
                            EmbeddedApngOptionsGrid.Visibility = Visibility.Visible;
                            break;
                        case EncoderType.FFmpeg:
                            FfmpegApngOptionsGrid.Visibility = Visibility.Visible;
                            break;
                    }

                    return;
                }
                case Export.Gif:
                {
                    if (!(selected is GifPreset gifPreset))
                        break;

                    switch (gifPreset.Encoder)
                    {
                        case EncoderType.ScreenToGif:
                            EmbeddedGifOptionsGrid.Visibility = Visibility.Visible;
                            break;
                        case EncoderType.System:
                            SystemGifOptionsGrid.Visibility = Visibility.Visible;
                            break;
                        case EncoderType.FFmpeg:
                            FfmpegGifOptionsGrid.Visibility = Visibility.Visible;
                            break;
                        case EncoderType.Gifski:
                            GifskiGifOptionsGrid.Visibility = Visibility.Visible;
                            break;
                    }

                    return;
                }
                case Export.Webp:
                    FfmpegWebpOptionsGrid.Visibility = Visibility.Visible;
                    break;

                //Videos.
                case Export.Avi:
                case Export.Mkv:
                case Export.Mov:
                case Export.Mp4:
                case Export.Webm:
                    FfmpegVideoOptionsGrid.Visibility = Visibility.Visible;
                    FfmpegAccelerationComboBox.SelectionChanged += FfmpegAccelerationComboBox_SelectionChanged;
                    break;

                //Images.
                case Export.Jpeg:
                case Export.Png:
                case Export.Bmp:
                    EmbeddedImageOptionsGrid.Visibility = Visibility.Visible;
                    break;

                //Others.
                case Export.Stg:
                    EmbeddedStgOptionsGrid.Visibility = Visibility.Visible;
                    break;
                case Export.Psd:
                    EmbeddedPsdOptionsGrid.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void AddPreset_Click(object sender, RoutedEventArgs e)
        {
            var add = new Preset
            {
                Current = PresetComboBox.SelectedItem as ExportPreset,
                IsNew = true
            };

            var result = add.ShowDialog();

            if (result != true)
                return;

            //Select the created preset.
            LoadPresets(add.Current.Type, add.Current);
        }

        private void SavePreset_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null)
                return;

            var list = UserSettings.All.ExportPresets.OfType<ExportPreset>().ToList();
            var oldPreset = list.FirstOrDefault(f => f.Type == CurrentPreset.Type && f.Title == CurrentPreset.Title);

            if (oldPreset != null)
                list.Remove(oldPreset);

            list.Add(CurrentPreset);

            UserSettings.All.ExportPresets = new ArrayList(list);
        }

        private void EditPreset_Click(object sender, RoutedEventArgs e)
        {
            var edit = new Preset
            {
                Current = PresetComboBox.SelectedItem as ExportPreset
            };

            var result = edit.ShowDialog();

            if (result != true)
                return;

            //Select the edited preset.
            LoadPresets(edit.Current.Type, edit.Current);
        }

        private void RemovePreset_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Let the user remove default presets (just not the main default for the type). Add a way to restore removed presets.

            //Ask if the user really wants to remove the preset.
            if (!(PresetComboBox.SelectedItem is ExportPreset preset) || !Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Presets.Ask.Delete.Title"), LocalizationHelper.Get("S.SaveAs.Presets.Ask.Delete.Instruction"),
                LocalizationHelper.Get("S.SaveAs.Presets.Ask.Delete.Message")))
                return;

            //Remove the preset directly from the settings and reaload all presets.
            UserSettings.All.ExportPresets.Remove(preset);
            LoadPresets(preset.Type);
        }

        private void ResetPreset_Click(object sender, RoutedEventArgs e)
        {
            //Ask if the user really wants to reset the preset to its default settings.
            if (!(PresetComboBox.SelectedItem is ExportPreset preset) || !Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Presets.Ask.Reset.Title"), LocalizationHelper.Get("S.SaveAs.Presets.Ask.Reset.Instruction"),
                LocalizationHelper.Get("S.SaveAs.Presets.Ask.Reset.Message")))
                return;

            var resetted = GeneratePresets(preset.Type, new List<ExportPreset>()).FirstOrDefault(f => f.TitleKey == preset.TitleKey);

            if (resetted == null)
                return; //TODO: What to do? Tell the user that this is an old preset, which is not being used anymore.

            UserSettings.All.ExportPresets.Remove(preset);
            UserSettings.All.ExportPresets.Add(resetted);
            LoadPresets(resetted.Type, resetted);
        }


        private void EncoderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || EncoderGrid.Children.OfType<Grid>().All(a => a.Visibility == Visibility.Collapsed))
                return;

            if (!(EncoderComboBox.SelectedValue is EncoderType encoder) || encoder == CurrentPreset?.Encoder)
                return;

            PresetComboBox.SelectedItem = PresetComboBox.ItemsSource.OfType<ExportPreset>().FirstOrDefault(f => f.Type == UserSettings.All.SaveType && f.Encoder == encoder && f.IsSelectedForEncoder) ??
                PresetComboBox.ItemsSource.OfType<ExportPreset>().FirstOrDefault(f => f.Type == UserSettings.All.SaveType && f.Encoder == encoder);
            QuantizerComboBox.Visibility = UserSettings.All.SaveType == Export.Gif && encoder == EncoderType.ScreenToGif ? Visibility.Visible : Visibility.Collapsed;
        }

        private void QuantizerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            switch (QuantizerComboBox.SelectedValue as ColorQuantizationType?)
            {
                case ColorQuantizationType.Neural:
                    SamplingTextBlock.Visibility = Visibility.Visible;
                    SamplingFactorSlider.Visibility = Visibility.Visible;
                    SamplingFactorGrid.Visibility = Visibility.Visible;
                    GlobalColorTableCheckBox.Visibility = Visibility.Visible;
                    break;
                case ColorQuantizationType.Octree:
                    SamplingTextBlock.Visibility = Visibility.Collapsed;
                    SamplingFactorSlider.Visibility = Visibility.Collapsed;
                    SamplingFactorGrid.Visibility = Visibility.Collapsed;
                    GlobalColorTableCheckBox.Visibility = Visibility.Collapsed;
                    break;
                case ColorQuantizationType.MedianCut:
                    SamplingTextBlock.Visibility = Visibility.Collapsed;
                    SamplingFactorSlider.Visibility = Visibility.Collapsed;
                    SamplingFactorGrid.Visibility = Visibility.Collapsed;
                    GlobalColorTableCheckBox.Visibility = Visibility.Visible;
                    break;
                case ColorQuantizationType.Grayscale:
                    SamplingTextBlock.Visibility = Visibility.Collapsed;
                    SamplingFactorSlider.Visibility = Visibility.Collapsed;
                    SamplingFactorGrid.Visibility = Visibility.Collapsed;
                    GlobalColorTableCheckBox.Visibility = Visibility.Collapsed;
                    break;
                case ColorQuantizationType.MostUsed:
                    SamplingTextBlock.Visibility = Visibility.Collapsed;
                    SamplingFactorSlider.Visibility = Visibility.Collapsed;
                    SamplingFactorGrid.Visibility = Visibility.Collapsed;
                    GlobalColorTableCheckBox.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void PredictionHelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://www.w3.org/TR/PNG-Filters.html");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to navigate to the PNG prediction method documentation.");
            }
        }

        private void PreviewCommand_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (!(CurrentPreset is IFfmpegPreset ffmpegPreset))
                return;

            var previewer = new CommandPreviewer
            {
                Parameters = ffmpegPreset.Parameters,
                Extension = CurrentPreset.Extension ?? CurrentPreset.DefaultExtension
            };
            previewer.ShowDialog();
        }

        private void FfmpegCodecComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (!(FfmpegCodecComboBox.SelectedItem is VideoCodec selected) || !(CurrentPreset is VideoPreset videoPreset))
                return;

            //That's a lot of work just to maintain the binding. Sure there must be an easy way, right?
            var containsPreset = selected.CodecPresets.Any(a => a.Type == videoPreset.CodecPreset);
            var containsFormat = selected.PixelFormats.Any(a => a.Type == videoPreset.PixelFormat);
            var codecPreset = videoPreset.CodecPreset;
            var pixelFormat = videoPreset.PixelFormat;

            //For some reason, if the same enum is being set, the combo does not display the selection.
            videoPreset.CodecPreset = VideoCodecPresets.NotSelected;
            videoPreset.PixelFormat = VideoPixelFormats.NotSelected;

            switch (selected.Type)
            {
                case VideoCodecs.Mpeg2:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.None;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.Mpeg4:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.None;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.X264:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.H264Amf:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Balanced;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.H264Nvenc:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.H264Qsv:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Auto;
                    break;
                case VideoCodecs.X265:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.HevcAmf:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Balanced;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.HevcNvenc:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.P010Le;
                    break;
                case VideoCodecs.HevcQsv:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Auto;
                    break;
                case VideoCodecs.Vp8:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
                case VideoCodecs.Vp9:
                    videoPreset.CodecPreset = containsPreset ? codecPreset : VideoCodecPresets.Medium;
                    videoPreset.PixelFormat = containsFormat ? pixelFormat : VideoPixelFormats.Yuv420p;
                    break;
            }
        }

        private void FfmpegAccelerationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            AdjustCodecs(CurrentPreset);

            if (FfmpegCodecComboBox.SelectedIndex == -1)
                FfmpegCodecComboBox.SelectedIndex = 0;
        }

        private void ImagesZipCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (!(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            LoadUploadPresets(preset);
        }

        private void ChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            if (!(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            try
            {
                var output = preset.OutputFolder ?? "";

                if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                    output = "";

                //It's only a relative path if not null/empty and there's no root folder declared.
                var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
                var notAlt = !string.IsNullOrWhiteSpace(output) && preset.OutputFolder.Contains(Path.DirectorySeparatorChar);

                var initial = Directory.Exists(output) ? output : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                {
                    #region Select folder

                    var fs = new FolderSelector
                    {
                        Description = LocalizationHelper.Get("S.SaveAs.File.SelectFolder"),
                        DefaultFolder = isRelative ? Path.GetFullPath(initial) : initial,
                        SelectedPath = isRelative ? Path.GetFullPath(initial) : initial
                    };

                    if (!fs.ShowDialog())
                        return;

                    preset.OutputFolder = fs.SelectedPath;
                    ChooseLocatioButton.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                    #endregion
                }
                else
                {
                    #region Save folder and file

                    var sfd = new SaveFileDialog
                    {
                        FileName = preset.OutputFilename,
                        InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial
                    };

                    #region Extensions

                    switch (preset.Type)
                    {
                        //Animated image.
                        case Export.Apng:
                            sfd.Filter = string.Format("{0}|*.png|{0}|*.apng", LocalizationHelper.Get("S.Editor.File.Apng"));
                            sfd.DefaultExt = preset.Extension ?? ".png";
                            break;
                        case Export.Gif:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Gif")} (.gif)|*.gif";
                            sfd.DefaultExt = ".gif";
                            break;
                        case Export.Webp:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Webp")} (.webp)|*.webp";
                            sfd.DefaultExt = ".webp";
                            break;

                        //Video.
                        case Export.Avi:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Avi")} (.avi)|*.avi";
                            sfd.DefaultExt = ".avi";
                            break;
                        case Export.Mkv:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Mkv")} (.mkv)|*.mkv";
                            sfd.DefaultExt = ".mkv";
                            break;
                        case Export.Mov:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Mov")} (.mov)|*.mov";
                            sfd.DefaultExt = ".mov";
                            break;
                        case Export.Mp4:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Mp4")} (.mp4)|*.mp4";
                            sfd.DefaultExt = ".mp4";
                            break;
                        case Export.Webm:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Webm")} (.webm)|*.webm";
                            sfd.DefaultExt = ".webm";
                            break;

                        //Images.
                        case Export.Bmp:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Image.Bmp")} (.bmp)|*.bmp|{LocalizationHelper.Get("S.Editor.File.Project.Image.Zip")} (.zip)|*.zip";
                            sfd.DefaultExt = preset.Extension ?? preset.DefaultExtension ?? ".bmp";
                            break;
                        case Export.Jpeg:
                            sfd.Filter = string.Format("{0}|*.jpg|{0}|*.jpeg|{1} (.zip)|*.zip", LocalizationHelper.Get("S.Editor.File.Image.Jpeg"), LocalizationHelper.Get("S.Editor.File.Project.Image.Zip"));
                            sfd.DefaultExt = preset.Extension ?? preset.DefaultExtension ?? ".jpg";
                            break;
                        case Export.Png:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Image.Png")} (.png)|*.png|{LocalizationHelper.Get("S.Editor.File.Project.Image.Zip")} (.zip)|*.zip";
                            sfd.DefaultExt = preset.Extension ?? preset.DefaultExtension ?? ".png";
                            break;

                        //Other.
                        case Export.Stg:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Project")} (.stg)|*.stg|{LocalizationHelper.Get("S.Editor.File.Project.Zip")} (.zip)|*.zip";
                            sfd.DefaultExt = preset.Extension ?? ".stg";
                            break;
                        case Export.Psd:
                            sfd.Filter = $"{LocalizationHelper.Get("S.Editor.File.Psd")} (.psd)|*.psd";
                            sfd.DefaultExt = ".psd";
                            break;
                    }

                    #endregion

                    var result = sfd.ShowDialog();

                    if (!result.HasValue || !result.Value)
                        return;

                    //TODO: process output before setting to property?

                    preset.OutputFolder = Path.GetDirectoryName(sfd.FileName);
                    preset.OutputFilename = Path.GetFileNameWithoutExtension(sfd.FileName);
                    preset.OverwriteOnSave = File.Exists(sfd.FileName);
                    preset.Extension = Path.GetExtension(sfd.FileName);

                    RaiseSaveEvent();

                    #endregion
                }

                //Converts to a relative path again.
                if (isRelative && !string.IsNullOrWhiteSpace(preset.OutputFolder))
                {
                    var selected = new Uri(preset.OutputFolder);
                    var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                    var relativeFolder = selected.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) == baseFolder.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) ?
                        "." : Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                    //This app even returns you the correct slashes/backslashes.
                    preset.OutputFolder = notAlt ? relativeFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
            }
            catch (ArgumentException sx)
            {
                LogWriter.Log(sx, "Error while trying to choose the output path and filename.", preset.OutputFolder + preset.OutputFilename);

                preset.OutputFolder = "";
                preset.OutputFilename = "";
                throw;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while trying to choose the output path and filename.", preset.OutputFolder + preset.OutputFilename);
                throw;
            }
        }

        private void FilenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || !(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            _searchTimer?.Stop();

            //If no file will be saved, there's no need to verify.
            if (!preset.PickLocation || preset.CanExportMultipleFiles)
            {
                FileExistsGrid.Visibility = Visibility.Collapsed;
                RaiseValidationRemovedEvent(StatusReasons.FileAlreadyExists);
                return;
            }

            _searchTimer?.Start();
        }

        private async void SearchTimer_Tick(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            _searchTimer.Stop();

            await Task.Run(() =>
            {
                try
                {
                    var preset = Dispatcher.Invoke(() => CurrentPreset);

                    if (preset == null)
                        return;

                    //Check if there's a file with the same path.
                    var exists = File.Exists(Path.Combine(preset.OutputFolder, PathHelper.ReplaceRegexInName(preset.OutputFilename) + preset.Extension));

                    Dispatcher.Invoke(() =>
                    {
                        FileExistsGrid.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;
                        RaiseValidationRemovedEvent(StatusReasons.FileAlreadyExists);
                    });
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Check if exists");

                    Dispatcher.Invoke(() =>
                    {
                        RaiseValidatedEvent("S.SaveAs.Warning.Filename.Invalid", StatusReasons.InvalidState, () => new ExceptionViewer(ex).Show());
                        FileExistsGrid.Visibility = Visibility.Collapsed;
                    });
                }
            });
        }

        private void SaveType_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            FilenameTextBox_TextChanged(null, null);
        }

        private void Extension_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            if (preset is ImagePreset imagePreset)
                imagePreset.ZipFilesInternal = (string)ExtensionComboBox.SelectedValue == ".zip";

            FilenameTextBox_TextChanged(null, null);
        }

        private void IncreaseNumber_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileNumber(1);
        }

        private void DecreaseNumber_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileNumber(-1);
        }

        private void FileHyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            if (!(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            try
            {
                //If the file name template result changed, it will be imposible to open the previous file. The user should simple try to save it again.
                Process.Start(Path.Combine(preset.OutputFolder, PathHelper.ReplaceRegexInName(preset.OutputFilename) + preset.Extension));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open file that already exists using the hyperlink");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, $"Error while trying to navigate to a given URI: '{e?.Uri?.AbsoluteUri}'.");
            }
        }

        private void UploadFileCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CurrentPreset.CopyType == CopyType.Link)
                CurrentPreset.CopyType = CopyType.File;
        }

        private void AddUploadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            var upload = new Upload { Type = preset.Extension == ".zip" ? Export.Zip : preset.Type };
            var result = upload.ShowDialog();

            if (result != true)
                return;

            UserSettings.All.UploadPresets.Add(upload.CurrentPreset);
            UserSettings.Save();

            LoadUploadPresets(preset, upload.CurrentPreset);
        }

        private void EditUploadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            if (!(UploadPresetComboBox.SelectedItem is UploadPreset selected))
                return;

            var upload = new Upload { CurrentPreset = selected.ShallowCopy(), IsEditing = true };
            var result = upload.ShowDialog();

            if (result != true)
                return;

            UserSettings.All.UploadPresets.Remove(selected);
            UserSettings.All.UploadPresets.Add(upload.CurrentPreset);
            UserSettings.Save();

            //Update the upload preset in all export presets.
            if (selected.Title != upload.CurrentPreset.Title)
            {
                foreach (var exportPreset in UserSettings.All.ExportPresets.OfType<ExportPreset>().Where(w => w.UploadService == selected.Title))
                    exportPreset.UploadService = upload.CurrentPreset.Title;
            }

            LoadUploadPresets(preset, upload.CurrentPreset);
        }

        private void HistoryUploadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!(UploadPresetComboBox.SelectedItem is UploadPreset selected))
                return;

            var history = new UploadHistory
            {
                CurrentPreset = selected
            };
            history.ShowDialog();
        }

        private void RemoveUploadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!(PresetComboBox.SelectedItem is ExportPreset preset))
                return;

            //Ask if the user really wants to remove the preset.
            if (!(UploadPresetComboBox.SelectedItem is UploadPreset selected) || !Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Title"), LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Instruction"),
                LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Message")))
                return;

            UserSettings.All.UploadPresets.Remove(selected);
            UserSettings.Save();

            //Remove the upload preset from all export presets.
            foreach (var exportPreset in UserSettings.All.ExportPresets.OfType<ExportPreset>().Where(w => w.UploadService == selected.Title))
                exportPreset.UploadService = null;

            LoadUploadPresets(preset);
        }

        private void ExportPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            UserSettings.Save();
        }

        #endregion
    }
}