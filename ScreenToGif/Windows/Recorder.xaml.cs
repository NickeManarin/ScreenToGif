using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Windows.Other;
using Cursors = System.Windows.Input.Cursors;
using Monitor = ScreenToGif.Util.Monitor;
using Size = System.Windows.Size;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    public partial class Recorder
    {
        #region Variables

        /// <summary>
        /// The window's left edge position.
        /// </summary>
        private int _left = 0;

        /// <summary>
        /// The window's top edge position.
        /// </summary>
        private int _top = 0;

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        private Task<Image> _captureTask;

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// The maximum size of the recording. Also the maximum size of the window.
        /// </summary>
        private System.Windows.Point _sizeScreen = new System.Windows.Point(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);

        /// <summary>
        /// The size of the recording area.
        /// </summary>
        private Size _size;

        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;

        /// <summary>
        /// True when the user stop the recording. 
        /// </summary>
        private bool _stopRequested;

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _scale = 1;

        /// <summary>
        /// The last window handle saved.
        /// </summary>
        private IntPtr _lastHandle;

        #region Flags

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// The delay of each frame took as snapshot.
        /// </summary>
        private int? _snapDelay = null;

        #endregion

        #endregion

        #region Timer

        private Timer _capture = new Timer();

        private readonly System.Timers.Timer _garbageTimer = new System.Timers.Timer();

        private readonly Timer _preStartTimer = new Timer();

        #endregion

        #region Inicialization

        public Recorder(bool hideBackButton = true)
        {
            InitializeComponent();

            BackVisibility = BackButton.Visibility = hideBackButton ? Visibility.Collapsed : Visibility.Visible;

            UpdateScreenDpi();

            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning(true))
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion

            //Config Timers - Todo: organize
            _preStartTimer.Tick += PreStart_Elapsed;
            _preStartTimer.Interval = 1000;

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook(true, true); //true for the mouse, true for the keyboard.
                _actHook.KeyDown += KeyHookTarget;
                _actHook.OnMouseActivity += MouseHookTarget;
            }
            catch (Exception) { }

            #endregion

            #region Temporary folder

            //If never configurated.
            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                UserSettings.All.TemporaryFolder = Path.GetTempPath();

            #endregion
        }

        private void Recorder_Loaded(object sender, RoutedEventArgs e)
        {
            #region If Snapshot

            if (UserSettings.All.SnapshotMode)
                EnableSnapshot_Executed(null, null);

            #endregion

            #region Timer

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

            #endregion

            CommandManager.InvalidateRequerySuggested();

            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            RecordPauseButton.Focus();
        }

        #endregion

        #region Hooks

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                return;

            if (Stage != Stage.Discarding && Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
                RecordPauseButton_Click(null, null);
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut)
                StopButton_Click(null, null);
            else if ((Stage == Stage.Paused || Stage == Stage.Snapping) && Keyboard.Modifiers.HasFlag(UserSettings.All.DiscardModifiers) && e.Key == UserSettings.All.DiscardShortcut)
                DiscardButton_Click(null, null);
            else
                _keyList.Add(new SimpleKeyGesture(e.Key, Keyboard.Modifiers, e.IsUppercase));
        }

        /// <summary>
        /// MouseHook event method, detects the mouse clicks.
        /// </summary>
        private void MouseHookTarget(object sender, CustomMouseEventArgs args)
        {
            if (WindowState == WindowState.Minimized)
                return;

            _recordClicked = args.LeftButton == MouseButtonState.Pressed || args.RightButton == MouseButtonState.Pressed || args.MiddleButton == MouseButtonState.Pressed;

            if (!IsMouseCaptured || Mouse.Captured == null)
                return;

            #region Get Handle and Window Rect

            var handle = Native.WindowFromPoint(new Native.PointW { X = args.PosX, Y = args.PosY });
            var scale = this.Scale();

            if (_lastHandle != handle)
            {
                if (_lastHandle != IntPtr.Zero)
                    Native.DrawFrame(_lastHandle, scale);

                _lastHandle = handle;
                Native.DrawFrame(handle, scale);
            }

            var rect = Native.TrueWindowRectangle(handle);

            #endregion

            if (args.LeftButton == MouseButtonState.Pressed && Mouse.LeftButton == MouseButtonState.Pressed)
                return;

            #region Mouse Up

            Cursor = Cursors.Arrow;

            try
            {
                #region Try to get the process

                uint id = 0;
                Native.GetWindowThreadProcessId(handle, out id);
                var target = Process.GetProcesses().FirstOrDefault(p => p.Id == id);

                #endregion

                if (target != null && target.ProcessName == "ScreenToGif") return;

                //Clear up the selected window frame.
                Native.DrawFrame(handle, scale);
                _lastHandle = IntPtr.Zero;

                #region Values

                //TODO: Test values with other versions of windows.
                var top = (rect.Y / scale) - Constants.TopOffset + 0;
                var left = (rect.X / scale) - Constants.LeftOffset + 0;
                var height = ((rect.Height + 1) / scale) + Constants.TopOffset + Constants.BottomOffset - 1;
                var width = ((rect.Width + 1) / scale) + Constants.LeftOffset + Constants.RightOffset - 1;

                #endregion

                #region Validate

                if (top < SystemParameters.VirtualScreenTop)
                    top = SystemParameters.VirtualScreenTop - 1;
                if (left < SystemParameters.VirtualScreenLeft)
                    left = SystemParameters.VirtualScreenLeft - 1;
                if (SystemInformation.VirtualScreen.Height < (height + top) * scale) //TODO: Check if works with 2 screens.
                    height = (SystemInformation.VirtualScreen.Height - top) / scale;
                if (SystemInformation.VirtualScreen.Width < (width + left) * scale)
                    width = (SystemInformation.VirtualScreen.Width - left) / scale;

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
            //var mutexLock = new Mutex(false, bitmap.GetHashCode().ToString());
            //mutexLock.WaitOne();

            bitmap.Save(filename);
            bitmap.Dispose();

            //GC.Collect(1);
            //mutexLock.ReleaseMutex();
        }

        /// <summary>
        /// Saves the Bitmap to the disk.
        /// </summary>
        /// <param name="filename">The final filename of the Bitmap.</param>
        /// <param name="bitmap">The Bitmap to save in the disk.</param>
        private void AddFrames(string filename, BitmapSource bitmap)
        {
            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                Dispatcher.Invoke(() =>
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                });
            }
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

                foreach (var frame in Project.Frames)
                {
                    try
                    {
                        File.Delete(frame.Path);
                    }
                    catch (Exception)
                    { }
                }

                try
                {
                    Directory.Delete(Project.FullPath, true);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Delete Temp Path");
                }

                #endregion

                Project.Frames.Clear();
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
                FpsIntegerUpDown.IsEnabled = true;
                HeightIntegerBox.IsEnabled = true;
                WidthIntegerBox.IsEnabled = true;
                OutterGrid.IsEnabled = true;

                Cursor = Cursors.Arrow;
                IsRecording = false;

                DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                if (!UserSettings.All.SnapshotMode)
                {
                    //Only display the Record text when not in snapshot mode. 
                    Title = "ScreenToGif";
                    Stage = Stage.Stopped;
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

        #region Buttons

        private void EnableSnapshot_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Stage == Stage.Stopped || Stage == Stage.Snapping || Stage == Stage.Paused) && OutterGrid.IsEnabled;
        }

        private void EnableThinMode_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Only allows if (it's stoped or snapping but with nothing recorded) and the outter grid is enabled.
            e.CanExecute = (Stage == Stage.Stopped || (Stage == Stage.Snapping && (Project == null || Project.Frames.Count == 0))) && OutterGrid.IsEnabled;
        }

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }


        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSettings.All.SnapshotMode)
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
            Stage = Stage.Discarding;

            OutterGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private void EnableSnapshot_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (UserSettings.All.SnapshotMode)
            {
                #region SnapShot Recording

                //Set to Snapshot Mode, change the text of the record button to "Snap" and 
                //every press of the button, takes a screenshot
                Stage = Stage.Snapping;
                Title = "ScreenToGif - " + FindResource("Recorder.Snapshot");

                AutoFitButtons();

                if (Project == null || Project.Frames.Count == 0)
                {
                    Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);
                }

                #endregion
            }
            else
            {
                #region Normal Recording

                _snapDelay = null;

                if (Project.Frames.Count > 0)
                {
                    Stage = Stage.Paused;
                    Title = FindResource("Recorder.Paused").ToString();

                    DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);
                }
                else
                {
                    Stage = Stage.Stopped;
                    Title = "ScreenToGif";
                }

                AutoFitButtons();

                FrameRate.Stop();

                #region Register the events

                UnregisterEvents();

                if (UserSettings.All.ShowCursor)
                {
                    if (UserSettings.All.AsyncRecording)
                        _capture.Tick += CursorAsync_Elapsed;
                    else
                        _capture.Tick += Cursor_Elapsed;
                }
                else
                {
                    if (UserSettings.All.AsyncRecording)
                        _capture.Tick += NormalAsync_Elapsed;
                    else
                        _capture.Tick += Normal_Elapsed;
                }

                #endregion

                #endregion
            }
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;

            var wasSnapshot = UserSettings.All.SnapshotMode;

            var options = new Options();
            options.ShowDialog();

            //Enables or disables the snapshot mode.
            if (wasSnapshot != UserSettings.All.SnapshotMode)
                EnableSnapshot_Executed(sender, e);

            Topmost = true;
        }

        private void SnapToWindow_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage == Stage.Stopped || (Stage == Stage.Snapping && (Project == null || Project.Frames.Count == 0));
        }

        private void SnapButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(this);

            Cursor = Cursors.Cross;
        }

        private void EnableThinMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Updates the Offsets of the two controls (because it's a static property, it will not update by itself).

            HeightIntegerBox.Offset = Constants.VerticalOffset;
            WidthIntegerBox.Offset = Constants.HorizontalOffset;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Timers

        private void PreStart_Elapsed(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                Title = $"ScreenToGif ({FindResource("Recorder.PreStart")} {_preStartCount}s)";
                _preStartCount--;
            }
            else
            {
                _preStartTimer.Stop();
                RecordPauseButton.IsEnabled = true;
                Title = "Screen To Gif";
                IsRecording = true;

                if (UserSettings.All.ShowCursor)
                {
                    #region If Show Cursor

                    if (UserSettings.All.AsyncRecording)
                    {
                        _capture.Tick += CursorAsync_Elapsed;
                        CursorAsync_Elapsed(null, null);
                    }
                    else
                    {
                        _capture.Tick += Cursor_Elapsed;
                        Cursor_Elapsed(null, null);
                    }

                    _capture.Start();

                    Stage = Stage.Recording;

                    AutoFitButtons();

                    #endregion
                }
                else
                {
                    #region If Not

                    if (UserSettings.All.AsyncRecording)
                    {
                        _capture.Tick += NormalAsync_Elapsed;
                        NormalAsync_Elapsed(null, null);
                    }
                    else
                    {
                        _capture.Tick += Normal_Elapsed;
                        Normal_Elapsed(null, null);
                    }

                    _capture.Start();

                    Stage = Stage.Recording;
                    AutoFitButtons();

                    #endregion
                }
            }
        }


        private async void NormalAsync_Elapsed(object sender, EventArgs e)
        {
            //Take a screenshot of the area.
            _captureTask = Task.Factory.StartNew(() => Native.Capture(_size, _left, _top));

            var bt = await _captureTask;

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), _keyList));

            _keyList.Clear();

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }

        private async void CursorAsync_Elapsed(object sender, EventArgs e)
        {
            if (_stopRequested)
                return;

            if (_captureTask != null && !_captureTask.IsCompleted)
                _captureTask.Wait();

            int cursorPosX = 0, cursorPosY = 0;
            _captureTask = Task.Factory.StartNew(() => Native.CaptureWithCursor(_size, _left, _top, out cursorPosX, out cursorPosY), TaskCreationOptions.PreferFairness);

            var bt = await _captureTask;

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            if (!OutterGrid.IsVisible)
                return;

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), cursorPosX, cursorPosY, _recordClicked, _keyList));

            _keyList.Clear();

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }


        private void Normal_Elapsed(object sender, EventArgs e)
        {
            //Take a screenshot of the area.
            var bt = Native.Capture(_size, _left, _top);

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), _keyList));

            _keyList.Clear();

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            var bt = Native.CaptureWithCursor(_size, _left, _top, out var cursorPosX, out var cursorPosY);

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), cursorPosX, cursorPosY, _recordClicked, _keyList));

            _keyList.Clear();

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }


        private void GarbageTimer_Tick(object sender, EventArgs e)
        {
            GC.Collect(UserSettings.All.LatestFps > 30 ? 6 : 2);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        internal async void RecordPause()
        {
            switch (Stage)
            {
                case Stage.Stopped:

                    #region To Record

                    _capture = new Timer { Interval = 1000 / FpsIntegerUpDown.Value };
                    _snapDelay = null;

                    Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                    _keyList.Clear();
                    FrameCount = 0;

                    await Task.Factory.StartNew(UpdateScreenDpi);

                    //Sizing.
                    _size = new Size((int)Math.Round((Width - Constants.HorizontalOffset) * _scale), (int)Math.Round((Height - Constants.VerticalOffset) * _scale));

                    HeightIntegerBox.IsEnabled = false;
                    WidthIntegerBox.IsEnabled = false;
                    FpsIntegerUpDown.IsEnabled = false;

                    IsRecording = true;
                    Topmost = true;

                    FrameRate.Start(_capture.Interval);
                    UnregisterEvents();

                    #region Start

                    if (UserSettings.All.UsePreStart)
                    {
                        Title = $"Screen To Gif ({FindResource("Recorder.PreStart")} {UserSettings.All.PreStartValue}s)";
                        RecordPauseButton.IsEnabled = false;

                        Stage = Stage.PreStarting;
                        _preStartCount = UserSettings.All.PreStartValue - 1;

                        _preStartTimer.Start();
                    }
                    else
                    {
                        if (UserSettings.All.ShowCursor)
                        {
                            #region If Show Cursor

                            if (UserSettings.All.AsyncRecording)
                                _capture.Tick += CursorAsync_Elapsed;
                            else
                                _capture.Tick += Cursor_Elapsed;

                            _capture.Start();

                            Stage = Stage.Recording;

                            AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region If Not

                            if (UserSettings.All.AsyncRecording)
                                _capture.Tick += NormalAsync_Elapsed;
                            else
                                _capture.Tick += Normal_Elapsed;

                            _capture.Start();

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
            if (Project == null || Project.Frames.Count == 0)
            {
                _size = new Size((int)Math.Round((Width - Constants.HorizontalOffset) * _scale), (int)Math.Round((Height - Constants.VerticalOffset) * _scale));

                DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                IsRecording = true;
            }

            _snapDelay = UserSettings.All.SnapshotDefaultDelay;

            #region Take Screenshot (All possibles types)

            if (UserSettings.All.ShowCursor)
            {
                if (UserSettings.All.AsyncRecording)
                    CursorAsync_Elapsed(null, null);
                else
                    Cursor_Elapsed(null, null);
            }
            else
            {
                if (UserSettings.All.AsyncRecording)
                    NormalAsync_Elapsed(null, null);
                else
                    Normal_Elapsed(null, null);
            }

            #endregion
        }

        /// <summary>
        /// Stops the recording or the Pre-Start countdown.
        /// </summary>
        private async void Stop()
        {
            try
            {
                _capture.Stop();
                FrameRate.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && Project.Any)
                {
                    #region Stop

                    if (UserSettings.All.AsyncRecording)
                        _stopRequested = true;

                    await Task.Delay(100);

                    Close();

                    #endregion
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !Project.Any)
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    if (Stage == Stage.PreStarting)
                    {
                        //Stop the pre-start timer to kill pre-start warming up
                        _preStartTimer.Stop();
                    }

                    //Only returns to the stopped stage if it was recording.
                    Stage = Stage == Stage.Snapping ? Stage.Snapping : Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    FpsIntegerUpDown.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    HeightIntegerBox.IsEnabled = true;
                    WidthIntegerBox.IsEnabled = true;

                    IsRecording = false;
                    Topmost = true;

                    Title = "Screen To Gif";

                    AutoFitButtons();

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                LogWriter.Log(nll, "NullPointer on the Stop function");

                ErrorDialog.Ok("ScreenToGif", "Error while stopping", nll.Message, nll);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error on the Stop function");

                ErrorDialog.Ok("ScreenToGif", "Error while stopping", ex.Message, ex);
            }
        }

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (LowerGrid.ActualWidth < 360)
            {
                RecordPauseButton.Style = (Style)FindResource("Style.Button.NoText");
                StopButton.Style = RecordPauseButton.Style;
                DiscardButton.Style = RecordPauseButton.Style;

                MinimizeVisibility = Visibility.Collapsed;

                if (IsThin)
                    CaptionText.Visibility = Visibility.Collapsed;
            }
            else
            {
                RecordPauseButton.Style = (Style)FindResource("Style.Button.Horizontal");
                StopButton.Style = RecordPauseButton.Style;
                DiscardButton.Style = RecordPauseButton.Style;

                MinimizeVisibility = Visibility.Visible;

                if (IsThin)
                    CaptionText.Visibility = Visibility.Visible;
            }
        }

        private void UnregisterEvents()
        {
            _capture.Tick -= Normal_Elapsed;
            _capture.Tick -= NormalAsync_Elapsed;

            _capture.Tick -= Cursor_Elapsed;
            _capture.Tick -= CursorAsync_Elapsed;
        }

        private void UpdateScreenDpi()
        {
            try
            {
                var source = Dispatcher.Invoke(() => PresentationSource.FromVisual(this));

                if (source?.CompositionTarget != null)
                    _scale = Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);

                Dispatcher.Invoke(() =>
                {
                    WidthIntegerBox.Scale = _scale;
                    HeightIntegerBox.Scale = _scale;
                });
            }
            finally
            {
                GC.Collect(1);
            }
        }

        private bool UpdatePositioning(bool startup = false)
        {
            var top = UserSettings.All.RecorderTop;
            var left = UserSettings.All.RecorderLeft;

            //If the position was never set.
            if (double.IsNaN(top) || double.IsNaN(left))
            {
                //Let it center on screen when the window is loading.
                if (startup)
                    return false;

                //Let the code below decide where to position the screen.
                top = 0;
                left = 0;
            }

            //The catch here is to get the closest monitor from current Top/Left point. 
            var monitors = Monitor.AllMonitorsScaled(this.Scale());
            var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new System.Windows.Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

            if (closest == null)
                return false;

            //To much to the Left.
            if (closest.WorkingArea.Left > UserSettings.All.RecorderLeft + UserSettings.All.RecorderWidth - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > UserSettings.All.RecorderTop + UserSettings.All.RecorderHeight - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < UserSettings.All.RecorderLeft + 100)
                left = closest.WorkingArea.Right - UserSettings.All.RecorderWidth;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < UserSettings.All.RecorderTop + 100)
                top = closest.WorkingArea.Bottom - UserSettings.All.RecorderHeight;

            UserSettings.All.RecorderTop = top;
            UserSettings.All.RecorderLeft = left;
            
            return true;
        }

        #endregion

        #region Other Events

        private void LightWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AutoFitButtons();
        }

        private void CommandGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove(); //await Task.Factory.StartNew(() => Dispatcher.Invoke(DragMove));
        }

        private async void Window_LocationChanged(object sender, EventArgs e)
        {
            //TestTextBlock.Text = $"{_left};{_top}";

            await Task.Factory.StartNew(UpdateScreenDpi);

            _left = (int)Math.Round((Math.Round(Left, MidpointRounding.AwayFromZero) + Constants.LeftOffset) * _scale);
            _top = (int)Math.Round((Math.Round(Top, MidpointRounding.AwayFromZero) + Constants.TopOffset) * _scale);
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

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs eventArgs)
        {
            UpdatePositioning();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Save Settings
            UserSettings.Save();

            #region Remove Hooks

            try
            {
                _actHook.OnMouseActivity -= MouseHookTarget;
                _actHook.KeyDown -= KeyHookTarget;
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            #endregion

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

            #region Stops the timers

            if (Stage != (int)Stage.Stopped)
            {
                _preStartTimer.Stop();
                _preStartTimer.Dispose();

                _capture.Stop();
                _capture.Dispose();
            }

            //Garbage Collector Timer.
            _garbageTimer.Stop();

            #endregion

            GC.Collect();
        }

        #endregion
    }
}