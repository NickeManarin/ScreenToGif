using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace ScreenToGif.Util
{
    public class Settings2
    {
        private static readonly ResourceDictionary Local;
        private static readonly ResourceDictionary AppData;
        private static readonly ResourceDictionary Default;

        static Settings2()
        {
            //Check current folder. (get default if key not available)
            var local = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (File.Exists(local))
            {
                Local = new ResourceDictionary { Source = new System.Uri(local, System.UriKind.RelativeOrAbsolute) };
                Application.Current.Resources.MergedDictionaries.Add(Local);
            }

            //Check Appdata. (get default if key not available)
            var appData = Path.Combine(Path.Combine(Path.GetTempPath(), "ScreenToGif"), "Settings.xaml");

            if (File.Exists(appData))
            {
                AppData = new ResourceDictionary { Source = new System.Uri(appData, System.UriKind.RelativeOrAbsolute) };
                Application.Current.Resources.MergedDictionaries.Add(AppData);
            }

            Default = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.EndsWith("/Settings.xaml"));
        }

        public static void Save()
        {
            var settings = new System.Xml.XmlWriterSettings { Indent = true };

            var filename = Local != null ? Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml") : Path.Combine(Path.Combine(Path.GetTempPath(), "ScreenToGif"), "Settings.xaml");

            var writer = System.Xml.XmlWriter.Create(filename, settings);

            if (Local != null)
            {
                foreach (var key in Default.Keys)
                {
                    if (Local.Contains(key))
                        Local[key] = Application.Current.Resources[key];
                    else
                        Local.Add(key, Application.Current.Resources[key]);
                }
            }

            if (AppData != null)
            {
                foreach (var key in Default.Keys)
                {
                    if (AppData.Contains(key))
                        AppData[key] = Application.Current.Resources[key];
                    else
                        AppData.Add(key, Application.Current.Resources[key]);
                }
            }

            System.Windows.Markup.XamlWriter.Save(Local ?? AppData ?? Default, writer);
        }

        private static object GetValue(object defaultValue, [CallerMemberName] string key = "")
        {
            if (Application.Current == null || Application.Current.Resources == null)
                return defaultValue;

            if (Application.Current.Resources.Contains(key))
                return Application.Current.FindResource(key);

            return defaultValue;
        }

        private static void SetValue(object value, [CallerMemberName] string key = "")
        {
            if (Local != null)
            {
                if (Local.Contains(key))
                    Local[key] = value;
                else
                    Local.Add(key, value);
            }

            if (AppData != null)
            {
                if (AppData.Contains(key))
                    AppData[key] = value;
                else
                    AppData.Add(key, value);
            }

            if (Application.Current.Resources.Contains(key))
                Application.Current.Resources.Add(key, value);
        }


        public static bool FullScreenMode
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }

        public static bool UsePreStart
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }

        public static bool ShowCursor
        {
            get { return (bool)GetValue(true); }
            set { SetValue(value); }
        }

        public static bool SnapshotMode
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }

        public static int StartUp
        {
            get { return (int)GetValue(0); }
            set { SetValue(value); }
        }

        public static bool DetectMouseClicks
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }

        public static string LatestOutputFolder
        {
            get { return (string)GetValue(""); }
            set { SetValue(value); }
        }

        public static string Language
        {
            get { return (string)GetValue("auto"); }
            set { SetValue(value); }
        }

        public static int LatestFps
        {
            get { return (int)GetValue(15); }
            set { SetValue(value); }
        }

        public static double RecorderLeft
        {
            get { return (int)GetValue(-1); }
            set { SetValue(value); }
        }

        public static double RecorderTop
        {
            get { return (int)GetValue(-1); }
            set { SetValue(value); }
        }

        public static int RecorderWidth
        {
            get { return (int)GetValue(518); }
            set { SetValue(value); }
        }

        public static int RecorderHeight
        {
            get { return (int)GetValue(269); }
            set { SetValue(value); }
        }

        public static Color ChromaKey
        {
            get { return (Color)GetValue(Color.FromRgb(50, 205, 50)); }
            set { SetValue(value); }
        }

        public static bool DetectUnchanged
        {
            get { return (bool)GetValue(true); }
            set { SetValue(value); }
        }

        public static bool PaintTransparent
        {
            get { return (bool)GetValue(true); }
            set { SetValue(value); }
        }

        public static bool Looped
        {
            get { return (bool)GetValue(true); }
            set { SetValue(value); }
        }

        public static int RepeatCount
        {
            get { return (int)GetValue(2); }
            set { SetValue(value); }
        }

        public static bool RepeatForever
        {
            get { return (bool)GetValue(true); }
            set { SetValue(value); }
        }

        public static int Quality
        {
            get { return (int)GetValue(10); }
            set { SetValue(value); }
        }

        public static System.Windows.Forms.Keys StartPauseKey
        {
            get { return (System.Windows.Forms.Keys)GetValue(System.Windows.Forms.Keys.F7); }
            set { SetValue(value); }
        }

        public static System.Windows.Forms.Keys StopKey
        {
            get { return (System.Windows.Forms.Keys)GetValue(System.Windows.Forms.Keys.F8); }
            set { SetValue(value); }
        }

        public static SolidColorBrush GridColor1
        {
            get { return (SolidColorBrush)GetValue(Color.FromRgb(245, 245, 245)); }
            set { SetValue(value); }
        }

        public static SolidColorBrush GridColor2
        {
            get { return (SolidColorBrush)GetValue(Color.FromRgb(240, 240, 240)); }
            set { SetValue(value); }
        }

        public static Rect GridSize
        {
            get { return (Rect)GetValue(new Rect(0, 0, 20, 20)); }
            set { SetValue(value); }
        }

        public static bool FixedFrameRate
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }

        public static int SnapshotDefaultDelay
        {
            get { return (int)GetValue(1000); }
            set { SetValue(value); }
        }

        public static double EditorTop
        {
            get { return (double)GetValue(-1); }
            set { SetValue(value); }
        }

        public static double EditorLeft
        {
            get { return (double)GetValue(-1); }
            set { SetValue(value); }
        }

        public static double EditorHeight
        {
            get { return (double)GetValue(-1); }
            set { SetValue(value); }
        }

        public static double EditorWidth
        {
            get { return (double)GetValue(-1); }
            set { SetValue(value); }
        }

        public static WindowState EditorWindowState
        {
            get { return (WindowState)GetValue(WindowState.Normal); }
            set { SetValue(value); }
        }

        public static bool EditorExtendChrome
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }


        #region Caption

        public static string CaptionText
        {
            get { return (string)GetValue("Text"); }
            set { SetValue(value); }
        }

        public static FontFamily CaptionFontFamily
        {
            get { return (FontFamily)GetValue("Segoe UI"); }
            set { SetValue(value); }
        }

        public static FontStyle CaptionFontStyle
        {
            get { return (FontStyle)GetValue("Normal"); }
            set { SetValue(value); }
        }

        public static FontWeight CaptionFontWeight
        {
            get { return (FontWeight)GetValue("Bold"); }
            set { SetValue(value); }
        }

        public static double CaptionFontSize
        {
            get { return (double)GetValue(30d); }
            set { SetValue(value); }
        }

        public static SolidColorBrush CaptionFontColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.White); }
            set { SetValue(value); }
        }

        public static double CaptionOutlineThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        public static SolidColorBrush CaptionOutlineColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public static VerticalAlignment CaptionVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(VerticalAlignment.Bottom); }
            set { SetValue(value); }
        }

        public static HorizontalAlignment CaptionHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignment.Center); }
            set { SetValue(value); }
        }

        public static double CaptionMargin
        {
            get { return (double)GetValue(new Thickness(10d)); }
            set { SetValue(value); }
        }

        #endregion

        #region Free Text

        public static string FreeTextText
        {
            get { return (string)GetValue("Text"); }
            set { SetValue(value); }
        }

        public static FontFamily FreeTextFontFamily
        {
            get { return (FontFamily)GetValue("Segoe UI"); }
            set { SetValue(value); }
        }

        public static FontStyle FreeTextFontStyle
        {
            get { return (FontStyle)GetValue("Normal"); }
            set { SetValue(value); }
        }

        public static FontWeight FreeTextFontWeight
        {
            get { return (FontWeight)GetValue("Bold"); }
            set { SetValue(value); }
        }

        public static double FreeTextFontSize
        {
            get { return (double)GetValue(14d); }
            set { SetValue(value); }
        }

        public static SolidColorBrush FreeTextFontColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        #endregion

        #region New Image

        public static int NewImageWidth
        {
            get { return (int)GetValue(500); }
            set { SetValue(value); }
        }

        public static int NewImageHeight
        {
            get { return (int)GetValue(200); }
            set { SetValue(value); }
        }

        public static SolidColorBrush NewImageColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.White); }
            set { SetValue(value); }
        }

        #endregion

        #region Title Frame

        public static string TitleFrameText
        {
            get { return (string)GetValue("Title"); }
            set { SetValue(value); }
        }

        public static FontFamily TitleFrameFontFamily
        {
            get { return (FontFamily)GetValue("Segoe UI"); }
            set { SetValue(value); }
        }

        public static FontStyle TitleFrameFontStyle
        {
            get { return (FontStyle)GetValue("Normal"); }
            set { SetValue(value); }
        }

        public static FontWeight TitleFrameFontWeight
        {
            get { return (FontWeight)GetValue("Bold"); }
            set { SetValue(value); }
        }

        public static double TitleFrameFontSize
        {
            get { return (double)GetValue(20d); }
            set { SetValue(value); }
        }

        public static SolidColorBrush TitleFrameFontColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public static VerticalAlignment TitleFrameVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(VerticalAlignment.Center); }
            set { SetValue(value); }
        }

        public static HorizontalAlignment TitleFrameHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignment.Center); }
            set { SetValue(value); }
        }

        public static SolidColorBrush TitleFrameBackgroundColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public static double TitleFrameMargin
        {
            get { return (double)GetValue(new Thickness(0d)); }
            set { SetValue(value); }
        }

        #endregion

        #region Watermark

        public static string WatermarkFilePath
        {
            get { return (string)GetValue(""); }
            set { SetValue(value); }
        }

        public static double WatermarkOpacity
        {
            get { return (double)GetValue(0.7); }
            set { SetValue(value); }
        }

        public static double WatermarkSize
        {
            get { return (double)GetValue(1); }
            set { SetValue(value); }
        }

        public static VerticalAlignment WatermarkVerticalAligment
        {
            get { return (VerticalAlignment)GetValue(VerticalAlignment.Bottom); }
            set { SetValue(value); }
        }

        public static HorizontalAlignment WatermarkHorizontalAligment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignment.Right); }
            set { SetValue(value); }
        }

        public static double WatermarkMargin
        {
            get { return (double)GetValue(new Thickness(10d)); }
            set { SetValue(value); }
        }

        #endregion

        #region Border

        public static SolidColorBrush BorderColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public static double BorderLeftThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        public static double BorderTopThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        public static double BorderRightThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        public static double BorderBottomThickness
        {
            get { return (double)GetValue(1d); }
            set { SetValue(value); }
        }

        #endregion

        #region Free Drawing

        public static int FreeDrawingPenWidth
        {
            get { return (int)GetValue(5); }
            set { SetValue(value); }
        }

        public static int FreeDrawingPenHeight
        {
            get { return (int)GetValue(5); }
            set { SetValue(value); }
        }

        public static SolidColorBrush FreeDrawingColor
        {
            get { return (SolidColorBrush)GetValue(Brushes.Black); }
            set { SetValue(value); }
        }

        public static StylusTip FreeDrawingStylusTip
        {
            get { return (StylusTip)GetValue(StylusTip.Ellipse); }
            set { SetValue(value); }
        }

        public static bool FreeDrawingIsHighlighter
        {
            get { return (bool)GetValue(false); }
            set { SetValue(value); }
        }

        public static bool FreeDrawingFitToCurve
        {
            get { return (bool)GetValue(StylusTip.Ellipse); }
            set { SetValue(value); }
        }

        public static int FreeDrawingEraserWidth
        {
            get { return (int)GetValue(10d); }
            set { SetValue(value); }
        }

        public static int FreeDrawingEraserHeight
        {
            get { return (int)GetValue(10d); }
            set { SetValue(value); }
        }

        public static StylusTip FreeDrawingEraserStylusTip
        {
            get { return (StylusTip)GetValue(StylusTip.Rectangle); }
            set { SetValue(value); }
        }

        #endregion

        #region Delay

        public static int PlaybackReplaceDelay
        {
            get { return (int)GetValue(66); }
            set { SetValue(value); }
        }

        public static int PlaybackIncrementDecrementDelay
        {
            get { return (int)GetValue(10); }
            set { SetValue(value); }
        }

        #endregion

        #region Cinemagraph



        #endregion









        public static SolidColorBrush StartupColor
        {
            get { return (SolidColorBrush)GetValue(new SolidColorBrush(Colors.DarkCyan)); }
            set { SetValue(value); }
        }
    }
}
