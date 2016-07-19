using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Writers;
using ScreenToGif.Windows.Other;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    public partial class Recorder
    {
        #region Variables

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        #region Flags

        public static readonly DependencyProperty StageProperty = DependencyProperty.Register("Stage", typeof(Stage), typeof(Recorder), new FrameworkPropertyMetadata(Stage.Stopped));

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage
        {
            get { return (Stage)GetValue(StageProperty); }
            set { SetValue(StageProperty, value); }
        }

        /// <summary>
        /// True if the BackButton should be hidden.
        /// </summary>
        private readonly bool _hideBackButton;

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// The action to be executed after closing this Window.
        /// </summary>
        public ExitAction ExitArg = ExitAction.Return;

        #endregion

        #region Counters

        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;

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

        /// <summary>
        /// The maximum size of the recording. Also the maximum size of the window.
        /// </summary>
        private System.Windows.Point _sizeScreen = new System.Windows.Point(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);

        /// <summary>
        /// The size of the recording area.
        /// </summary>
        private Size _size;

        /// <summary>
        /// Holds the position of the cursor.
        /// </summary>
        private System.Windows.Point _posCursor;

        private Bitmap _bt;
        private Graphics _gr;

        /// <summary>
        /// Displays a tray icon.
        /// </summary>
        private readonly TrayIcon _trayIcon = new TrayIcon();

        /// <summary>
        /// The delay of each frame took as snapshot.
        /// </summary>
        private int? _snapDelay = null;

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _scale = 1;

        /// <summary>
        /// The last window handle saved.
        /// </summary>
        private IntPtr _lastHandle;

        #endregion

        #region Timer

        private Timer _capture = new Timer();

        private readonly System.Timers.Timer _garbageTimer = new System.Timers.Timer();

        private readonly Timer _preStartTimer = new Timer();

        #endregion

        #region Inicialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Recorder(bool hideBackButton = false)
        {
            InitializeComponent();

            _hideBackButton = hideBackButton;

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

            #region Temporary folder

            if (string.IsNullOrWhiteSpace(Settings.Default.TemporaryFolder))
            {
                Settings.Default.TemporaryFolder = Path.GetTempPath();
            }

            _pathTemp = Path.Combine(Settings.Default.TemporaryFolder, "ScreenToGif", "Recording", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

            #endregion
        }

        private void Recorder_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_hideBackButton)
            {
                HideBackButton();
            }

            //TODO:Better way
            #region Location

            if (Math.Abs(Settings.Default.RecorderLeft - -1) < 0.5)
                Settings.Default.RecorderLeft = (SystemParameters.VirtualScreenWidth - Width) / 2;
            if (Math.Abs(Settings.Default.RecorderTop - -1) < 0.5)
                Settings.Default.RecorderTop = (SystemParameters.VirtualScreenHeight - Height) / 2;

            if (Settings.Default.RecorderLeft > SystemParameters.VirtualScreenWidth)
                Settings.Default.RecorderLeft = SystemParameters.VirtualScreenWidth - 50;
            if (Settings.Default.RecorderTop > SystemParameters.VirtualScreenHeight)
                Settings.Default.RecorderTop = SystemParameters.VirtualScreenHeight - 50;

            #endregion

            #region If Snapshot

            if (Settings.Default.Snapshot)
            {
                EnableSnapshot_Executed(null, null);
            }

            #endregion

            UpdateScreenDpi();

            #region Timer

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

            #endregion

            CommandManager.InvalidateRequerySuggested();

            SystemEvents.PowerModeChanged += System_PowerModeChanged;
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
                if (_lastHandle != IntPtr.Zero)
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

            Cursor = Cursors.Arrow;

            try
            {
                #region Try to get the process

                Process target = null;// Process.GetProcesses().FirstOrDefault(p => p.Handle == handle);

                var processes = Process.GetProcesses();
                foreach (var proc in processes)
                {
                    try
                    {
                        if (proc.MainWindowHandle != IntPtr.Zero && proc.HandleCount > 0 && (proc.Handle == handle || proc.MainWindowHandle == handle))
                        {
                            target = proc;
                            break;
                        }
                    }
                    catch (Exception)
                    { }
                }

                #endregion

                if (target != null && target.ProcessName == "ScreenToGif") return;

                //Clear up the selected window frame.
                Native.DrawFrame(handle, scale);
                _lastHandle = IntPtr.Zero;

                #region Values

                var top = (rect.Top / scale) - Constants.TopOffset;
                var left = (rect.Left / scale) - Constants.LeftOffset;
                var height = ((rect.Bottom - rect.Top + 1) / scale) + Constants.TopOffset + Constants.BottomOffset;
                var width = ((rect.Right - rect.Left + 1) / scale) + Constants.LeftOffset + Constants.RightOffset;

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
        private static void AddFrames(string filename, Bitmap bitmap)
        {
            //var mutexLock = new Mutex(false, bitmap.GetHashCode().ToString());
            //mutexLock.WaitOne();

            bitmap.Save(filename);
            bitmap.Dispose();

            //GC.Collect(1);
            //mutexLock.ReleaseMutex();
        }

        #endregion

        #region Discard Async

        private delegate void DiscardFrames();

        private DiscardFrames _discardFramesDel;

        private void Discard()
        {
            try
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
            catch (IOException io)
            {
                LogWriter.Log(io, "Error while trying to Discard the Recording");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the recording", ex.Message));
                LogWriter.Log(ex, "Error while trying to Discard the Recording");
            }
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                //Enables the controls that are disabled while recording;
                FpsNumericUpDown.IsEnabled = true;
                HeightTextBox.IsEnabled = true;
                WidthTextBox.IsEnabled = true;
                OutterGrid.IsEnabled = true;

                Cursor = Cursors.Arrow;
                IsRecording(false);

                DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                if (!Settings.Default.Snapshot)
                {
                    //Only display the Record text when not in snapshot mode. 
                    Title = "Screen To Gif";
                }
                else
                {
                    Stage = Stage.Snapping;
                    EnableSnapshot_Executed(null, null);
                }

                AutoFitButtons();
            });

            GC.Collect();
        }

        #endregion

        #region Timers

        private void PreStart_Elapsed(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                Title = $"Screen To Gif ({FindResource("Recorder.PreStart")} {_preStartCount}s)";
                _preStartCount--;
            }
            else
            {
                _preStartTimer.Stop();
                RecordPauseButton.IsEnabled = true;
                Title = "Screen To Gif";

                if (Settings.Default.ShowCursor)
                {
                    #region If Show Cursor

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

                    AutoFitButtons();

                    #endregion
                }
                else
                {
                    #region If Not

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
                    AutoFitButtons();

                    #endregion
                }
            }
        }

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new Point((int)((Left + Constants.LeftOffset) * _scale), (int)((Top + Constants.TopOffset) * _scale)));

            //Take a screenshot of the area.
            var bt = Native.Capture(_size, lefttop.X, lefttop.Y);

            if (bt == null) return;

            string fileName = $"{_pathTemp}{FrameCount}.bmp";

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new Point((int)((Left + Constants.LeftOffset) * _scale), (int)((Top + Constants.TopOffset) * _scale)));

            #region TODO: 2 monitors

            //They share the same resolution count. Position matters.
            //They have different DPI.
            //CopyFromScreen ignores DPI. So I need to adjust the position, multiplying by the DPI scalling factor: 125%, 150%.
            //_size matters too.

            #endregion

            var bt = Native.Capture(_size, lefttop.X, lefttop.Y);
            
            if (bt == null) return;

            string fileName = $"{_pathTemp}{FrameCount}.bmp";
            
            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay),
                new CursorInfo(CaptureCursor.CaptureImageCursor(ref _posCursor), OutterGrid.PointFromScreen(_posCursor), _recordClicked || Mouse.LeftButton == MouseButtonState.Pressed, _scale)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }

        private void Full_Elapsed(object sender, EventArgs e)
        {
            var bt = Native.Capture(new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y), 0, 0);

            if (bt == null) return;

            string fileName = $"{_pathTemp}{FrameCount}.bmp";

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }

        private void FullCursor_Elapsed(object sender, EventArgs e)
        {
            var bt = Native.Capture(new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y), 0, 0);

            if (bt == null) return;

            string fileName = $"{_pathTemp}{FrameCount}.bmp";

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay),
                new CursorInfo(CaptureCursor.CaptureImageCursor(ref _posCursor), OutterGrid.PointFromScreen(_posCursor), _recordClicked, _scale)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }

        private void GarbageTimer_Tick(object sender, EventArgs e)
        {
            GC.Collect(1);
        }

        #endregion

        #region Buttons

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Settings.Default.Snapshot)
                RecordPause();
            else
                Snap();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _capture.Stop();
            FrameRate.Stop();
            FrameCount = 0;
            Stage = Stage.Stopped;

            OutterGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private void EnableSnapshot_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Stage == Stage.Stopped || Stage == Stage.Snapping || Stage == Stage.Paused) && OutterGrid.IsEnabled;
        }

        private void EnableSnapshot_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Extras.CreateTemp(_pathTemp);

            if (Settings.Default.Snapshot)
            {
                #region SnapShot Recording

                FpsNumericUpDown.IsEnabled = false;
                Topmost = true;

                //Set to Snapshot Mode, change the text of the record button to "Snap" and 
                //every press of the button, takes a screenshot
                Stage = Stage.Snapping;
                Title = "Screen To Gif - " + FindResource("Recorder.Snapshot");

                AutoFitButtons();

                #endregion
            }
            else
            {
                #region Normal Recording

                _snapDelay = null;

                if (ListFrames.Count > 0)
                {
                    Stage = Stage.Paused;
                    Title = FindResource("Recorder.Paused").ToString();

                    DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard,
                        HandoffBehavior.Compose);
                }
                else
                {
                    Stage = Stage.Stopped;
                    Title = "Screen To Gif";
                }

                AutoFitButtons();

                FrameRate.Stop();

                #region Register the events

                UnregisterEvents();

                if (Settings.Default.ShowCursor)
                {
                    if (!Settings.Default.FullScreen)
                    {
                        _capture.Tick += Cursor_Elapsed;
                    }
                    else
                    {
                        _capture.Tick += FullCursor_Elapsed;
                    }
                }
                else
                {
                    if (!Settings.Default.FullScreen)
                    {
                        _capture.Tick += Normal_Elapsed;
                    }
                    else
                    {
                        _capture.Tick += Full_Elapsed;
                    }
                }

                #endregion

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
            options.ShowDialog();

            Topmost = true;
        }

        private void SnapToWindow_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage == Stage.Stopped || (Stage == Stage.Snapping && ListFrames.Count == 0);
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
                    UpdateScreenDpi();

                    #region If Fullscreen

                    if (Settings.Default.FullScreen)
                    {
                        _size = new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y);

                        HideWindowAndShowTrayIcon();

                        //TODO: Hide the top of the window, add a little drag component, position this window at the bottom.
                    }
                    else
                    {
                        _size = new System.Drawing.Size((int)((Width - Constants.HorizontalOffset) * _scale), (int)((Height - Constants.VerticalOffset) * _scale));
                    }

                    #endregion

                    HeightTextBox.IsEnabled = false;
                    WidthTextBox.IsEnabled = false;
                    FpsNumericUpDown.IsEnabled = false;

                    IsRecording(true);
                    Topmost = true;

                    FrameRate.Start(_capture.Interval);
                    UnregisterEvents();

                    #region Start

                    if (Settings.Default.PreStart)
                    {
                        Title = $"Screen To Gif ({FindResource("Recorder.PreStart")} 2s)";
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

                            AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region If Not

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

                            AutoFitButtons();

                            #endregion
                        }
                    }
                    break;

                #endregion

                #endregion

                case Stage.Recording:

                    #region To Pause

                    Stage = Stage.Paused;
                    Title = FindResource("Recorder.Paused").ToString();

                    DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                    AutoFitButtons();

                    _capture.Stop();

                    FrameRate.Stop();
                    break;

                #endregion

                case Stage.Paused:

                    #region To Record Again

                    Stage = Stage.Recording;
                    Title = "Screen To Gif";

                    DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                    AutoFitButtons();

                    FrameRate.Start(_capture.Interval);

                    _capture.Start();
                    break;

                    #endregion
            }
        }

        private void Snap()
        {
            if (ListFrames.Count == 0)
            {
                #region If Fullscreen

                if (Settings.Default.FullScreen)
                {
                    _size = new System.Drawing.Size((int)_sizeScreen.X, (int)_sizeScreen.Y);

                    HideWindowAndShowTrayIcon();
                }
                else
                {
                    _size = new System.Drawing.Size((int)((Width - Constants.HorizontalOffset) * _scale), (int)((Height - Constants.VerticalOffset) * _scale));
                }

                #endregion

                IsRecording(true);
            }

            _snapDelay = Settings.Default.SnapshotDefaultDelay;

            #region Take Screenshot (All possibles types)

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

            #endregion
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

                FrameCount = 0;

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

                    Title = "Screen To Gif";

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
                RecordPauseButton.Style = (Style)FindResource("Style.Button.NoText");
                StopButton.Style = RecordPauseButton.Style;
                DiscardButton.Style = RecordPauseButton.Style;

                HideMinimizeAndMaximize(true);
            }
            else
            {
                RecordPauseButton.Style = (Style)FindResource("Style.Button.Horizontal");
                StopButton.Style = RecordPauseButton.Style;
                DiscardButton.Style = RecordPauseButton.Style;

                HideMinimizeAndMaximize(false);
            }
        }

        private void UnregisterEvents()
        {
            _capture.Tick -= Cursor_Elapsed;
            _capture.Tick -= FullCursor_Elapsed;
            _capture.Tick -= Normal_Elapsed;
            _capture.Tick -= Full_Elapsed;
        }

        private void UpdateScreenDpi()
        {
            var source = PresentationSource.FromVisual(this);

            if (source?.CompositionTarget != null)
                _scale = source.CompositionTarget.TransformToDevice.M11;

            //TODO: Get the current screen info to calculate the screen size.
            //_size = new System.Drawing.Size((int)((_size.Width - Constants.HorizontalOffset) * _scale), (int)((_size.Height - Constants.VerticalOffset) * _scale));
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
            //Visibility = Visibility.Visible;
        }

        private void NotifyIconClicked(object sender, EventArgs eventArgs)
        {
            //Visibility = Visibility.Visible;
            RecordPause();
        }

        #endregion

        #region Sizing

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
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

            var step = Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Control) ? 50 :
                Keyboard.Modifiers == ModifierKeys.Shift ? 10
                : Keyboard.Modifiers == ModifierKeys.Control ? 5 : 1;

            textBox.Value = e.Delta > 0 ? textBox.Value + step : textBox.Value - step;

            AdjustToSize();
        }

        private void AdjustToSize()
        {
            HeightTextBox.Value = Convert.ToInt32(HeightTextBox.Text) + Constants.VerticalOffset;
            WidthTextBox.Value = Convert.ToInt32(WidthTextBox.Text) + Constants.HorizontalOffset;

            Width = WidthTextBox.Value;
            Height = HeightTextBox.Value;
        }

        #endregion

        #region Other Events

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            UpdateScreenDpi();
        }

        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (Stage == Stage.Recording)
                    RecordPause();
                else if (Stage == Stage.PreStarting)
                    Stop();

                GC.Collect();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;

            if (Stage != (int)Stage.Stopped)
            {
                _preStartTimer.Stop();
                _preStartTimer.Dispose();

                _capture.Stop();
                _capture.Dispose();
            }

            _trayIcon.Dispose();

            // Garbage Collector Timer
            _garbageTimer.Stop();

            GC.Collect();
        }

        #endregion
    }
}
