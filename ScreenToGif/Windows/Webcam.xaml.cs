using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.FileWriters;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
using ScreenToGif.Webcam.DirectX;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Webcam.xaml
    /// </summary>
    public partial class Webcam
    {
        #region Variables

        private CaptureWebcam _capture = null;
        private Filters _filters;

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        #region Flags

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage { get; set; }

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
        private readonly string _pathTemp = System.IO.Path.GetTempPath() +
            String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")); //TODO: Change to a more dynamic folder naming.

        private Timer _timer = new Timer();

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

            for (int i = 0; i < _filters.VideoInputDevices.Count; i++)
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

                _actHook.Start(false, true); //false for the mouse, true for the keyboard.
            });

            #endregion
        }

        #endregion

        #region Inicialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Webcam()
        {
            InitializeComponent();

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            //TODO: I need a better way of comparing the keys.
            if (e.Key.ToString().Equals(Settings.Default.StartPauseKey.ToString()))
            {
                RecordPauseButton_Click(null, null);
            }
            else if (e.Key.ToString().Equals(Settings.Default.StopKey.ToString()))
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
                //Get current devices and dispose of capture object,
                //because the video device can only be changed by creating a new Capture object.
                Filter videoDevice = null;

                //To change the video device, a dispose is needed.
                if (_capture != null)
                {
                    _capture.Dispose();
                    _capture = null;
                }

                //Get new video device.
                videoDevice = (VideoDevicesComboBox.SelectedIndex > -1 ? _filters.VideoInputDevices[VideoDevicesComboBox.SelectedIndex] : null);

                //Create capture object.
                if (videoDevice != null)
                {
                    _capture = new CaptureWebcam(videoDevice) { PreviewWindow = this };
                    _capture.StartPreview();

                    //TODO: Check with High DPI.
                    Width = (Height - 70) * ((double)_capture.Width / (double)_capture.Height);

                    //Height = _capture.Height + 70;
                    //Width = _capture.Width;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Video device not supported");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            if (Stage != (int)Stage.Stopped)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            if (_capture != null)
            {
                _capture.StopPreview();
                _capture.Dispose();
            }
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

        private void CallBack(IAsyncResult r)
        {
            //if (!this.IsLoaded) return;

            _addDel.EndInvoke(r);
        }

        #endregion

        #region Timer

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);
            ListFrames.Add(new FrameInfo(fileName, _timer.Interval));

            _addDel.BeginInvoke(fileName, new Bitmap(_capture.GetFrame()), CallBack, null);

            //ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(_capture.GetFrame())); });
            
            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
            GC.Collect(1);
        }

        #endregion

        #region Click Events

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            Extras.CreateTemp(_pathTemp);

            _capture.PrepareCapture();

            if (Stage == Stage.Stopped)
            {
                #region To Record

                _timer = new Timer { Interval = 1000 / (int)FpsNumericUpDown.Value };

                ListFrames = new List<FrameInfo>();

                RefreshButton.IsEnabled = false;
                VideoDevicesComboBox.IsEnabled = false;
                FpsNumericUpDown.IsEnabled = false;
                Topmost = true;

                _addDel = AddFrames;
                _capture.GetFrame();

                #region Start - Normal or Snap

                if (!Settings.Default.Snapshot)
                {
                    #region Normal Recording

                    _timer.Tick += Normal_Elapsed;
                    Normal_Elapsed(null, null);
                    _timer.Start();

                    Stage = Stage.Recording;
                    RecordPauseButton.Text = Properties.Resources.Pause;
                    RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause");

                    #endregion
                }
                else
                {
                    #region SnapShot Recording

                    Stage = Stage.Snapping;
                    RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Add");
                    RecordPauseButton.Text = Properties.Resources.btnSnap;
                    Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

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
                RecordPauseButton.Text = Properties.Resources.btnRecordPause_Continue;
                RecordPauseButton.Content = (Canvas)FindResource("Vector.Record.Dark");
                Title = Properties.Resources.TitlePaused;

                _timer.Stop();

                #endregion
            }
            else if (Stage == Stage.Paused)
            {
                #region To Record Again

                Stage = Stage.Recording;
                RecordPauseButton.Text = Properties.Resources.Pause;
                RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause");
                Title = Properties.Resources.TitleRecording;

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

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //e.CanExecute = Stage != Stage.Stopped;
            e.CanExecute = true;
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

                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Record;
                    RecordPauseButton.Content = (Canvas)FindResource("Vector.Record.Dark");
                    Title = Properties.Resources.TitleStoped;

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                var errorViewer = new ExceptionViewer(nll);
                errorViewer.ShowDialog();
                LogWriter.Log(nll, "NullPointer in the Stop function");
            }
            catch (Exception ex)
            {
                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
                LogWriter.Log(ex, "Error in the Stop function");
            }
        }

        private void NotRecording_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
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
            StopButton.IsEnabled = false;

            VideoDevicesComboBox.ItemsSource = null;

            //Check again for video devices.
            _loadDel = LoadVideoDevices;
            _loadDel.BeginInvoke(LoadCallBack, null);
        }

        #endregion
    }
}
