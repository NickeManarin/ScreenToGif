using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace ScreenToGif.Util
{
    public class Settings2
    {
        #region Variables

        private static readonly ResourceDictionary Local;
        private static readonly ResourceDictionary AppData;
        private static readonly ResourceDictionary Default;

        public static Settings2 All { get; } = new Settings2();

        #endregion

        static Settings2()
        {
            //Check current folder.
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (File.Exists(local))
            {
                Local = LoadOrDefault(local);
                Application.Current.Resources.MergedDictionaries.Add(Local);
            }

            //Check AppData.
            var appData = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            if (File.Exists(appData))
            {
                AppData = LoadOrDefault(appData);
                Application.Current.Resources.MergedDictionaries.Add(AppData);
            }

            Default = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.EndsWith("/Settings.xaml"));
        }

        public static void Save()
        {
            var filename = Local != null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml") : 
                Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            var settings = new XmlWriterSettings { Indent = true };

            using (var writer = XmlWriter.Create(filename, settings))
                XamlWriter.Save(Local ?? AppData ?? Default, writer);

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

        private static object GetValue(object defaultValue, [CallerMemberName] string key = "")
        {
            if (Application.Current == null || Application.Current.Resources == null)
                return defaultValue;

            if (Application.Current.Resources.Contains(key))
                return Application.Current.FindResource(key);

            return defaultValue;
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
            if (Local != null)
            {
                if (Local.Contains(key))
                    Local[key] = value;
                else
                    Local.Add(key, value);
            }

            //Updates or inserts the value to the AppData resource.
            if (AppData != null)
            {
                if (AppData.Contains(key))
                    AppData[key] = value;
                else
                    AppData.Add(key, value);
            }

            //Updates the current value of the resource already loaded.
            if (Application.Current.Resources.Contains(key))
                Application.Current.Resources[key] = value;
        }

        private static ResourceDictionary LoadOrDefault(string path)
        {
            ResourceDictionary resource = null;

            try
            {
                //Tries to load the resource from disk. 
                resource = new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) };
            }
            finally
            {
                //Sets a default value if null.
                resource = resource ?? new ResourceDictionary();
            }

            return resource;
        }

        #region Properties

        public bool FullScreenMode
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

        public string LatestOutputFolder
        {
            get { return (string)GetValue(); }
            set { SetValue(value); }
        }

        public string Language
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
            get { return (int)GetValue(); }
            set { SetValue(value); }
        }

        public double RecorderTop
        {
            get { return (int)GetValue(); }
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

        public SolidColorBrush GridColor1
        {
            get { return (SolidColorBrush)GetValue(); }
            set { SetValue(value); }
        }

        public SolidColorBrush GridColor2
        {
            get { return (SolidColorBrush)GetValue(); }
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

        public bool EditorExtendChrome
        {
            get { return (bool)GetValue(); }
            set { SetValue(value); }
        }


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

        public SolidColorBrush CaptionFontColor
        {
            get { return (SolidColorBrush)GetValue(); }
            set { SetValue(value); }
        }

        public double CaptionOutlineThickness
        {
            get { return (double)GetValue(); }
            set { SetValue(value); }
        }

        public SolidColorBrush CaptionOutlineColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public VerticalAlignment CaptionVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(VerticalAlignment.Bottom); }
            set { SetValue(value); }
        }

        public HorizontalAlignment CaptionHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignment.Center); }
            set { SetValue(value); }
        }

        public double CaptionMargin
        {
            get { return (double)GetValue(new Thickness(10d)); }
            set { SetValue(value); }
        }

        #endregion

        #region Free Text

        public string FreeTextText
        {
            get { return (string)GetValue("Text"); }
            set { SetValue(value); }
        }

        public FontFamily FreeTextFontFamily
        {
            get { return (FontFamily)GetValue("Segoe UI"); }
            set { SetValue(value); }
        }

        public FontStyle FreeTextFontStyle
        {
            get { return (FontStyle)GetValue("Normal"); }
            set { SetValue(value); }
        }

        public FontWeight FreeTextFontWeight
        {
            get { return (FontWeight)GetValue("Bold"); }
            set { SetValue(value); }
        }

        public double FreeTextFontSize
        {
            get { return (double)GetValue(14d); }
            set { SetValue(value); }
        }

        public SolidColorBrush FreeTextFontColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        #endregion

        #region New Image

        public int NewImageWidth
        {
            get { return (int)GetValue(500); }
            set { SetValue(value); }
        }

        public int NewImageHeight
        {
            get { return (int)GetValue(200); }
            set { SetValue(value); }
        }

        public SolidColorBrush NewImageColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.White); }
            set { SetValue(value); }
        }

        #endregion

        #region Title Frame

        public string TitleFrameText
        {
            get { return (string)GetValue("Title"); }
            set { SetValue(value); }
        }

        public FontFamily TitleFrameFontFamily
        {
            get { return (FontFamily)GetValue("Segoe UI"); }
            set { SetValue(value); }
        }

        public FontStyle TitleFrameFontStyle
        {
            get { return (FontStyle)GetValue("Normal"); }
            set { SetValue(value); }
        }

        public FontWeight TitleFrameFontWeight
        {
            get { return (FontWeight)GetValue("Bold"); }
            set { SetValue(value); }
        }

        public double TitleFrameFontSize
        {
            get { return (double)GetValue(20d); }
            set { SetValue(value); }
        }

        public SolidColorBrush TitleFrameFontColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public VerticalAlignment TitleFrameVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(VerticalAlignment.Center); }
            set { SetValue(value); }
        }

        public HorizontalAlignment TitleFrameHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignment.Center); }
            set { SetValue(value); }
        }

        public SolidColorBrush TitleFrameBackgroundColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public double TitleFrameMargin
        {
            get { return (double)GetValue(new Thickness(0d)); }
            set { SetValue(value); }
        }

        #endregion

        #region Watermark

        public string WatermarkFilePath
        {
            get { return (string)GetValue(""); }
            set { SetValue(value); }
        }

        public double WatermarkOpacity
        {
            get { return (double)GetValue(0.7); }
            set { SetValue(value); }
        }

        public double WatermarkSize
        {
            get { return (double)GetValue(1); }
            set { SetValue(value); }
        }

        public VerticalAlignment WatermarkVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(VerticalAlignment.Bottom); }
            set { SetValue(value); }
        }

        public HorizontalAlignment WatermarkHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignment.Right); }
            set { SetValue(value); }
        }

        public double WatermarkMargin
        {
            get { return (double)GetValue(new Thickness(10d)); }
            set { SetValue(value); }
        }

        #endregion

        #region Border

        public SolidColorBrush BorderColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public double BorderLeftThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        public double BorderTopThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        public double BorderRightThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        public double BorderBottomThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        #endregion

        #region Free Drawing

        public int FreeDrawingPenWidth
        {
            get { return (int)GetValue(5); }
            set { SetValue(value); }
        }

        public int FreeDrawingPenHeight
        {
            get { return (int)GetValue(5); }
            set { SetValue(value); }
        }

        public SolidColorBrush FreeDrawingColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public StylusTip FreeDrawingStylusTip
        {
            get { return (StylusTip)GetValue(StylusTip.Ellipse); }
            set { SetValue(value); }
        }

        public bool FreeDrawingIsHighlighter
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }

        public bool FreeDrawingFitToCurve
        {
            get { return (bool)GetValue(StylusTip.Ellipse); }
            set { SetValue(value); }
        }

        public int FreeDrawingEraserWidth
        {
            get { return (int)GetValue(10d); }
            set { SetValue(value); }
        }

        public int FreeDrawingEraserHeight
        {
            get { return (int)GetValue(10d); }
            set { SetValue(value); }
        }

        public StylusTip FreeDrawingEraserStylusTip
        {
            get { return (StylusTip)GetValue(StylusTip.Rectangle); }
            set { SetValue(value); }
        }

        #endregion

        #region Delay

        public int PlaybackReplaceDelay
        {
            get { return (int)GetValue(66); }
            set { SetValue(value); }
        }

        public int PlaybackIncrementDecrementDelay
        {
            get { return (int)GetValue(10); }
            set { SetValue(value); }
        }

        #endregion

        #region Cinemagraph



        #endregion









        public SolidColorBrush StartupColor
        {
            get { return (SolidColorBrush)GetValue(new SolidColorBrush(Colors.DarkCyan)); }
            set { SetValue(value); }
        }

        #endregion
    }
}
