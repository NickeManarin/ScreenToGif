using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
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
                _actHook.OnMouseActivity += MouseHookTarget;
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

            #region If Snapshot

            if (Settings.Default.Snapshot)
            {
                //Settings.Default.Snapshot = false;
                RecordPause();
            }

            #endregion

            #region DPI

            var source = PresentationSource.FromVisual(Application.Current.MainWindow);

            if (source != null)
                if (source.CompositionTarget != null)
                    _dpi = source.CompositionTarget.TransformToDevice.M11;

            #endregion

            CommandManager.InvalidateRequerySuggested();

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
            _recordClicked = keyEventArgs.Button == MouseButton.Left && keyEventArgs.State == MouseButtonState.Pressed;
            _posCursor = new System.Windows.Point(keyEventArgs.PosX, keyEventArgs.PosY);

            if (!IsMouseCaptured || Mouse.Captured == null)
                return;

            #region Get Handle and Window Rect

            var handle = Native.WindowFromPoint(keyEventArgs.PosX, keyEventArgs.PosY);
            var scale = this.Scale();

            if (_lastHandle != handle)
            {
                Native.DrawFrame(_lastHandle, scale);
                _lastHandle = handle;
                Native.DrawFrame(handle, scale);
            }

            Native.RECT rect;
            if (!Native.GetWindowRect(handle, out rect)) return;

            #endregion

            if (keyEventArgs.State == MouseButtonState.Pressed)
                return;

            #region Mouse Up

            this.Cursor = Cursors.Arrow;

            try
            {
                #region Try to get the process

                Process target = null;// Process.GetProcesses().FirstOrDefault(p => p.Handle == handle);

                var processes = Process.GetProcesses();
                foreach (var proc in processes)
                {
                    try
                    {
                        if (proc.MainWindowHandle!= IntPtr.Zero && proc.HandleCount > 0 && (proc.Handle == handle || proc.MainWindowHandle == handle))
                        {
                            target = proc;
                            break;
                        }
                    }
                    catch (Exception)
                    {}
                }

                #endregion

                if (target != null && target.ProcessName == "ScreenToGif") return;

                //Clear up the selected window frame.
                Native.DrawFrame(handle, scale);

                #region Values

                //TODO: Check the position, different OS'.
                var top = (rect.Top / scale) - 32;
                var left = (rect.Left / scale) - 6;
                var height = ((rect.Bottom - rect.Top + 1) / scale) + 58;
                var width = ((rect.Right - rect.Left + 1) / scale) + 12;

                #endregion

                #region Validate

                //TODO: Validate screensize...
                if (top < 0)
                    top = 0 - 1;
                if (left < 0)
                    left = 0 - 1;
                if (SystemInformation.VirtualScreen.Height < height + top)
                    height = SystemInformation.VirtualScreen.Height - top;
                if (SystemInformation.VirtualScreen.Width < width + left)
                    width = SystemInformation.VirtualScreen.Width - left;

                #endregion

                Top = top;
                Left = left;
                Height = height;
                Width = width;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Snap To Window");
            }
            finally
            {
                ReleaseMouseCapture();
            }

            #endregion
        }

        #endregion

        #region Record Async

        /// <summary>
        /// Saves the Bitmap to the disk.
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
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause");
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                        AutoFitButtons();
                    }
                    else
                    {
                        Stage = Stage.Snapping;
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Add");
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
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause");
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                        AutoFitButtons();
                    }
                    else
                    {
                        Stage = Stage.Snapping;
                        RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Add");
                        RecordPauseButton.Text = Properties.Resources.btnSnap;
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                        Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                        AutoFitButtons();
                    }

                    #endregion
                }
            }
        }

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new Point((int)((Left + 9) * _dpi), (int)((Top + 34) * _dpi)));

            //Take a screenshot of the area.
            var bt = Native.Capture(_size, lefttop.X, lefttop.Y);

            if (bt == null) return;

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new Point((int)((Left + 9) * _dpi), (int)((Top + 34) * _dpi)));

            #region TODO: 2 monitors

            //They share the same resolution count. Position matters.
            //They have different DPI.
            //CopyFromScreen ignores DPI. So I need to adjust the position, multiplying by the DPI scalling factor: 125%, 150%.
            //_size matters too.

            #endregion

            var bt = Native.Capture(_size, lefttop.X, lefttop.Y);

            if (bt == null) return;

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay),
                new CursorInfo(CaptureCursor.CaptureImageCursor(ref _posCursor), OutterGrid.PointFromScreen(_posCursor), _recordClicked, _dpi)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        private void Full_Elapsed(object sender, EventArgs e)
        {
            var bt = Native.Capture(new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y), 0, 0);

            if (bt == null) return;

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        private void FullCursor_Elapsed(object sender, EventArgs e)
        {
            var bt = Native.Capture(new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y), 0, 0);

            if (bt == null) return;

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay),
                new CursorInfo(CaptureCursor.CaptureImageCursor(ref _posCursor), OutterGrid.PointFromScreen(_posCursor), _recordClicked, _dpi)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
        }

        #endregion

        #region Buttons

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            RecordPause();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _capture.Stop();
            FrameRate.Stop();
            _frameCount = 0;
            Stage = Stage.Stopped;

            #region Remove all the files

            foreach (FrameInfo frame in ListFrames)
            {
                try
                {
                    File.Delete(frame.ImageLocation);
                }
                catch (Exception)
                {}
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

            //Enables the controls that are disabled while recording;
            FpsNumericUpDown.IsEnabled = true;
            RecordPauseButton.IsEnabled = true;
            HeightTextBox.IsEnabled = true;
            WidthTextBox.IsEnabled = true;

            IsRecording(false);

            DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

            RecordPauseButton.Text = Properties.Resources.btnRecordPause_Record;
            RecordPauseButton.Content = (Canvas)FindResource("Vector.Record.Dark");
            RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
            Title = "Screen To Gif"; //Properties.Resources.TitleStoped; //TODO: Title idle

            AutoFitButtons();
            
            GC.Collect();
        }

        private void EnableSnapshot_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage == Stage.Stopped || Stage == Stage.Snapping;
        }

        private void EnableSnapshot_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Settings.Default.Snapshot)
            {
                RecordPause();
            }
            else
            {
                _snapDelay = null;
                Stage = Stage.Paused;
                RecordPauseButton.Text = Properties.Resources.btnRecordPause_Continue;
                RecordPauseButton.Content = (Canvas)FindResource("Vector.Record.Dark");
                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                Title = Properties.Resources.TitlePaused;

                if (ListFrames.Count > 0)
                    DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                AutoFitButtons();

                FrameRate.Stop();

                #region Register the events

                if (Settings.Default.ShowCursor)
                {
                    if (!Settings.Default.FullScreen)
                    {
                        _capture.Tick -= Cursor_Elapsed;
                        _capture.Tick += Cursor_Elapsed;
                    }
                    else
                    {
                        _capture.Tick -= FullCursor_Elapsed;
                        _capture.Tick += FullCursor_Elapsed;
                    }
                }
                else
                {
                    if (!Settings.Default.FullScreen)
                    {
                        _capture.Tick -= Normal_Elapsed;
                        _capture.Tick += Normal_Elapsed;
                    }
                    else
                    {
                        _capture.Tick -= Full_Elapsed;
                        _capture.Tick += Full_Elapsed;
                    }
                }

                #endregion
            }
        }

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;

            var options = new Options();
            options.ShowDialog(); //TODO: If recording started, maybe disable some properties.

            Topmost = true;
        }

        private void SnapToWindow_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage == Stage.Stopped;
        }

        private void SnapButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(this);
            
            this.Cursor = Cursors.Cross;
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

                    _capture = new Timer { Interval = 1000 / (int)FpsNumericUpDown.Value };
                    _snapDelay = null;

                    ListFrames = new List<FrameInfo>();

                    #region If Fullscreen

                    if (Settings.Default.FullScreen)
                    {
                        _size = new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y);

                        HideWindowAndShowTrayIcon();
                    }
                    else
                    {
                        _size = new System.Drawing.Size((int)((Width - 18) * _dpi), (int)((Height - 69) * _dpi));
                    }

                    #endregion

                    HeightTextBox.IsEnabled = false;
                    WidthTextBox.IsEnabled = false;
                    FpsNumericUpDown.IsEnabled = false;

                    IsRecording(true);
                    Topmost = true;

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
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause");
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
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Add");
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
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause");
                                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                                AutoFitButtons();

                                #endregion
                            }
                            else
                            {
                                #region SnapShot Recording

                                Stage = Stage.Snapping;
                                RecordPauseButton.Content = (Canvas)FindResource("Vector.Camera.Add");
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

                    DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

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
                    RecordPauseButton.Content = (Canvas)FindResource("Vector.Pause");
                    RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                    Title = Properties.Resources.TitleRecording;

                    DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

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
                _capture.Stop();
                FrameRate.Stop();

                _frameCount = 0;

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
                    RecordPauseButton.Content = (Canvas)FindResource("Vector.Record.Dark");
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
                LogWriter.Log(nll, "NullPointer on the Stop function");
            }
            catch (Exception ex)
            {
                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
                LogWriter.Log(ex, "Error on the Stop function");
            }
        }

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (LowerGrid.ActualWidth < 250)
            {
                RecordPauseButton.Style = (Style) FindResource("Style.Button.NoText");
                StopButton.Style = RecordPauseButton.Style;
                DiscardButton.Style = RecordPauseButton.Style;

                HideMinimizeAndMaximize(true);
            }
            else
            {
                //If already using the horizontal style. TODO: Test with high DPI.
                if (RecordPauseButton.Width > 40) return; 

                RecordPauseButton.Style = (Style)FindResource("Style.Button.Horizontal");
                StopButton.Style = RecordPauseButton.Style;
                DiscardButton.Style = RecordPauseButton.Style;

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
            HeightTextBox.Value = (int)Math.Round(Height, MidpointRounding.AwayFromZero);
            WidthTextBox.Value = (int)Math.Round(Width, MidpointRounding.AwayFromZero);

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
                _actHook.OnMouseActivity -= MouseHookTarget;
                _actHook.KeyDown -= KeyHookTarget;
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            if (Stage != (int)Stage.Stopped)
            {
                _preStartTimer.Stop();
                _preStartTimer.Dispose();

                _capture.Stop();
                _capture.Dispose();
            }

            _trayIcon.Dispose();

            GC.Collect();
        }

        #endregion
    }
}
