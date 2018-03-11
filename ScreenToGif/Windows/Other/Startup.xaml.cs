using System;
using System.ComponentModel;
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

            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning())
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion
        }

        #region Events

        private void Startup_Loaded(object sender, RoutedEventArgs e)
        {
            if (UserSettings.All.CheckForUpdates)
                CheckLatestRelease();
        }

        private void Update_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _newRelease != null;
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
                if (Dialog.Ask("ScreenToGif", FindResource("Update.CloseThis").ToString(), FindResource("Update.CloseThis.Detail").ToString()))
                    Environment.Exit(25);
            }
        }

        private void Startup_Closing(object sender, CancelEventArgs e)
        {
            //Manually get the position/size of the window, so it's possible opening multiple instances.
            UserSettings.All.StartupTop = Top;
            UserSettings.All.StartupLeft = Left;
            UserSettings.All.StartupWidth = Width;
            UserSettings.All.StartupHeight = Height;
            UserSettings.All.StartupWindowState = WindowState;
            UserSettings.Save();
        }

        #endregion

        #region Methods

        private async void CheckLatestRelease()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/NickeManarin/ScreenToGif/releases/latest");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
                request.Proxy = WebHelper.GetProxy();

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

                        if (current >= internet)
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

        private bool UpdatePositioning()
        {
            var top = UserSettings.All.StartupTop;
            var left = UserSettings.All.StartupLeft;

            //If the position was never set, let it center on screen. 
            if (double.IsNaN(top) && double.IsNaN(left))
                return false;

            //The catch here is to get the closest monitor from current Top/Left point. 
            var monitors = Monitor.AllMonitorsScaled(this.Scale());
            var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new System.Windows.Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary);

            if (closest == null)
                return false;

            //To much to the Left.
            if (closest.WorkingArea.Left > UserSettings.All.StartupLeft + UserSettings.All.StartupWidth - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > UserSettings.All.StartupTop + UserSettings.All.StartupHeight - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < UserSettings.All.StartupLeft + 100)
                left = closest.WorkingArea.Right - UserSettings.All.StartupWidth;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < UserSettings.All.StartupTop + 100)
                top = closest.WorkingArea.Bottom - UserSettings.All.StartupHeight;

            Top = top;
            Left = left;
            Width = UserSettings.All.StartupWidth;
            Height = UserSettings.All.StartupHeight;
            WindowState = UserSettings.All.StartupWindowState;

            return true;
        }

        #endregion
    }
}