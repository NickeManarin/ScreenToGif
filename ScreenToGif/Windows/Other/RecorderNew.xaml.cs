using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Model;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Drawing.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Monitor = ScreenToGif.Util.Monitor;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows.Other
{
    public partial class RecorderNew : Window
    {
        #region Variables

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        /// <summary>
        /// The project information about the current recording.
        /// </summary>
        internal ProjectInfo Project { get; set; }

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// The delay of each frame took as snapshot.
        /// </summary>
        private int? _snapDelay = null;

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _scale = 1;

        /// <summary>
        /// The rectangle to record.
        /// </summary>
        private Rect _rect = Rect.Empty;

        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;

        /// <summary>
        /// True when the user stop the recording. 
        /// </summary>
        private bool _stopRequested;

        private Task<Image> _captureTask;

        /// <summary>
        /// The action to be executed after closing this Window.
        /// </summary>
        public ExitAction ExitArg = ExitAction.Return;

        private Point _latestPosition;

        #region Timer

        private Timer _capture = new Timer();

        private readonly System.Timers.Timer _garbageTimer = new System.Timers.Timer();

        private readonly Timer _preStartTimer = new Timer();

        #endregion

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register("IsPickingRegion", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty WasRegionPickedProperty = DependencyProperty.Register("WasRegionPicked", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register("IsRecording", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register("IsDragging", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register("Region", typeof(Rect), typeof(RecorderNew),
            new PropertyMetadata(Rect.Empty));

        public static readonly DependencyProperty StageProperty = DependencyProperty.Register("Stage", typeof(Stage), typeof(RecorderNew),
            new FrameworkPropertyMetadata(Stage.Stopped));

        public static readonly DependencyProperty FrameCountProperty = DependencyProperty.Register("FrameCount", typeof(int), typeof(RecorderNew),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region Properties

        public bool IsPickingRegion
        {
            get => (bool)GetValue(IsPickingRegionProperty);
            set => SetValue(IsPickingRegionProperty, value);
        }

        public bool WasRegionPicked
        {
            get => (bool)GetValue(WasRegionPickedProperty);
            set => SetValue(WasRegionPickedProperty, value);
        }

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage
        {
            get => (Stage)GetValue(StageProperty);
            set => SetValue(StageProperty, value);
        }

        public bool IsRecording
        {
            get => (bool)GetValue(IsRecordingProperty);
            set => SetValue(IsRecordingProperty, value);
        }

        public bool IsDragging
        {
            get => (bool)GetValue(IsDraggingProperty);
            set => SetValue(IsDraggingProperty, value);
        }

        /// <summary>
        /// Get or set the selected region in window coordinates.
        /// </summary>
        public Rect Region
        {
            get => (Rect)GetValue(RegionProperty);
            set => SetValue(RegionProperty, value);
        }

        /// <summary>
        /// Get the selected region in screen coordinates.
        /// </summary>
        public Rect ScreenRegion
        {
            get
            {
                var region = Region;
                region.Offset(Left, Top);
                return region;
            }
        }

        /// <summary>
        /// The frame count of the current recording.
        /// </summary>
        [Bindable(true), Category("Common"), Description("The frame count of the current recording.")]
        public int FrameCount
        {
            get => (int)GetValue(FrameCountProperty);
            set => SetValue(FrameCountProperty, value);
        }

        #endregion

        public RecorderNew(bool hideBackButton = false)
        {
            InitializeComponent();

            BackButton.Visibility = hideBackButton ? Visibility.Collapsed : Visibility.Visible;

            #region Fill entire working space

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

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

            //SystemEvents.DisplaySettingsChanged += (sender, args) => { Dialog.Ok("a", "b", "v"); };
        }

        #region Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Factory.StartNew(UpdateScreenDpi);

            Region = UserSettings.All.SelectedRegion;

            if (!Region.IsEmpty)
                WasRegionPicked = true;

            #region Center the main UI

            var screen = Monitor.AllMonitorsScaled(_scale).FirstOrDefault(x => x.Bounds.Contains(Native.GetMousePosition(_scale))) ??
                Monitor.AllMonitorsScaled(_scale).FirstOrDefault(x => x.IsPrimary);

            if (screen != null)
            {
                //Update the main UI size.
                MainBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                MainBorder.Arrange(new Rect(MainBorder.DesiredSize));

                //Coordinates in window are all positive. So the screen points should minus the window location.
                Canvas.SetLeft(MainBorder, (screen.WorkingArea.Left + screen.WorkingArea.Width / 2) - (MainBorder.ActualWidth / 2) - Left);
                Canvas.SetTop(MainBorder, screen.WorkingArea.Top + screen.WorkingArea.Height / 2 - MainBorder.ActualHeight / 2 - Top);

                AdjustControls();

                MainCanvas.Visibility = Visibility.Visible;
            }

            #endregion

            #region Garbage collector

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

            #endregion

            #region If Snapshot

            if (UserSettings.All.SnapshotMode)
                EnableSnapshot_Executed(null, null);

            #endregion
        }
        
        private void Window_StateChanged(object sender, EventArgs e)
        {
            //TODO: Detect that the window was minimized before. E01
            if (WindowState != WindowState.Minimized && Stage == Stage.Recording && SelectControl.Mode == SelectControl.ModeType.Fullscreen)
            {
                RecordPauseButton_Click(sender, null);
                Topmost = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            #region Validate

            if (Stage != Stage.Stopped && Stage != Stage.Snapping)
                return;

            #endregion

            DialogResult = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            #region Validate

            if (Stage != Stage.Stopped && Stage != Stage.Snapping)
                return;

            #endregion

            Close();
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var draggableControl = sender as Border;

            if (draggableControl == null)
                return;

            IsDragging = true;

            _latestPosition = e.GetPosition(MainBorder);
            draggableControl.CaptureMouse();

            e.Handled = true;
        }

        private void MainBorder_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as Border;

            if (!IsDragging || draggableControl == null) return;

            var currentPosition = e.GetPosition(Parent as UIElement);

            //TODO: Validate position.
            //TODO: Detect video options change, reposition the MainBorder.

            Canvas.SetLeft(MainBorder, currentPosition.X - _latestPosition.X);
            Canvas.SetTop(MainBorder, currentPosition.Y - _latestPosition.Y);

            e.Handled = true;
        }

        private void MainBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsDragging = false;

            var draggable = sender as Border;
            draggable?.ReleaseMouseCapture();

            e.Handled = true;
        }


        private void RegionButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion(SelectControl.ModeType.Region);
        }

        private void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion(SelectControl.ModeType.Window);
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion(SelectControl.ModeType.Fullscreen);
        }


        private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            UserSettings.All.SelectedRegion = Region = SelectControl.Selected;

            WasRegionPicked = true;

            AdjustControls();
        }

        private void SelectControl_SelectionCanceled(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            AdjustControls();
        }


        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (Stage == Stage.SelectingRegion || WindowState == WindowState.Minimized || Region.IsEmpty || !WasRegionPicked)
                return;

            if (Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
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
            Stage = Stage.Stopped;

            //OutterGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }

        private void EnableSnapshot_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Stage == Stage.Stopped || Stage == Stage.Snapping || Stage == Stage.Paused) && RecordControlsGrid.IsVisible;
        }


        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var wasSnapshot = UserSettings.All.SnapshotMode;

            var options = new Options();
            options.ShowDialog();

            //Enables or disables the snapshot mode.
            if (wasSnapshot != UserSettings.All.SnapshotMode)
                EnableSnapshot_Executed(sender, e);
        }

        private void EnableSnapshot_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (UserSettings.All.SnapshotMode)
            {
                #region SnapShot Recording

                //Set to Snapshot Mode, change the text of the record button to "Snap" and every press of the button, takes a screenshot.
                Stage = Stage.Snapping;
                Title = "ScreenToGif - " + FindResource("Recorder.Snapshot");

                if (Project == null || Project.Frames.Count == 0)
                    Project = new ProjectInfo().CreateProjectFolder();

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

        private void Window_Closing(object sender, CancelEventArgs e)
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

            //SystemEvents.PowerModeChanged -= System_PowerModeChanged;

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
        
        #region Methods

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!IsPickingRegion)
                    Close();

                IsPickingRegion = false;
            }
            else if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                SelectControl.Accept();
            }

            base.OnKeyDown(e);
        }

        private void PickRegion(SelectControl.ModeType mode)
        {
            SelectControl.Mode = mode;
            SelectControl.BackImage = CaptureBackground();
            
            //Reset the values.
            SelectControl.Scale = _scale;
            SelectControl.Retry();

            if (SelectControl.Mode == SelectControl.ModeType.Region)
                SelectControl.Selected = Region;

            IsPickingRegion = true;
        }

        private void EndPickRegion()
        {
            IsPickingRegion = false;
        }

        private void AdjustControls()
        {
            //Maybe animate the movement.
            //await Task.Delay(1000);

            var monitors = Monitor.AllMonitors;

            var bottom = new Rect(Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2, Region.Bottom + 10, MainBorder.ActualWidth, MainBorder.ActualHeight);
            var top = new Rect(Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2, Region.Top - MainBorder.ActualHeight - 10, MainBorder.ActualWidth, MainBorder.ActualHeight);
            var left = new Rect(Region.Left - MainBorder.ActualWidth - 10, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2, MainBorder.ActualWidth, MainBorder.ActualHeight);
            var right = new Rect(Region.Right + 10, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2, MainBorder.ActualWidth, MainBorder.ActualHeight);

            if (SystemParameters.VirtualScreenHeight - (Region.Top + Region.Height) > 100 && monitors.Any(x => x.Bounds.Contains(bottom)))
            {
                //Show at the bottom of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                Canvas.SetTop(MainBorder, Region.Bottom + 10);
                return;
            }

            if (Region.Top > 100 && monitors.Any(x => x.Bounds.Contains(top)))
            {
                //Show on top of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                Canvas.SetTop(MainBorder, Region.Top - MainBorder.ActualHeight - 10);
                return;
            }

            if (Region.Left > 100 && monitors.Any(x => x.Bounds.Contains(left)))
            {
                //Show to the left of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left - MainBorder.ActualWidth - 10);
                Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                return;
            }

            if (SystemParameters.VirtualScreenWidth - (Region.Left + Region.Width) > 100 && monitors.Any(x => x.Bounds.Contains(right)))
            {
                //Show to the right of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Right + 10);
                Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                return;
            }

            if (Region.Width > 100 && Region.Height > 100)
            {
                //Show inside the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
            }
        }

        private BitmapSource CaptureBackground()
        {
            //A 7 pixel border is added to allow the crop by the magnifying glass.
            return Native.CaptureBitmapSource((int)Math.Round((Width + 14) * _scale), (int)Math.Round((Height + 14) * _scale), 
                (int)Math.Round((Left - 7) * _scale), (int)Math.Round((Top - 7) * _scale));
        }

        private void UnregisterEvents()
        {
            _capture.Tick -= Normal_Elapsed;
            _capture.Tick -= NormalAsync_Elapsed;

            _capture.Tick -= Cursor_Elapsed;
            _capture.Tick -= CursorAsync_Elapsed;
        }

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private async void RecordPause()
        {
            switch (Stage)
            {
                case Stage.Stopped:

                    #region To Record

                    _capture = new Timer { Interval = 1000 / FpsIntegerUpDown.Value };
                    _snapDelay = null;

                    Project = new ProjectInfo().CreateProjectFolder();

                    _keyList.Clear();
                    FrameCount = 0;

                    await Task.Factory.StartNew(UpdateScreenDpi);

                    _rect = ScreenRegion.Scale(_scale).Offset(Util.Other.RoundUpValue(_scale));

                    FpsIntegerUpDown.IsEnabled = false;

                    IsRecording = true;
                    Topmost = true;

                    //TODO: Adjust fullscreen recording usability.
                    //TODO: Detect that the window needs to be minimized. E01
                    if (SelectControl.Mode == SelectControl.ModeType.Fullscreen)
                    {
                        WindowState = WindowState.Minimized;
                        Topmost = false;
                    }

                    FrameRate.Start(_capture.Interval);
                    UnregisterEvents();

                    ReselectSplitButton.BeginStoryboard(this.FindStoryboard("HideReselectStoryboard"), HandoffBehavior.Compose);

                    #region Start

                    if (UserSettings.All.UsePreStart)
                    {
                        Title = $"ScreenToGif ({FindResource("Recorder.PreStart")} {UserSettings.All.PreStartValue}s)";
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

                    DiscardButton.BeginStoryboard(this.FindStoryboard("ShowDiscardStoryboard"), HandoffBehavior.Compose);

                    _capture.Stop();

                    FrameRate.Stop();
                    break;

                #endregion

                case Stage.Paused:

                    #region To Record Again

                    Stage = Stage.Recording;
                    Title = "ScreenToGif";

                    DiscardButton.BeginStoryboard(this.FindStoryboard("HideDiscardStoryboard"), HandoffBehavior.Compose);

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
                _rect = ScreenRegion.Scale(_scale).Offset(Util.Other.RoundUpValue(_scale));

                ReselectSplitButton.BeginStoryboard(this.FindStoryboard("HideReselectStoryboard"), HandoffBehavior.Compose);
                DiscardButton.BeginStoryboard(this.FindStoryboard("ShowDiscardStoryboard"), HandoffBehavior.Compose);

                Project = new ProjectInfo().CreateProjectFolder();

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

                    ExitArg = ExitAction.Recorded;
                    DialogResult = false;

                    #endregion
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !Project.Any)
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    //Only returns to the stopped stage if it was recording.
                    Stage = Stage == Stage.Snapping ? Stage.Snapping : Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    FpsIntegerUpDown.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;

                    IsRecording = false;
                    Topmost = true;

                    ReselectSplitButton.BeginStoryboard(this.FindStoryboard("ShowReselectStoryboard"), HandoffBehavior.Compose);

                    Title = "ScreenToGif";

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

        private void UpdateScreenDpi()
        {
            try
            {
                var source = Dispatcher.Invoke(() => PresentationSource.FromVisual(this));

                if (source?.CompositionTarget != null)
                    _scale = Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);
            }
            finally
            {
                GC.Collect(1);
            }
        }

        private void StageUpdate()
        {
            switch (Stage)
            {
                case Stage.Stopped:

                    break;
                case Stage.SelectingRegion:

                    break;
                case Stage.PreStarting:

                    break;
                case Stage.Snapping:

                    break;
                case Stage.Recording:

                    break;
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
                RecordControlsGrid.IsEnabled = true;

                Cursor = Cursors.Arrow;
                IsRecording = false;

                DiscardButton.BeginStoryboard(this.FindStoryboard("HideDiscardStoryboard"), HandoffBehavior.Compose);
                ReselectSplitButton.BeginStoryboard(this.FindStoryboard("ShowReselectStoryboard"), HandoffBehavior.Compose);

                if (!UserSettings.All.SnapshotMode)
                {
                    //Only display the Record text when not in snapshot mode. 
                    Title = "ScreenToGif";
                }
                else
                {
                    Stage = Stage.Snapping;
                    EnableSnapshot_Executed(null, null);
                }
            });

            GC.Collect();
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
                Title = "ScreenToGif";
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

                    #endregion
                }
            }
        }


        private async void NormalAsync_Elapsed(object sender, EventArgs e)
        {
            //Take a screenshot of the area.
            _captureTask = Task.Factory.StartNew(() => Native.Capture(_rect.Size, (int)_rect.X, (int)_rect.Y));

            var bt = await _captureTask;

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), new List<SimpleKeyGesture>(_keyList)));

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
            _captureTask = Task.Factory.StartNew(() => Native.CaptureWithCursor(_rect.Size, (int)_rect.X, (int)_rect.Y, out cursorPosX, out cursorPosY), TaskCreationOptions.PreferFairness);

            var bt = await _captureTask;

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            if (!RecordControlsGrid.IsVisible)
                return;

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), cursorPosX, cursorPosY, _recordClicked, new List<SimpleKeyGesture>(_keyList)));

            _keyList.Clear();

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }


        private void Normal_Elapsed(object sender, EventArgs e)
        {
            //Take a screenshot of the area.
            var bt = Native.Capture(_rect.Size, (int)_rect.X, (int)_rect.Y);

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), new List<SimpleKeyGesture>(_keyList)));

            _keyList.Clear();

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            var bt = Native.CaptureWithCursor(_rect.Size, (int)_rect.X, (int)_rect.Y, out int cursorPosX, out int cursorPosY);

            if (bt == null || !IsLoaded)
                return;

            var fileName = $"{Project.FullPath}{FrameCount}.png";

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay), cursorPosX, cursorPosY, _recordClicked, new List<SimpleKeyGesture>(_keyList)));

            _keyList.Clear();

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(bt)); });

            FrameCount++;
        }


        private void GarbageTimer_Tick(object sender, EventArgs e)
        {
            GC.Collect(UserSettings.All.LatestFps > 30 ? 6 : 2);
        }

        #endregion
    }
}