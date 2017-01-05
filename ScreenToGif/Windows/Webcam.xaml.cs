using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Webcam.DirectX;
using ScreenToGif.Windows.Other;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Webcam.xaml
    /// </summary>
    public partial class Webcam
    {
        #region Variables

        //private CaptureWebcam _capture = null;
        private Filters _filters;

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        #region Flags

        public static readonly DependencyProperty StageProperty = DependencyProperty.Register("Stage", typeof(Stage), typeof(Webcam), new FrameworkPropertyMetadata(Stage.Stopped));

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage
        {
            get { return (Stage)GetValue(StageProperty); }
            set { SetValue(StageProperty, value); }
        }

        /// <summary>
        /// The action to be executed after closing this Window.
        /// </summary>
        public ExitAction ExitArg = ExitAction.Return;

        #endregion

        #region Counters

        /// <summary>
        /// The numbers of frames, this is updated while recording.
        /// </summary>
        private int _frameCount = 0;

        #endregion

        /// <summary>
        /// Lists of cursors.
        /// </summary>
        public List<FrameInfo> ListFrames = new List<FrameInfo>();

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private readonly string _pathTemp;

        private Timer _timer = new Timer();

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _scale = 1;

        /// <summary>
        /// The amount of pixels of the window border. Width.
        /// </summary>
        private int _offsetX;

        /// <summary>
        /// The amout of pixels of the window border. Height.
        /// </summary>
        private int _offsetY;

        /// <summary>
        /// True if the BackButton should be hidden.
        /// </summary>
        private readonly bool _hideBackButton;

        #endregion

        #region Async Load

        public delegate List<string> Load();

        private Load _loadDel;

        /// <summary>
        /// Loads the list of video devices.
        /// </summary>
        private List<string> LoadVideoDevices()
        {
            var devicesList = new List<string>();
            _filters = new Filters();

            for (var i = 0; i < _filters.VideoInputDevices.Count; i++)
            {
                devicesList.Add(_filters.VideoInputDevices[i].Name);
            }

            return devicesList;
        }

        private void LoadCallBack(IAsyncResult r)
        {
            var result = _loadDel.EndInvoke(r);

            #region If no devices detected

            if (result.Count == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    RecordPauseButton.IsEnabled = false;
                    FpsNumericUpDown.IsEnabled = false;
                    VideoDevicesComboBox.IsEnabled = false;

                    WebcamControl.Visibility = Visibility.Collapsed;
                    NoVideoLabel.Visibility = Visibility.Visible;
                });

                return;
            }

            #endregion

            #region Detected at least one device

            Dispatcher.Invoke(() =>
            {
                VideoDevicesComboBox.ItemsSource = result;
                VideoDevicesComboBox.SelectedIndex = 0;

                RecordPauseButton.IsEnabled = true;
                FpsNumericUpDown.IsEnabled = true;
                VideoDevicesComboBox.IsEnabled = true;

                WebcamControl.Visibility = Visibility.Visible;
                NoVideoLabel.Visibility = Visibility.Collapsed;

                _actHook.Start(false, true); //false for the mouse, true for the keyboard.
            });

            #endregion
        }

        #endregion

        #region Inicialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Webcam(bool hideBackButton = false)
        {
            InitializeComponent();

            _hideBackButton = hideBackButton;

            //Load
            _timer.Tick += Normal_Elapsed;

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook();
                _actHook.KeyDown += KeyHookTarget;
            }
            catch (Exception) { }

            #endregion

            #region Temporary folder

            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
            {
                UserSettings.All.TemporaryFolder = Path.GetTempPath();
            }

            _pathTemp = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")) + "\\";

            #endregion
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hideBackButton)
            {
                BackButton.Visibility = Visibility.Collapsed;
            }

            SystemEvents.PowerModeChanged += System_PowerModeChanged;

            #region DPI

            var source = PresentationSource.FromVisual(Application.Current.MainWindow);

            if (source?.CompositionTarget != null)
                _scale = source.CompositionTarget.TransformToDevice.M11;

            #endregion

            #region Window Offset

            //Gets the window chrome offset
            _offsetX = (int)Math.Round((ActualWidth - ((Grid)Content).ActualWidth) / 2);
            _offsetY = (int)Math.Round((ActualHeight - ((Grid)Content).ActualHeight) - _offsetX);

            #endregion

            _loadDel = LoadVideoDevices;
            _loadDel.BeginInvoke(LoadCallBack, null);
        }

        #endregion

        #region Hooks

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (!IsActive)
                return;

            //TODO: I need a better way of comparing the keys.
            if (e.Key.ToString().Equals(UserSettings.All.StartPauseKey.ToString()))
            {
                RecordPauseButton_Click(null, null);
            }
            else if (e.Key.ToString().Equals(UserSettings.All.StopKey.ToString()))
            {
                Stop_Executed(null, null);
            }
        }

        #endregion

        #region Other Events

        private void VideoDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (VideoDevicesComboBox.SelectedIndex == -1)
                {
                    WebcamControl.VideoDevice = null;
                    return;
                }

                WebcamControl.VideoDevice = _filters.VideoInputDevices[VideoDevicesComboBox.SelectedIndex];
                WebcamControl.Refresh();

                Width = WebcamControl.VideoWidth * _scale / 2;
                Height = (WebcamControl.VideoHeight + 31) * _scale / 2;

                if (Top < 0)
                    Top = 0;

                if (Left < 0)
                    Left = 0;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Video device not supported");
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;

            Width = WebcamControl.VideoWidth * _scale * ScaleSlider.Value;
            Height = (WebcamControl.VideoHeight + 31) * _scale * ScaleSlider.Value;

            if (Top < 0)
                Top = 0;

            if (Left < 0)
                Left = 0;
        }

        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (Stage == Stage.Recording)
                    RecordPauseButton_Click(null, null);
                else if (Stage == Stage.PreStarting)
                    Stop_Executed(null, null);

                GC.Collect();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;
        }

        #endregion

        #region Record Async

        /// <summary>
        /// Saves the Bitmap to the disk and adds the filename in the list of frames.
        /// </summary>
        /// <param name="filename">The final filename of the Bitmap.</param>
        /// <param name="bitmap">The Bitmap to save in the disk.</param>
        public delegate void AddFrame(string filename, Bitmap bitmap);

        private AddFrame _addDel;

        private void AddFrames(string filename, Bitmap bitmap)
        {
            bitmap.Save(filename);
            bitmap.Dispose();
        }

        public delegate void AddRenderFrame(string filename, RenderTargetBitmap bitmap);

        private AddRenderFrame _addRenderDel;

        private void AddRenderFrames(string filename, RenderTargetBitmap bitmap)
        {
            var bitmapEncoder = new BmpBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var filestream = new FileStream(filename, FileMode.Create))
                bitmapEncoder.Save(filestream);
        }

        private void CallBack(IAsyncResult r)
        {
            //if (!this.IsLoaded) return;

            _addDel.EndInvoke(r);
        }

        #endregion

        #region Discard Async

        private delegate void DiscardFrames();

        private DiscardFrames _discardFramesDel;

        private void Discard()
        {
            #region Remove all the files

            foreach (var frame in ListFrames)
            {
                try
                {
                    File.Delete(frame.ImageLocation);
                }
                catch (Exception)
                { }
            }

            try
            {
                Directory.Delete(_pathTemp, true);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Delete Temp Path");
            }

            #endregion

            ListFrames.Clear();
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                //Enables the controls that are disabled while recording;
                FpsNumericUpDown.IsEnabled = true;
                RefreshButton.IsEnabled = true;
                VideoDevicesComboBox.IsEnabled = true;
                LowerGrid.IsEnabled = true;

                DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                Cursor = Cursors.Arrow;
                Title = "Screen To Gif";

                GC.Collect();
            });

            GC.Collect();
        }

        #endregion

        #region Timer

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            string fileName = $"{_pathTemp}{_frameCount}.png";
            ListFrames.Add(new FrameInfo(fileName, _timer.Interval));

            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new System.Drawing.Point((int)Math.Round((Left + _offsetX) * _scale, MidpointRounding.AwayFromZero), 
                (int)Math.Round((Top + _offsetY) * _scale,  MidpointRounding.AwayFromZero)));

            //Take a screenshot of the area.
            var bt = Native.Capture(new System.Windows.Size((int)Math.Round(WebcamControl.ActualWidth * _scale, MidpointRounding.AwayFromZero),
                (int)Math.Round(WebcamControl.ActualHeight * _scale, MidpointRounding.AwayFromZero)), lefttop.X, lefttop.Y);

            _addDel.BeginInvoke(fileName, new Bitmap(bt), null, null); //CallBack
            //_addDel.BeginInvoke(fileName, new Bitmap(WebcamControl.Capture.GetFrame()), CallBack, null);
            //_addRenderDel.BeginInvoke(fileName, WebcamControl.GetRender(this.Dpi(), new System.Windows.Size()), CallBack, null);

            //ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(_capture.GetFrame())); });

            Dispatcher.Invoke(() => Title = $"Screen To Gif • {_frameCount}");

            _frameCount++;
            GC.Collect(1);
        }

        #endregion

        #region Click Events

        private void BackButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ScaleButton_Click(object sender, RoutedEventArgs e)
        {
            ScalePopup.IsOpen = true;
        }

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            Extras.CreateTemp(_pathTemp);

            WebcamControl.Capture.PrepareCapture();

            if (Stage == Stage.Stopped)
            {
                #region To Record

                _timer = new Timer { Interval = 1000 / FpsNumericUpDown.Value };

                ListFrames = new List<FrameInfo>();

                RefreshButton.IsEnabled = false;
                VideoDevicesComboBox.IsEnabled = false;
                FpsNumericUpDown.IsEnabled = false;
                Topmost = true;

                _addDel = AddFrames;
                _addRenderDel = AddRenderFrames;
                //WebcamControl.Capture.GetFrame();

                #region Start - Normal or Snap

                if (!UserSettings.All.SnapshotMode)
                {
                    #region Normal Recording

                    _timer.Tick += Normal_Elapsed;
                    Normal_Elapsed(null, null);
                    _timer.Start();

                    Stage = Stage.Recording;

                    #endregion
                }
                else
                {
                    #region SnapShot Recording

                    Stage = Stage.Snapping;
                    Title = "Screen To Gif - " + FindResource("Recorder.Snapshot");

                    Normal_Elapsed(null, null);

                    #endregion
                }

                #endregion

                #endregion
            }
            else if (Stage == Stage.Recording)
            {
                #region To Pause

                Stage = Stage.Paused;
                Title = FindResource("Recorder.Paused").ToString();

                DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                _timer.Stop();

                #endregion
            }
            else if (Stage == Stage.Paused)
            {
                #region To Record Again

                Stage = Stage.Recording;
                Title = "Screen To Gif";

                _timer.Start();

                #endregion
            }
            else if (Stage == Stage.Snapping)
            {
                #region Take Screenshot

                Normal_Elapsed(null, null);

                #endregion
            }
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _frameCount = 0;
            Stage = Stage.Stopped;

            Cursor = Cursors.AppStarting;
            LowerGrid.IsEnabled = false;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames.Count > 0;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _frameCount = 0;

                _timer.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && ListFrames.Any())
                {
                    #region If not Already Stoped nor Pre Starting and FrameCount > 0, Stops

                    ExitArg = ExitAction.Recorded;
                    DialogResult = false;

                    #endregion
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !ListFrames.Any())
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    Stage = Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    FpsNumericUpDown.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    VideoDevicesComboBox.IsEnabled = true;
                    Topmost = true;

                    Title = "Screen To Gif";

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                LogWriter.Log(nll, "NullPointer in the Stop function");

                ErrorDialog.Ok("ScreenToGif", "Error while stopping", nll.Message, nll);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error in the Stop function");

                ErrorDialog.Ok("ScreenToGif", "Error while stopping", ex.Message, ex);
            }
        }

        private void NotRecording_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting && LowerGrid.IsEnabled;
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;

            var options = new Options();
            options.ShowDialog();

            Topmost = true;
        }

        private void CheckVideoDevices_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RecordPauseButton.IsEnabled = false;

            VideoDevicesComboBox.ItemsSource = null;

            //Check again for video devices.
            _loadDel = LoadVideoDevices;
            _loadDel.BeginInvoke(LoadCallBack, null);
        }

        #endregion
    }
}
