using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace ScreenToGif.Util
{
    internal sealed class UserSettings : INotifyPropertyChanged
    {
        #region Variables

        private static ResourceDictionary _local;
        private static ResourceDictionary _appData;
        private static readonly ResourceDictionary Default;

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

        public static void Save()
        {
            //Only writes if there's something changed. Should not write the default dictionary.
            if (_local == null && _appData == null)
                return;

            //Filename: Local or AppData.
            var filename = _local != null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml") :
                Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            #region Create folder

            var folder = Path.GetDirectoryName(filename);

            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            #endregion

            var settings = new XmlWriterSettings { Indent = true };

            using (var writer = XmlWriter.Create(filename, settings))
                XamlWriter.Save(_local ?? _appData, writer);

            #region Old

            //if (Local != null)
            //{
            //    foreach (var key in Default.Keys)
            //    {
            //        if (Local.Contains(key))
            //            Local[key] = Application.Current.Resources[key]; //Does not make sense here, I already do this when SetValue.
            //        else
            //            Local.Add(key, Application.Current.Resources[key]); //Will load all settings.
            //    }
            //}

            //if (AppData != null)
            //{
            //    foreach (var key in Default.Keys)
            //    {
            //        if (AppData.Contains(key))
            //            AppData[key] = Application.Current.Resources[key];
            //        else
            //            AppData.Add(key, Application.Current.Resources[key]);
            //    }
            //}

            #endregion
        }

        private static object GetValue([CallerMemberName] string key = "")
        {
            if (Application.Current == null || Application.Current.Resources == null)
                return Default[key];

            if (Application.Current.Resources.Contains(key))
                return Application.Current.FindResource(key);

            return Default[key];
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

        private static ResourceDictionary LoadOrDefault(string path)
        {
            ResourceDictionary resource = null;

            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    try
                    {
                        //Read in ResourceDictionary File
                        resource = (ResourceDictionary)XamlReader.Load(fs);
                    }
                    catch (Exception)
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
            var appData = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            if (File.Exists(appData))
                File.Delete(appData);

            _appData = null; //TODO: Should I remove from the merged dictionaries?
        }

        #region Property Changed

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Properties

        public bool FullScreenMode
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool AsyncRecording
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool UsePreStart
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool ShowCursor
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool SnapshotMode
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public int StartUp
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public bool DetectMouseClicks
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public Color ClickColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public string LanguageCode
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public int LatestFps
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public double RecorderLeft
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double RecorderTop
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public int RecorderWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int RecorderHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public System.Windows.Forms.Keys StartPauseKey
        {
            get { return (System.Windows.Forms.Keys)GetValue(); }
            set { SetValue(value); }
        }

        public System.Windows.Forms.Keys StopKey
        {
            get { return (System.Windows.Forms.Keys)GetValue(); }
            set { SetValue(value); }
        }

        public bool CheckForUpdates
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public Color GridColor1
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Color GridColor2
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Color RecorderBackground
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Color RecorderForeground
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Rect GridSize
        {
            get { return (Rect)GetValue(); }
            set { SetValue(value); }
        }

        public bool FixedFrameRate
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public int SnapshotDefaultDelay
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public double EditorTop
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double EditorLeft
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double EditorHeight
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double EditorWidth
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public WindowState EditorWindowState
        {
            get { return (WindowState)GetValue(); }
            set { SetValue(value); }
        }

        public Color InsertFillColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public int LatestFpsImport
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        #region Options

        public Color BoardGridBackground
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Color BoardGridColor1
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Color BoardGridColor2
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Rect BoardGridSize
        {
            get { return (Rect)GetValue(); }
            set { SetValue(value); }
        }

        public bool EditorExtendChrome
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool RecorderThinMode
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public string LogsFolder
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public string TemporaryFolder
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public string FfmpegLocation
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Board

        public int BoardWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int BoardHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public Color BoardColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public int BoardStylusHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int BoardStylusWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public StylusTip BoardStylusTip
        {
            get { return (StylusTip)GetValue(); }
            set { SetValue(value); }
        }

        public bool BoardFitToCurve
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool BoardIsHighlighter
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public int BoardEraserHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int BoardEraserWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public StylusTip BoardEraserStylusTip
        {
            get { return (StylusTip)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Editor

        public PasteBehavior PasteBehavior
        {
            get { return (PasteBehavior)GetValue(); }
            set { SetValue(value); }
        }

        #region Save As

        public Export SaveType
        {
            get { return (Export)GetValue(); }
            set { SetValue(value); }
        }

        public GifEncoderType GifEncoder
        {
            get { return (GifEncoderType)GetValue(); }
            set { SetValue(value); }
        }

        public VideoEncoderType VideoEncoder
        {
            get { return (VideoEncoderType)GetValue(); }
            set { SetValue(value); }
        }

        public int AviQuality
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public Color ChromaKey
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public bool DetectUnchanged
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool PaintTransparent
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool Looped
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public int RepeatCount
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public bool RepeatForever
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public int Quality
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int MaximumColors
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int OutputFramerate
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public string ExtraParameters
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public string LatestOutputFolder
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public string LatestFilename
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public bool OverwriteOnSave
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Caption

        public string CaptionText
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public FontFamily CaptionFontFamily
        {
            get { return (FontFamily)GetValue(); }
            set { SetValue(value); }
        }

        public FontStyle CaptionFontStyle
        {
            get { return (FontStyle)GetValue(); }
            set { SetValue(value); }
        }

        public FontWeight CaptionFontWeight
        {
            get { return (FontWeight)GetValue(); }
            set { SetValue(value); }
        }

        public double CaptionFontSize
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public Color CaptionFontColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public double CaptionOutlineThickness
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public Color CaptionOutlineColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public VerticalAlignment CaptionVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(); }
            set { SetValue(value); }
        }

        public HorizontalAlignment CaptionHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(); }
            set { SetValue(value); }
        }

        public double CaptionMargin
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Free Text

        public string FreeTextText
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public FontFamily FreeTextFontFamily
        {
            get { return (FontFamily)GetValue(); }
            set { SetValue(value); }
        }

        public FontStyle FreeTextFontStyle
        {
            get { return (FontStyle)GetValue(); }
            set { SetValue(value); }
        }

        public FontWeight FreeTextFontWeight
        {
            get { return (FontWeight)GetValue(); }
            set { SetValue(value); }
        }

        public double FreeTextFontSize
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public Color FreeTextFontColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region New Animation

        public int NewAnimationWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int NewAnimationHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public Color NewAnimationColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Title Frame

        public string TitleFrameText
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public int TitleFrameDelay
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public FontFamily TitleFrameFontFamily
        {
            get { return (FontFamily)GetValue(); }
            set { SetValue(value); }
        }

        public FontStyle TitleFrameFontStyle
        {
            get { return (FontStyle)GetValue(); }
            set { SetValue(value); }
        }

        public FontWeight TitleFrameFontWeight
        {
            get { return (FontWeight)GetValue(); }
            set { SetValue(value); }
        }

        public double TitleFrameFontSize
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public Color TitleFrameFontColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public VerticalAlignment TitleFrameVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(); }
            set { SetValue(value); }
        }

        public HorizontalAlignment TitleFrameHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(); }
            set { SetValue(value); }
        }

        public Color TitleFrameBackgroundColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public double TitleFrameMargin
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Watermark

        public string WatermarkFilePath
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public double WatermarkOpacity
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double WatermarkSize
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double WatermarkTop
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double WatermarkLeft
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Border

        public Color BorderColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public double BorderLeftThickness
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double BorderTopThickness
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double BorderRightThickness
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public double BorderBottomThickness
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Free Drawing

        public int FreeDrawingPenWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int FreeDrawingPenHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public Color FreeDrawingColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public StylusTip FreeDrawingStylusTip
        {
            get { return (StylusTip)GetValue(); }
            set { SetValue(value); }
        }

        public bool FreeDrawingIsHighlighter
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool FreeDrawingFitToCurve
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public int FreeDrawingEraserWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int FreeDrawingEraserHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public StylusTip FreeDrawingEraserStylusTip
        {
            get { return (StylusTip)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Delay

        public int OverrideDelay
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int IncrementDecrementDelay
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Cinemagraph

        public Color CinemagraphColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public int CinemagraphEraserWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int CinemagraphEraserHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public StylusTip CinemagraphEraserStylusTip
        {
            get { return (StylusTip)GetValue(); }
            set { SetValue(value); }
        }

        public bool CinemagraphIsHighlighter
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public bool CinemagraphFitToCurve
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public int CinemagraphPenWidth
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public int CinemagraphPenHeight
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public StylusTip CinemagraphStylusTip
        {
            get { return (StylusTip)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Progress

        public Color ProgressColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public Color ProgressFontColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
        }

        public FontFamily ProgressFontFamily
        {
            get { return (FontFamily)GetValue(); }
            set { SetValue(value); }
        }

        public FontStyle ProgressFontStyle
        {
            get { return (FontStyle)GetValue(); }
            set { SetValue(value); }
        }

        public FontWeight ProgressFontWeight
        {
            get { return (FontWeight)GetValue(); }
            set { SetValue(value); }
        }

        public double ProgressFontSize
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public VerticalAlignment ProgressVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(); }
            set { SetValue(value); }
        }

        public HorizontalAlignment ProgressHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(); }
            set { SetValue(value); }
        }

        public Orientation ProgressOrientation
        {
            get { return (Orientation)GetValue(); }
            set { SetValue(value); }
        }

        public int ProgressPrecision
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public string ProgressFormat
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public double ProgressThickness
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public bool ProgressShowTotal
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }

        public ProgressType ProgressType
        {
            get { return (ProgressType)GetValue(); }
            set { SetValue(value); }
        }

        #endregion

        #region Transitions

        public FadeToType FadeToType
        {
            get { return (FadeToType)GetValue(); }
            set { SetValue(value); }
        }

        public int FadeFrameCount
        {
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public Color FadeToColor
        {
            get { return (Color)GetValue(); }
            set { SetValue(value); }
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
    }
}
