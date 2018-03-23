﻿using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Xaml;
using System.Xml;
using XamlParseException = System.Windows.Markup.XamlParseException;
using XamlReader = System.Windows.Markup.XamlReader;
using XamlWriter = System.Windows.Markup.XamlWriter;

namespace ScreenToGif.Util
{
    internal sealed class UserSettings : INotifyPropertyChanged
    {
        #region Variables

        private static ResourceDictionary _local;
        private static ResourceDictionary _appData;
        private static readonly ResourceDictionary Default;

        public event PropertyChangedEventHandler PropertyChanged;

        public static UserSettings All { get; } = new UserSettings();

        #endregion

        static UserSettings()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            //Paths.
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");
            var appData = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            //Only creates an empty AppData settings file if there's no local settings defined.
            if (!File.Exists(local) && !File.Exists(appData))
            {
                var directory = Path.GetDirectoryName(appData);

                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                //Just creates an empty filewithout writting anything. 
                File.Create(appData).Dispose();
            }

            //Loads AppData settings.
            if (File.Exists(appData))
            {
                _appData = LoadOrDefault(appData);
                Application.Current.Resources.MergedDictionaries.Add(_appData);
            }

            //Loads Local settings.
            if (File.Exists(local))
            {
                _local = LoadOrDefault(local);
                Application.Current.Resources.MergedDictionaries.Add(_local);
            }

            //Reads the default settings (It's loaded by default).
            Default = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.EndsWith("/Settings.xaml"));
        }

        #region Methods

        public static void Save()
        {
            //Only writes if there's something changed. Should not write the default dictionary.
            if (_local == null && _appData == null)
                return;

            //Filename: Local or AppData.
            var filename = _local != null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml") :
                Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");
            var backup = _local != null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml.bak") :
                Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml.bak");

            #region Create folder

            var folder = Path.GetDirectoryName(filename);

            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            #endregion

            //Create backup.
            if (File.Exists(filename))
                File.Copy(filename, backup, true);

            try
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    CheckCharacters = true,
                    CloseOutput = true,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Encoding = Encoding.UTF8,
                };

                using (var writer = XmlWriter.Create(filename, settings))
                    XamlWriter.Save(_local ?? _appData, writer);

                if (File.ReadAllText(filename).All(x => x == '\0'))
                    File.Copy(backup, filename, true);

                File.Delete(backup);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Saving settings");
            }
        }

        private static object GetValue([CallerMemberName] string key = "", object defaultValue = null)
        {
            if (Default == null)
                return defaultValue;

            if (Application.Current == null || Application.Current.Resources == null)
                return Default[key];

            if (Application.Current.Resources.Contains(key))
                return Application.Current.Resources[key];

            return Default[key] ?? defaultValue;
        }

        private static void SetValue(object value, [CallerMemberName] string key = "")
        {
            //Updates or inserts the value to the Local resource.
            if (_local != null)
            {
                if (_local.Contains(key))
                    _local[key] = value;
                else
                    _local.Add(key, value);
            }

            //Updates or inserts the value to the AppData resource.
            if (_appData != null)
            {
                if (_appData.Contains(key))
                    _appData[key] = value;
                else
                    _appData.Add(key, value);
            }

            //Updates/Adds the current value of the resource.
            if (Application.Current.Resources.Contains(key))
                Application.Current.Resources[key] = value;
            else
                Application.Current.Resources.Add(key, value);

            All.OnPropertyChanged(key);
        }

        private static ResourceDictionary LoadOrDefault(string path, int trial = 0, XamlObjectWriterException exception = null)
        {
            ResourceDictionary resource = null;

            try
            {
                if (!File.Exists(path))
                    return new ResourceDictionary();

                if (exception != null)
                {
                    var content = File.ReadAllLines(path).ToList();
                    content.RemoveAt(exception.LineNumber - 1);

                    File.WriteAllLines(path, content);
                }

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    try
                    {
                        //Read in ResourceDictionary File
                        resource = (ResourceDictionary)XamlReader.Load(fs);
                    }
                    catch (XamlParseException xx)
                    {
                        if (xx.InnerException is XamlObjectWriterException inner && trial < 5)
                            return LoadOrDefault(path, trial + 1, inner);

                        resource = new ResourceDictionary();
                    }
                    catch (Exception ex)
                    {
                        //Sets a default value if null.
                        resource = new ResourceDictionary();
                    }
                }

                //Tries to load the resource from disk. 
                //resource = new ResourceDictionary {Source = new Uri(path, UriKind.RelativeOrAbsolute)};
            }
            catch (Exception)
            {
                //Sets a default value if null.
                resource = new ResourceDictionary();
            }

            return resource;
        }

        public static void CreateLocalSettings()
        {
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (!File.Exists(local))
                File.Create(local).Dispose();

            _local = LoadOrDefault(local);
        }

        public static void RemoveLocalSettings()
        {
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (File.Exists(local))
                File.Delete(local);

            _local = null; //TODO: Should I remove from the merged dictionaries?
        }

        public static void RemoveAppDataSettings()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml");

            if (File.Exists(appData))
                File.Delete(appData);

            _appData = null; //TODO: Should I remove from the merged dictionaries?
        }

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Startup

        public double StartupTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double StartupLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double StartupHeight
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double StartupWidth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public WindowState StartupWindowState
        {
            get => (WindowState)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Recorder

        public Rect SelectedRegion
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }

        public int RecorderModeIndex
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Application

        public int StartUp
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool ShowCursor
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool DetectMouseClicks
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public Color ClickColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool UsePreStart
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int PreStartValue
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool SnapshotMode
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int SnapshotDefaultDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool FixedFrameRate
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AsyncRecording
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NewRecorder
        {
            get => (bool)GetValue(nameof(NewRecorder), true);
            set => SetValue(value);
        }

        public bool Magnifier
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool ShowNotificationIcon
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool KeepOpen
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NotifyFrameDeletion
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NotifyProjectDiscard
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NotifyWhileClosingEditor
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool DrawOutlineOutside
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool CheckForUpdates 
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Shortcuts

        public Key RecorderShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys RecorderModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key WebcamRecorderShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys WebcamRecorderModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key BoardRecorderShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys BoardRecorderModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key EditorShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys EditorModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key OptionsShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys OptionsModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key ExitShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys ExitModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }


        public Key StartPauseShortcut
        {
            get => (Key)GetValue();
            set => SetValue(value);
        }

        public ModifierKeys StartPauseModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key StopShortcut
        {
            get => (Key)GetValue();
            set => SetValue(value);
        }

        public ModifierKeys StopModifiers
        {
            get => (ModifierKeys)GetValue();
            set => SetValue(value);
        }

        public Key DiscardShortcut
        {
            get => (Key)GetValue();
            set => SetValue(value);
        }

        public ModifierKeys DiscardModifiers
        {
            get => (ModifierKeys)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Language

        public string LanguageCode
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Cloud

        //Proxy
        public ProxyType ProxyMode
        {
            get => (ProxyType)GetValue(defaultValue:ProxyType.Disabled);
            set => SetValue(value);
        }

        public string ProxyHost
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public int ProxyPort
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public string ProxyUsername
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string ProxyPassword
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Imgur (Anonymous)
        public bool ImgurAnonymousUseDirectLinks
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool ImgurAnonymousUseGifvLink
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        //Imgur
        public string ImgurOAuthToken
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string ImgurAccessToken
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string ImgurRefreshToken
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public DateTime? ImgurExpireDate
        {
            get => (DateTime?)GetValue();
            set => SetValue(value);
        }

        public bool ImgurUseDirectLinks
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool ImgurUseGifvLink
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool ImgurUploadToAlbum
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string ImgurSelectedAlbum
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public ArrayList ImgurAlbumList
        {
            get => (ArrayList)GetValue();
            set => SetValue(value);
        }

        //Yandex
        public string YandexDiskOAuthToken
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Properties

        public int LatestFps
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public double RecorderLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double RecorderTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public int RecorderWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int RecorderHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color GridColor1
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color GridColor2
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color RecorderBackground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color RecorderForeground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }


        public Rect GridSize
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }


        public double EditorTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorHeight
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorWidth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public WindowState EditorWindowState
        {
            get => (WindowState)GetValue();
            set => SetValue(value);
        }

        public Color InsertFillColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int LatestFpsImport
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #region Options



        public Color BoardGridBackground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color BoardGridColor1
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color BoardGridColor2
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Rect BoardGridSize
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }

        public bool EditorExtendChrome
        {
            get => (bool)GetValue(defaultValue: false);
            set => SetValue(value);
        }

        public bool RecorderThinMode
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool TripleClickSelection
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AutomaticallySizeOnContent
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AutomaticallyFitImage
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string LogsFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string TemporaryFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool AutomaticCleanUp
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int AutomaticCleanUpDays
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public string FfmpegLocation
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string GifskiLocation
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Board

        public int BoardWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color BoardColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int BoardStylusHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardStylusWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip BoardStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool BoardFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool BoardIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int BoardEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip BoardEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor

        public PasteBehavior PasteBehavior
        {
            get => (PasteBehavior)GetValue();
            set => SetValue(value);
        }

        #region Save As

        //Type and encoder.
        public bool IsSaveTypeExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public Export SaveType
        {
            get => (Export)GetValue();
            set => SetValue(value);
        }

        public GifEncoderType GifEncoder
        {
            get => (GifEncoderType)GetValue();
            set => SetValue(value);
        }

        public VideoEncoderType VideoEncoder
        {
            get => (VideoEncoderType)GetValue();
            set => SetValue(value);
        }

        public bool IsGifOptionsExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool IsApngOptionsExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool IsVideoOptionsExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool IsSaveOptionsExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }
        
        //Gif.
        public int Quality
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int GifskiQuality
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int MaximumColors
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool Looped
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool RepeatForever
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int RepeatCount
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public ColorQuantizationType ColorQuantization
        {
            get => (ColorQuantizationType)GetValue();
            set => SetValue(value);
        }

        public bool DetectUnchanged
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool PaintTransparent
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public Color ChromaKey
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public string ExtraParametersGif
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Gif > Save options.
        public bool PickLocation
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool OverwriteOnSave
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SaveAsProjectToo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool UploadFile
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public UploadService LatestUploadService
        {
            get => (UploadService)GetValue();
            set => SetValue(value);
        }

        public bool SaveToClipboard
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public CopyType LatestCopyType
        {
            get => (CopyType)GetValue();
            set => SetValue(value);
        }

        public bool ExecuteCustomCommands
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string CustomCommands
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Apng.
        public bool DetectUnchangedApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool PaintTransparentApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool LoopedApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int RepeatCountApng
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool RepeatForeverApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string LatestApngOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestApngFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestApngExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Apng > Save options.
        public bool PickLocationApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool OverwriteOnSaveApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SaveAsProjectTooApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SaveToClipboardApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public CopyType LatestCopyTypeApng
        {
            get => (CopyType)GetValue();
            set => SetValue(value);
        }

        public bool ExecuteCustomCommandsApng
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string CustomCommandsApng
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Video.
        public int AviQuality
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool FlipVideo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int OutputFramerate
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public string ExtraParameters
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestVideoOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestVideoFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestVideoExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Video > Save options.
        public bool PickLocationVideo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool OverwriteOnSaveVideo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SaveAsProjectTooVideo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SaveToClipboardVideo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public CopyType LatestCopyTypeVideo
        {
            get => (CopyType)GetValue();
            set => SetValue(value);
        }

        public bool ExecuteCustomCommandsVideo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string CustomCommandsVideo
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Project.
        public string LatestProjectOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestProjectFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestProjectExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Project > Save options.
        public bool OverwriteOnSaveProject
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SaveToClipboardProject
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public CopyType LatestCopyTypeProject
        {
            get => (CopyType)GetValue();
            set => SetValue(value);
        }

        //Images.
        public bool ZipImages
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string LatestImageOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestImageFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestImageExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Images > Save options.
        public bool OverwriteOnSaveImages
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        //Photoshop.
        public string LatestPhotoshopOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestPhotoshopFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestPhotoshopExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Caption

        public string CaptionText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public FontFamily CaptionFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle CaptionFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight CaptionFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double CaptionFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color CaptionFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double CaptionOutlineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color CaptionOutlineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment CaptionVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment CaptionHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public double CaptionMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Free Text

        public string FreeTextText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public FontFamily FreeTextFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle FreeTextFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight FreeTextFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double FreeTextFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color FreeTextFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region New Animation

        public int NewAnimationWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int NewAnimationHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color NewAnimationColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Title Frame

        public string TitleFrameText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public int TitleFrameDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public FontFamily TitleFrameFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle TitleFrameFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight TitleFrameFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double TitleFrameFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color TitleFrameFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment TitleFrameVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment TitleFrameHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public Color TitleFrameBackgroundColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double TitleFrameMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Key Strokes

        public bool IsKeyStrokesKeysExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesIgnoreNonModifiers
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesEarlier
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesEarlierBy
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public string KeyStrokesSeparator
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesExtended
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesDelay
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public bool IsKeyStrokesFontExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public FontFamily KeyStrokesFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle KeyStrokesFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight KeyStrokesFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool IsKeyStrokesOutlineExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesOutlineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesOutlineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesBackgroundColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool IsKeyStrokesLayoutExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment KeyStrokesVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment KeyStrokesHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesPadding
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Watermark

        public string WatermarkFilePath
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public double WatermarkOpacity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Border

        public Color BorderColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double BorderLeftThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderTopThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderRightThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderBottomThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Obfuscate

        public int PixelSize
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool UseMedian
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Free Drawing

        public int FreeDrawingPenWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingPenHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color FreeDrawingColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public StylusTip FreeDrawingStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool FreeDrawingIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool FreeDrawingFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip FreeDrawingEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Reduce 

        public int ReduceFactor
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int ReduceCount
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Remove Duplicates

        public double DuplicatesSimilarity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public DuplicatesRemovalType DuplicatesRemoval
        {
            get => (DuplicatesRemovalType)GetValue();
            set => SetValue(value);
        }

        public DuplicatesDelayType DuplicatesDelay
        {
            get => (DuplicatesDelayType)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Delay

        public int OverrideDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int IncrementDecrementDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Cinemagraph

        public Color CinemagraphColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip CinemagraphEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool CinemagraphIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool CinemagraphFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphPenWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphPenHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip CinemagraphStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Progress

        public Color ProgressColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color ProgressFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public FontFamily ProgressFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle ProgressFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight ProgressFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double ProgressFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment ProgressVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment ProgressHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public Orientation ProgressOrientation
        {
            get => (Orientation)GetValue();
            set => SetValue(value);
        }

        public int ProgressPrecision
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public string ProgressFormat
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public double ProgressThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public bool ProgressShowTotal
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public ProgressType ProgressType
        {
            get => (ProgressType)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Transitions

        public FadeToType FadeToType
        {
            get => (FadeToType)GetValue();
            set => SetValue(value);
        }

        public Color FadeToColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int FadeTransitionLength
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FadeTransitionDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int SlideTransitionLength
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int SlideTransitionDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #endregion

        public string Version
        {
            get
            {
                var version = Assembly.GetEntryAssembly().GetName().Version;
                var result = $"{version.Major}.{version.Minor}";

                if (version.Build > 0)
                    result += $".{version.Build}";

                if (version.Revision > 0)
                    result += $".{version.Revision}";

                return result;
            }
        }

        #endregion


        #region Obsolete

        [Obsolete]
        public bool FullScreenMode
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        [Obsolete]
        public string ExtraParametersGifski
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        [Obsolete]
        public int LatestUploadIndex
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion
    }
}