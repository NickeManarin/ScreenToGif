using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Startup : Window
    {
        private XElement _newRelease;

        public Startup()
        {
            InitializeComponent();
        }

        #region Events

        private void Startup_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Argument.FileNames.Any())
                Editor_Executed(sender, null);

            if (UserSettings.All.CheckForUpdates)
                CheckLatestRelease();
        }

        private void Buttons_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Update_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _newRelease != null;
        }
        
        private void Recorder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var recorder = new Recorder { Owner = this };
            Application.Current.MainWindow = recorder;

            Hide();

            var result = recorder.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // If Close
                Environment.Exit(0);
            }
            else if (result.HasValue)
            {
                #region If Backbutton or Stop Clicked

                if (recorder.ExitArg == ExitAction.Recorded)
                {
                    var editor = new Editor { ListFrames = recorder.ListFrames };

                    GenericShowDialog(editor);
                    return;
                }

                Show();

                #endregion
            }
        }

        private void WebcamRecorder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var webcam = new Webcam { Owner = this };
            Application.Current.MainWindow = webcam;

            Hide();

            var result = webcam.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // If Close
                Environment.Exit(0);
            }
            else if (result.HasValue)
            {
                #region If Backbutton or Stop Clicked

                if (webcam.ExitArg == ExitAction.Recorded)
                {
                    var editor = new Editor { ListFrames = webcam.ListFrames };

                    GenericShowDialog(editor);
                    return;
                }

                Show();

                #endregion
            }
        }

        private void Board_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var board = new Board { Owner = this };
            Application.Current.MainWindow = board;

            Hide();

            var result = board.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // If Close
                Environment.Exit(0);
            }
            else if (result.HasValue)
            {
                #region If Backbutton or Stop Clicked

                if (board.ExitArg == ExitAction.Recorded)
                {
                    var editor = new Editor { ListFrames = board.ListFrames };

                    GenericShowDialog(editor);
                    return;
                }

                Show();

                #endregion
            }
        }

        private void Editor_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var editor = new Editor();
            GenericShowDialog(editor);
        }

        private void Update_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var download = new DownloadDialog
            {
                Element = _newRelease,
                Owner = this
            };

            var result = download.ShowDialog();

            if (result.HasValue && result.Value)
            {
                if (Dialog.Ask("Screen To Gif", "Do you want to close this app?", "This is the old release, you downloaded the new version already."))
                    Environment.Exit(25);
            }
        }
        
        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var options = new Options { Owner = this };
            options.ShowDialog();
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var test = new TestField();
            test.ShowDialog();
        }

        #endregion

        #region Methods

        private void GenericShowDialog(Window window)
        {
            Hide();

            window.Owner = this;
            Application.Current.MainWindow = window;

            window.ShowDialog();

            Close();
        }

        private async void CheckLatestRelease()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/NickeManarin/ScreenToGif/releases/latest");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";

                var response = (HttpWebResponse) await request.GetResponseAsync();

                using (var resultStream = response.GetResponseStream())
                {
                    if (resultStream == null)
                        return;

                    using (var reader = new StreamReader(resultStream))
                    {
                        var result = reader.ReadToEnd();

                        var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new System.Xml.XmlDictionaryReaderQuotas());

                        var release = await Task<XElement>.Factory.StartNew(() => XElement.Load(jsonReader));

                        var versionSplit = release.XPathSelectElement("tag_name").Value.Split('.');
                        var major = Convert.ToInt32(versionSplit[0]);
                        var minor = Convert.ToInt32(versionSplit[1]);
                        var build = versionSplit.Length > 2 ? Convert.ToInt32(versionSplit[2]) : 0;

                        var current = Assembly.GetExecutingAssembly().GetName().Version;
                        var internet = new Version(major, minor, build);

                        if (current > internet)
                        {
                            UpdateTextBlock.Visibility = Visibility.Collapsed;
                            return;
                        }

                        UpdateRun.Text = string.Format(FindResource("NewRelease") as string ?? "New release available • {0}", release.XPathSelectElement("tag_name").Value);
                        UpdateTextBlock.Visibility = Visibility.Visible;

                        _newRelease = release;

                        CommandManager.InvalidateRequerySuggested();
                    }
                }
            }
            catch (Exception)
            {
                UpdateTextBlock.Visibility = Visibility.Collapsed;
            }

            GC.Collect();
        }

        #endregion
    }
}
