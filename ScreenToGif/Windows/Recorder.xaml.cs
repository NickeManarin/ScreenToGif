using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Drawing.Point;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Recorder.xaml
    /// </summary>
    public partial class Recorder
    {
        #region Inicialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Recorder(bool hideBackButton = false)
        {
            InitializeComponent();

            _hideBackButton = hideBackButton;

            //Load
            _capture.Tick += Normal_Elapsed;

            //Config Timers - Todo: organize
            _preStartTimer.Tick += PreStart_Elapsed;
            _preStartTimer.Interval = 1000;

            //TrayIcon event.
            _trayIcon.NotifyIconClicked += NotifyIconClicked;

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook();
                _actHook.KeyDown += KeyHookTarget;
                _actHook.Start(true, true); //true for the mouse, true for the keyboard.
            }
            catch (Exception) { }

            #endregion
        }

        private void Recorder_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_hideBackButton)
            {
                HideBackButton();
            }

            #region DPI

            var source = PresentationSource.FromVisual(Application.Current.MainWindow);

            if (source != null)
                if (source.CompositionTarget != null)
                    _dpi = source.CompositionTarget.TransformToDevice.M11;

            #endregion

            //SystemEvents.DisplaySettingsChanged += (o, args) => { };
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
                StopButton_Click(null, null);
            }
        }

        /// <summary>
        /// MouseHook event method, detects the mouse clicks.
        /// </summary>
        private void MouseHookTarget(object sender, CustomMouseEventArgs keyEventArgs)
        {
            _recordClicked = keyEventArgs.Button == MouseButton.Left;
        }

        #endregion

        #region Record Async

        /// <summary>
        /// Saves the Bitmap to the disk and adds the filename in the list of frames.
        /// </summary>
        /// <param name="filename">The final filename of the Bitmap.</param>
        /// <param name="bitmap">The Bitmap to save in the disk.</param>
        private void AddFrames(string filename, Bitmap bitmap)
        {
            var mutexLock = new Mutex(false, bitmap.GetHashCode().ToString());
            mutexLock.WaitOne();

            bitmap.Save(filename);
            bitmap.Dispose();

            GC.Collect(1);
            mutexLock.ReleaseMutex();
        }

        #endregion

        #region Timers

        private void PreStart_Elapsed(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                Title = String.Format("Screen To Gif ({0} {1}", _preStartCount, Properties.Resources.TitleSecondsToGo);
                _preStartCount--;
            }
            else
            {
                _preStartTimer.Stop();
                RecordPauseButton.IsEnabled = true;

                if (Settings.Default.ShowCursor)
                {
                    #region If Show Cursor

                    if (!Settings.Default.Snapshot)
                    {
                        _actHook.OnMouseActivity += MouseHookTarget;

                        if (!Settings.Default.FullScreen)
                        {
                            IsRecording(true);
                            _capture.Tick += Cursor_Elapsed;
                            Cursor_Elapsed(null, null);
                            _capture.Start();
                        }
                        else
                        {
                            _capture.Tick += FullCursor_Elapsed;
                            FullCursor_Elapsed(null, null);
                            _capture.Start();
                        }

                        Stage = Stage.Recording;
                        RecordPauseButton.Text = Properties.Resources.Pause;
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause.Round");
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                        AutoFitButtons();
                    }
                    else
                    {
                        Stage = Stage.Snapping;
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Old");
                        RecordPauseButton.Text = Properties.Resources.btnSnap;
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                        Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                        AutoFitButtons();
                    }

                    #endregion
                }
                else
                {
                    #region If Not

                    if (!Settings.Default.Snapshot)
                    {
                        if (!Settings.Default.FullScreen)
                        {
                            IsRecording(true);
                            _capture.Tick += Normal_Elapsed;
                            Normal_Elapsed(null, null);
                            _capture.Start();
                        }
                        else
                        {
                            _capture.Tick += Full_Elapsed;
                            Full_Elapsed(null, null);
                            _capture.Start();
                        }

                        Stage = Stage.Recording;
                        RecordPauseButton.Text = Properties.Resources.Pause;
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause.Round");
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                        AutoFitButtons();
                    }
                    else
                    {
                        Stage = Stage.Snapping;
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Old");
                        RecordPauseButton.Text = Properties.Resources.btnSnap;
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                        Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                        AutoFitButtons();
                    }

                    #endregion
                }
            }
        }

        //TODO: Make all capture modes.
        private void Normal_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new Point((int)((Left + 9)*_dpi), (int)((Top + 34) * _dpi)));

            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, _size, CopyPixelOperation.SourceCopy);

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(_bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new Point((int)((Left + 9) * _dpi), (int)((Top + 34) * _dpi)));

            //TODO: 2 monitors.
            //They share the same resolution count. Position matters.
            //They have different DPI.
            //CopyFromScreen ignores DPI. So I need to adjust the position, multiplying by the DPI scalling factor: 125%, 150%.
            //_size matters too.

            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, _size, CopyPixelOperation.SourceCopy);

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay),
                new CursorInfo(CaptureCursor.CaptureImageCursor(ref _posCursor), OutterGrid.PointFromScreen(_posCursor), _recordClicked, _dpi)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(_bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        private void Full_Elapsed(object sender, EventArgs e)
        {
            _gr.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y), CopyPixelOperation.SourceCopy);

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(_bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        private void FullCursor_Elapsed(object sender, EventArgs e)
        {
            _gr.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y), CopyPixelOperation.SourceCopy);

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay),
                new CursorInfo(CaptureCursor.CaptureImageCursor(ref _posCursor), OutterGrid.PointFromScreen(_posCursor), _recordClicked, _dpi)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(_bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        #endregion

        #region Click Events

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            RecordPause();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = false;

            //TODO: If recording started, maybe disable some properties.
            var options = new Options();
            options.ShowDialog();

            Topmost = true;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void RecordPause()
        {
            Extras.CreateTemp(_pathTemp);

            switch (Stage)
            {
                case Stage.Stopped:

                    #region To Record

                    _capture = new Timer { Interval = 1000 / FpsNumericUpDown.Value };
                    _snapDelay = null;

                    ListFrames = new List<FrameInfo>();

                    #region If Fullscreen

                    if (Settings.Default.FullScreen)
                    {
                        _bt = new Bitmap((int)_sizeScreen.X, (int)_sizeScreen.Y);

                        HideWindowAndShowTrayIcon();
                    }
                    else
                    {
                        _bt = new Bitmap((int)((Width - 18) * _dpi), (int)((Height - 69) * _dpi));
                    }

                    #endregion

                    _gr = Graphics.FromImage(_bt);

                    HeightTextBox.IsEnabled = false;
                    WidthTextBox.IsEnabled = false;
                    FpsNumericUpDown.IsEnabled = false;

                    IsRecording(true);
                    Topmost = true;

                    _size = new System.Drawing.Size(_bt.Size.Width, _bt.Size.Height);
                    FrameRate.Start(_capture.Interval);

                    #region Start

                    if (Settings.Default.PreStart)
                    {
                        Title = "Screen To Gif (2 " + Properties.Resources.TitleSecondsToGo;
                        RecordPauseButton.IsEnabled = false;

                        Stage = Stage.PreStarting;
                        _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                        _preStartTimer.Start();
                    }
                    else
                    {
                        if (Settings.Default.ShowCursor)
                        {
                            #region If Show Cursor

                            if (!Settings.Default.Snapshot)
                            {
                                #region Normal Recording

                                if (!Settings.Default.FullScreen)
                                {
                                    //To start recording right away, I call the tick before starting the timer,
                                    //because the first tick will only occur after the delay.
                                    _capture.Tick += Cursor_Elapsed;
                                    //Cursor_Elapsed(null, null);
                                    _capture.Start();
                                }
                                else
                                {
                                    _capture.Tick += FullCursor_Elapsed;
                                    //FullCursor_Elapsed(null, null);
                                    _capture.Start();
                                }

                                Stage = Stage.Recording;
                                RecordPauseButton.Text = Properties.Resources.Pause;
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause.Round");
                                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                                AutoFitButtons();

                                #endregion
                            }
                            else
                            {
                                #region SnapShot Recording

                                //Set to Snapshot Mode, change the text of the record button to "Snap" and 
                                //every press of the button, takes a screenshot
                                Stage = Stage.Snapping;
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Old");
                                RecordPauseButton.Text = Properties.Resources.btnSnap;
                                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                                Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                                AutoFitButtons();

                                #endregion
                            }

                            #endregion
                        }
                        else
                        {
                            #region If Not

                            if (!Settings.Default.Snapshot)
                            {
                                #region Normal Recording

                                _actHook.OnMouseActivity += MouseHookTarget;

                                if (!Settings.Default.FullScreen)
                                {
                                    _capture.Tick += Normal_Elapsed;
                                    //Normal_Elapsed(null, null);
                                    _capture.Start();
                                }
                                else
                                {
                                    _capture.Tick += Full_Elapsed;
                                    //Full_Elapsed(null, null);
                                    _capture.Start();
                                }

                                Stage = Stage.Recording;
                                RecordPauseButton.Text = Properties.Resources.Pause;
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause.Round");
                                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                                AutoFitButtons();

                                #endregion
                            }
                            else
                            {
                                #region SnapShot Recording

                                Stage = Stage.Snapping;
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Old");
                                RecordPauseButton.Text = Properties.Resources.btnSnap;
                                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                                Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                                AutoFitButtons();

                                #endregion
                            }

                            #endregion
                        }
                    }
                    break;

                    #endregion

                    #endregion

                case Stage.Recording:

                    #region To Pause

                    Stage = Stage.Paused;
                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Continue;
                    RecordPauseButton.Content = (Canvas)FindResource("Vector.Record.Dark");
                    RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                    Title = Properties.Resources.TitlePaused;

                    AutoFitButtons();

                    _capture.Stop();
                    //ModifyCaptureTimerAndChangeTrayIconVisibility(false);

                    FrameRate.Stop();
                    break;

                    #endregion

                case Stage.Paused:

                    #region To Record Again

                    Stage = Stage.Recording;
                    RecordPauseButton.Text = Properties.Resources.Pause;
                    RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause.Round");
                    RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                    Title = Properties.Resources.TitleRecording;

                    AutoFitButtons();

                    FrameRate.Start(_capture.Interval);

                    _capture.Start();
                    break;
                //ModifyCaptureTimerAndChangeTrayIconVisibility(true);

                    #endregion

                case Stage.Snapping:

                    #region Take Screenshot (All possibles types)

                    _snapDelay = Settings.Default.SnapshotDefaultDelay;

                    if (Settings.Default.ShowCursor)
                    {
                        if (Settings.Default.FullScreen)
                        {
                            FullCursor_Elapsed(null, null);
                        }
                        else
                        {
                            Cursor_Elapsed(null, null);
                        }
                    }
                    else
                    {
                        if (Settings.Default.FullScreen)
                        {
                            Full_Elapsed(null, null);
                        }
                        else
                        {
                            Normal_Elapsed(null, null);
                        }
                    }
                    break;

                    #endregion
            }
        }

        /// <summary>
        /// Stops the recording or the Pre-Start countdown.
        /// </summary>
        private void Stop()
        {
            try
            {
                _frameCount = 0;

                _capture.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && ListFrames.Any())
                {
                    #region Stop

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
                    HeightTextBox.IsEnabled = true;
                    WidthTextBox.IsEnabled = true;

                    IsRecording(false);
                    Topmost = true;

                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Record;
                    RecordPauseButton.Content = (Canvas)FindResource("RecordDark");
                    RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                    Title = Properties.Resources.TitleStoped;

                    AutoFitButtons();

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

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (LowerGrid.ActualWidth < 250)
            {
                if (RecordPauseButton.HorizontalContentAlignment != HorizontalAlignment.Left) return;

                RecordPauseButton.Text = String.Empty;
                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Center;
                StopButton.Text = String.Empty;
                StopButton.HorizontalContentAlignment = HorizontalAlignment.Center;

                HideMinimizeAndMaximize(true);
            }
            else
            {
                if (RecordPauseButton.HorizontalContentAlignment != HorizontalAlignment.Center) return;

                if (Stage == Stage.Recording)
                    RecordPauseButton.Text = Properties.Resources.Pause;
                else if (Stage == Stage.Paused)
                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Continue;
                else if (Settings.Default.Snapshot)
                    RecordPauseButton.Text = Properties.Resources.btnSnap;
                else
                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Record;

                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                StopButton.Text = Properties.Resources.Label_Stop;
                StopButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                HideMinimizeAndMaximize(false);
            }
        }

        #endregion

        #region NotifyIcon Methods

        private void ChangeTrayIconVisibility(bool isTrayIconEnabled)
        {
            if (Settings.Default.FullScreen)
            {
                if (isTrayIconEnabled)
                {
                    HideWindowAndShowTrayIcon();
                }
                else
                {
                    ShowWindowAndHideTrayIcon();
                }
            }
        }

        private void HideWindowAndShowTrayIcon()
        {
            _trayIcon.ShowTrayIcon();
            Visibility = Visibility.Hidden;
        }

        private void ShowWindowAndHideTrayIcon()
        {
            _trayIcon.HideTrayIcon();
            Visibility = Visibility.Visible;
        }

        private void NotifyIconClicked(object sender, EventArgs eventArgs)
        {
            Visibility = Visibility.Visible;
            RecordPause();
        }

        #endregion

        #region Sizing

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AdjustToSize();
            }
        }

        private void HeightTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AdjustToSize();
        }

        private void LightWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HeightTextBox.Value = (int)Height;
            WidthTextBox.Value = (int)Width;

            AutoFitButtons();
        }

        private void SizeBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as NumericTextBox;

            if (textBox == null) return;

            textBox.Value = e.Delta > 0 ? textBox.Value + 1 : textBox.Value - 1;

            AdjustToSize();
        }

        private void AdjustToSize()
        {
            HeightTextBox.Value = Convert.ToInt32(HeightTextBox.Text) + 69; //was 65
            WidthTextBox.Value = Convert.ToInt32(WidthTextBox.Text) + 18; //was 16

            Width = WidthTextBox.Value;
            Height = HeightTextBox.Value;
        }

        #endregion

        #region Other Events

        private void LightWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region Save Settings

            Settings.Default.LastFps = Convert.ToInt32(FpsNumericUpDown.Value);
            Settings.Default.Width = (int)Width;
            Settings.Default.Height = (int)Height;

            Settings.Default.Save();

            #endregion

            try
            {
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            if (Stage != (int)Stage.Stopped)
            {
                _capture.Stop();
                _capture.Dispose();
            }

            _trayIcon.Dispose();

            GC.Collect();
        }

        #endregion
    }
}
