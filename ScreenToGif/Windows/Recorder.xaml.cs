using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Capture;
using ScreenToGif.Windows.Other;
using Cursors = System.Windows.Input.Cursors;
using DpiChangedEventArgs = System.Windows.DpiChangedEventArgs;
using Monitor = ScreenToGif.Util.Monitor;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    public partial class Recorder
    {
        #region Variables

        /// <summary>
        /// The region's left edge position.
        /// </summary>
        private int _left = 0;

        /// <summary>
        /// The region's top edge position.
        /// </summary>
        private int _top = 0;

        /// <summary>
        /// The region's width.
        /// </summary>
        private int _width = 0;

        /// <summary>
        /// The region's height.
        /// </summary>
        private int _height = 0;

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        /// <summary>
        /// Deals with all screen capture methods.
        /// </summary>
        private ICapture _capture;


        private Task<int> _captureTask;

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// The maximum size of the recording. Also the maximum size of the window.
        /// </summary>
        private System.Windows.Point _sizeScreen = new System.Windows.Point(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);



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

        #endregion

        #region Mouse cursor follow up

        /// <summary>
        /// The previous position of the cursor in the X axis.
        /// </summary>
        private int _prevPosX = 0;

        /// <summary>
        /// The previous position of the cursor in the Y axis.
        /// </summary>
        private int _prevPosY = 0;

        /// <summary>
        /// The latest position of the cursor in the X axis.
        /// </summary>
        private int _posX = 0;

        /// <summary>
        /// The latest position of the cursor in the Y axis.
        /// </summary>
        private int _posY = 0;

        /// <summary>
        /// The offset in pixels. Used for moving the recorder around the X axis.
        /// </summary>
        private double _offsetX = 0;

        /// <summary>
        /// The offset in pixels. Used for moving the recorder around the Y axis.
        /// </summary>
        private double _offsetY = 0;

        #endregion

        #endregion

        #region Timer

        private Timer _captureTimer = new Timer();

        private readonly System.Timers.Timer _garbageTimer = new System.Timers.Timer();

        private readonly Timer _preStartTimer = new Timer();

        private readonly Timer _followTimer = new Timer();
        private readonly Timer _showBorderTimer = new Timer();

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSize();
            UpdateLocation();

            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning(true))
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion

            DetectCaptureFrequency();
            DetectThinMode();

            #region Timers

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

            _showBorderTimer.Interval = 500;
            _showBorderTimer.Tick += ShowBorderTimer_Tick;

            _followTimer.Tick += FollowTimer_Tick;

            #endregion

            CommandManager.InvalidateRequerySuggested();

            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            RecordPauseButton.Focus();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (IsFollowing && UserSettings.All.FollowShortcut == Key.None)
            {
                IsFollowing = false;

                Dialog.Ok(LocalizationHelper.Get("S.StartUp.Recorder"), LocalizationHelper.Get("S.Options.Warning.Follow.Header"),
                    LocalizationHelper.Get("S.Options.Warning.Follow.Message"), Icons.Warning);
            }
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
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.FollowModifiers) && e.Key == UserSettings.All.FollowShortcut)
                IsFollowing = !IsFollowing;
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

            _posX = args.PosX;
            _posY = args.PosY;

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

                Native.GetWindowThreadProcessId(handle, out var id);
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
                LogWriter.Log(ex, "Error while trying to Discard the Recording");
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the recording", ex.Message));
            }
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                //Enables the controls that are disabled while recording;
                FrequencyIntegerUpDown.IsEnabled = true;
                HeightIntegerBox.IsEnabled = true;
                WidthIntegerBox.IsEnabled = true;
                OutterGrid.IsEnabled = true;

                Cursor = Cursors.Arrow;
                IsRecording = false;

                DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                DetectCaptureFrequency();
                AutoFitButtons();
            }));

            GC.Collect();
        }

        #endregion

        #region Buttons

        private void SwitchCaptureFrequency_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Stage == Stage.Stopped || Stage == Stage.Snapping || Stage == Stage.Paused) && OutterGrid.IsEnabled;
        }

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }


        private async void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSettings.All.CaptureFrequency == CaptureFrequency.Manual)
            {
                await Snap();
                return;
            }

            await RecordPause();
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            await Stop();
        }

        private async void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _captureTimer.Stop();
            FrameRate.Stop();
            FrameCount = 0;
            Stage = Stage.Discarding;
            await _capture.Stop();

            OutterGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;

            var options = new Options(Options.RecorderIndex);
            options.ShowDialog();

            DetectCaptureFrequency();
            DetectThinMode();

            Topmost = true;
        }

        private void SwitchCaptureFrequency_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //When this event is fired from clicking on the switch button.
            if (e.Parameter != null)
            {
                switch (UserSettings.All.CaptureFrequency)
                {
                    case CaptureFrequency.Manual:
                        UserSettings.All.CaptureFrequency = CaptureFrequency.PerSecond;
                        break;

                    case CaptureFrequency.PerSecond:
                        UserSettings.All.CaptureFrequency = CaptureFrequency.PerMinute;
                        break;

                    case CaptureFrequency.PerMinute:
                        UserSettings.All.CaptureFrequency = CaptureFrequency.PerHour;
                        break;

                    default: //PerHour.
                        UserSettings.All.CaptureFrequency = CaptureFrequency.Manual;
                        break;
                }
            }

            //When event is fired when the frequency is picked from the context menu, just switch the labels.
            DetectCaptureFrequency();
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
                Title = $"ScreenToGif ({LocalizationHelper.Get("S.Recorder.PreStart")} {_preStartCount}s)";
                _preStartCount--;
            }
            else
            {
                _preStartTimer.Stop();
                RecordPauseButton.IsEnabled = true;
                Title = "ScreenToGif";
                IsRecording = true;

                if (UserSettings.All.ShowCursor)
                {
                    #region If Show Cursor

                    if (UserSettings.All.AsyncRecording)
                    {
                        _captureTimer.Tick += CursorAsync_Elapsed;
                        CursorAsync_Elapsed(null, null);
                    }
                    else
                    {
                        _captureTimer.Tick += Cursor_Elapsed;
                        Cursor_Elapsed(null, null);
                    }

                    _captureTimer.Start();

                    Stage = Stage.Recording;
                    AutoFitButtons();

                    #endregion
                }
                else
                {
                    #region If Not

                    if (UserSettings.All.AsyncRecording)
                    {
                        _captureTimer.Tick += NormalAsync_Elapsed;
                        NormalAsync_Elapsed(null, null);
                    }
                    else
                    {
                        _captureTimer.Tick += Normal_Elapsed;
                        Normal_Elapsed(null, null);
                    }

                    _captureTimer.Start();

                    Stage = Stage.Recording;
                    AutoFitButtons();

                    #endregion
                }
            }
        }


        private async void NormalAsync_Elapsed(object sender, EventArgs e)
        {
            if (_stopRequested)
                return;

            _captureTask = _capture.CaptureAsync(new FrameInfo(_recordClicked, _keyList));
            FrameCount = await _captureTask;

            _keyList.Clear();
        }

        private async void CursorAsync_Elapsed(object sender, EventArgs e)
        {
            if (_stopRequested)
                return;

            _captureTask = _capture.CaptureWithCursorAsync(new FrameInfo(_recordClicked, _keyList));
            FrameCount = await _captureTask;

            _keyList.Clear();
        }

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            FrameCount = _capture.Capture(new FrameInfo(false, _keyList));

            _keyList.Clear();
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            FrameCount = _capture.CaptureWithCursor(new FrameInfo(_recordClicked, _keyList));

            _keyList.Clear();
        }


        private void GarbageTimer_Tick(object sender, EventArgs e)
        {
            GC.Collect(UserSettings.All.LatestFps > 30 ? 6 : 2);
        }


        private void FollowTimer_Tick(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal || _prevPosX == _posX && _prevPosY == _posY || Stage == Stage.Paused || Stage == Stage.Stopped || Stage == Stage.Discarding ||
                (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers == UserSettings.All.DisableFollowModifiers))
                return;

            _prevPosX = _posX;
            _prevPosY = _posY;

            //TODO: Test with 2 monitors.
            //if (isCentered)
            //{
            //    //Hide the UI.
            //    _showBorderTimer.Stop();
            //    BeginStoryboard(this.FindStoryboard("HideRectangleStoryboard"), HandoffBehavior.SnapshotAndReplace);
            //    _showBorderTimer.Start();

            //    //_offsetX = _posX - Region.Width / 2d;
            //    //_offsetY = _posY - Region.Height / 2d;

            //    //Region = new Rect(new Point(_offsetX.Clamp(-1, Width - Region.Width + 1), _offsetY.Clamp(-1, Height - Region.Height + 1)), Region.Size);
            //    //DashedRectangle.Refresh();
            //}
            //else
            {
                if (_width < 1)
                    UpdateSize();

                //Only move to the left if 'Mouse.X < Rect.L' and only move to the right if 'Mouse.X > Rect.R'
                _offsetX = _posX - UserSettings.All.FollowBuffer < _left ? _posX - _left - UserSettings.All.FollowBuffer : _posX + UserSettings.All.FollowBuffer > _left + _width ?
                    _posX - (_left + _width) + UserSettings.All.FollowBuffer : 0;
                _offsetY = _posY - UserSettings.All.FollowBuffer < _top ? _posY - _top - UserSettings.All.FollowBuffer : _posY + UserSettings.All.FollowBuffer > _top + _height ?
                    _posY - (_top + _height) + UserSettings.All.FollowBuffer : 0;

                //Hide the UI when moving.
                if (_posX - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < _left || _posX + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > _left + _width ||
                    _posY - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < _top || _posY + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > _top + _height)
                {
                    _showBorderTimer.Stop();
                    HideInternals();
                    BeginStoryboard(this.FindStoryboard("HideWindowStoryboard"), HandoffBehavior.Compose);
                    _showBorderTimer.Start();
                }

                this.Refresh();
            }

            //Rearrange the window.
            Left = (Left + _offsetX / _scale).Clamp(0 - Constants.LeftOffset, SystemParameters.VirtualScreenWidth - Width + Constants.RightOffset);
            Top = (Top + _offsetY / _scale).Clamp(0 - Constants.TopOffset, SystemParameters.VirtualScreenHeight - Height + Constants.BottomOffset);
        }

        private void ShowBorderTimer_Tick(object sender, EventArgs e)
        {
            _showBorderTimer.Stop();

            this.Refresh();
            ShowInternals();

            BeginStoryboard(this.FindStoryboard("ShowWindowStoryboard"), HandoffBehavior.Compose);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        internal async Task RecordPause()
        {
            try
            {
                switch (Stage)
                {
                    case Stage.Stopped:

                        #region To record

                        //Take into account the frequency mode.
                        _captureTimer = new Timer
                        {
                            Interval = GetCaptureInterval()
                        };

                        Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                        _keyList.Clear();
                        FrameCount = 0;

                        await Task.Factory.StartNew(UpdateScreenDpi);

                        await PrepareNewCapture();

                        HeightIntegerBox.IsEnabled = false;
                        WidthIntegerBox.IsEnabled = false;
                        FrequencyIntegerUpDown.IsEnabled = false;

                        IsRecording = true;
                        Topmost = true;

                        FrameRate.Start(HasFixedDelay(), GetFixedDelay());
                        UnregisterEvents();

                        #region Start

                        if (UserSettings.All.UsePreStart)
                        {
                            Title = $"ScreenToGif ({LocalizationHelper.Get("S.Recorder.PreStart")} {UserSettings.All.PreStartValue}s)";
                            RecordPauseButton.IsEnabled = false;

                            Stage = Stage.PreStarting;
                            _preStartCount = UserSettings.All.PreStartValue - 1;

                            _preStartTimer.Start();
                        }
                        else
                        {
                            if (UserSettings.All.ShowCursor)
                            {
                                #region Show the cursor

                                if (UserSettings.All.AsyncRecording)
                                    _captureTimer.Tick += CursorAsync_Elapsed;
                                else
                                    _captureTimer.Tick += Cursor_Elapsed;

                                _captureTimer.Start();

                                //Manually capture the first frame on timelapse mode.
                                if (UserSettings.All.CaptureFrequency == CaptureFrequency.PerMinute || UserSettings.All.CaptureFrequency == CaptureFrequency.PerHour)
                                {
                                    if (UserSettings.All.AsyncRecording)
                                        CursorAsync_Elapsed(null, null);
                                    else
                                        Cursor_Elapsed(null, null);
                                }

                                Stage = Stage.Recording;
                                AutoFitButtons();

                                #endregion
                            }
                            else
                            {
                                #region Don't show the cursor

                                if (UserSettings.All.AsyncRecording)
                                    _captureTimer.Tick += NormalAsync_Elapsed;
                                else
                                    _captureTimer.Tick += Normal_Elapsed;

                                _captureTimer.Start();

                                //Manually capture the first frame on timelapse mode.
                                if (UserSettings.All.CaptureFrequency == CaptureFrequency.PerMinute || UserSettings.All.CaptureFrequency == CaptureFrequency.PerHour)
                                {
                                    if (UserSettings.All.AsyncRecording)
                                        NormalAsync_Elapsed(null, null);
                                    else
                                        Normal_Elapsed(null, null);
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

                        #region To pause

                        _captureTimer.Stop();
                        FrameRate.Stop();

                        Stage = Stage.Paused;
                        Title = LocalizationHelper.Get("S.Recorder.Paused");
                        FrequencyIntegerUpDown.IsEnabled = true;

                        DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                        AutoFitButtons();
                        break;

                    #endregion

                    case Stage.Paused:

                        #region To record again

                        Stage = Stage.Recording;
                        Title = "ScreenToGif";
                        FrequencyIntegerUpDown.IsEnabled = false;

                        DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);
                        
                        AutoFitButtons();

                        FrameRate.Start(HasFixedDelay(), GetFixedDelay());
                        
                        _captureTimer.Interval = GetCaptureInterval();
                        _captureTimer.Start();
                        break;

                        #endregion
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to start the recording.");
                ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.StartPauseNotPossible"), e.Message, e);
            }
        }

        private async Task Snap()
        {
            if (Project == null || Project.Frames.Count == 0)
            {
                try
                {
                    DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                    Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                    await PrepareNewCapture();

                    HeightIntegerBox.IsEnabled = false;
                    WidthIntegerBox.IsEnabled = false;
                    IsRecording = true;
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Impossible to start the screencasting.");
                    ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), ex.Message, ex);
                    return;
                }
            }

            #region Take the screenshot

            try
            {
                var limit = 0;
                do
                {
                    FrameCount = await _capture.ManualCaptureAsync(new FrameInfo(_recordClicked, _keyList), UserSettings.All.ShowCursor);

                    _keyList.Clear();

                    if (limit > 5)
                        throw new Exception("Impossible to capture the manual screenshot.");

                    limit++;
                }
                while (FrameCount == 0);
            }
            catch (Exception e)
            {
                HeightIntegerBox.IsEnabled = true;
                WidthIntegerBox.IsEnabled = true;
                IsRecording = false;

                LogWriter.Log(e, "Impossible to capture the manual screenshot.");
                ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible.Info"), e);
            }

            #endregion
        }

        /// <summary>
        /// Stops the recording or the Pre-Start countdown.
        /// </summary>
        private async Task Stop()
        {
            try
            {
                StopButton.IsEnabled = false;
                RecordPauseButton.IsEnabled = false;
                DiscardButton.IsEnabled = false;
                Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.Stopping");
                Cursor = Cursors.AppStarting;

                _captureTimer.Stop();
                FrameRate.Stop();
                
                if (_capture != null)
                    await _capture.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && Project?.Any == true)
                {
                    #region Stop

                    if (UserSettings.All.AsyncRecording)
                        _stopRequested = true;

                    await Task.Delay(100);

                    Close();

                    #endregion
                }
                else if (Stage == Stage.PreStarting || Stage == Stage.Snapping || Project?.Any != true)
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
                    FrequencyIntegerUpDown.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    HeightIntegerBox.IsEnabled = true;
                    WidthIntegerBox.IsEnabled = true;

                    IsRecording = false;
                    Topmost = true;

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
            finally
            {
                if (IsLoaded)
                {
                    Title = "ScreenToGif";
                    Cursor = Cursors.Arrow;
                    StopButton.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    DiscardButton.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (LowerGrid.ActualWidth < 360)
            {
                if (MinimizeVisibility == Visibility.Collapsed)
                    return;

                RecordPauseButton.Style = (Style)FindResource("Style.Button.NoText");
                StopButton.Style = RecordPauseButton.Style;
                DiscardButton.Style = RecordPauseButton.Style;

                MinimizeVisibility = Visibility.Collapsed;

                if (IsThin)
                    CaptionText.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (MinimizeVisibility == Visibility.Visible)
                    return;

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
            _captureTimer.Tick -= Normal_Elapsed;
            _captureTimer.Tick -= NormalAsync_Elapsed;

            _captureTimer.Tick -= Cursor_Elapsed;
            _captureTimer.Tick -= CursorAsync_Elapsed;
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
                    UpdateSize();
                    UpdateLocation();

                    WidthIntegerBox.Scale = _scale;
                    HeightIntegerBox.Scale = _scale;
                });
            }
            finally
            {
                GC.Collect(1);
            }
        }

        private void UpdateSize()
        {
            _width = (int)Math.Round((Width - Constants.HorizontalOffset) * _scale);
            _height = (int)Math.Round((Height - Constants.VerticalOffset) * _scale);

            if (_capture != null)
            {
                _capture.Width = _width;
                _capture.Height = _height;
            }
        }

        private void UpdateLocation()
        {
            //TestTextBlock.Text = $"{_left};{_top}";

            _left = (int)Math.Round((Math.Round(Left, MidpointRounding.AwayFromZero) + Constants.LeftOffset) * _scale);
            _top = (int)Math.Round((Math.Round(Top, MidpointRounding.AwayFromZero) + Constants.TopOffset) * _scale);

            if (_capture == null)
                return;

            _capture.Left = _left;
            _capture.Top = _top;
        }

        private bool UpdatePositioning(bool startup = false)
        {
            try
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

                Top = UserSettings.All.RecorderTop = top;
                Left = UserSettings.All.RecorderLeft = left;

                return true;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to position the recorder window.");
                return false;
            }
        }

        internal override void OnFollowingChanged()
        {
            Follow();

            base.OnFollowingChanged();
        }

        private void Follow()
        {
            if (IsFollowing && UserSettings.All.FollowShortcut != Key.None)
            {
                _followTimer.Interval = (1000 / UserSettings.All.LatestFps) / 2;
                _followTimer.Start();
                return;
            }

            _followTimer.Stop();
        }

        private async Task PrepareNewCapture()
        {
            if (_capture != null)
                await _capture.Dispose();

            if (UserSettings.All.UseDesktopDuplication)
            {
                //Check if Windows 8 or newer.
                if (!Util.Other.IsWin8OrHigher())
                    throw new Exception(LocalizationHelper.Get("S.Recorder.Warning.Windows8"));

                //Check if SharpDx is available.
                if (!Util.Other.IsSharpDxPresent())
                    throw new Exception(LocalizationHelper.Get("S.Recorder.Warning.MissingSharpDx"));

                _capture = GetDirectCapture();
            }
            else
            {
                //Capture with BitBlt.
                _capture = UserSettings.All.UseMemoryCache ? new CachedCapture() : new ImageCapture();
            }

            _capture.OnError += exception =>
            {
                Dispatcher?.Invoke(async () =>
                {
                    //Pause the recording and show the error.  
                    await RecordPause();

                    ErrorDialog.Ok("ScreenToGif", LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), exception.Message, exception);
                });
            };

            var dpi = Monitor.AllMonitors.FirstOrDefault(f => f.IsPrimary)?.Dpi ?? 96d;

            _capture.Start(GetCaptureInterval(), _left, _top, _width, _height, dpi / 96d, Project);
        }

        private ICapture GetDirectCapture()
        {
            return UserSettings.All.UseMemoryCache ? new DirectCachedCapture() : new DirectImageCapture();
        }

        private void DetectCaptureFrequency()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.Manual:
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Manual.Short");
                    FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                    FrequencyViewbox.Visibility = Visibility.Collapsed;
                    AdjustToManual();
                    break;
                case CaptureFrequency.PerSecond:
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Fps.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fps");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fps.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;

                case CaptureFrequency.PerMinute:
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Fpm.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;

                default: //PerHour.
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Fph.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fph");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fph.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;
            }
        }

        private void AdjustToManual()
        {
            Stage = Stage.Snapping;
            Title = "ScreenToGif";

            AutoFitButtons();
        }

        private void AdjustToAutomatic()
        {
            if (Project?.Frames?.Count > 0)
            {
                Stage = Stage.Paused;
                Title = LocalizationHelper.Get("S.Recorder.Paused");
            }
            else
            {
                Stage = Stage.Stopped;
                Title = "ScreenToGif";
            }

            AutoFitButtons();
            FrameRate.Stop();

            //Register the events
            UnregisterEvents();

            if (UserSettings.All.ShowCursor)
            {
                if (UserSettings.All.AsyncRecording)
                    _captureTimer.Tick += CursorAsync_Elapsed;
                else
                    _captureTimer.Tick += Cursor_Elapsed;
            }
            else
            {
                if (UserSettings.All.AsyncRecording)
                    _captureTimer.Tick += NormalAsync_Elapsed;
                else
                    _captureTimer.Tick += Normal_Elapsed;
            }
        }

        private int GetCaptureInterval()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.PerHour: //15 frames per hour = 240,000 ms (240 sec, 4 min).
                    return (1000 * 60 * 60) / FrequencyIntegerUpDown.Value;

                case CaptureFrequency.PerMinute: //15 frames per minute = 4,000 ms (4 sec).
                    return (1000 * 60) / FrequencyIntegerUpDown.Value;

                default: //PerSecond. 15 frames per second = 66 ms.
                    return 1000 / FrequencyIntegerUpDown.Value;
            }
        }

        private bool HasFixedDelay()
        {
            return UserSettings.All.CaptureFrequency != CaptureFrequency.PerSecond || UserSettings.All.FixedFrameRate;
        }

        private int GetFixedDelay()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.Manual:
                    return UserSettings.All.PlaybackDelayManual;
                case CaptureFrequency.PerMinute:
                    return UserSettings.All.PlaybackDelayMinute;
                case CaptureFrequency.PerHour:
                    return UserSettings.All.PlaybackDelayHour;
                default: //When the capture is 'PerSecond', the fixed delay is set to use the current framerate.
                    return 1000 / FrequencyIntegerUpDown.Value;
            }
        }

        private void DetectThinMode()
        {
            //Updates the Offsets of the two controls (because it's a static property, it will not update by itself).
            HeightIntegerBox.Offset = Constants.VerticalOffset;
            WidthIntegerBox.Offset = Constants.HorizontalOffset;
        }

        #endregion

        #region Other Events

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            _scale = e.NewDpi.DpiScaleX;

            UpdateSize();
            UpdateLocation();

            WidthIntegerBox.Scale = _scale;
            HeightIntegerBox.Scale = _scale;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();

            AutoFitButtons();
        }

        private void CommandGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();

            //Dispatcher?.BeginInvoke(new Action(DragMove));
            //await Task.Factory.StartNew(() => Dispatcher.Invoke(DragMove));
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            UpdateLocation();
        }

        private async void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (Stage == Stage.Recording)
                    await RecordPause();
                else if (Stage == Stage.PreStarting)
                    await Stop();

                GC.Collect();
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs eventArgs)
        {
            UpdatePositioning();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Save Settings
            UserSettings.All.RecorderTop = Top;
            UserSettings.All.RecorderLeft = Left;
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

                _captureTimer.Stop();
                _captureTimer.Dispose();
            }

            //Garbage Collector Timer.
            _garbageTimer?.Stop();
            _followTimer?.Stop();

            #endregion

            //Clean all capture resources.
            if (_capture != null)
                await _capture.Dispose();

            GC.Collect();
        }

        #endregion
    }
}