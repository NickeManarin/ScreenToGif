using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.Capture;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Windows
{
    public partial class NewRecorder : RecorderWindow
    {
        //This window is just the main recorder controls:
        //  Record/Pause or Capture single frame
        //  Stop
        //  Discard (Only shown when paused)
        //  ---
        //  Select area
        //      Maybe type of selection stays active on top?
        //  FPS
        //  Options (opens directly the recorder entry selected)
        //      Maybe add quick options such as snapshot mode, and which others?
        //  Maybe add option for compact mode?

        //When selecting the screen area to be recorded:
        //  Free area mode:
        //      Open a selector window for each screen and let it rest on each screen area.
        //      The selector is basically that transparent canvas which lets you click + drag to draw rectangles.
        //  Window mode:
        //      Same as the free area.
        //      Detect each window which is present inside each monitor and display a rectangle there.
        //  Screen mode:
        //      Same as before.

        //When capturing:
        //  If there no space on screen to put the UI
        //      Minimize the UI
        //      Warn the user that the recording can be stoped by pressing the shortcut or by restoring the UI into view.

        #region Variables

        /// <summary>
        /// 
        /// </summary>
        private readonly RegionSelection _regionSelection = new RegionSelection();

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// True when the user stop the recording. 
        /// </summary>
        private bool _stopRequested;

        /// <summary>
        /// Deals with all screen capture methods.
        /// </summary>
        private ICapture _capture;

        /// <summary>
        /// When the recording is async and was stopped, it needs to wait for the last task to finish.
        /// </summary>
        private Task<int> _captureTask;

        private Timer _captureTimer = new Timer();

        #endregion

        #region Dependency Properties

        //public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register(nameof(IsPickingRegion), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        //public static readonly DependencyProperty WasRegionPickedProperty = DependencyProperty.Register(nameof(WasRegionPicked), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        //public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        //public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        //public static readonly DependencyProperty IsFollowingProperty = DependencyProperty.Register(nameof(IsFollowing), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false, IsFollowing_PropertyChanged));
        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register(nameof(Region), typeof(Rect), typeof(NewRecorder), new PropertyMetadata(Rect.Empty));

        #endregion

        #region Properties

        //public bool IsPickingRegion
        //{
        //    get => (bool)GetValue(IsPickingRegionProperty);
        //    set => SetValue(IsPickingRegionProperty, value);
        //}

        //public bool WasRegionPicked
        //{
        //    get => (bool)GetValue(WasRegionPickedProperty);
        //    set => SetValue(WasRegionPickedProperty, value);
        //}


        //public bool IsRecording
        //{
        //    get => (bool)GetValue(IsRecordingProperty);
        //    set => SetValue(IsRecordingProperty, value);
        //}

        //public bool IsDragging
        //{
        //    get => (bool)GetValue(IsDraggingProperty);
        //    set => SetValue(IsDraggingProperty, value);
        //}

        //public bool IsFollowing
        //{
        //    get => (bool)GetValue(IsFollowingProperty);
        //    set => SetValue(IsFollowingProperty, value);
        //}

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
                if (Region.IsEmpty)
                    return Region;

                var region = Region;
                region.Offset(Left, Top);
                return region;
            }
        }

        #endregion

        public NewRecorder()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await UpdatePositioning(true);
        }

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var wasSnapshot = UserSettings.All.SnapshotMode;

            try
            {
                Topmost = false;

                var options = new Options(Options.RecorderIndex);
                options.ShowDialog();
            }
            finally
            {
                Topmost = true;
            }

            //Enables or disables the snapshot mode.
            if (wasSnapshot != UserSettings.All.SnapshotMode)
                AdjustToCaptureMode();
        }

        private void HeaderGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            #region Validate

            if (Stage != Stage.Stopped && Stage != Stage.Snapping)
                return;

            #endregion

            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _regionSelection.Close();
        }


        private async void RegionButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(SelectControl2.ModeType.Region);
        }

        private async void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(SelectControl2.ModeType.Window);
        }

        private async void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(SelectControl2.ModeType.Fullscreen);
        }


        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #region Timers

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
            FrameCount = _capture.Capture(new FrameInfo(_recordClicked, _keyList));

            _keyList.Clear();
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            FrameCount = _capture.CaptureWithCursor(new FrameInfo(_recordClicked, _keyList));

            _keyList.Clear();
        }

        #endregion

        #region Methods

        private async Task UpdatePositioning(bool startup = false)
        {
            var monitors = Monitor.AllMonitorsGranular();

            if (!startup)
            {
                #region When the recorder was already opened
                
                //When in selection mode, cancel selection.
                if (RegionSelectHelper.IsSelecting)
                    RegionSelectHelper.Abort();

                //TODO: When discarding or other stages?
                if (Stage == Stage.PreStarting)
                    await Stop();
                else if (Stage == Stage.Recording)
                    await RecordPause();

                if (Stage == Stage.Paused || Stage == Stage.Stopped)
                {
                    //Move region to the closest screen.
                    //TODO: What if the region was bigger than now?
                    Region = MoveToClosestScreen();
                }

                #endregion
            }
            else
            {
                #region Previously selected region

                //If a region was previously selected.
                if (!UserSettings.All.SelectedRegion.IsEmpty)
                {
                    //Check if the previous selection can be positioned inside a screen.
                    if (monitors.Any(x => x.Bounds.Contains(UserSettings.All.SelectedRegion)))
                        Region = UserSettings.All.SelectedRegion;
                }

                #endregion
            }

            #region Adjust the position of the main controls

            if (Region.IsEmpty)
            {
                //TODO: Scale.
                var screen = monitors.FirstOrDefault(x => x.Bounds.Contains(Native.GetMousePosition(1, Left, Top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

                if (screen == null)
                    throw new Exception("It was not possible to get a list of known screens.");

                Left = screen.WorkingArea.Left + screen.WorkingArea.Width / 2 - RecorderWindow.ActualWidth / 2;
                Top = screen.WorkingArea.Top + screen.WorkingArea.Height / 2 - RecorderWindow.ActualHeight / 2;
            }
            else
            {
                MoveCommandPanel();
            }

            #endregion
        }

        private void UnregisterEvents()
        {
            _captureTimer.Tick -= Normal_Elapsed;
            _captureTimer.Tick -= NormalAsync_Elapsed;

            _captureTimer.Tick -= Cursor_Elapsed;
            _captureTimer.Tick -= CursorAsync_Elapsed;
        }

        internal async Task RecordPause()
        {

        }

        private async Task Snap()
        {

        }

        private async Task Stop()
        {

        }

        /// <summary>
        /// If the capture mode was changed, the recorder must take different actions.
        /// </summary>
        private void AdjustToCaptureMode()
        {
            if (UserSettings.All.SnapshotMode)
            {
                #region Snapshot mode

                //Set to Snapshot Mode, change the text of the record button to "Snap" and every press of the button, takes a screenshot.
                Stage = Stage.Snapping;
                Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.Snapshot");

                #endregion
            }
            else
            {
                #region Normal mode

                if (_capture != null)
                    _capture.SnapDelay = null;

                if (Project.Frames?.Count > 0)
                {
                    Stage = Stage.Paused;
                    Title = LocalizationHelper.Get("S.Recorder.Paused");

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

                #endregion

                #endregion
            }
        }

        private async Task PickRegion(SelectControl2.ModeType mode)
        {
            _regionSelection.Hide();
            Hide();

            //TODO: What happens if the selection is aborted?
            var region = await RegionSelectHelper.Select(mode, Region);

            if (region != Rect.Empty)
                Region = region;

            DisplaySelection();
            MoveCommandPanel();
            Show();
        }

        private void DisplaySelection()
        {
            if (Region.IsEmpty)
            {
                if (_regionSelection.IsVisible)
                    _regionSelection.Hide();
            }
            else
            {
                _regionSelection.Select(Region);
            }
        }

        private void MoveCommandPanel()
        {

        }

        /// <summary>
        /// Move the selection region to the closest screen when outside of any.
        /// </summary>
        private Rect MoveToClosestScreen()
        {
            //If the position was never set.
            if (Region.IsEmpty)
                return Rect.Empty;

            var top = Region.Top;
            var left = Region.Left;

            //TODO: Take into consideration that each screen has it's own DPI.
            //Will the size change?

            ////The catch here is to get the closest monitor from current Top/Left point. 
            //var monitors = Monitor.AllMonitorsScaled(this.Scale());
            //var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new System.Windows.Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

            //if (closest == null)
            //    throw new Exception("It was not possible to move the current selected region to the closest monitor.");

            ////To much to the Left.
            //if (closest.WorkingArea.Left > Region.Left + Region.Width - 100)
            //    left = closest.WorkingArea.Left;

            ////Too much to the top.
            //if (closest.WorkingArea.Top > Region.Top + Region.Height - 100)
            //    top = closest.WorkingArea.Top;

            ////Too much to the right.
            //if (closest.WorkingArea.Right < Region.Left + 100)
            //    left = closest.WorkingArea.Right - Region.Width;

            ////Too much to the bottom.
            //if (closest.WorkingArea.Bottom < Region.Top + 100)
            //    top = closest.WorkingArea.Bottom - Region.Height;

            return new Rect(left, top, Region.Width, Region.Height);
        }

        #endregion
    }
}