using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Windows.Other;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Encoder = ScreenToGif.Windows.Other.Encoder;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Size = System.Windows.Size;
using System.Windows.Data;
using System.Windows.Media.Effects;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.UserControls;
using ScreenToGif.ViewModel;
using ScreenToGif.ViewModel.Tasks;
using VideoSource = ScreenToGif.Windows.Other.VideoSource;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util.Codification.Apng;
using ScreenToGif.Util.Codification.Gif.Decoder;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.ExportPresets;
using ScreenToGif.ViewModel.ExportPresets.Image;
using ScreenToGif.ViewModel.ExportPresets.Other;

namespace ScreenToGif.Windows
{
    public partial class Editor : Window, INotification, IEncoding
    {
        #region Properties

        public static readonly DependencyProperty FilledListProperty = DependencyProperty.Register(nameof(FilledList), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty NotPreviewingProperty = DependencyProperty.Register(nameof(NotPreviewing), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty TotalDurationProperty = DependencyProperty.Register(nameof(TotalDuration), typeof(TimeSpan), typeof(Editor));
        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register(nameof(CurrentTime), typeof(TimeSpan), typeof(Editor));
        public static readonly DependencyProperty FrameSizeProperty = DependencyProperty.Register(nameof(FrameSize), typeof(Size), typeof(Editor));
        public static readonly DependencyProperty FrameScaleProperty = DependencyProperty.Register(nameof(FrameScale), typeof(int), typeof(Editor));
        public static readonly DependencyProperty AverageDelayProperty = DependencyProperty.Register(nameof(AverageDelay), typeof(double), typeof(Editor));
        public static readonly DependencyProperty FrameDpiProperty = DependencyProperty.Register(nameof(FrameDpi), typeof(double), typeof(Editor));
        public static readonly DependencyProperty IsCancelableProperty = DependencyProperty.Register(nameof(IsCancelable), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty HasImprecisePlaybackProperty = DependencyProperty.Register(nameof(HasImprecisePlayback), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// True if there is a value inside the list of frames.
        /// </summary>
        public bool FilledList
        {
            get => (bool)GetValue(FilledListProperty);
            set => SetValue(FilledListProperty, value);
        }

        /// <summary>
        /// True if not in preview mode.
        /// </summary>
        public bool NotPreviewing
        {
            get => (bool)GetValue(NotPreviewingProperty);
            set => SetValue(NotPreviewingProperty, value);
        }

        /// <summary>
        /// True if loading frames.
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// The total duration of the animation. Used by the statistics tab.
        /// </summary>
        private TimeSpan TotalDuration
        {
            get => (TimeSpan)GetValue(TotalDurationProperty);
            set => SetValue(TotalDurationProperty, value);
        }

        /// <summary>
        /// The cumulative duration of the animation. Used by the statistics tab.
        /// </summary>
        private TimeSpan CurrentTime
        {
            get => (TimeSpan)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        /// <summary>
        /// The size of the frames. Used by the statistics tab.
        /// </summary>
        private Size FrameSize
        {
            get => (Size)GetValue(FrameSizeProperty);
            set => SetValue(FrameSizeProperty, value);
        }

        /// <summary>
        /// The scale of the frames in %. Used by the statistics tab.
        /// </summary>
        private int FrameScale
        {
            get => (int)GetValue(FrameScaleProperty);
            set => SetValue(FrameScaleProperty, value);
        }

        /// <summary>
        /// The average delay of the animation. Used by the statistics tab.
        /// </summary>
        private double AverageDelay
        {
            get => (double)GetValue(AverageDelayProperty);
            set => SetValue(AverageDelayProperty, value);
        }

        /// <summary>
        /// The DPI of the frames. Used by the statistics tab.
        /// </summary>
        private double FrameDpi
        {
            get => (double)GetValue(FrameDpiProperty);
            set => SetValue(FrameDpiProperty, value);
        }

        /// <summary>
        /// True if the current recording being loaded can be cancelled.
        /// </summary>
        public bool IsCancelable
        {
            get => (bool)GetValue(IsCancelableProperty);
            set => SetValue(IsCancelableProperty, value);
        }

        /// <summary>
        /// True if the system can't playback the animation at the correct speed.
        /// </summary>
        public bool HasImprecisePlayback
        {
            get => (bool)GetValue(HasImprecisePlaybackProperty);
            set => SetValue(HasImprecisePlaybackProperty, value);
        }

        #endregion

        #region Variables

        /// <summary>
        /// The current project.
        /// </summary>
        public ProjectInfo Project { get; set; }

        /// <summary>
        /// Last selected frame index. Used to track users last selection and decide which frame to show.
        /// </summary>
        private int LastSelected { get; set; } = -1;

        /// <summary>
        /// True if the user was selecting frames using the FirstFrame/Previous/Next/LastFrame commands or the scroll wheel.
        /// </summary>
        private bool WasChangingSelection { get; set; }

        /// <summary>
        /// True if the user was previewing the recording.
        /// </summary>
        private bool WasPreviewing { get; set; }

        /// <summary>
        /// True if the PC is sleeping.
        /// </summary>
        private bool Slept { get; set; }

        /// <summary>
        /// True if this is the encoder window.
        /// </summary>
        public bool IsEncoderWindow { get; } = false;

        private readonly EditorViewModel _viewModel;

        private CancellationTokenSource _previewToken;

        private Action<object, RoutedEventArgs> _applyAction = null;

        private bool _abortLoading;

        /// <summary>
        /// Lock used to prevent firing multiple times (at the same time) both the Activated/Deactivated events.
        /// </summary>
        public static readonly object ActivateLock = new object();

        #endregion

        public Editor()
        {
            InitializeComponent();

            DataContext = _viewModel = new EditorViewModel();
        }

        #region Main Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += System_DisplaySettingsChanged;
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;

            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning())
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion

            ScrollSynchronizer.SetScrollGroup(ZoomBoxControl.GetScrollViewer(), "Canvas");
            ScrollSynchronizer.SetScrollGroup(MainScrollViewer, "Canvas");
            ScrollSynchronizer.SetScrollGroup(BehindScrollViewer, "Canvas");

            DisplayUpdatePromoter();

            #region Load

            if (Project != null)
            {
                ShowProgress(LocalizationHelper.Get("S.Editor.Preparing"), Project.Frames.Count, true);

                Cursor = Cursors.AppStarting;
                IsLoading = true;
                IsCancelable = true;

                ActionStack.Project = Project;

                var result = await Task.Run(Load);
                LoadCallback(result);

                return;
            }

            #endregion

            //Open with...
            LoadFromArguments();
            Arguments.ClearAutomationArgs();

            RibbonTabControl.SelectedIndex = 0;

            WelcomeTextBlock.Text = LocalizationHelper.Get(Humanizer.WelcomeInfo());
            SymbolTextBlock.Text = Humanizer.Welcome();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            lock (ActivateLock)
            {
                RibbonTabControl.UpdateVisual();

                //Returns the preview if was playing before the deactivation of the window.
                if (WasPreviewing)
                {
                    WasPreviewing = false;
                    PlayPause();
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            lock (ActivateLock)
            {
                try
                {
                    //Debug.WriteLine("Deactivated");
                    RibbonTabControl.UpdateVisual(false);

                    //Pauses the recording preview.
                    if (_previewToken != null)
                    {
                        WasPreviewing = true;
                        Pause();
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Exception when losing focus on window.");
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
                e.Handled = true;
        }

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            if (Math.Abs(e.NewDpi.PixelsPerInchX - e.OldDpi.PixelsPerInchX) < 0.01)
                return;

            ZoomBoxControl.RefreshImage();

            Cancel_Executed(sender, null);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //TODO: What if there's any processing happening? I need to try to stop.

            Pause();
            ClosePanel();

            if (Project != null && Project.Any)
            {
                Project.Persist();

                if (UserSettings.All.NotifyWhileClosingEditor && !Dialog.Ask(LocalizationHelper.Get("S.Editor.Exiting.Title"), LocalizationHelper.Get("S.Editor.Exiting.Instruction"),
                        LocalizationHelper.Get(UserSettings.All.AutomaticCleanUp ? "S.Editor.Exiting.Message2" : "S.Editor.Exiting.Message")))
                {
                    e.Cancel = true;
                    return;
                }

                Project.Clear();

                //Remove the ActionStack.
                ActionStack.Clear();
            }

            //Manually get the position/size of the window, so it's possible opening multiple instances.
            UserSettings.All.EditorTop = Top;
            UserSettings.All.EditorLeft = Left;
            UserSettings.All.EditorWidth = Width;
            UserSettings.All.EditorHeight = Height;
            UserSettings.All.EditorWindowState = WindowState;
            UserSettings.Save();

            Encoder.TryClose();

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged -= System_DisplaySettingsChanged;
            SystemParameters.StaticPropertyChanged -= SystemParameters_StaticPropertyChanged;
        }


        private void ZoomBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt)
            {
                #region Translate the Element (Scroll)

                if (sender.GetType() == typeof(ScrollViewer))
                {
                    switch (Keyboard.Modifiers)
                    {
                        case ModifierKeys.Alt:

                            var verDelta = e.Delta > 0 ? -10.5 : 10.5;
                            MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + verDelta);

                            break;
                        case ModifierKeys.Shift:

                            var horDelta = e.Delta > 0 ? -10.5 : 10.5;
                            MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + horDelta);

                            break;
                    }

                    return;
                }

                #endregion

                e.Handled = false;
                return;
            }

            WasChangingSelection = true;

            if (e.Delta > 0)
            {
                if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == _viewModel.Frames.Count - 1)
                {
                    FrameListView.SelectedIndex = 0;
                    return;
                }

                //Show next frame.
                FrameListView.SelectedIndex++;
            }
            else
            {
                if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == 0)
                {
                    FrameListView.SelectedIndex = _viewModel.Frames.Count - 1;
                    return;
                }

                //Show previous frame.
                FrameListView.SelectedIndex--;
            }
        }

        private void ZoomBoxControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Perhaps ignore when the mouse up happened because of a drag?
            if (_previewToken != null || !NotPreviewing)
                (FindResource("Command.Play") as RoutedUICommand)?.Execute(null, this);
        }

        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Slept = true;
                Pause();
                GC.Collect();
                return;
            }

            Slept = false;
        }

        private void System_DisplaySettingsChanged(object sender, EventArgs e)
        {
            UpdatePositioning(false);
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Slept)
                return;

            //If the window color changes, update the tabs style.
            if (e.PropertyName == "WindowGlassColor")
                RibbonTabControl.UpdateVisual(IsActive);
        }

        #endregion

        #region Frame Selection

        private void FrameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region If nothing selected

            if (FrameListView.SelectedIndex == -1)
            {
                UpdateOtherStatistics();
                ZoomBoxControl.ImageSource = null;
                return;
            }

            #endregion

            if (LastSelected == -1 || _previewToken != null || WasChangingSelection || LastSelected >= _viewModel.Frames.Count || (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0))
                LastSelected = FrameListView.SelectedIndex;

            FrameViewModel current;

            if (_previewToken != null || WasChangingSelection)
            {
                current = _viewModel.Frames[FrameListView.SelectedIndex];
            }
            else
            {
                //TODO: Test with other key shortcuts, because Ctrl + Z/Y was breaking this code.

                //current = _viewModel.Frames.GetItemAt(LastSelected) as FrameListBoxItem;
                if (Keyboard.FocusedElement is ListViewItem focused && focused.IsVisible && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                    current = FrameListView.ItemContainerGenerator.ItemFromContainer(focused) as FrameViewModel;
                else
                    current = _viewModel.Frames.FirstOrDefault(x => FrameListView.ItemContainerGenerator.ContainerFromItem(x) is ListViewItem container && (container.IsFocused || container.IsSelected));
            }

            //If there's no focused item.
            if (current == null)
            {
                if (_viewModel.Frames.Count - 1 > LastSelected)
                    FrameListView.SelectedIndex = LastSelected;
                else
                    FrameListView.SelectedIndex = LastSelected = _viewModel.Frames.Count - 1;

                if (FrameListView.SelectedIndex > -1)
                    current = _viewModel.Frames[FrameListView.SelectedIndex];
            }

            if (current != null)
            {
                var index = _viewModel.Frames.IndexOf(current);

                if (index > -1 && Project.Frames.Count > index)
                {
                    ZoomBoxControl.ImageSource = Project.Frames[index].Path;
                    FrameListView.ScrollIntoView(current);
                }

                if (FrameListView.ItemContainerGenerator.ContainerFromIndex(index) is ListViewItem container)
                {
                    if (!container.IsFocused && _previewToken == null)// && !WasChangingSelection)
                        container.Focus();
                }
            }

            if (_previewToken == null)
                UpdateOtherStatistics();

            WasChangingSelection = false;
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameViewModel item) // && !WasChangingSelection)
                return;

            LastSelected = item.Number;

            if (FrameListView.ItemContainerGenerator.ContainerFromItem(item) is ListViewItem container)
                Keyboard.Focus(container);
        }

        #endregion


        #region File Tab

        #region New/Open

        private void NewRecording_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && !e.Handled && Application.Current.Windows.OfType<Window>().All(a => !(a is BaseRecorder));
        }

        private void NewProject_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && !e.Handled;
        }

        private void NewRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            Encoder.Minimize();
            ClosePanel(removeEvent: true);

            App.MainViewModel.OpenRecorder.Execute(this);
        }

        private void NewWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            ClosePanel(removeEvent: true);

            App.MainViewModel.OpenWebcamRecorder.Execute(this);
        }

        private void NewBoardRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            ClosePanel(removeEvent: true);

            App.MainViewModel.OpenBoardRecorder.Execute(this);
        }

        private void NewProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.NewAnimation, LocalizationHelper.Get("S.Editor.File.Blank", true), "Vector.File.New", ApplyNewProjectButton_Click);
        }

        private void ApplyNewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            //Start new project.
            var project = new ProjectInfo().CreateProjectFolder(ProjectByType.Editor);

            var fileName = Path.Combine(project.FullPath, "0.png");

            #region Create and Save Image

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var bitmapSource = ImageMethods.CreateEmtpyBitmapSource(UserSettings.All.NewAnimationColor, UserSettings.All.NewAnimationWidth, UserSettings.All.NewAnimationHeight, this.Dpi(), PixelFormats.Indexed1);

                if (bitmapSource.Format != PixelFormats.Bgra32)
                    bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0);

                var bitmapFrame = BitmapFrame.Create(bitmapSource);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(bitmapFrame);
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            GC.Collect();

            #endregion

            ClosePanel();

            project.Frames = new List<FrameInfo> { new FrameInfo(fileName, 66) };

            LoadProject(project);
            ShowHint("S.Hint.NewAnimation");
        }

        public void RecorderCallback(ProjectInfo project)
        {
            Activate();

            if (project?.Any == true)
            {
                LoadProject(project);
                ShowHint("S.Hint.NewRecording");
            }

            Encoder.Restore();
            ShowInTaskbar = true;
            WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
        }

        #endregion

        #region Insert

        private void Insert_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Frames.Count > 0 && FrameListView.SelectedIndex != -1 && !IsLoading && !e.Handled && Application.Current.Windows.OfType<Window>().All(a => !(a is BaseRecorder));
        }

        private void InsertFromMedia_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Frames.Count > 0 && FrameListView.SelectedIndex != -1 && !IsLoading && !e.Handled;
        }

        private async void InsertRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            WindowState = WindowState.Minimized;
            Encoder.Minimize();

            ProjectInfo project = null;

            if (UserSettings.All.NewRecorder)
            {
                var recorder = new NewRecorder();
                recorder.ShowDialog();
                project = recorder.Project;

                if (recorder.Project?.Frames == null || !recorder.Project.Any)
                {
                    GC.Collect();

                    Encoder.Restore();
                    WindowState = WindowState.Normal;
                    return;
                }
            }
            else
            {
                var recorder = new Recorder();
                recorder.ShowDialog();
                project = recorder.Project;

                if (recorder.Project?.Frames == null || !recorder.Project.Any)
                {
                    GC.Collect();

                    Encoder.Restore();
                    WindowState = WindowState.Normal;
                    return;
                }
            }

            #region Insert

            var insert = new Insert(Project.Frames.CopyList(), project.Frames, FrameListView.SelectedIndex) { Owner = this };
            var result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Project.Frames = insert.CurrentList;
                await LoadSelectedStarter(0);
            }

            #endregion

            Encoder.Restore();
            WindowState = WindowState.Normal;
        }

        private async void InsertWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var recorder = new Webcam();
            recorder.ShowDialog();

            #region If recording cancelled

            if (recorder.Project?.Frames == null || !recorder.Project.Any)
            {
                GC.Collect();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(Project.Frames.CopyList(), recorder.Project.Frames, FrameListView.SelectedIndex) { Owner = this };

            var result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Project.Frames = insert.CurrentList;
                await LoadSelectedStarter(0);
            }

            #endregion
        }

        private async void InsertBoardRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var recorder = new Board();
            recorder.ShowDialog();

            #region If recording cancelled

            if (recorder.Project?.Frames == null || !recorder.Project.Any)
            {
                GC.Collect();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(Project.Frames.CopyList(), recorder.Project.Frames, FrameListView.SelectedIndex) { Owner = this };

            var result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Project.Frames = insert.CurrentList;
                await LoadSelectedStarter(0);
            }

            #endregion
        }

        private async void InsertFromMedia_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                AddExtension = true,
                CheckFileExists = true,
                Title = LocalizationHelper.Get("S.Editor.File.OpenMedia"),
                Filter = $"{LocalizationHelper.Get("S.Editor.File.All")} (*.apng, *.avi, *.bmp, *.gif, *.jpg, *.jpeg, *.mkv, *.mp4, *.png, *.webp, *.webm, *.wmv)|*.apng;*.avi;*.bmp;*.gif;*.jpg;*.jpeg;*.mkv;*.mp4;*.png;*.webp;*.webm;*.wmv|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Image")} (*.apng, *.bmp, *.gif, *.jpg, *.jpeg, *.png)|*.apng;*.bmp;*.gif;*.jpg;*.jpeg;*.png|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Video")} (*.avi, *.mkv, *.mp4, *.webp, *.webm, *.wmv)|*.avi;*.mkv;*.mp4;*.webp;*.webm;*.wmv",
            };

            var result = ofd.ShowDialog();

            #region Validation

            var extensionList = ofd.FileNames.Select(Path.GetExtension).ToList();

            var media = new[] { "apng", "avi", "bmp", "gif", "jpg", "jpeg", "mkv", "mp4", "png", "webp", "webm", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.InvalidLoadingFiles")));
                return;
            }

            #endregion

            if (result.HasValue && result.Value)
            {
                var done = await InsertImportFrom(ofd.FileNames.ToList());

                if (!done)
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;
                    HideProgress();

                    ClosePanel(removeEvent: true);

                    FrameListView.Focus();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        #endregion

        #region File

        private void File_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Any && !IsLoading && !e.Handled;
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ShowPanel(PanelTypes.SaveAs, LocalizationHelper.Get("S.Editor.File.Save", true), "Vector.Save", SaveAsButton_Click);
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            StatusList.Remove(StatusType.Warning);

            try
            {
                if (CustomContentControl.Content is not ExportPanel panel)
                    return;

                //Lock UI.
                IsLoading = true;

                if (!await panel.IsValid())
                    return;

                var preset = panel.GetPreset();

                //Set some transient properties.
                var size = Project.Frames[0].Path.SizeOf();
                preset.Width = size.Width;
                preset.Height = size.Height;
                preset.Scale = this.Scale();

                if (await Task.Run(() => SaveAsync(preset)))
                    ClosePanel();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while exporting");
                StatusList.Error(ex.Message, StatusReasons.InvalidState); //TODO: Put a proper message and localize it.
            }
            finally
            {
                //Workaround for not disabling the CanExecute of the panel.
                _applyAction = SaveAsButton_Click;

                //Return state of UI.
                Cursor = Cursors.Arrow;
                IsLoading = false;

                HideProgress();

                CommandManager.InvalidateRequerySuggested();
            }
        }


        private async void Load_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                AddExtension = true,
                CheckFileExists = true,
                Title = LocalizationHelper.Get("S.Editor.File.OpenMediaProject"),
                Filter = $"{LocalizationHelper.Get("S.Editor.File.All")} (*.apng, *.avi, *.bmp, *.gif, *.jpg, *.jpeg, *.mkv, *.mp4, *.png, *.stg, *.webp, *.webm, *.wmv, *.zip)|*.apng;*.avi;*.bmp;*.gif;*.jpg;*.jpeg;*.mkv;*.mp4;*.png;*.stg;*.webp;*.webm;*.wmv;*.zip|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Image")} (*.apng, *.bmp, *.gif, *.jpg, *.jpeg, *.png)|*.apng;*.bmp;*.gif;*.jpg;*.jpeg;*.png|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Video")} (*.avi, *.mkv, *.mp4, *.webp, *.webm, *.wmv)|*.avi;*.mkv;*.mp4;*.webp;*.webm;*.wmv|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Project")} (*.stg, *.zip) |*.stg;*.zip",
            };

            var result = ofd.ShowDialog();

            #region Validation

            var extensionList = ofd.FileNames.Select(s => Path.GetExtension(s).ToLowerInvariant()).ToList();

            var media = new[] { "apng", "avi", "bmp", "gif", "jpg", "jpeg", "mkv", "mp4", "png", "webp", "webm", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals(".stg") || x.Equals(".zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.InvalidLoadingFiles")));
                return;
            }

            #endregion

            if (result.HasValue && result.Value)
            {
                await Task.Run(() => ImportFrom(ofd.FileNames.ToList()));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void LoadRecent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            ShowPanel(PanelTypes.LoadRecent, LocalizationHelper.Get("S.Editor.File.LoadRecent", true), "Vector.Project", LoadRecentButton_Click);
        }

        private void RecentDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoadRecentButton_Click(sender, e);
        }

        private void RecentDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Return or Key.Enter)
            {
                LoadRecentButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void LoadRecentButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecentDataGrid.SelectedIndex < 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Recent.Warning.NoSelection"));
                return;
            }

            try
            {
                if (RecentDataGrid.SelectedItem is not ProjectInfo project)
                    throw new Exception("Nothing selected");

                if (Project != null && Project.RelativePath == project.RelativePath)
                {
                    StatusList.Warning(LocalizationHelper.Get("S.Recent.Warning.SameProject"));
                    return;
                }

                if (MutexList.IsInUse(project.RelativePath))
                {
                    StatusList.Warning(LocalizationHelper.Get("S.Recent.Warning.AnotherEditor"));
                    return;
                }

                LoadProject(project, true, false, true);
            }
            catch (Exception ex)
            {
                ErrorDialog.Ok("ScreenToGif", "Error while trying to load", ex.Message, ex);
                return;
            }

            _applyAction = null;

            ClosePanel();
        }

        private void DiscardProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Discard(UserSettings.All.NotifyProjectDiscard);
        }

        #endregion

        #endregion

        #region Home Tab

        #region Action Stack

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanUndo() && !IsLoading && !e.Handled;
        }

        private void Reset_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanReset() && !IsLoading && !e.Handled;
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanRedo() && !IsLoading && !e.Handled;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            Project.Frames = ActionStack.Undo(Project.Frames.CopyList());
            LoadProject(Project, false, false);

            ShowHint("S.Hint.Undo");
        }

        private void Reset_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            Project.Frames = ActionStack.Reset(Project.Frames.CopyList());
            LoadProject(Project, false, false);

            ShowHint("S.Hint.Reset");
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            Project.Frames = ActionStack.Redo(Project.Frames.CopyList());
            LoadProject(Project, false, false);

            ShowHint("S.Hint.Redo");
        }

        #endregion

        #region ClipBoard

        private void ClipBoard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading && !e.Handled;
        }

        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            #region Validation

            if (FrameListView.SelectedItems.Count == _viewModel.Frames.Count)
            {
                Dialog.Ok(FindResource("S.Editor.Clipboard.InvalidCut.Title").ToString(),
                    FindResource("S.Editor.Clipboard.InvalidCut.Instruction").ToString(),
                    FindResource("S.Editor.Clipboard.InvalidCut.Message").ToString(), Icons.Info);
                return;
            }

            #endregion

            var index = FrameListView.SelectedItems.OfType<FrameViewModel>().OrderBy(x => x.Number).First().Number;

            ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, SelectedFramesIndex());

            var selected = FrameListView.SelectedItems.OfType<FrameViewModel>().ToList();
            var list = selected.Select(item => Project.Frames[item.Number]).ToList();

            FrameListView.SelectedIndex = -1;

            if (!Util.Clipboard.Cut(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to cut selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");

                Undo_Executed(null, null);

                return;
            }

            selected.OrderByDescending(x => x.Number).ToList().ForEach(x => Project.Frames.RemoveAt(x.Number));
            selected.OrderByDescending(x => x.Number).ToList().ForEach(x => _viewModel.Frames.Remove(x));

            AdjustFrameNumbers(index);
            SelectNear(index);

            #region Item

            var imageItem = new ExtendedListBoxItem
            {
                Author = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentUICulture)
            };

            if (selected.Count > 1)
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.ImportVideo.Frames")} {string.Join(", ", selected.Select(x => x.Number))}";
                imageItem.Icon = FindResource("Vector.ImageStack") as Brush;
                imageItem.Content = $"{list.Count} Images";
            }
            else
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.Editor.List.Frame")} {selected[0].Number}";
                imageItem.Icon = FindResource("Vector.Image") as Brush;
                imageItem.Content = $"{list.Count} Image";
            }

            #endregion

            ClipboardListBox.Items.Add(imageItem);
            ClipboardListBox.SelectedIndex = ClipboardListBox.Items.Count - 1;

            ShowHint("S.Hint.Cut", false, selected.Count);

            ShowPanel(PanelTypes.Clipboard, LocalizationHelper.Get("S.Editor.Home.Clipboard", true), "Vector.Paste");
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var selected = FrameListView.SelectedItems.OfType<FrameViewModel>().ToList();
            var list = selected.Select(item => Project.Frames[item.Number]).ToList();

            if (!Util.Clipboard.Copy(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to copy selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");
                return;
            }

            #region Item

            var imageItem = new ExtendedListBoxItem
            {
                Author = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentUICulture)
            };

            if (list.Count > 1)
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.ImportVideo.Frames")} {string.Join(", ", selected.Select(x => x.Number))}";
                imageItem.Icon = FindResource("Vector.ImageStack") as Brush;
                imageItem.Content = LocalizationHelper.GetWithFormat("S.Clipboard.Entry.Images", "{0} images", list.Count);
            }
            else
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.Editor.List.Frame")} {selected[0].Number}";
                imageItem.Icon = FindResource("Vector.Image") as Brush;
                imageItem.Content = LocalizationHelper.GetWithFormat("S.Clipboard.Entry.Image", "{0} image", list.Count);
            }

            #endregion

            ClipboardListBox.Items.Add(imageItem);
            ClipboardListBox.SelectedIndex = ClipboardListBox.Items.Count - 1;

            ShowHint("S.Hint.Copy", false, selected.Count);

            ShowPanel(PanelTypes.Clipboard, LocalizationHelper.Get("S.Editor.Home.Clipboard", true), "Vector.Paste");
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && Util.Clipboard.Items.Count > 0 && ClipboardListBox.SelectedItem != null;
        }

        private async void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var index = FrameListView.SelectedItems.OfType<FrameViewModel>().Last().Number;
            index = PasteBeforeRadioButton.IsChecked.HasValue && PasteBeforeRadioButton.IsChecked.Value ? index : index + 1;

            var clipData = Util.Clipboard.Paste(Project.FullPath, ClipboardListBox.SelectedIndex, ClipboardListBox.SelectedIndex);

            ActionStack.SaveState(ActionStack.EditAction.Add, index, clipData.Count);

            Project.Frames.InsertRange(index, clipData);

            ClosePanel();

            await LoadSelectedStarter(index, Project.Frames.Count - 1);

            ShowHint("S.Hint.Paste", false, clipData.Count);
        }

        private void ShowClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelTypes.Clipboard, LocalizationHelper.Get("S.Editor.Home.Clipboard", true), "Vector.Paste");
        }


        private void ClipBoardSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ClipboardListBox.SelectedItem != null && !IsLoading;
        }

        private void ExploreClipBoard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var selected = Util.Clipboard.Items[ClipboardListBox.SelectedIndex];

                ProcessHelper.StartWithShell(Path.GetDirectoryName(selected[0].Path));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to browse the clipboard folder");
                Dialog.Ok(Title, "Impossible to browse the clipboard folder", ex.Message);
            }
        }

        private void RemoveClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Util.Clipboard.Remove(ClipboardListBox.SelectedIndex);
            ClipboardListBox.Items.RemoveAt(ClipboardListBox.SelectedIndex);
        }

        #endregion

        #region Zoom

        private void Zoom_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Any && !IsLoading && !OverlayGrid.IsVisible && FrameListView.SelectedIndex != -1;
        }

        private void Zoom100_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomBoxControl.Zoom = 1.0;
            ZoomBoxControl.SaveCurrentZoom();

            ShowHint("S.Hint.Zoom", false, 100);
        }

        private void SizeToContent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            ZoomBoxControl.UpdateLayout();

            var size = ZoomBoxControl.GetElementSize(true);

            //Tried to get the size again.
            if (size.Width < 2)
                size = Project.Frames[0].Path.NonScaledSize();

            //If failed again, abort.
            if (size.Width < 2)
                return;

            var scale = this.Scale();

            var borderHeight = ActualHeight - MainGrid.ActualHeight;
            var borderWidth = ActualWidth - MainGrid.ActualWidth;

            //Bug: I need to take into consideration that the RibbonTabControl.ActualHeight can change, because the tab headers can occupy 2 rows.
            var width = (size.Width * ZoomBoxControl.Zoom / ZoomBoxControl.ScaleDiff + 60) + borderWidth;
            var height = (size.Height * ZoomBoxControl.Zoom / ZoomBoxControl.ScaleDiff + (RibbonTabControl.ActualHeight + FrameListView.ActualHeight + LowerGrid.ActualHeight)) + borderHeight;

            //If image is too small, size to the minimum size.
            if (width < 770)
                width = 770;

            if (height < 575)
                height = 575;

            var screen = MonitorHelper.AllMonitorsScaled(scale).FirstOrDefault(x => x.Bounds.Contains(new Point(Left, Top))) ??
                         MonitorHelper.AllMonitorsScaled(scale).FirstOrDefault(x => x.IsPrimary);

            if (screen != null)
            {
                //If the resulting size is too big, fit the window on the available working area.
                if (screen.WorkingArea.Width < width)
                    width = screen.WorkingArea.Width;

                if (screen.WorkingArea.Height < height)
                    height = screen.WorkingArea.Height;

                //If the window overflows, put back in place.
                if (Left + width > screen.WorkingArea.Right)
                    Left = screen.WorkingArea.Right - width;

                if (Top + height > screen.WorkingArea.Bottom)
                    Top = screen.WorkingArea.Bottom - height;
            }

            Width = width;
            Height = height;
        }

        private void FitImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Reset the zoom, to get the actual image size.
            ZoomBoxControl.Zoom = 1;
            ZoomBoxControl.UpdateLayout();

            var size = ZoomBoxControl.GetElementSize();

            if (size.Width < 2)
                size = Project.Frames[0].Path.ScaledSize();

            #region Calculate the Zoom

            var zoomHeight = 1D;
            var zoomWidth = 1D;

            if (size.Width > ZoomBoxControl.ActualWidth)
                zoomWidth = ZoomBoxControl.ActualWidth / size.Width;// * this.Scale();

            if (size.Height > ZoomBoxControl.ActualHeight)
                zoomHeight = ZoomBoxControl.ActualHeight / size.Height;// * this.Scale();

            #endregion

            #region Apply the zoom

            if (zoomHeight > 0 && zoomHeight < zoomWidth)
                ZoomBoxControl.Zoom = zoomHeight;
            else if (zoomWidth > 0 && zoomWidth < zoomHeight)
                ZoomBoxControl.Zoom = zoomWidth;
            else
                ZoomBoxControl.Zoom = 1;

            ZoomBoxControl.SaveCurrentZoom();

            #endregion

            ShowHint("S.Hint.Zoom", false, Convert.ToInt32(ZoomBoxControl.Zoom * 100));

            GC.Collect(1);
        }

        #endregion

        #region Select

        private void Selection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && FrameListView != null && FrameListView.HasItems;
        }

        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            FrameListView.SelectAll();

            ShowHint("S.Hint.SelectAll");
        }

        private void GoTo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var go = new GoTo(Project.Frames.Count - 1) { Owner = this };
            var result = go.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            WasChangingSelection = true;
            FrameListView.SelectedIndex = go.Selected;

            ShowHint("S.Hint.SelectSingle", false, go.Selected);
        }

        private void InverseSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            foreach (var item in _viewModel.Frames)
            {
                if (FrameListView.ItemContainerGenerator.ContainerFromItem(item) is ListViewItem element)
                    element.IsSelected = !element.IsSelected;
            }

            ShowHint("S.Hint.SelectInverse");
        }

        private void DeselectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            FrameListView.SelectedIndex = -1;

            ShowHint("S.Hint.Deselect");
        }

        #endregion

        #endregion

        #region Playback Tab

        private void Playback_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Frames.Count > 1 && !IsLoading;
        }

        private void FirstFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;
            FrameListView.SelectedIndex = 0;
        }

        private void PreviousFrame_Executed(object sender, EventArgs e)
        {
            Pause();

            WasChangingSelection = true;

            if (FrameListView.SelectedIndex is -1 or 0)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    return;

                FrameListView.SelectedIndex = _viewModel.Frames.Count - 1;
                return;
            }

            //Show previous frame.
            //Control = 5
            //Alt = 10
            //Control + Alt = 20
            if (e != null && (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == (ModifierKeys.Alt | ModifierKeys.Control))
                FrameListView.SelectedIndex = Math.Max(0, FrameListView.SelectedIndex - 20);
            else if (e != null && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                FrameListView.SelectedIndex = Math.Max(0, FrameListView.SelectedIndex - 5);
            else if (e != null && (Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                FrameListView.SelectedIndex = Math.Max(0, FrameListView.SelectedIndex - 10);
            else
                FrameListView.SelectedIndex--;
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayPause();
        }

        private void NextFrame_Executed(object sender, EventArgs e)
        {
            Pause();

            WasChangingSelection = true;

            if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == _viewModel.Frames.Count - 1)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    return;

                FrameListView.SelectedIndex = 0;
                return;
            }

            //Show next frame.
            //Control = 5
            //Alt = 10
            //Control + Alt = 20
            if (e != null && (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == (ModifierKeys.Alt | ModifierKeys.Control))
                FrameListView.SelectedIndex = Math.Min(_viewModel.Frames.Count - 1, FrameListView.SelectedIndex + 20);
            else if (e != null && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                FrameListView.SelectedIndex = Math.Min(_viewModel.Frames.Count - 1, FrameListView.SelectedIndex + 5);
            else if (e != null && (Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                FrameListView.SelectedIndex = Math.Min(_viewModel.Frames.Count - 1, FrameListView.SelectedIndex + 10);
            else
                FrameListView.SelectedIndex++;
        }

        private void LastFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;
            FrameListView.SelectedIndex = _viewModel.Frames.Count - 1;
        }

        #endregion

        #region Edit Tab

        #region Frames

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && !IsLoading;
        }

        private void DeletePrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && !IsLoading &&
                FrameListView.SelectedItems.OfType<FrameViewModel>().Min(s => s.Number) > 0;
        }

        private void DeleteNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && !IsLoading &&
                FrameListView.SelectedItems.OfType<FrameViewModel>().Max(s => s.Number) < _viewModel.Frames.Count - 1;
        }

        private void Reduce_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && !IsLoading && _viewModel.Frames.Count > 5;
        }

        private void RemoveDuplicates_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && !IsLoading && _viewModel.Frames.Count > 1;
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            try
            {
                #region Validation

                if (Project.Frames.Count == FrameListView.SelectedItems.Count)
                {
                    //If the user wants to delete all frames, discard the project.
                    if (!UserSettings.All.NotifyProjectDiscard ||
                        Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteAll.Title"), LocalizationHelper.Get("S.Editor.DeleteAll.Instruction"), LocalizationHelper.Get("S.Editor.DeleteAll.Message"), false))
                        Discard(false);

                    return;
                }

                if (UserSettings.All.NotifyFrameDeletion)
                {
                    if (!Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteFrames.Title"), LocalizationHelper.Get("S.Editor.DeleteFrames.Instruction"),
                        string.Format(LocalizationHelper.Get("S.Editor.DeleteFrames.Message"), FrameListView.SelectedItems.Count)))
                        return;
                }

                #endregion

                var selected = FrameListView.SelectedItems.OfType<FrameViewModel>().ToList();
                var selectedOrdered = selected.OrderByDescending(x => x.Number).ToList();
                var list = selectedOrdered.Select(item => Project.Frames[item.Number]).ToList();

                ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, SelectedFramesIndex());

                FrameListView.SelectedItem = null;

                list.ForEach(x => File.Delete(x.Path));
                selectedOrdered.ForEach(x => Project.Frames.RemoveAt(x.Number));
                selectedOrdered.ForEach(x => _viewModel.Frames.Remove(x));

                AdjustFrameNumbers(selectedOrdered.Last().Number);

                SelectNear(selectedOrdered.Last().Number);

                Project.Persist();
                UpdateStatistics();
                ShowHint("S.Hint.DeleteFrames", false, selected.Count);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error While Trying to Delete Frames");

                ErrorDialog.Ok(LocalizationHelper.Get("S.Editor.Title"), "Error while trying to delete frames", ex.Message, ex);
            }
        }

        private void DeletePrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var firstFrame = FrameListView.SelectedItems.OfType<FrameViewModel>().Min(x => x.Number);

            if (UserSettings.All.NotifyFrameDeletion)
            {
                if (!Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteFrames.Title"), LocalizationHelper.Get("S.Editor.DeleteFrames.Instruction"),
                    string.Format(LocalizationHelper.Get("S.Editor.DeleteFrames.Message"), firstFrame)))
                    return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, Util.Other.ListOfIndexesOld(0, firstFrame - 1));

            for (var index = firstFrame - 1; index >= 0; index--)
                DeleteFrame(index);

            AdjustFrameNumbers(0);
            SelectNear(0);

            Project.Persist();
            UpdateStatistics();
            ShowHint("S.Hint.DeleteFrames", false, firstFrame);
        }

        private void DeleteNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var lastFrame = FrameListView.SelectedItems.OfType<FrameViewModel>().Max(m => m.Number);
            var count = _viewModel.Frames.Count - lastFrame - 1;

            if (UserSettings.All.NotifyFrameDeletion)
            {
                if (!Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteFrames.Title"), LocalizationHelper.Get("S.Editor.DeleteFrames.Instruction"),
                    string.Format(LocalizationHelper.Get("S.Editor.DeleteFrames.Message"), count)))
                    return;
            }

            var countList = _viewModel.Frames.Count - 1;

            ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, Util.Other.ListOfIndexes(lastFrame + 1, count));

            //From the end to the start.
            for (var i = countList; i > lastFrame; i--)
                DeleteFrame(i);

            SelectNear(lastFrame);

            Project.Persist();
            UpdateStatistics();
            ShowHint("S.Hint.DeleteFrames", false, count);
        }

        private void RemoveDuplicates_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.RemoveDuplicates, LocalizationHelper.Get("S.Editor.Edit.Frames.Duplicates", true), "Vector.RemoveImage", ApplyRemoveDuplicatesCountButton_Click);
        }

        private async void ApplyRemoveDuplicatesCountButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.AppStarting;

            var index = await Task.Run(() => RemoveDuplicatesAsync((decimal)UserSettings.All.DuplicatesSimilarity, UserSettings.All.DuplicatesRemoval, UserSettings.All.DuplicatesDelay));

            for (var i = _viewModel.Frames.Count - 1; i >= Project.Frames.Count; i--)
                _viewModel.Frames.RemoveAt(i);

            SelectNear(LastSelected);
            await Task.Run(() => Project.Persist());

            await LoadSelectedStarter(index, Project.Frames.Count - 1);

            ShowHint("S.Hint.Duplicates");

            ClosePanel();
        }

        private void Reduce_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.ReduceFrames, LocalizationHelper.Get("S.Editor.Edit.Frames.Reduce", true), "Vector.RemoveImage", ApplyReduceFrameCountButton_Click);
        }

        private async void ApplyReduceFrameCountButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = UserSettings.All.ReduceApplyToAll ? Util.Other.ListOfIndexes(0, Project.Frames.Count - 1) : SelectedFramesIndex();

            if (selected.Count == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Reduce.Warning.NoSelection"));
                return;
            }

            //Detects if there's any non-consecutive frames selected.
            if (!selected.All(v => selected.Contains(v + 1) || v == selected.Last()))
            {
                StatusList.Warning(LocalizationHelper.Get("S.Reduce.Warning.NonConsecutive"));
                return;
            }

            if (selected.Count <= UserSettings.All.ReduceFactor)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Reduce.Warning.SmallerThanFactor"));
                return;
            }

            Cursor = Cursors.AppStarting;

            await Task.Run(() => ReduceFrameCount(selected, UserSettings.All.ReduceFactor, UserSettings.All.ReduceCount, UserSettings.All.ReduceDelay));

            for (var i = _viewModel.Frames.Count - 1; i >= Project.Frames.Count; i--)
                _viewModel.Frames.RemoveAt(i);

            SelectNear(LastSelected);
            await Task.Run(() => Project.Persist());

            //TODO: Load from the start.
            await LoadSelectedStarter(ReduceFactorIntegerUpDown.Value - 1, Project.Frames.Count - 1);

            ShowHint("S.Hint.Reduce");

            ClosePanel();
        }

        private void SmoothLoop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.SmoothLoop, LocalizationHelper.Get("S.Editor.Edit.Frames.SmoothLoop", true), "Vector.Repeat", ApplySmoothLoopButton_Click);
        }

        private async void ApplySmoothLoopButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSettings.All.SmoothLoopStartThreshold > Project.Frames.Count - 1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.SmoothLoop.Warning.Threshold"));
                return;
            }

            Cursor = Cursors.AppStarting;
            var initialCount = Project.Frames.Count;

            var index = await Task.Run(() => SmoothLoopAsync((decimal)UserSettings.All.SmoothLoopSimilarity, UserSettings.All.SmoothLoopStartThreshold, UserSettings.All.SmoothLoopFrom));

            //If nothing changed, it means that no frame was removed.
            if (Project.Frames.Count == initialCount)
            {
                //The reason could be for no loop found or loop already smooth.
                if (index == Project.Frames.Count - 1)
                    StatusList.Info(LocalizationHelper.Get("S.SmoothLoop.Warning.AlreadySmoothLoop"));
                else
                    StatusList.Warning(LocalizationHelper.Get("S.SmoothLoop.Warning.NoLoopFound"));

                //Workaround for not disabling the CanExecute of the panel.
                _applyAction = ApplySmoothLoopButton_Click;

                //Return state of UI.
                Cursor = Cursors.Arrow;
                IsLoading = false;

                HideProgress();

                CommandManager.InvalidateRequerySuggested();
                return;
            }

            for (var i = _viewModel.Frames.Count - 1; i >= Project.Frames.Count; i--)
                _viewModel.Frames.RemoveAt(i);

            SelectNear(LastSelected);
            await Task.Run(() => Project.Persist());

            await LoadSelectedStarter(index, Project.Frames.Count - 1);

            ClosePanel();
        }

        #endregion

        #region Reordering

        private void Reordering_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && !IsLoading && _viewModel.Frames.Count > 1;
        }

        private async void Reverse_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Reorder, Project.Frames.CopyList());

            Project.Frames.Reverse();
            await LoadSelectedStarter(0);

            ShowHint("S.Hint.Reverse");
        }

        private async void Yoyo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Add, Project.Frames.Count, Project.Frames.Count);

            Project.Frames = Util.Other.Yoyo(Project.Frames);
            await LoadSelectedStarter(0);

            ShowHint("S.Hint.Yoyo");
        }

        private void MoveLeft_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            Pause();
            ActionStack.SaveState(ActionStack.EditAction.Reorder, Project.Frames.CopyList());

            var selection = FrameListView.SelectedItems.OfType<FrameViewModel>().Select(s =>
            {
                var index = _viewModel.Frames.IndexOf(s);
                return (Current: s, CurrentIndex: index, NextIndex: index > 0 ? index - 1 : _viewModel.Frames.Count - 1);
            }).ToList();

            //Reorder the frames.
            foreach (var item in selection.OrderBy(o => o.CurrentIndex))
            {
                if (_viewModel.Frames.IndexOf(item.Current) == item.NextIndex)
                    continue;

                _viewModel.Frames.Move(item.CurrentIndex, item.NextIndex);
                Project.Frames.Move(item.CurrentIndex, item.NextIndex);
            }

            //Since each frame has a number, upon reordering the numbers must be updated.
            AdjustFrameNumbers(selection.Select(s => Math.Min(s.CurrentIndex, s.NextIndex)).Min());

            FocusOnSelectedFrames();
            ShowHint("S.Hint.MoveLeft");
        }

        private void MoveRight_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            Pause();
            ActionStack.SaveState(ActionStack.EditAction.Reorder, Project.Frames.CopyList());

            var selection = FrameListView.SelectedItems.OfType<FrameViewModel>().Select(s =>
            {
                var index = _viewModel.Frames.IndexOf(s);
                return (Current: s, CurrentIndex: index, NextIndex: index < _viewModel.Frames.Count - 1 ? index + 1 : 0);
            }).ToList();

            //Reorder the frames.
            foreach (var item in selection.OrderByDescending(o => o.CurrentIndex))
            {
                if (_viewModel.Frames.IndexOf(item.Current) == item.NextIndex)
                    continue;

                _viewModel.Frames.Move(item.CurrentIndex, item.NextIndex);
                Project.Frames.Move(item.CurrentIndex, item.NextIndex);
            }

            //Since each frame has a number, upon reordering the numbers must be updated.
            AdjustFrameNumbers(selection.Select(s => Math.Min(s.CurrentIndex, s.NextIndex)).Min());

            FocusOnSelectedFrames();
            ShowHint("S.Hint.MoveRight");
        }

        #endregion

        #region Delay (Duration)

        private void OverrideDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.OverrideDelay, LocalizationHelper.Get("S.Editor.Edit.Delay.Override", true), "Vector.OverrideDelay", ApplyOverrideDelayButton_Click);
        }

        private async void ApplyOverrideDelayButton_Click(object sender, RoutedEventArgs e)
        {
            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            await Task.Run(() => DelayAsync(DelayViewModel.FromSettings(DelayUpdateModes.Override), false, false));

            Cursor = Cursors.Arrow;

            UpdateStatistics();
            HideProgress();
            IsLoading = false;

            ShowHint("S.Hint.Delay");

            CommandManager.InvalidateRequerySuggested();
            SetFocusOnCurrentFrame();

            ClosePanel();
        }


        private void IncreaseDecreaseDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.IncreaseDecreaseDelay, LocalizationHelper.Get("S.Editor.Edit.Delay.IncreaseDecrease", true), "Vector.IncreaseDecreaseDelay", ApplyIncreaseDecreaseDelayButtonClick);
        }

        private async void ApplyIncreaseDecreaseDelayButtonClick(object sender, RoutedEventArgs e)
        {
            if (IncreaseDecreaseDelayIntegerUpDown.Value == 0)
            {
                ClosePanel();
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            await Task.Run(() => DelayAsync(DelayViewModel.FromSettings(DelayUpdateModes.IncreaseDecrease), false, false));

            Cursor = Cursors.Arrow;

            UpdateStatistics();
            HideProgress();
            IsLoading = false;

            ShowHint("S.Hint.Delay");

            CommandManager.InvalidateRequerySuggested();
            SetFocusOnCurrentFrame();

            ClosePanel();
        }

        private void ScaleDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.ScaleDelay, LocalizationHelper.Get("S.Editor.Edit.Delay.Scale", true), "Vector.ScaleDelay", ApplyScaleDelayButtonClick);
        }

        private async void ApplyScaleDelayButtonClick(object sender, RoutedEventArgs e)
        {
            if (ScaleDelayIntegerUpDown.Value == 0 || ScaleDelayIntegerUpDown.Value == 100)
            {
                ClosePanel();
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            await Task.Run(() => DelayAsync(DelayViewModel.FromSettings(DelayUpdateModes.Scale), false, false));

            Cursor = Cursors.Arrow;

            UpdateStatistics();
            HideProgress();
            IsLoading = false;

            ShowHint("S.Hint.Delay");

            CommandManager.InvalidateRequerySuggested();
            SetFocusOnCurrentFrame();

            ClosePanel();
        }

        #endregion

        #endregion

        #region Image Tab

        private void Image_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && !IsLoading;
        }

        #region Size and Position

        private void Resize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowPanel(PanelTypes.Resize, LocalizationHelper.Get("S.Editor.Image.Resize", true), "Vector.Resize", ApplyResizeButton_Click);
        }

        private async void ApplyResizeButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            //Checks with the non scaled size.
            var size = Project.Frames[0].Path.ScaledSize();
            var settings = ResizePanel.DataContext as ResizeViewModel;

            if (settings == null || Math.Abs(size.Width - settings.Width) < 0.1 && Math.Abs(size.Height - settings.Height) < 0.1 && (int)Math.Round(Project.Frames[0].Path.DpiOf()) == (int)Math.Round(settings.Dpi))
            {
                StatusList.Warning(LocalizationHelper.Get("S.Resize.Warning"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            await Task.Run(() => Resize(settings.Width, settings.Height, settings.Dpi, settings.ScalingMode));

            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            ClosePanel();

            ShowHint("S.Hint.Resize");
        }


        private void Crop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Crop, LocalizationHelper.Get("S.Editor.Image.Crop", true), "Vector.Crop", ApplyCropButton_Click);
        }

        private CroppingAdorner _cropAdorner;
        private FrameworkElement _currentElement = null;
        private bool _resizing = false;

        private void AddCropToElement(FrameworkElement fel)
        {
            if (_currentElement != null)
                RemoveCropElements();

            var rcInterior = new Rect((int)(fel.Width * 0.2), (int)(fel.Height * 0.2), (int)(fel.Width * 0.6), (int)(fel.Height * 0.6));

            var aly = AdornerLayer.GetAdornerLayer(fel);
            _cropAdorner = new CroppingAdorner(fel, rcInterior);
            aly.Add(_cropAdorner);

            _cropAdorner.CropChanged += CropChanged;
            _currentElement = fel;

            _cropAdorner.Fill = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0));
            RefreshCropImage();
        }

        private void RemoveCropElements()
        {
            var aly = AdornerLayer.GetAdornerLayer(_currentElement);
            aly.Remove(_cropAdorner);

            _currentElement = null;
            _cropAdorner.CropChanged -= CropChanged;
            _cropAdorner = null;
        }

        private void CropChanged(object sender, RoutedEventArgs rea)
        {
            RefreshCropImage();

            _resizing = true;

            TopCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Top;
            LeftCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Left;
            BottomCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Bottom;
            RightCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Right;

            var scale = this.Scale();
            CropSizeTextBlock.Text = $"{(int)Math.Round(_cropAdorner.ClipRectangle.Width * scale)} × {(int)Math.Round(_cropAdorner.ClipRectangle.Height * scale)}";

            _resizing = false;
        }

        private void RefreshCropImage()
        {
            if (_cropAdorner == null || Math.Abs(ZoomBoxControl.Zoom - 1) > 0) return;

            try
            {
                var rect = new Int32Rect((int)(_cropAdorner.ClipRectangle.X * ZoomBoxControl.ScaleDiff),
                    (int)(_cropAdorner.ClipRectangle.Y * ZoomBoxControl.ScaleDiff),
                    (int)(_cropAdorner.ClipRectangle.Width * ZoomBoxControl.ScaleDiff),
                    (int)(_cropAdorner.ClipRectangle.Height * ZoomBoxControl.ScaleDiff));

                if (rect.HasArea)
                    CropImage.Source = (ZoomBoxControl.ImageSource ?? Project.Frames[LastSelected].Path).CropFrom(rect);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Crop preview", _cropAdorner.ClipRectangle + " => " + FrameSize);
            }
        }

        private void CropIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (_cropAdorner == null)
                return;

            if (_resizing)
                return;

            var top = TopCropNumericUpDown.Value;
            var left = LeftCropNumericUpDown.Value;
            var bottom = BottomCropNumericUpDown.Value;
            var right = RightCropNumericUpDown.Value;

            _cropAdorner.ClipRectangle = new Rect(new Point(left, top), new Point(right, bottom));
        }

        private async void ApplyCropButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
            RefreshCropImage();

            var rect = new Int32Rect((int)Math.Round(_cropAdorner.ClipRectangle.X * ZoomBoxControl.ScaleDiff, MidpointRounding.AwayFromZero),
                (int)Math.Round(_cropAdorner.ClipRectangle.Y * ZoomBoxControl.ScaleDiff, MidpointRounding.AwayFromZero),
                (int)Math.Round(_cropAdorner.ClipRectangle.Width * ZoomBoxControl.ScaleDiff),
                (int)Math.Round(_cropAdorner.ClipRectangle.Height * ZoomBoxControl.ScaleDiff));

            if (!rect.HasArea)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Crop.Warning"));
                return;
            }

            if (rect.Width < 10 || rect.Height < 10)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Crop.Warning.Bigger"));
                return;
            }

            if (CropImage.Source == null)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Crop.Warning"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            await Task.Run(() => Crop(rect));
            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            RemoveCropElements();
            ClosePanel();

            ShowHint("S.Hint.Crop");
        }


        private void FlipRotate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.FlipRotate, LocalizationHelper.Get("S.Editor.Image.FlipRotate", true), "Vector.FlipHorizontal", ApplyFlipRotateButton_Click);
        }

        private async void ApplyFlipRotateButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            Cursor = Cursors.AppStarting;

            var type = FlipHorizontalRadioButton.IsChecked == true
                ? FlipRotateType.FlipHorizontal : FlipVerticalRadioButton.IsChecked == true
                ? FlipRotateType.FlipVertical : RotateLeftRadioButton.IsChecked == true ?
                  FlipRotateType.RotateLeft90 : FlipRotateType.RotateRight90;

            //If it's a rotate operation, the entire list of frames will be altered.
            var selectedIndexes = type == FlipRotateType.RotateLeft90 || type == FlipRotateType.RotateRight90
                ? Util.Other.ListOfIndexes(0, Project.Frames.Count)
                : SelectedFramesIndex();

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, selectedIndexes);

            await Task.Run(() => FlipRotate(type));
            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            ClosePanel();

            ShowHint("S.Hint.FlipRotate");
        }

        #endregion

        #region Text

        private void Caption_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Caption, LocalizationHelper.Get("S.Editor.Image.Caption", true), "Vector.Caption", ApplyCaptionButton_Click);
        }

        private async void ApplyCaptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CaptionTextBox.Text.Trim().Length == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Caption.WarningNoText"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Caption.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            var render = CaptionOverlayGrid.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            var selected = await Task.Run(() => OverlayAsync(render, false));

            ShowHint("S.Hint.Overlay");

            await LoadSelectedStarter(selected.Min(), selected.Max());

            ClosePanel();
        }


        private void FreeText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.FreeText, LocalizationHelper.Get("S.Editor.Image.FreeText", true), "Vector.FreeText", ApplyFreeTextButton_Click);
        }

        private void FreeTextTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            //TODO: This event is not fired when the entire text is deleted.

            FreeTextOverlayControl.AdjustContent();
        }

        private async void ApplyFreeTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeTextTextBox.Text.Length == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Caption.WarningNoText"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.FreeText.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            FreeTextOverlayControl.CanMove = false;

            var render = FreeTextOverlayControl.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            FreeTextOverlayControl.CanMove = true;

            Cursor = Cursors.AppStarting;

            var selected = await Task.Run(() => OverlayAsync(render, false));

            ShowHint("S.Hint.Overlay");

            await LoadSelectedStarter(selected.Min(), selected.Max());

            ClosePanel();
        }


        private void TitleFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.TitleFrame, LocalizationHelper.Get("S.Editor.Image.TitleFrame", true), "Vector.TitleFrame", ApplyTitleFrameButton_Click);
        }

        private async void ApplyTitleFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.TitleFrame.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex, 1);

            var render = TitleFrameOverlayGrid.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            var index = FrameListView.SelectedIndex;
            var dpi = ZoomBoxControl.ImageDpi;
            var selected = await Task.Run(() => TitleFrame(render, index, dpi));

            await LoadSelectedStarter(selected, Project.Frames.Count - 1);

            ClosePanel();
        }


        private void KeyStrokes_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.KeyStrokes, LocalizationHelper.Get("S.Editor.Image.KeyStrokes", true), "Vector.Keyboard", ApplyKeyStrokesButton_Click);
        }

        private void EditKeyStrokesButton_Click(object sender, RoutedEventArgs e)
        {
            var keyStrokes = new KeyStrokes
            {
                InternalList = new ObservableCollection<FrameInfo>(Project.Frames.CopyList())
            };

            var result = keyStrokes.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            for (var i = 0; i < keyStrokes.InternalList.Count; i++)
                Project.Frames[i].KeyList = new List<IKeyGesture>(keyStrokes.InternalList[i].KeyList);
        }

        private async void ApplyKeyStrokesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Project.Frames.Any(x => x.KeyList != null && x.KeyList.Any()))
            {
                StatusList.Warning(LocalizationHelper.Get("S.KeyStrokes.Warning.None"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            await Task.Run(() => KeyStrokesAsync(KeyStrokesViewModel.FromSettings()));

            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            ClosePanel();
        }

        #endregion

        #region Overlay

        private void FreeDrawing_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.FreeDrawing, LocalizationHelper.Get("S.Editor.Image.FreeDrawing", true), "Vector.FreeDrawing", ApplyFreeDrawingButton_Click);
        }

        private async void ApplyFreeDrawingButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeDrawingInkCanvas.Strokes.Count == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.Warning.NoDrawing"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            var render = FreeDrawingInkCanvas.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            FreeDrawingInkCanvas.Strokes.Clear();

            var selected = await Task.Run(() => OverlayAsync(render, false));

            ShowHint("S.Hint.Overlay");

            await LoadSelectedStarter(selected.Min(), selected.Max());

            ClosePanel();
        }


        private void Shapes_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Shapes, LocalizationHelper.Get("S.Editor.Image.Shape", true), "Vector.Ellipse", ApplyShapesButton_Click);
        }

        private void ShapeModes_Checked(object sender, RoutedEventArgs e)
        {
            ShapeDrawingCanvas.DrawingMode = AddModeRadioButton.IsChecked == true ? DrawingCanvas.DrawingModes.Shape : DrawingCanvas.DrawingModes.Select;
        }

        private void ShapeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ListBox listBox))
                return;

            switch (listBox.SelectedIndex)
            {
                case 0:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Rectangle;
                    break;
                case 1:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Ellipse;
                    break;
                case 2:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Triangle;
                    break;
                case 3:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Arrow;
                    break;
            }
        }

        private void ShapeProperties_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ShapeDrawingCanvas.StrokeThickness = ShapeOutlineDoubleUpDown.Value;
            ShapeDrawingCanvas.Stroke = ShapeOutlineColorBox.SelectedBrush;
            ShapeDrawingCanvas.Radius = ShapeRadiusDoubleUpDown.Value;
            ShapeDrawingCanvas.Fill = ShapesFillColorBox.SelectedBrush;
        }

        private async void ApplyShapesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShapeDrawingCanvas.ShapesCount == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.Warning.NoDrawing"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.WarningSelection"));
                return;
            }

            ShapeDrawingCanvas.DeselectAll();

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            var render = ShapeDrawingCanvas.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            ShapeDrawingCanvas.RemoveAllShapes();

            var selected = await Task.Run(() => OverlayAsync(render, false));

            ShowHint("S.Hint.Overlay");

            await LoadSelectedStarter(selected.Min(), selected.Max());

            ClosePanel();
        }


        private void MouseEvents_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.MouseEvents, LocalizationHelper.Get("S.Editor.Image.MouseEvents", true), "Vector.Cursor", ApplyMouseEventsButton_Click);
        }

        private async void ApplyMouseEventsButton_Click(object sender, RoutedEventArgs e)
        {
            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            await Task.Run(() => MouseEventsAsync(MouseEventsViewModel.FromSettings()));

            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            ClosePanel();
        }


        private void Watermark_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Watermark, LocalizationHelper.Get("S.Editor.Image.Watermark", true), "Vector.Watermark", ApplyWatermarkButton_Click);

            TopWatermarkDoubleUpDown.Scale = LeftWatermarkDoubleUpDown.Scale = this.Scale();
            TopWatermarkDoubleUpDown.Value = UserSettings.All.WatermarkTop;
            LeftWatermarkDoubleUpDown.Value = UserSettings.All.WatermarkLeft;

            if (string.IsNullOrEmpty(UserSettings.All.WatermarkFilePath))
            {
                if (TopWatermarkDoubleUpDown.Value < 0)
                    TopWatermarkDoubleUpDown.Value = 0;

                if (LeftWatermarkDoubleUpDown.Value < 0)
                    LeftWatermarkDoubleUpDown.Value = 0;
            }
        }

        private void SelectWatermark_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = LocalizationHelper.Get("S.Watermark.Select", true),
                Filter = $"{LocalizationHelper.Get("S.Editor.File.Image")} (*.bmp, *.jpg, *.jpeg, *.png)|*.bmp;*.jpg;*.jpeg;*.png",
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            UserSettings.All.WatermarkFilePath = ofd.FileName;
            UserSettings.All.WatermarkSize = 1;

            WatermarkOverlayCanvas.AdjustContent();
        }

        private async void ApplyWatermarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UserSettings.All.WatermarkFilePath) || !File.Exists(UserSettings.All.WatermarkFilePath))
            {
                StatusList.Warning(LocalizationHelper.Get("S.Watermark.WarningNoImage"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Watermark.WarningSelection"));
                return;
            }

            UserSettings.All.WatermarkTop = TopWatermarkDoubleUpDown.Value;
            UserSettings.All.WatermarkLeft = LeftWatermarkDoubleUpDown.Value;

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            WatermarkOverlayCanvas.CanMove = false;
            WatermarkOverlayCanvas.CanResize = false;

            var render = WatermarkOverlayCanvas.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            WatermarkOverlayCanvas.CanMove = true;
            WatermarkOverlayCanvas.CanResize = true;

            #region Remove adorners

            var adornerLayer = AdornerLayer.GetAdornerLayer(WatermarkImage);

            //Remove all the adorners.
            foreach (var adorner in (adornerLayer.GetAdorners(WatermarkImage) ?? new Adorner[0]).OfType<ResizingAdorner>())
            {
                adorner.Destroy();
                adornerLayer.Remove(adorner);
            }

            #endregion

            Cursor = Cursors.AppStarting;

            var selected = await Task.Run(() => OverlayAsync(render, false));

            ShowHint("S.Hint.Overlay");

            await LoadSelectedStarter(selected.Min(), selected.Max());

            ClosePanel();
        }


        private void Border_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Border, LocalizationHelper.Get("S.Editor.Image.Border", true), "Vector.Border", ApplyBorderButton_Click);
        }

        private void BorderProperties_ValueChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CaptionOverlayGrid.Width < 0)
                    return;

                //Measure the border size.
                var left = Math.Min(0, UserSettings.All.BorderLeftThickness);
                var top = Math.Min(0, UserSettings.All.BorderTopThickness);
                var right = Math.Min(0, UserSettings.All.BorderRightThickness);
                var bottom = Math.Min(0, UserSettings.All.BorderBottomThickness);
                var width = CaptionOverlayGrid.Width + Math.Abs(left) + +Math.Abs(right);
                var height = CaptionOverlayGrid.Height + Math.Abs(top) + Math.Abs(bottom);

                BorderBehindOverlayBorder.Width = width;
                BorderBehindOverlayBorder.Height = height;
                BorderPreviewGrid.Margin = new Thickness(left, top, right, bottom);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while trying to measure dropshadow size.");
            }
        }

        private async void ApplyBorderButton_Click(object sender, RoutedEventArgs e)
        {
            var model = BorderViewModel.FromSettings(true);

            if (Math.Abs(model.LeftThickness) < 0.001 && Math.Abs(model.TopThickness) < 0.001 && Math.Abs(model.RightThickness) < 0.001 && Math.Abs(model.BottomThickness) < 0.001)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Border.WarningThickness"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Border.WarningSelection"));
                return;
            }

            if (model.LeftThickness < 0 || model.TopThickness < 0 || model.RightThickness < 0 || model.BottomThickness < 0)
                ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));
            else
                ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            await Task.Run(() => BorderAsync(model));

            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            ClosePanel();
        }


        private void Shadow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Shadow, LocalizationHelper.Get("S.Editor.Image.Shadow", true), "Vector.Shadow", ApplyShadowButton_Click);
        }

        private void ShadowProperties_ValueChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CaptionOverlayGrid.Width < 0)
                    return;

                //Converts the direction in degrees to radians.
                var radians = Math.PI / 180.0 * UserSettings.All.ShadowDirection;
                var offsetX = UserSettings.All.ShadowDepth * Math.Cos(radians);
                var offsetY = UserSettings.All.ShadowDepth * Math.Sin(radians);

                //Each side can have a different offset based on the direction of the shadow.
                var offsetLeft = offsetX < 0 ? offsetX * -1 : 0;
                var offsetTop = offsetY > 0 ? offsetY : 0;
                var offsetRight = offsetX > 0 ? offsetX : 0;
                var offsetBottom = offsetY < 0 ? offsetY * -1 : 0;

                //Measure drop shadow space.
                var marginLeft = offsetLeft > 0 ? offsetLeft + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d - offsetLeft, 0); //- offsetX
                var marginTop = offsetTop > 0 ? offsetTop + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d - offsetTop, 0); //- offsetY
                var marginRight = offsetRight > 0 ? offsetRight + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d + offsetRight, 0); //+ offsetX
                var marginBottom = offsetBottom > 0 ? offsetBottom + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d + offsetBottom, 0); //+ offsetY

                ShadowPreviewGrid.Width = marginLeft + CaptionOverlayGrid.Width + marginRight;
                ShadowPreviewGrid.Height = Math.Round(marginTop + CaptionOverlayGrid.Height + marginBottom, 0);

                ShadowPreviewGrid.Margin = new Thickness(marginRight - marginLeft, marginBottom - marginTop, 0, 0);
                ShadowInternalGrid.Margin = new Thickness(marginLeft, marginTop, marginRight, marginBottom);

                ShadowInternalGrid.InvalidateVisual();
                ShadowPreviewGrid.InvalidateVisual();
                ShadowInternalGrid.InvalidateProperty(EffectProperty);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while trying to measure dropshadow size for the previewer.");
            }
        }

        private async void ApplyShadowButton_Click(object sender, RoutedEventArgs e)
        {
            var model = ShadowViewModel.FromSettings();

            if (Math.Abs(model.Depth) < 0.1 && Math.Abs(model.BlurRadius) < 0.1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Shadow.Warning.Behind"));
                return;
            }

            if (Math.Abs(model.Opacity) < 0.1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Shadow.Warning.Invisible"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            await Task.Run(() => ShadowAsync(model));

            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            ClosePanel();
        }


        private void Obfuscate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Obfuscate, LocalizationHelper.Get("S.Editor.Image.Obfuscate", true), "Vector.Obfuscate", ApplyObfuscateButton_Click);
        }

        private async void ApplyObfuscateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObfuscateOverlaySelectControl.Selected.IsEmpty)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Obfuscate.Warning"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            var region = ObfuscateOverlaySelectControl.Selected;
            var scale = this.Scale();
            var selected = await Task.Run(() => ObfuscateAsync(region, scale, false));

            ShowHint("S.Hint.Overlay");

            await LoadSelectedStarter(selected.Min(), selected.Max());

            ClosePanel();
        }


        private void Cinemagraph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Cinemagraph, LocalizationHelper.Get("S.Editor.Image.Cinemagraph", true), "Vector.Cinemagraph", ApplyCinemagraphButton_Click);
        }

        private async void ApplyCinemagraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (CinemagraphInkCanvas.Strokes.Count == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Cinemagraph.WarningNoDrawing"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            #region Get the Strokes and Clip the Image

            var image = Project.Frames[0].Path.SourceFrom();
            var rectangle = new RectangleGeometry(new Rect(new Point(0, 0), new Size(image.PixelWidth, image.PixelHeight)));
            var geometry = Geometry.Empty;

            foreach (var stroke in CinemagraphInkCanvas.Strokes)
                geometry = Geometry.Combine(geometry, stroke.GetGeometry(), GeometryCombineMode.Union, null);

            geometry = Geometry.Combine(geometry, rectangle, GeometryCombineMode.Xor, null);

            //Since the geometry is bound to the screen, it needs to be scaled to follow the image scale.
            geometry.Transform = new ScaleTransform(this.Scale() / ZoomBoxControl.ImageScale, this.Scale() / ZoomBoxControl.ImageScale);

            var clippedImage = new Image
            {
                Source = image,
                Clip = geometry
            };

            clippedImage.Measure(new Size(image.Width, image.Height));
            clippedImage.Arrange(new Rect(clippedImage.DesiredSize));

            //The ScaleDiff (ScreenScale / ImageScale) must be calculated as if the screen has 96DPI, since the clippedImage is not being attached to any visual.
            //var imageRender = clippedImage.GetScaledRender(1 / ZoomBoxControl.ImageScale, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());
            var imageRender = clippedImage.GetScaledRender(1, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            #endregion

            Cursor = Cursors.AppStarting;

            var selected = await Task.Run(() => OverlayAsync(imageRender, false));

            ShowHint("S.Hint.Overlay");

            await LoadSelectedStarter(selected.Min(), selected.Max());

            ClosePanel();
        }


        private void Progress_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var size = Project.Frames[0].Path.ScaledSize();
            ProgressHorizontalRectangle.Width = size.Width / 2;
            ProgressVerticalRectangle.Height = size.Height / 2;

            ShowPanel(PanelTypes.Progress, LocalizationHelper.Get("S.Editor.Image.Progress", true), "Vector.Progress", ApplyProgressButton_Click);
        }

        private void ProgressPrecisionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || !ProgressGrid.IsVisible || TextRadioButton.IsChecked == false)
                return;

            ChangeProgressTextToCurrent();
        }

        private void ExtendedCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || !ProgressGrid.IsVisible || TextRadioButton.IsChecked == false)
                return;

            ChangeProgressTextToCurrent();
        }

        private void CustomProgressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || !ProgressGrid.IsVisible || TextRadioButton.IsChecked == false)
                return;

            ChangeProgressTextToCurrent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                ProcessHelper.StartWithShell(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, $"Error while trying to navigate to a given URI: '{e?.Uri?.AbsoluteUri}'.");
            }
        }

        private async void ApplyProgressButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            await Task.Run(() => ProgressAsync(ProgressViewModel.FromSettings()));

            await LoadSelectedStarter(0, Project.Frames.Count - 1);

            ClosePanel();
        }

        #endregion

        #endregion

        #region Transitions Tab

        private void Transition_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && FrameListView?.SelectedItems != null && !IsLoading && FrameListView.SelectedIndex != -1;
        }

        private void Fade_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Fade, LocalizationHelper.Get("S.Editor.Fade.Title", true), "Vector.Fade", ApplyFadeButtonButton_Click);
        }

        private async void ApplyFadeButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Fade.WarningSelection"));
                return;
            }

            if (UserSettings.All.FadeToType == FadeModes.Color && UserSettings.All.FadeToColor.A == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Fade.WarningColor"));
                return;
            }

            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex + 1, (int)FadeSlider.Value);

            var index = FrameListView.SelectedIndex;
            var value = (int)FadeSlider.Value;
            var selected = await Task.Run(() => Fade(index, value, null));

            await LoadSelectedStarter(selected, Project.Frames.Count - 1);

            ShowHint("S.Hint.Transition");

            ClosePanel();
        }


        private void Slide_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelTypes.Slide, LocalizationHelper.Get("S.Editor.Slide.Title", true), "Vector.Slide", ApplySlideButtonButton_Click);
        }

        private async void ApplySlideButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Slide.WarningSelection"));
                return;
            }

            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex + 1, (int)SlideSlider.Value);

            var index = FrameListView.SelectedIndex;
            var value = (int)SlideSlider.Value;
            var selected = await Task.Run(() => Slide(index, value, SlideFromType.Right));

            await LoadSelectedStarter(selected, Project.Frames.Count - 1);

            ShowHint("S.Hint.Transition");

            ClosePanel();
        }

        #endregion


        #region Other Events

        #region Panel

        private void PanelAction_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            e.CanExecute = _applyAction != null || ClipboardGrid.IsVisible || LoadRecentGrid.IsVisible;// && ActionGrid.Width > 50 && ActionLowerGrid.IsVisible;
        }

        private void Ok_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ApplyButton.Focus();

            _applyAction?.Invoke(sender, e);

            //If the StatusBand started displaying the message, it means that the action failed.
            if (!StatusList.Children.OfType<StatusBand>().Any(a => a.Starting))
                _applyAction = null;
        }

        private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _applyAction = null;

            ClosePanel(true);
        }

        #endregion

        #region Frame ListView

        private void ListFramesSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView.SelectedIndex != -1 && !IsLoading;
        }

        private void OpenImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                ProcessHelper.StartWithShell(Project.Frames[FrameListView.SelectedIndex].Path);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open Image");
                Dialog.Ok("Open Image", "Impossible to open image.", ex.Message);
            }
        }

        private void ExploreFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{Project.Frames[FrameListView.SelectedIndex].Path}\"");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open Image Folder");
                Dialog.Ok("Open Image", "Impossible to open the image folder.", ex.Message);
            }
        }

        private void FrameListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = Keyboard.Modifiers == ModifierKeys.Alt ? e.SystemKey : e.Key;

            switch (key)
            {
                case Key.Space:
                {
                    if (PlayButton.IsEnabled)
                        PlayPause();

                    //Avoids the selection of the frame by using the Space key.
                    e.Handled = true;
                    break;
                }

                case Key.Right:
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0 || (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == (ModifierKeys.Alt | ModifierKeys.Control))
                    {
                        NextFrame_Executed(sender, null);
                        e.Handled = true;
                    }

                    break;
                }

                case Key.PageDown:
                {
                    NextFrame_Executed(sender, EventArgs.Empty);
                    e.Handled = true;
                    break;
                }
                
                case Key.Left:
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0 || (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == (ModifierKeys.Alt | ModifierKeys.Control))
                    {
                        PreviousFrame_Executed(sender, null);
                        e.Handled = true;
                    }
                    
                    break;
                }

                case Key.PageUp:
                {
                    PreviousFrame_Executed(sender, EventArgs.Empty);
                    e.Handled = true;
                    break;
                }

                case Key.Home:
                {
                    FirstFrame_Executed(sender, null);
                    e.Handled = true;
                    break;
                }

                case Key.End:
                {
                    LastFrame_Executed(sender, null);
                    e.Handled = true;
                    break;
                }
            }
        }

        #endregion

        private void PreviewLoop(int selectedIndex)
        {
            using (var resolution = new TimerResolution(1))
            {
                if (!resolution.SuccessfullySetTargetResolution)
                {
                    LogWriter.Log($"Imprecise timer resolution... Target: {resolution.TargetResolution}, Current: {resolution.CurrentResolution}");
                    Dispatcher.Invoke(() => HasImprecisePlayback = true);
                }

                #region Preview loop

                var sw = new Stopwatch();

                while (_previewToken != null && !_previewToken.IsCancellationRequested)
                {
                    sw.Restart();

                    long frameDelay = Project.Frames[selectedIndex].Delay;

                    // Change active frame
                    Dispatcher.Invoke(() => FrameListView.SelectedIndex = selectedIndex);

                    // Wait for application UI to render changes (there is no point in ordering change of next frame if the previous one is not displayed yet)
                    // Loaded priority could be used but input can become laggy
                    Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

                    var pass = 0;
                    do
                    {
                        pass++;

                        if (Project.Frames.Count - 1 == selectedIndex)
                        {
                            //If the playback should not loop, it will stop at the latest frame.
                            if (!UserSettings.All.LoopedPlayback)
                            {
                                // This will ensure that latest frame will be shown if drops frames behind is enabled
                                if (UserSettings.All.DropFramesDuringPreviewIfBehind && pass > 1)
                                    break;

                                Dispatcher.Invoke(Pause);
                                return;
                            }

                            selectedIndex = 0;
                        }
                        else
                        {
                            selectedIndex++;
                        }

                        if (!UserSettings.All.DropFramesDuringPreviewIfBehind)
                            break;

                        if (pass >= 2)
                            frameDelay += Project.Frames[selectedIndex].Delay;
                    }
                    while (sw.ElapsedMilliseconds >= frameDelay);

                    if (Project.Frames[selectedIndex].Delay == 0)
                        Project.Frames[selectedIndex].Delay = 10;

                    //Wait rest of actual frame delay time
                    if (sw.ElapsedMilliseconds >= frameDelay)
                        continue;

                    while (sw.Elapsed.TotalMilliseconds < frameDelay)
                        Thread.Sleep(1);

                    //SpinWait.SpinUntil(() => sw.ElapsedMilliseconds >= frameDelay);
                }

                sw.Stop();

                #endregion
            }
        }

        private void Control_DragEnter(object sender, DragEventArgs e)
        {
            Pause();

            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private async void Control_Drop(object sender, DragEventArgs e)
        {
            Pause();

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] fileNames || fileNames.Length == 0)
                return;

            #region Validation

            var extensionList = fileNames.Select(s => Path.GetExtension(s).ToLowerInvariant()).ToList();

            var media = new[] { ".jpg", ".jpeg", ".gif", ".bmp", ".png", ".avi", ".mp4", ".wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals(".stg") || x.Equals(".zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(Path.GetExtension(x)));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dialog.Ok(LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Title"),
                    LocalizationHelper.Get("S.Editor.DragDrop.MultipleFiles.Instruction"),
                    LocalizationHelper.Get("S.Editor.DragDrop.MultipleFiles.Message"), Icons.Warning);
                return;
            }

            if (mediaCount == 0 && projectCount == 0)
            {
                Dialog.Ok(LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Title"),
                    LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Instruction"),
                    LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Message"), Icons.Warning);
                return;
            }

            //if (projectCount > 0)
            //{
            //    Dialog.Ok(LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Title"),
            //        LocalizationHelper.Get("S.Editor.DragDrop.InvalidProject.Instruction"),
            //        LocalizationHelper.Get("S.Editor.DragDrop.InvalidProject.Message"), Dialog.Icons.Warning);
            //    return;
            //}

            #endregion

            #region Importing options

            //If inserted into new recording or forced into new one.
            if (Project == null || !Project.Any || e.KeyStates == DragDropKeyStates.ControlKey || projectCount > 0)
                await Task.Run(() => ImportFrom(fileNames.ToList()));
            else
                await InsertImportFrom(fileNames.ToList());

            #endregion

            ClosePanel(removeEvent: true);
            CommandManager.InvalidateRequerySuggested();
            GC.Collect();
        }

        private void CancelLoadingButton_Click(object sender, RoutedEventArgs e)
        {
            _abortLoading = true;
            CancelLoadingButton.IsEnabled = false;
        }

        private void InkCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || sender is not InkCanvas canvas)
                return;

            //This event only exists because the InkPanel eats the Esc key used in Commands.
            //if something is not selected, run the command to close the panel.
            if (canvas.ActiveEditingMode != InkCanvasEditingMode.Select && canvas.GetSelectedStrokes().Any())
                CancelCommandBinding.Command.Execute(null);
        }

        #endregion

        #region Methods

        #region Load

        internal async void LoadFromArguments()
        {
            if (!Arguments.FileNames.Any())
                return;

            #region Validation

            var extensionList = Arguments.FileNames.Select(Path.GetExtension).ToList();

            var media = new[] { "jpg", "jpeg", "gif", "bmp", "png", "apng", "avi", "mkv", "mp4", "webp", "webm", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            //TODO: Later I need to implement another validation for multiple video files.

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(FindResource("S.Editor.InvalidLoadingFiles").ToString()));
                return;
            }

            if (projectCount > 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(FindResource("S.Editor.InvalidLoadingProjects").ToString()));
                return;
            }

            #endregion

            await Task.Run(() => ImportFrom(Arguments.FileNames));

            ClosePanel(removeEvent: true);
            CommandManager.InvalidateRequerySuggested();
            GC.Collect();
        }

        #region Async Loading

        /// <summary>
        /// Loads the new frames and clears the old ones.
        /// </summary>
        /// <param name="newProject">The project to load.</param>
        /// <param name="isNew">True if this is a new project.</param>
        /// <param name="clear">True if should clear the current list of frames.</param>
        /// <param name="createFlag">True if it should create a flag for single use, a mutex.</param>
        internal async void LoadProject(ProjectInfo newProject, bool isNew = true, bool clear = true, bool createFlag = false)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;

            _viewModel.Frames.Clear();
            ZoomBoxControl.Zoom = 1;

            #region Discard

            if (clear || isNew)
            {
                //Clear clipboard data.
                ClipboardListBox.Items.Clear();
                Util.Clipboard.Items?.Clear();
            }

            //TODO: Settings to choose if the project will be discarded.
            if (clear && Project != null && Project.Any)
            {
                Project.Persist();

                if (!UserSettings.All.NotifyProjectDiscard || Dialog.Ask(LocalizationHelper.Get("S.Editor.DiscardProject.Title"), LocalizationHelper.Get("S.Editor.DiscardPreviousProject.Instruction"),
                        LocalizationHelper.Get("S.Editor.DiscardPreviousProject.Message"), false))
                {
                    await Task.Run(() => Discard(Project));
                    FilledList = false;
                    FrameListView.SelectionChanged += FrameListView_SelectionChanged;

                    Project = newProject;

                    ActionStack.Clear();
                    ActionStack.Project = Project;

                    LoadCallback(await Task.Run(Load));

                    GC.Collect();
                    return;
                }

                Project.Clear();
                ActionStack.Clear();
            }

            #endregion

            if (isNew)
            {
                if (!clear) //Already clears the project above if flag 'clear' is true.
                    Project?.Clear();

                Project = newProject;

                if (createFlag)
                    Project.CreateMutex();

                ActionStack.Clear();
                ActionStack.Project = Project;
            }

            var result = await Task.Run(Load);
            LoadCallback(result);
        }

        private bool Load()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    IsCancelable = true;
                });

                if (!Project.IsNew)
                    Project.Persist();

                var corruptedList = new List<FrameInfo>();
                var count = 0;

                #region Get images from cache

                if (File.Exists(Project.CachePath))
                {
                    ShowProgress(LocalizationHelper.Get("S.Editor.RetrievingFromCache"), Project.Frames.Count);

                    using (var fileStream = new FileStream(Project.CachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var deflateStream = new DeflateStream(fileStream, CompressionMode.Decompress))
                        {
                            var number = 0;

                            for (var index = 0; index < Project.Frames.Count; index++)
                            {
                                var frame = Project.Frames[index];

                                if (_abortLoading)
                                    return false;

                                Dispatcher.Invoke(() => { UpdateProgress(number++); });
                                BitmapSource source;

                                try
                                {
                                    var array = deflateStream.ReadBytesUntilFull((int)frame.DataLength);

                                    if (Project.BitDepth == 24)
                                    {
                                        //24 bits: ((Project.Width * 24 + 31) / 32) * 4
                                        source = BitmapSource.Create(Project.Width, Project.Height, Project.Dpi, Project.Dpi, PixelFormats.Bgr24, null, array, ((Project.Width * 24 + 31) / 32) * 4);
                                        source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                                    }
                                    else
                                    {
                                        //32 bits: 4 * Project.Width
                                        source = BitmapSource.Create(Project.Width, Project.Height, Project.Dpi, Project.Dpi, PixelFormats.Bgra32, null, array, 4 * Project.Width);
                                    }
                                }
                                catch (EndOfStreamException d)
                                {
                                    LogWriter.Log(d, "It was not possible to read more bytes from the frame cache, since it reached the end");
                                    break;
                                }

                                using (var stream = new FileStream(frame.Path, FileMode.Create))
                                {
                                    var encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(source));
                                    encoder.Save(stream);
                                    stream.Close();
                                }
                            }

                            GC.Collect();
                        }
                    }

                    File.Delete(Project.CachePath);
                }

                #endregion

                ShowProgress(LocalizationHelper.Get("S.Editor.LoadingFrames"), Project.Frames.Count);

                #region Check if there's any missing frames (and remove them)

                var processedFrame = 0;

                foreach (var frame in Project.Frames)
                {
                    if (_abortLoading)
                        break;

                    if (!File.Exists(frame.Path))
                        corruptedList.Add(frame);

                    UpdateProgress(processedFrame++);
                }

                if (_abortLoading)
                    return false;

                //Remove the corrupted frames.
                foreach (var frame in corruptedList)
                    Project.Frames.Remove(frame);

                if (Project.Frames.Count == 0)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        Dialog.Ok(LocalizationHelper.Get("S.Editor.LoadingFrames"), LocalizationHelper.Get("S.Editor.LoadingFrames.ProjectCorrupted.Instruction"),
                            LocalizationHelper.Get("S.Editor.LoadingFrames.ProjectCorrupted.Message"));
                    });
                    return false;
                }

                #endregion

                //If the project was never loaded inside the editor. Projects created with any version older than 2.14 won't enter here.
                if (Project.IsNew)
                {
                    Project.IsNew = false;
                    Project.Persist();

                    //Get enabled tasks.
                    var tasks = UserSettings.All.AutomatedTasksList?.Cast<BaseTaskViewModel>().Where(w => w != null && w.IsEnabled).ToList() ?? new List<BaseTaskViewModel>();

                    if (tasks.Any())
                    {
                        #region Initialize the previewer

                        Dispatcher.Invoke(() =>
                        {
                            ZoomBoxControl.LoadFromPath(Project.Frames[0].Path);

                            OverlayGrid.Visibility = Visibility.Visible;
                            OverlayGrid.Opacity = 1;
                            OverlayGrid.UpdateLayout();

                            var size = ZoomBoxControl.GetElementSize();

                            CaptionOverlayGrid.Width = size.Width;
                            CaptionOverlayGrid.Height = size.Height;
                            CaptionOverlayGrid.UpdateLayout();
                        });

                        #endregion

                        foreach (var task in tasks)
                        {
                            if (_abortLoading)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ZoomBoxControl.ImageSource = null;

                                    OverlayGrid.Visibility = Visibility.Collapsed;
                                    OverlayGrid.Opacity = 0;
                                });

                                return false;
                            }

                            try
                            {
                                switch (task.TaskType)
                                {
                                    case TaskTypes.MouseEvents:
                                    {
                                        if (Project.CreatedBy == ProjectByType.ScreenRecorder)
                                            MouseEventsAsync(task as MouseEventsViewModel ?? MouseEventsViewModel.FromSettings());

                                        break;
                                    }

                                    case TaskTypes.KeyStrokes:
                                    {
                                        if (Project.CreatedBy == ProjectByType.ScreenRecorder)
                                            KeyStrokesAsync(task as KeyStrokesViewModel ?? KeyStrokesViewModel.FromSettings());

                                        break;
                                    }

                                    case TaskTypes.Delay:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            DelayAsync(task as DelayViewModel ?? DelayViewModel.FromSettings(), true, true);

                                        break;
                                    }

                                    case TaskTypes.Progress:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            ProgressAsync(task as ProgressViewModel ?? ProgressViewModel.FromSettings());

                                        break;
                                    }

                                    case TaskTypes.Border:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            BorderAsync(task as BorderViewModel ?? BorderViewModel.FromSettings());

                                        break;
                                    }

                                    case TaskTypes.Shadow:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            ShadowAsync(task as ShadowViewModel ?? ShadowViewModel.FromSettings());

                                        break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                LogWriter.Log(e, "Error while applying automatic task");
                                Dispatcher.Invoke(() => { ExceptionDialog.Ok(e, "ScreenToGif", "Error while executing a task", e.Message); });
                            }
                        }

                        #region Reset the previewer state

                        Dispatcher.Invoke(() =>
                        {
                            ZoomBoxControl.ImageSource = null;

                            OverlayGrid.Visibility = Visibility.Collapsed;
                            OverlayGrid.Opacity = 0;
                        });

                        #endregion
                    }
                }

                #region Load frames into the ListView

                Dispatcher.Invoke(() => _viewModel.Frames.Clear());

                foreach (var frame in Project.Frames)
                {
                    if (_abortLoading)
                        break;

                    frame.Index = count++;

                    Dispatcher.Invoke(() =>
                    {
                        var item = new FrameViewModel
                        {
                            Number = frame.Index,
                            Image = frame.Path,
                            Delay = frame.Delay
                        };

                        _viewModel.Frames.Add(item);

                        UpdateProgress(item.Number);
                    });
                }

                if (_abortLoading)
                    return false;

                if (corruptedList.Any())
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        Dialog.Ok(LocalizationHelper.Get("S.Editor.LoadingFrames"), LocalizationHelper.Get("S.Editor.LoadingFrames.FramesCorrupted.Instruction"),
                            LocalizationHelper.Get("S.Editor.LoadingFrames.FramesCorrupted.Message"));
                    });
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error in loading frames");
                Dispatcher.Invoke(() => ErrorDialog.Ok(Title, "Error loading frames", "It was not possible to load all the frames.", ex));

                return false;
            }
            finally
            {
                _abortLoading = false;
            }
        }

        private async void LoadCallback(bool result)
        {
            Cursor = Cursors.Arrow;
            IsLoading = false;
            IsCancelable = false;

            if (Project.Any)
                FilledList = true;

            if (!result)
            {
                CancelLoadingButton.IsEnabled = true; //TODO: Is this right?

                await Task.Run(() => Discard(Project));

                WelcomeGrid.BeginStoryboard(this.FindStoryboard("ShowWelcomeBorderStoryboard"), HandoffBehavior.Compose);

                FilledList = false;
                IsLoading = false;

                WelcomeTextBlock.Text = LocalizationHelper.Get(Humanizer.WelcomeInfo());
                SymbolTextBlock.Text = Humanizer.Welcome();

                UpdateStatistics();

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;

                CommandManager.InvalidateRequerySuggested();

                return;
            }

            FrameListView.SelectedIndex = -1;
            FrameListView.SelectedIndex = 0; //TODO: Get the latest selected frame if it's the same project.
            ZoomBoxControl.PixelSize = Project.Frames[0].Path.ScaledSize();
            ZoomBoxControl.ImageScale = Project.Frames[0].Path.ScaleOf();
            ZoomBoxControl.RefreshImage();

            //ListBoxSelector.SetIsEnabled(FrameListView, true);
            //new ListBoxSelector(FrameListView);

            HideProgress();
            UpdateStatistics();

            WelcomeGrid.BeginStoryboard(this.FindStoryboard("HideWelcomeBorderStoryboard"), HandoffBehavior.Compose);

            CommandManager.InvalidateRequerySuggested();

            SetFocusOnCurrentFrame();

            //Adjust the window size based on the frame size.
            if (UserSettings.All.AutomaticallySizeOnContent && SizeToContentCommand.Command != null && SizeToContentCommand.Command.CanExecute(null))
                SizeToContentCommand.Command.Execute(null);

            //Adjust the frame zoom based on the window size.
            if (UserSettings.All.AutomaticallyFitImage && FitImageCommand.Command != null && FitImageCommand.Command.CanExecute(null))
                FitImageCommand.Command.Execute(null);
        }

        #endregion

        #region Async Selective Loading

        private async Task LoadSelectedStarter(int start, int? end = null)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;
            ShowProgress(LocalizationHelper.Get("S.Editor.UpdatingFrames"), Project.Frames.Count, true);

            //Persists the project to the disk.
            await Task.Run(() => Project.Persist());

            await Task.Run(() => LoadSelected(start, end));

            //Adjust the UI.
            Cursor = Cursors.Arrow;
            HideProgress();

            if (LastSelected != -1)
            {
                ZoomBoxControl.ImageSource = null;

                var valid = Project.ValidIndex(LastSelected);

                if (valid > -1)
                {
                    ZoomBoxControl.ImageSource = Project.Frames[valid].Path;
                    ZoomBoxControl.PixelSize = Project.Frames[0].Path.ScaledSize();
                    ZoomBoxControl.ImageScale = Project.Frames[0].Path.ScaleOf();
                    ZoomBoxControl.RefreshImage();
                    FrameListView.ScrollIntoView(_viewModel.Frames[valid]);
                }
            }

            UpdateStatistics();

            IsLoading = false;
            CommandManager.InvalidateRequerySuggested();

            SetFocusOnCurrentFrame();
        }

        private bool LoadSelected(int start, int? end)
        {
            end ??= Project.Frames.Count - 1;
            UpdateProgress(0);

            try
            {
                //Perhaps there's no need for part of this anymore, since there's a working virtualization now.
                Dispatcher.Invoke(() =>
                {
                    //For each changed frame.
                    for (var index = start; index <= end; index++)
                    {
                        //Check if within limits.
                        if (index <= _viewModel.Frames.Count - 1)
                        {
                            #region Edit the existing frame

                            var frame = _viewModel.Frames[index];
                            frame.Number = index;
                            frame.Delay = Project.Frames[index].Delay;
                            frame.Image = null; //To update the image.
                            frame.Image = Project.Frames[index].Path;

                            Project.Frames[index].Index = index;

                            #endregion
                        }
                        else
                        {
                            #region Create another frame

                            _viewModel.Frames.Add(new FrameViewModel
                            {
                                Number = index,
                                Image = Project.Frames[index].Path,
                                Delay = Project.Frames[index].Delay
                            });

                            Project.Frames[index].Index = index;

                            #endregion
                        }

                        UpdateProgress(index);
                    }
                });

                if (Project.Frames.Count > 0)
                    Dispatcher.Invoke(() => { FilledList = true; });

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading Error");
                return false;
            }
        }

        #endregion

        #region Async Import

        private List<FrameInfo> InsertInternal(string fileName, string pathTemp, ref Size previousSize, ref double previousDpi, ref bool warn, ref bool warnSize)
        {
            List<FrameInfo> listFrames;

            try
            {
                switch (fileName.Split('.').Last().ToLowerInvariant())
                {
                    case "stg":
                    case "zip":
                    {
                        listFrames = ImportFromProject(fileName, pathTemp);
                        break;
                    }

                    case "gif":
                    {
                        listFrames = ImportFromGif(fileName, pathTemp);
                        break;
                    }

                    case "avi":
                    case "mkv":
                    case "mp4":
                    case "wmv":
                    case "webp":
                    case "webm":
                    {
                        listFrames = ImportFromVideo(fileName, pathTemp);
                        break;
                    }

                    case "apng":
                    case "png":
                    {
                        listFrames = ImportFromPng(fileName, pathTemp, ref previousSize, ref previousDpi, ref warn, ref warnSize);
                        break;
                    }

                    default:
                    {
                        listFrames = ImportFromImage(fileName, pathTemp, ref previousSize, ref previousDpi, ref warn, ref warnSize);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Import error");

                return new List<FrameInfo>();
            }

            return listFrames;
        }

        private bool ImportFrom(List<string> fileList)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(LocalizationHelper.Get("S.Editor.PreparingImport"), 100, false);

            var project = new ProjectInfo().CreateProjectFolder(ProjectByType.Editor);
            var currentDpi = 0D;
            var currentSize = Size.Empty;
            var wasWarned = false;
            var wasWarnedSized = false;

            //Adds each image to a list.
            foreach (var file in fileList)
            {
                if (Dispatcher.HasShutdownStarted)
                    return false;

                var warn = false;
                var warnSize = false;
                var frame = InsertInternal(file, project.FullPath, ref currentSize, ref currentDpi, ref warn, ref warnSize);

                //Size and DPI validations.
                if (warnSize && !wasWarnedSized)
                {
                    wasWarnedSized = true;
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.Warning.DifferentSize")));
                    continue;
                }

                if (warn && !wasWarned)
                {
                    wasWarned = true;
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.Warning.DifferentDpi")));
                    continue;
                }

                if (frame != null)
                    project.Frames.AddRange(frame);
            }

            if (project.Frames.Count == 0)
            {
                if (Dispatcher.HasShutdownStarted)
                    return false;

                Dispatcher.Invoke(() =>
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;

                    if (project.Any)
                        FilledList = true;

                    HideProgress();

                    CommandManager.InvalidateRequerySuggested();
                    SetFocusOnCurrentFrame();
                });

                return false;
            }

            Dispatcher.Invoke(() => LoadProject(project));

            return true;
        }

        private async Task<bool> InsertImportFrom(List<string> fileList)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(LocalizationHelper.Get("S.Editor.PreparingImport"), 100);

            var project = new ProjectInfo().CreateProjectFolder(ProjectByType.Editor);
            var currentDpi = 0D;
            var currentSize = Size.Empty;
            var wasWarned = false;
            var wasWarnedSized = false;

            //Adds each image to a list.
            foreach (var file in fileList)
            {
                if (Dispatcher.HasShutdownStarted)
                    return false;

                var warn = false;
                var warnSize = false;
                var frame = InsertInternal(file, project.FullPath, ref currentSize, ref currentDpi, ref warn, ref warnSize);

                //Size and DPI validations.
                if (warnSize && !wasWarnedSized)
                {
                    wasWarnedSized = true;
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.Warning.DifferentSize")));
                    continue;
                }

                if (warn && !wasWarned)
                {
                    wasWarned = true;
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.Warning.DifferentDpi")));
                    continue;
                }

                if (frame != null)
                    project.Frames.AddRange(frame);
            }

            if (!project.Any)
            {
                project.ReleaseMutex();

                Dispatcher.Invoke(() =>
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;
                });

                return false;
            }

            var list = Dispatcher.Invoke(() =>
            {
                #region Insert

                var insert = new Insert(Project.Frames, project.Frames, FrameListView.SelectedIndex) { Owner = this };
                var result = insert.ShowDialog();

                if (result.HasValue && result.Value)
                    return insert.CurrentList;

                return null;

                #endregion
            });

            project.ReleaseMutex();

            if (list != null)
            {
                ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex, project.Frames.Count);

                Dispatcher.Invoke(() => Project.Frames = list);

                await LoadSelectedStarter(FrameListView.SelectedIndex, Project.Frames.Count - 1); //Check

                return true;
            }

            #region Enabled the UI

            Dispatcher.Invoke(() =>
            {
                HideProgress();

                if (LastSelected != -1)
                {
                    ZoomBoxControl.ImageSource = null;
                    ZoomBoxControl.ImageSource = Project.Frames[LastSelected].Path;

                    FrameListView.ScrollIntoView(_viewModel.Frames[LastSelected]);
                }

                Cursor = Cursors.Arrow;
                IsLoading = false;
            });

            #endregion

            return false;
        }

        #endregion

        private List<FrameInfo> ImportFromProject(string source, string pathTemp)
        {
            try
            {
                //Extract to the folder of the newly created project.
                ZipFile.ExtractToDirectory(source, pathTemp);

                List<FrameInfo> list;

                if (File.Exists(Path.Combine(pathTemp, "Project.json")))
                {
                    //Read as text.
                    var json = File.ReadAllText(Path.Combine(pathTemp, "Project.json"));

                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        var ser = new DataContractJsonSerializer(typeof(ProjectInfo));
                        var project = ser.ReadObject(ms) as ProjectInfo;
                        list = project.Frames;
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(pathTemp, "List.sb")))
                        throw new Exception("Project not compatible with this version");

                    throw new FileNotFoundException("Impossible to open project.", "List.sb");
                }

                //Shows the ProgressBar
                ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), list.Count);

                var count = 0;
                foreach (var frame in list)
                {
                    //Change the file path to the current one.
                    frame.Path = Path.Combine(pathTemp, Path.GetFileName(frame.Path));

                    count++;
                    UpdateProgress(count);
                }

                return list;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Importing project");
                Dispatcher.Invoke(() => Dialog.Ok("ScreenToGif", "Impossible to load project", ex.Message));
                return new List<FrameInfo>();
            }
        }

        private List<FrameInfo> ImportFromGif(string source, string pathTemp)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), 50, true);

            var listFrames = new List<FrameInfo>();

            var decoder = ImageMethods.GetDecoder(source, out var gifMetadata) as GifBitmapDecoder;

            ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), decoder?.Frames?.Count ?? 0);

            if (decoder.Frames.Count <= 0)
                return listFrames;

            var fullSize = ImageMethods.GetFullSize(decoder, gifMetadata);
            var index = 0;

            BitmapSource baseFrame = null;
            foreach (var rawFrame in decoder.Frames)
            {
                var metadata = ImageMethods.GetFrameMetadata(decoder, gifMetadata, index);

                var bitmapSource = ImageMethods.MakeFrame(fullSize, rawFrame, metadata, baseFrame, 96D);

                #region Disposal Method

                switch (metadata.DisposalMethod)
                {
                    case FrameDisposalMethod.None:
                    case FrameDisposalMethod.DoNotDispose:
                        baseFrame = bitmapSource;
                        break;
                    case FrameDisposalMethod.RestoreBackground:
                        baseFrame = ImageMethods.IsFullFrame(metadata, fullSize) ? null : ImageMethods.ClearArea(bitmapSource, metadata, 96D);
                        break;
                    case FrameDisposalMethod.RestorePrevious:
                        //Reuse same base frame.
                        break;
                }

                #endregion

                #region Each Frame

                var fileName = Path.Combine(pathTemp, $"{index} {DateTime.Now:hh-mm-ss-ffff}.png");

                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(stream);
                    stream.Close();
                }

                //It should not throw a overflow exception because of the maximum value for the milliseconds.
                var frame = new FrameInfo(fileName, (int)metadata.Delay.TotalMilliseconds);
                listFrames.Add(frame);

                UpdateProgress(index);

                GC.Collect(1);

                #endregion

                index++;
            }

            return listFrames;
        }

        private List<FrameInfo> ImportFromPng(string source, string pathTemp, ref Size previousSize, ref double previousDpi, ref bool warn, ref bool warnSize)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), 50, true);

            using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var apng = new Apng(stream);
                var success = apng.ReadFrames();

                if (!success)
                    return ImportFromImage(source, pathTemp, ref previousSize, ref previousDpi, ref warn, ref warnSize);

                var fullSize = new System.Drawing.Size((int)apng.Ihdr.Width, (int)apng.Ihdr.Height);
                var list = new List<FrameInfo>();

                BitmapSource baseFrame = null;
                for (var index = 0; index < apng.Actl.NumFrames; index++)
                {
                    var metadata = apng.GetFrame(index);
                    var rawFrame = metadata.ImageData.SourceFrom();

                    var bitmapSource = Apng.MakeFrame(fullSize, rawFrame, metadata, baseFrame);

                    #region Disposal Method

                    switch (metadata.DisposeOp)
                    {
                        case Apng.DisposeOps.None: //No disposal is done on this frame before rendering the next; the contents of the output buffer are left as is.
                            baseFrame = bitmapSource;
                            break;
                        case Apng.DisposeOps.Background: //The frame's region of the output buffer is to be cleared to fully transparent black before rendering the next frame.
                            baseFrame = baseFrame == null || Apng.IsFullFrame(metadata, fullSize) ? null : Apng.ClearArea(baseFrame, metadata);
                            break;
                        case Apng.DisposeOps.Previous: //The frame's region of the output buffer is to be reverted to the previous contents before rendering the next frame.
                            //Reuse same base frame.
                            break;
                    }

                    #endregion

                    #region Each Frame

                    var fileName = Path.Combine(pathTemp, $"{index} {DateTime.Now:hh-mm-ss-ffff}.png");

                    //TODO: Do I need to verify the DPI of the image?

                    using (var output = new FileStream(fileName, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(output);
                        stream.Close();
                    }

                    list.Add(new FrameInfo(fileName, metadata.Delay));

                    UpdateProgress(index);

                    GC.Collect(1);

                    #endregion
                }

                return list;
            }
        }

        private List<FrameInfo> ImportFromImage(string source, string pathTemp, ref Size previousSize, ref double previousDpi, ref bool warn, ref bool warnSize)
        {
            var fileName = Path.Combine(pathTemp, $"{0} {DateTime.Now:hh-mm-ss-ffff}.png");

            #region Save the Image to the Recording Folder

            BitmapSource bitmap = new BitmapImage(new Uri(source));

            //Don't let it import multiple images with different DPI's.
            if (previousDpi > 0 && Math.Abs(previousDpi - bitmap.DpiX) > 0.09)
            {
                warn = true;
                return null;
            }

            if (!previousSize.IsEmpty && (bitmap.PixelHeight != (int)previousSize.Height || bitmap.PixelWidth != (int)previousSize.Width))
            {
                warnSize = true;
                return null;
            }

            if (Math.Abs(previousDpi) < 0.01)
                previousDpi = bitmap.DpiX;

            if (bitmap.PixelHeight != (int)previousSize.Height || bitmap.PixelWidth != (int)previousSize.Width)
                previousSize = new Size(bitmap.PixelWidth, bitmap.PixelHeight);

            if (bitmap.Format != PixelFormats.Bgra32)
                bitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                stream.Close();
            }

            GC.Collect();

            #endregion

            return new List<FrameInfo> { new(fileName, 66) };
        }

        private List<FrameInfo> ImportFromVideo(string source, string pathTemp)
        {
            //Get frames from video.
            return Dispatcher?.Invoke(() =>
            {
                var vid = new VideoSource
                {
                    RootFolder = pathTemp,
                    VideoPath = source,
                    Owner = this
                };
                var result = vid.ShowDialog();

                return result.HasValue && result.Value ? vid.Frames : null;
            });
        }

        #endregion

        #region Playback

        private void PlayPause()
        {
            lock (UserSettings.Lock)
            {
                if (_previewToken != null || !NotPreviewing)
                {
                    if (_previewToken != null)
                    {
                        _previewToken.Cancel();
                        _previewToken.Dispose();
                        _previewToken = null;
                    }

                    NotPreviewing = true;
                    PlayButton.Text = LocalizationHelper.Get("S.Editor.Playback.Play");
                    PlayButton.Icon = FindResource("Vector.Play") as Brush;
                    PlayPauseButton.Icon = FindResource("Vector.Play") as Brush;

                    PlayMenuItem.Header = LocalizationHelper.Get("S.Editor.Playback.Play");
                    PlayMenuItem.Icon = FindResource("Vector.Play") as Brush;

                    SetFocusOnCurrentFrame();
                    UpdateOtherStatistics();
                }
                else
                {
                    NotPreviewing = false;
                    PlayButton.Text = LocalizationHelper.Get("S.Editor.Playback.Pause");
                    PlayButton.Icon = FindResource("Vector.Pause") as Brush;
                    PlayPauseButton.Icon = FindResource("Vector.Pause") as Brush;

                    PlayMenuItem.Header = LocalizationHelper.Get("S.Editor.Playback.Pause");
                    PlayMenuItem.Icon = FindResource("Vector.Pause") as Brush;

                    #region Starts playing the next frame

                    if (Project.Frames.Count - 1 == FrameListView.SelectedIndex)
                        FrameListView.SelectedIndex = 0;
                    else
                        FrameListView.SelectedIndex++;

                    #endregion

                    if (Project.Frames[FrameListView.SelectedIndex].Delay == 0)
                        Project.Frames[FrameListView.SelectedIndex].Delay = 10;

                    _previewToken = new CancellationTokenSource();
                    var selectedIndex = FrameListView.SelectedIndex;

                    Task.Run(() => PreviewLoop(selectedIndex), _previewToken.Token);
                }
            }
        }

        private void Pause()
        {
            lock (UserSettings.Lock)
            {
                if (_previewToken == null && NotPreviewing)
                    return;

                if (_previewToken != null)
                {
                    _previewToken.Cancel();
                    _previewToken.Dispose();
                    _previewToken = null;
                }

                NotPreviewing = true;
                PlayButton.Text = LocalizationHelper.Get("S.Editor.Playback.Play");
                PlayButton.Icon = FindResource("Vector.Play") as Brush;
                PlayPauseButton.Icon = FindResource("Vector.Play") as Brush;

                PlayMenuItem.Header = LocalizationHelper.Get("S.Editor.Playback.Play");
                PlayMenuItem.Icon = FindResource("Vector.Play") as Brush;
            }

            SetFocusOnCurrentFrame();
            UpdateOtherStatistics();
        }

        #endregion

        #region UI

        #region Progress

        private void ShowProgress(string description, int maximum, bool isIndeterminate = false)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Content = description;
                StatusProgressBar.Maximum = maximum;
                StatusProgressBar.Value = 0;
                StatusProgressBar.IsIndeterminate = isIndeterminate;
                StatusGrid.Visibility = Visibility.Visible;

                TaskbarItemInfo.ProgressState = isIndeterminate ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = 0;
            }, DispatcherPriority.Loaded);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = MathExtensions.CrossMultiplication(StatusProgressBar.Maximum, value, null) / 100d;

                StatusProgressBar.IsIndeterminate = false;
                StatusProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                StatusGrid.Visibility = Visibility.Collapsed;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            });
        }

        #endregion

        private void SelectNear(int index)
        {
            if (_viewModel.Frames.Count - 1 < index)
            {
                SelectFrame(_viewModel.Frames.Count - 1, true);
                return;
            }

            SelectFrame(index, true);
        }

        private void SelectFrame(int index, bool focus)
        {
            var forceScroll = FrameListView.SelectedIndex == index;

            WasChangingSelection = true;
            FrameListView.SelectedIndex = index;

            if (focus)
            {
                FrameListView.Focus();

                if (forceScroll)
                {
                    FrameListView.ScrollIntoView(_viewModel.Frames[FrameListView.SelectedIndex]);

                    SetFocusOnCurrentFrame();
                }
            }
        }

        private void AdjustFrameNumbers(int startIndex)
        {
            for (var index = startIndex; index < _viewModel.Frames.Count; index++)
            {
                Project.Frames[index].Index = index;
                _viewModel.Frames[index].Number = index;
            }
        }

        private void FocusOnSelectedFrames()
        {
            FrameListView.Focus();

            var item = FrameListView.SelectedItems.OfType<FrameViewModel>().FirstOrDefault();

            if (item != null)
                FrameListView.ScrollIntoView(item);

            SetFocusOnCurrentFrame();
        }

        private async void ShowPanel(PanelTypes type, string title, string vector, Action<object, RoutedEventArgs> apply = null)
        {
            var focusFirstVisibleChild = true;

            HideAllVisibleGrids();

            #region Overlay

            ZoomBoxControl.SaveCurrentZoom();

            if (Project != null && Project.Any && type < 0)
            {
                ZoomBoxControl.Zoom = 1.0;
                var size = ZoomBoxControl.GetElementSize();

                CaptionOverlayGrid.Width = size.Width;
                CaptionOverlayGrid.Height = size.Height;
            }

            #endregion

            #region Commons

            ActionTitleTextBlock.Text = title;
            ActionIconBorder.Background = FindResource(vector) as Brush;

            Util.Other.RemoveRoutedEventHandlers(ApplyButton, ButtonBase.ClickEvent);

            if (apply != null)
            {
                ApplyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Action.Apply");
                ApplyButton.Icon = FindResource("Vector.Ok") as Brush;
                _applyAction = apply;

                ActionLowerGrid.Visibility = Visibility.Visible;
            }
            else
            {
                ActionLowerGrid.Visibility = Visibility.Collapsed;
            }

            #endregion

            #region Type

            switch (type)
            {
                case PanelTypes.NewAnimation:
                    NewGrid.Visibility = Visibility.Visible;
                    break;
                case PanelTypes.SaveAs:
                    ApplyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Action.Save");
                    ApplyButton.Icon = FindResource("Vector.Save") as Brush;

                    //WARNING: Temporary!!!
                    var grid = new ExportPanel();
                    grid.Save += (sender, args) =>
                    {
                        SaveAs_Executed(null, null);
                    };
                    grid.Validated += (sender, args) =>
                    {
                        StatusList.Warning(LocalizationHelper.Get(args.MessageKey), args.Reason, args.Action);
                    };
                    grid.ValidationRemoved += (sender, args) =>
                    {
                        StatusList.Remove(StatusType.Warning, args.Reason);
                    };

                    var frameCountBinding = new Binding();
                    frameCountBinding.ElementName = "FrameListView";
                    frameCountBinding.Path = new PropertyPath("Items.Count");
                    frameCountBinding.Mode = BindingMode.OneWay;
                    frameCountBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(grid, ExportPanel.FrameCountProperty, frameCountBinding);

                    var durationBinding = new Binding();
                    durationBinding.ElementName = "EditorWindow";
                    durationBinding.Path = new PropertyPath("TotalDuration");
                    durationBinding.Mode = BindingMode.OneWay;
                    durationBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(grid, ExportPanel.TotalTimeProperty, durationBinding);

                    var selectionCountBinding = new Binding();
                    selectionCountBinding.ElementName = "FrameListView";
                    selectionCountBinding.Path = new PropertyPath("SelectedItems.Count");
                    selectionCountBinding.Mode = BindingMode.OneWay;
                    selectionCountBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(grid, ExportPanel.SelectionCountProperty, selectionCountBinding);

                    var currentFrameBinding = new Binding
                    {
                        //Path = new PropertyPath("Frames[CurrentIndex]"), // I don't really get why data context is already a FrameViewModel here instead of the EditorViewModel but it means we need no Path
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    };
                    BindingOperations.SetBinding(grid, ExportPanel.CurrentFrameProperty, currentFrameBinding);

                    CustomContentControl.Content = grid;
                    CustomContentControl.Visibility = Visibility.Visible;

                    focusFirstVisibleChild = false;
                    break;
                case PanelTypes.LoadRecent:
                    ApplyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Action.Open");
                    ApplyButton.Icon = FindResource("Vector.Open") as Brush;
                    LoadRecentGrid.Visibility = Visibility.Visible;

                    //Load list.
                    await Task.Run(LoadRecentAsync);

                    Cursor = Cursors.Arrow;
                    IsLoading = false;

                    HideProgress();
                    CommandManager.InvalidateRequerySuggested();

                    break;
                case PanelTypes.Clipboard:
                    ClipboardGrid.Visibility = Visibility.Visible;
                    break;
                case PanelTypes.Resize:
                {
                    var image = Project.Frames[0].Path.SourceFrom();

                    ResizePanel.DataContext = ResizeViewModel.FromSettings(image.PixelWidth, image.PixelHeight, image.DpiX);
                    ResizePanel.Visibility = Visibility.Visible;

                    ShowHint("S.Hint.ApplyAll", true);

                    break;
                }

                case PanelTypes.FlipRotate:
                    FlipRotateGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.FlipRotate2", true);
                    break;
                case PanelTypes.Crop:

                    #region Crop

                    CropGrid.Visibility = Visibility.Visible;

                    AddCropToElement(CropAreaGrid);

                    BottomCropNumericUpDown.Scale = TopCropNumericUpDown.Scale = RightCropNumericUpDown.Scale = LeftCropNumericUpDown.Scale = this.Scale();

                    BottomCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height - (CaptionOverlayGrid.Height * .1));
                    TopCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height * .1);

                    RightCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width - (CaptionOverlayGrid.Width * .1));
                    LeftCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width * .1);

                    ShowHint("S.Hint.ApplyAll", true);

                    #endregion

                    break;
                case PanelTypes.Caption:
                    CaptionGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelTypes.FreeText:
                    FreeTextGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelTypes.TitleFrame:
                    TitleFrameGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.TitleFrame2", true);
                    break;
                case PanelTypes.KeyStrokes:
                    KeyStrokesLabel.Text = "Ctrl + C";
                    KeyStrokesGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelTypes.FreeDrawing:
                    FreeDrawingGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelTypes.Shapes:
                    ShapesGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);

                    ShapeProperties_Changed(this, null);
                    ShapeType_SelectionChanged(ShapesListBox, null);
                    break;
                case PanelTypes.Watermark:

                    #region Watermark

                    //#region Arrange

                    //if (UserSettings.All.WatermarkLeft < 0)
                    //    UserSettings.All.WatermarkLeft = 10;

                    //if (UserSettings.All.WatermarkLeft + 10 > CaptionOverlayGrid.Width)
                    //    UserSettings.All.WatermarkLeft = 10;

                    //if (UserSettings.All.WatermarkTop < 0)
                    //    UserSettings.All.WatermarkTop = 10;

                    //if (UserSettings.All.WatermarkTop + 10 > CaptionOverlayGrid.Height)
                    //    UserSettings.All.WatermarkTop = 10;

                    //#endregion

                    WatermarkGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);

                    #endregion

                    break;
                case PanelTypes.Border:
                    BorderProperties_ValueChanged(null, null);
                    BorderGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelectedOrAll", true);
                    break;
                case PanelTypes.Obfuscate:
                    ObfuscateOverlaySelectControl.Scale = this.Scale();
                    ObfuscateOverlaySelectControl.Retry();
                    ObfuscateGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelTypes.Progress:
                    ProgressGrid.Visibility = Visibility.Visible;
                    ChangeProgressTextToCurrent();
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelTypes.Shadow:
                    ShadowProperties_ValueChanged(null, null);
                    ShadowGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelTypes.OverrideDelay:
                    OverrideDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelTypes.IncreaseDecreaseDelay:
                    IncreaseDecreaseDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelTypes.ScaleDelay:
                    ScaleDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelTypes.Cinemagraph:
                    CinemagraphGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.Cinemagraph", true);
                    break;
                case PanelTypes.Fade:
                    FadeGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Transitions.Info", true);
                    break;
                case PanelTypes.Slide:
                    SlideGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Transitions.Info", true);
                    break;
                case PanelTypes.ReduceFrames:
                    ReduceGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelectedOrAll", true);
                    break;
                case PanelTypes.RemoveDuplicates:
                    RemoveDuplicatesGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelTypes.MouseEvents:
                    MouseEventsGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelTypes.SmoothLoop:
                    SmoothLoopGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
            }

            #endregion

            #region Focus

            if (focusFirstVisibleChild)
            {
                var visible = ActionInternalGrid.Children.OfType<Grid>().FirstOrDefault(x => x.Visibility == Visibility.Visible);

                if (visible != null)
                {
                    visible.Focus();
                    visible.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }

            #endregion

            #region Animate

            if (type is PanelTypes.SaveAs or PanelTypes.LoadRecent && ActionGrid.Width < 300)
                ActionGrid.BeginStoryboard(this.FindStoryboard("ShowExtendedPanelStoryboard"), HandoffBehavior.Compose);
            else if (type != PanelTypes.SaveAs && type != PanelTypes.LoadRecent && ActionGrid.Width is < 5 or > 280)
                ActionGrid.BeginStoryboard(this.FindStoryboard("ShowPanelStoryboard"), HandoffBehavior.Compose);

            #endregion

            #region Overlay Grid

            if (OverlayGrid.Opacity < 1 && type < 0)
                OverlayGrid.BeginStoryboard(this.FindStoryboard("ShowOverlayGridStoryboard"), HandoffBehavior.Compose);
            else if (OverlayGrid.Opacity > 0 && type > 0)
                OverlayGrid.BeginStoryboard(this.FindStoryboard("HideOverlayGridStoryboard"), HandoffBehavior.Compose);

            #endregion

            CommandManager.InvalidateRequerySuggested();
        }

        private void HideAllVisibleGrids()
        {
            foreach (var child in ActionInternalGrid.Children.OfType<FrameworkElement>().Where(x => x.Visibility == Visibility.Visible))
            {
                child.Visibility = Visibility.Collapsed;

                //Persist the latest settings if the panel has any to be saved.
                if (child.DataContext is IPersistent per)
                    per.Persist();
            }

            CustomContentControl.Content = null;

            ShapeDrawingCanvas.DeselectAll();
        }

        private void ClosePanel(bool isCancel = false, bool removeEvent = false)
        {
            StatusList.Remove(StatusType.Warning);

            if (ActionGrid.ActualWidth > 0)
                ZoomBoxControl.RestoreSavedZoom();

            HideHint();

            if (isCancel)
                SetFocusOnCurrentFrame();

            if (removeEvent)
                _applyAction = null;

            BeginStoryboard(this.FindStoryboard("HidePanelStoryboard"), HandoffBehavior.Compose);
            BeginStoryboard(this.FindStoryboard("HideOverlayGridStoryboard"), HandoffBehavior.Compose);

            HideAllVisibleGrids();
        }

        private List<int> SelectedFramesIndex()
        {
            return FrameListView.SelectedItems.OfType<FrameViewModel>().Select(x => _viewModel.Frames.IndexOf(x)).OrderBy(y => y).ToList();
        }

        private bool UpdatePositioning(bool onLoad = true)
        {
            //TODO: When the DPI changes, these values are still from the latest dpi.
            var top = onLoad ? UserSettings.All.EditorTop : Top;
            var left = onLoad ? UserSettings.All.EditorLeft : Left;
            var width = onLoad ? UserSettings.All.EditorWidth : Width;
            var height = onLoad ? UserSettings.All.EditorHeight : Height;
            var state = onLoad ? UserSettings.All.EditorWindowState : WindowState;

            //If the position was never set, let it center on screen.
            if (double.IsNaN(top) && double.IsNaN(left))
                return false;

            //The catch here is to get the closest monitor from current Top/Left point.
            var monitors = MonitorHelper.AllMonitorsScaled(this.Scale());
            var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary);

            if (closest == null)
                return false;

            //To much to the Left.
            if (closest.WorkingArea.Left > left + width - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > top + height - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < left + 100)
                left = closest.WorkingArea.Right - width;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < top + 100)
                top = closest.WorkingArea.Bottom - height;

            if (top > int.MaxValue || top < int.MinValue || left > int.MaxValue || left < int.MinValue || width > int.MaxValue || width < 0 || height > int.MaxValue || height < 0)
            {
                var desc = $"On load: {onLoad}\nScale: {this.Scale()}\n\n" +
                           $"Screen: {closest.AdapterName}\nBounds: {closest.Bounds}\n\nTopLeft: {top}x{left}\nWidthHeight: {width}x{height}\n\n" +
                           $"TopLeft Settings: {UserSettings.All.EditorTop}x{UserSettings.All.EditorLeft}\nWidthHeight Settings: {UserSettings.All.EditorWidth}x{UserSettings.All.EditorHeight}";
                LogWriter.Log("Wrong Editor window sizing", desc);
                return false;
            }

            //To eliminate the flicker of moving the window to the correct screen, hide and then show it again.
            if (onLoad)
                Opacity = 0;

            //First move the window to the final monitor, so that the UI scale can be adjusted.
            this.MoveToScreen(closest);

            Top = top;
            Left = left;
            Width = width;
            Height = height;
            WindowState = state;

            if (onLoad)
                Opacity = 1;

            return true;
        }

        #endregion

        #region Other

        public void NotificationUpdated()
        {
            RibbonTabControl.UpdateNotifications();
        }

        public EncoderListViewItem EncodingAdded(int id)
        {
            return RibbonTabControl.AddEncoding(id, IsActive);
        }

        public void EncodingUpdated(int? id = null, bool onlyStatus = false)
        {
            RibbonTabControl.UpdateEncoding(id, onlyStatus);
        }

        public EncoderListViewItem EncodingRemoved(int id)
        {
            return RibbonTabControl.RemoveEncoding(id);
        }


        /// <summary>
        /// If there's an update available, it will adjust the UI to warn the user about it.
        /// It does not control the notification list, that's separated.
        /// </summary>
        private void DisplayUpdatePromoter()
        {
            if (Global.UpdateAvailable == null)
            {
                UpdateStackPanel.Visibility = Visibility.Collapsed;
                return;
            }

            UpdateVersionRun.Text = $"{Global.UpdateAvailable.Version}";
            UpdateSizeRun.Text = Global.UpdateAvailable.InstallerSize > 0 ? Humanizer.BytesToString(Global.UpdateAvailable.InstallerSize) : "";
            UpdateStackPanel.Visibility = Visibility.Visible;
        }

        private async void Discard(bool notify = true)
        {
            Pause();

            if (notify && !Dialog.Ask(LocalizationHelper.Get("S.Editor.DiscardProject.Title"), LocalizationHelper.Get("S.Editor.DiscardProject.Instruction"), LocalizationHelper.Get("S.Editor.DiscardProject.Message"), false))
                return;

            #region Prepare UI

            ClosePanel();

            FrameListView.SelectedIndex = -1;
            FrameListView.SelectionChanged -= FrameListView_SelectionChanged;

            _viewModel.Frames.Clear();
            ClipboardListBox.Items.Clear();
            Util.Clipboard.Items.Clear();
            ZoomBoxControl.Clear();

            #endregion

            if (Project == null || !Project.Any)
                return;

            await Task.Run(() => Discard(Project));

            WelcomeGrid.BeginStoryboard(this.FindStoryboard("ShowWelcomeBorderStoryboard"), HandoffBehavior.Compose);

            FilledList = false;
            IsLoading = false;

            WelcomeTextBlock.Text = LocalizationHelper.Get(Humanizer.WelcomeInfo());
            SymbolTextBlock.Text = Humanizer.Welcome();

            UpdateStatistics();

            FrameListView.SelectionChanged += FrameListView_SelectionChanged;

            CommandManager.InvalidateRequerySuggested();
        }

        private void DeleteFrame(int index)
        {
            //Delete the File from the disk.
            File.Delete(Project.Frames[index].Path);

            //Remove from the list.
            Project.Frames.RemoveAt(index);
            _viewModel.Frames.RemoveAt(index);
        }

        private List<FrameInfo> SelectedFrames()
        {
            var selectedIndexList = Dispatcher.Invoke(SelectedFramesIndex);
            return Project.Frames.Where(x => selectedIndexList.Contains(Project.Frames.IndexOf(x))).ToList();
        }

        private void UpdateStatistics()
        {
            TotalDuration = TimeSpan.FromMilliseconds(Project.Frames.Sum(x => x.Delay));
            FrameSize = Project.Frames.Count > 0 ? Project.Frames[0].Path.ScaledSize() : new Size(0, 0);
            FrameScale = Project.Frames.Count > 0 ? Convert.ToInt32(Project.Frames[0].Path.DpiOf() / 96d * 100d) : 0;
            AverageDelay = Project.Frames.Count > 0 ? Project.Frames.Average(x => x.Delay) : 0;
            FrameDpi = Project.Frames.Count > 0 ? Math.Round(Project.Frames[0].Path.DpiOf(), 0) : 0d;
        }

        private void UpdateOtherStatistics()
        {
            if (FrameListView.SelectedIndex > -1 && FrameListView.SelectedIndex < Project.Frames.Count)
                CurrentTime = TimeSpan.FromMilliseconds(Project.Frames.Take(FrameListView.SelectedIndex + 1).Sum(x => x.Delay));
            else
                CurrentTime = TimeSpan.Zero;
        }

        private void ShowHint(string hint, bool isPermanent = false, params object[] values)
        {
            if (HintTextBlock.Visibility == Visibility.Visible)
                BeginStoryboard(this.FindStoryboard("HideHintStoryboard"), HandoffBehavior.Compose);

            HintTextBlock.Text = values.Length == 0 ? LocalizationHelper.Get(hint) : LocalizationHelper.GetWithFormat(hint, values);

            BeginStoryboard(this.FindStoryboard(isPermanent ? "ShowPermanentHintStoryboard" : "ShowHintStoryboard"), HandoffBehavior.Compose);
        }

        private void HideHint()
        {
            if (HintTextBlock.Visibility == Visibility.Visible)
                BeginStoryboard(this.FindStoryboard("HideHintStoryboard"), HandoffBehavior.Compose);
        }

        private void SetFocusOnCurrentFrame()
        {
            FrameListView.Focus();

            if (FrameListView.SelectedItem is not FrameViewModel current)
                return;

            if (FrameListView.ItemContainerGenerator.ContainerFromItem(current) is ListViewItem container)
            {
                Keyboard.Focus(container);
                container.Focus();
            }
        }

        private string GetProgressText(int precision, bool showTotal, string format, string dateFormat, int startNumber, long cumulative, long total, int current)
        {
            try
            {
                switch (precision)
                {
                    case 0: //Minutes
                        return showTotal ? TimeSpan.FromMilliseconds(cumulative).ToString(@"m\:ss") + "/" + TimeSpan.FromMilliseconds(total).ToString(@"m\:ss") : TimeSpan.FromMilliseconds(cumulative).ToString(@"m\:ss");
                    case 1: //Seconds
                        return showTotal ? (int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds + "/" + TimeSpan.FromMilliseconds(total).TotalSeconds + " s" : (int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds + " s";
                    case 2: //Milliseconds
                        return showTotal ? cumulative + "/" + total + " ms" : cumulative + " ms";
                    case 3: //Percentage
                        var count = (double)Project.Frames.Count;
                        return (current / count * 100).ToString("##0.#", CultureInfo.CurrentUICulture) + (showTotal ? "/100%" : " %");
                    case 4: //Frame number
                        return showTotal ? (startNumber + current - 1) + "/" + (startNumber + Project.Frames.Count - 1) : current.ToString();
                    case 5: //Custom
                        return format
                            .Replace("$ms", cumulative.ToString())
                            .Replace("$s", ((int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds).ToString())
                            .Replace("$m", TimeSpan.FromMilliseconds(cumulative).ToString())
                            .Replace("$p", (current / (double)Project.Frames.Count * 100).ToString("##0.#", CultureInfo.CurrentUICulture))
                            .Replace("$f", (startNumber + current - 1).ToString())
                            .Replace("@ms", total.ToString())
                            .Replace("@s", ((int)TimeSpan.FromMilliseconds(total).TotalSeconds).ToString())
                            .Replace("@m", TimeSpan.FromMilliseconds(total).ToString(@"m\:ss"))
                            .Replace("@p", "100")
                            .Replace("@f", (startNumber + Project.Frames.Count - 1).ToString());
                    case 6: //Actual date/time
                        return showTotal ? $"{Project.CreationDate.AddMilliseconds(cumulative).ToString(dateFormat)} -> {Project.CreationDate.AddMilliseconds(total).ToString(dateFormat)}"
                            : Project.CreationDate.AddMilliseconds(cumulative).ToString(dateFormat);
                    default:
                        return "???";
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Invalid progress format.");
                return "???";
            }
        }

        private void ChangeProgressTextToCurrent()
        {
            var total = Project.Frames.Sum(y => y.Delay);
            var cumulative = 0L;

            for (var j = 0; j < FrameListView.SelectedIndex; j++)
                cumulative += Project.Frames[j].Delay;

            ProgressHorizontalTextBlock.Text = GetProgressText(UserSettings.All.ProgressPrecision, UserSettings.All.ProgressShowTotal, UserSettings.All.ProgressFormat, UserSettings.All.ProgressDateFormat,
                UserSettings.All.ProgressStartNumber, cumulative, total, FrameListView.SelectedIndex + 1);
        }

        #endregion

        #endregion


        #region Async

        private void LoadRecentAsync()
        {
            ShowProgress(LocalizationHelper.Get("S.Recent.Searching"), 100, true);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Enumerate recent projects

            var list = new List<ProjectInfo>();

            try
            {
                Dispatcher.Invoke(() => RecentDataGrid.ItemsSource = null);

                var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Recording");

                if (!Directory.Exists(path))
                {
                    Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => RecentDataGrid.ItemsSource = null));
                    return;
                }

                var folderList = Directory.GetDirectories(path).Select(x => new DirectoryInfo(x)).ToList();

                foreach (var folder in folderList)
                {
                    var file = Path.Combine(folder.FullName, "Project.json");

                    if (!File.Exists(file))
                        continue;

                    var json = File.ReadAllText(file);

                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        var ser = new DataContractJsonSerializer(typeof(ProjectInfo));

                        //Ignore empty projects.
                        if (ser.ReadObject(ms) is not ProjectInfo project || project.Frames.Count == 0 || !project.Frames.Any(x => File.Exists(x.Path)))
                            continue;

                        list.Add(project);
                    }
                }

                //Waits the animation to complete before filling the grid.
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    RecentDataGrid.ItemsSource = list;

                    if (RecentDataGrid.Items.Count > 0)
                    {
                        RecentDataGrid.Sort(ListSortDirection.Descending, "CreationDate");
                        RecentDataGrid.FocusOnFirstCell();
                    }
                }));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Loading list of recent");

                Dispatcher.Invoke(() => ErrorDialog.Ok("ScreenToGif", "Error while enumerating recent projects", ex.Message, ex));
            }

            #endregion
        }

        private bool SaveAsync(ExportPreset preset)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.PreparingSaving"), 1, true);

            #region Filter out frames

            var indexes = new List<int>();

            if (preset.ExportPartially)
            {
                switch (preset.PartialExport)
                {
                    case PartialExportModes.FrameExpression:
                    {
                        var blocks = preset.PartialExportFrameExpression.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var block in blocks)
                        {
                            //Frame range.
                            if (block.Contains("-"))
                            {
                                var subs = block.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                                if (!int.TryParse(subs[0], out var start) || !int.TryParse(subs[1], out var end))
                                    continue;

                                if (start == end)
                                {
                                    //Single frame.
                                    indexes.Add(start); //indexes.Add(frames[start]);
                                }
                                else if (start < end)
                                {
                                    //Normal range.
                                    for (var i = start; i <= end; i++)
                                        indexes.Add(i); //indexes.Add(frames[i]);
                                }
                                else
                                {
                                    //Reversed range.
                                    //The CopyToExport() method below has no support for this yet, so it just copies the frames in order anyway.
                                    for (var i = start; i >= end; i--)
                                        indexes.Add(i); //indexes.Add(frames[i]);
                                }
                            }
                            else
                            {
                                if (int.TryParse(block, out var number))
                                    indexes.Add(number);
                            }
                        }

                        break;
                    }

                    case PartialExportModes.FrameRange:
                    {
                        indexes = Enumerable.Range(preset.PartialExportFrameStart, preset.PartialExportFrameEnd - preset.PartialExportFrameStart).ToList();
                        break;
                    }

                    case PartialExportModes.TimeRange:
                    {
                        var span = TimeSpan.Zero;

                        //Playback mode.
                        if (preset.PartialExportTimeStart < preset.PartialExportTimeEnd)
                        {
                            //Normal playback.
                            for (var i = 0; i < Project.Frames.Count; i++)
                            {
                                if (span >= preset.PartialExportTimeStart && span <= preset.PartialExportTimeEnd)
                                    indexes.Add(i);

                                if (span > preset.PartialExportTimeEnd)
                                    break;

                                span = span.Add(TimeSpan.FromMilliseconds(Project.Frames[i].Delay));
                            }
                        }
                        else
                        {
                            //Reversed playback.
                            //The CopyToExport() method below has no support for this yet, so it just copies the frames in order anyway.
                            span = TimeSpan.FromMilliseconds(Project.Frames.Sum(s => s.Delay));

                            for (var i = Project.Frames.Count - 1; i >= 0; i--)
                            {
                                if (span >= preset.PartialExportTimeEnd && span <= preset.PartialExportTimeStart)
                                    indexes.Add(i);

                                if (span > preset.PartialExportTimeStart)
                                    break;

                                span = span.Subtract(TimeSpan.FromMilliseconds(Project.Frames[i].Delay));
                            }
                        }

                        break;
                    }

                    case PartialExportModes.Selection:
                    {
                        indexes = Dispatcher.Invoke(SelectedFramesIndex);
                        break;
                    }
                }
            }

            #endregion

            #region Last minute validation

            //Validates each file name when saving multiple images (if more than one image, that will not be zipped).
            if (preset is ImagePreset { ZipFiles: false })
            {
                if (preset.OverwriteMode != OverwriteModes.Allow)
                {
                    var output = Path.Combine(preset.OutputFolder, preset.ResolvedFilename);
                    var padSize = (Project.Frames.Count - 1).ToString().Length;

                    //With or without exporting it partially.
                    var any = preset.ExportPartially ? indexes.Any(a => File.Exists($"{output} {(a + "").PadLeft(padSize, '0')}" + preset.Extension)) :
                        Project.Frames.Any(a => File.Exists($"{output} {(a.Index + "").PadLeft(padSize, '0')}" + preset.Extension));

                    if (any)
                    {
                        if (preset.OverwriteMode == OverwriteModes.Prompt)
                        {
                            if (Dispatcher.Invoke(() => !Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Dialogs.Overwrite.Title"), LocalizationHelper.Get("S.SaveAs.Dialogs.OverwriteMultiple.Instruction"),
                                LocalizationHelper.Get("S.SaveAs.Dialogs.OverwriteMultiple.Message"))))
                            {
                                Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.SaveAs.Warning.Overwrite")));
                                return false;
                            }
                        }
                        else
                        {
                            Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.SaveAs.Warning.Overwrite")));
                            return false;
                        }
                    }
                }

                if (indexes.Count > 1 && !Dispatcher.Invoke(() => Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Dialogs.Multiple.Title"),
                    LocalizationHelper.Get("S.SaveAs.Dialogs.Multiple.Instruction"), LocalizationHelper.GetWithFormat("S.SaveAs.Dialogs.Multiple.Message", indexes.Count))))
                {
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.SaveAs.Warning.Canceled")));
                    return false;
                }
            }

            #endregion

            //Copy the frames, so it can be manipulated without problem.
            var copied = Project.CopyToExport(indexes, preset.Type == ExportFormats.Stg, preset.Type == ExportFormats.Gif && preset.Encoder == EncoderTypes.ScreenToGif);

            EncodingManager.StartEncoding(copied, preset);

            if (preset.ExportAsProjectToo)
            {
                var copiedAux = Project.CopyToExport(indexes, true);

                //Get default project encoder settings.
                var projectPreset = (UserSettings.All.ExportPresets.OfType<StgPreset>().FirstOrDefault(f => f.IsSelectedForEncoder) ?? UserSettings.All.ExportPresets.OfType<StgPreset>().FirstOrDefault() ?? StgPreset.Default).ShallowCopy();
                projectPreset.OutputFolder = preset.OutputFolder;
                projectPreset.OutputFilename = preset.OutputFilename;
                projectPreset.ResolvedFilename = preset.ResolvedFilename;
                projectPreset.ExportPartially = false;
                projectPreset.PickLocation = true;
                projectPreset.UploadFile = false;
                projectPreset.SaveToClipboard = false;
                projectPreset.ExecuteCustomCommands = false;

                EncodingManager.StartEncoding(copiedAux, projectPreset);
            }

            return true;
        }

        private void Discard(ProjectInfo project)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.DiscardingFrames"), project.Frames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            try
            {
                var count = 0;
                foreach (var frame in project.Frames)
                {
                    File.Delete(frame.Path);

                    UpdateProgress(count++);
                }

                var folderList = Directory.EnumerateDirectories(project.FullPath).ToList();

                ShowProgress(LocalizationHelper.Get("S.Editor.DiscardingFolders"), folderList.Count);

                count = 0;
                foreach (var folder in folderList)
                {
                    if (!folder.Contains("Encode "))
                        Directory.Delete(folder, true);

                    UpdateProgress(count++);
                }

                //Deletes the JSON file.
                if (File.Exists(project.ProjectPath))
                    File.Delete(project.ProjectPath);
            }
            catch (IOException io)
            {
                LogWriter.Log(io, "Error while trying to Discard the Project");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the project", ex.Message));
                LogWriter.Log(ex, "Error while trying to Discard the Project");
            }

            ActionStack.Clear();
            project.Clear();

            HideProgress();
        }

        private void Resize(int width, int height, double dpi, BitmapScalingMode scalingQuality)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ResizingFrames"), Project.Frames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in Project.Frames)
            {
                var png = new PngBitmapEncoder();
                png.Frames.Add(ImageMethods.ResizeImage((BitmapImage)frame.Path.SourceFrom(), width, height, 0, dpi, scalingQuality));

                using (Stream stm = File.OpenWrite(frame.Path))
                    png.Save(stm);

                UpdateProgress(count++);
            }
        }

        private void Crop(Int32Rect rect)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.CroppingFrames"), Project.Frames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in Project.Frames)
            {
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(frame.Path.CropFrom(rect)));

                using (Stream stm = File.OpenWrite(frame.Path))
                    png.Save(stm);

                UpdateProgress(count++);
            }
        }

        private void ProgressAsync(ProgressViewModel model)
        {
            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var total = Project.Frames.Sum(y => y.Delay);
            var thickness = model.Thickness * ZoomBoxControl.ScaleDiff;
            var fontSize = model.FontSize * ZoomBoxControl.ScaleDiff;

            var count = 1;
            var cumulative = 0L;

            foreach (var frame in Project.Frames)
            {
                if (_abortLoading)
                    return;

                var image = frame.Path.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));

                    //TODO: Test with high dpi.
                    if (model.Type == ProgressTypes.Bar)
                    {
                        #region Bar

                        if (model.Orientation == Orientation.Horizontal)
                        {
                            //Width changes (Current/Total * Available size), Height is thickness.
                            var width = count / (double)Project.Frames.Count * image.Width; //* image.Width instead?
                            var left = model.HorizontalAlignment == HorizontalAlignment.Left ? 0 :
                                model.HorizontalAlignment == HorizontalAlignment.Right ? image.Width - width :
                                (image.Width - width) / 2d;
                            var top = model.VerticalAlignment == VerticalAlignment.Top ? 0 :
                                model.VerticalAlignment == VerticalAlignment.Bottom ? image.Height - thickness :
                                (image.Height - thickness) / 2d;

                            drawingContext.DrawRectangle(new SolidColorBrush(model.Color), null, new Rect(Math.Round(left, 0), Math.Round(top, 0), Math.Round(width, 0), thickness));
                        }
                        else
                        {
                            //Height changes (Current/Total * Available size), Width is thickness.
                            var height = count / (double)Project.Frames.Count * image.Height;
                            var left = model.HorizontalAlignment == HorizontalAlignment.Left ? 0 :
                                model.HorizontalAlignment == HorizontalAlignment.Right ? image.Width - thickness :
                                (image.Width - thickness) / 2d;
                            var top = model.VerticalAlignment == VerticalAlignment.Top ? 0 :
                                model.VerticalAlignment == VerticalAlignment.Bottom ? image.Height - height :
                                (image.Height - height) / 2d;

                            drawingContext.DrawRectangle(new SolidColorBrush(model.Color), null, new Rect(Math.Round(left, 0), Math.Round(top, 0), thickness, Math.Round(height, 0)));
                        }

                        #endregion
                    }
                    else
                    {
                        #region Text

                        if (count > 0)
                            cumulative += Project.Frames[count - 1].Delay;

                        //Calculate size.
                        var text = GetProgressText(model.Precision, model.ShowTotal, model.Format, model.DateFormat, model.StartNumber, cumulative, total, count); //FrameListView.SelectedIndex
                        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                            new Typeface(model.FontFamily, model.FontStyle, model.FontWeight, default), fontSize, new SolidColorBrush(model.FontColor),
                            null, TextFormattingMode.Ideal);

                        var width = formatted.Width + 4; //2px padding for both sides.
                        var height = formatted.Height;
                        var left = model.HorizontalAlignment == HorizontalAlignment.Left ? 0 :
                            model.HorizontalAlignment == HorizontalAlignment.Right ? image.Width - width :
                            (image.Width - width) / 2d;
                        var top = model.VerticalAlignment == VerticalAlignment.Top ? 0 :
                            model.VerticalAlignment == VerticalAlignment.Bottom ? image.Height - height :
                            (image.Height - height) / 2d;

                        //Draw background rectangle and the text.
                        drawingContext.DrawRectangle(new SolidColorBrush(model.Color), null, new Rect(Math.Round(left, 0), Math.Round(top, 0), Math.Round(width, 0), Math.Round(height, 0)));
                        drawingContext.DrawText(formatted, new Point(Math.Round(left + 2, 0), Math.Round(top, 0)));

                        #endregion
                    }
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress((count++) - 1);
            }
        }

        private List<int> OverlayAsync(RenderTargetBitmap render, bool forAll = false)
        {
            var frameList = forAll ? Project.Frames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? Project.Frames.Select(x => Project.Frames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), frameList.Count);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.Path.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawImage(render, new Rect(0, 0, render.Width, render.Height));
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }

            return selectedList;
        }

        private int TitleFrame(RenderTargetBitmap render, int selected, double dpi)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.CreatingTitleFrame"), 1, true);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Save Image

            var name = Path.GetFileNameWithoutExtension(Project.Frames[selected].Path);
            var folder = Path.GetDirectoryName(Project.Frames[selected].Path);
            var fileName = Path.Combine(folder, $"{name} TF {DateTime.Now:hh-mm-ss}.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(render));

            //Saves the image into a file using the encoder.
            using (Stream stream = File.Create(fileName))
                encoder.Save(stream);

            GC.Collect();

            #endregion

            //Adds to the List
            Project.Frames.Insert(selected, new FrameInfo(fileName, UserSettings.All.TitleFrameDelay));

            return selected;
        }

        private void KeyStrokesAsync(KeyStrokesViewModel model)
        {
            Dispatcher?.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var auxList = Project.Frames.CopyList();

            #region Make the keystrokes start earlier

            if (model.KeyStrokesEarlier)
            {
                //Checks if there is a key stroke that needs to be shown earlier, into the previous frames.
                for (var outer = 0; outer < auxList.Count; outer++)
                {
                    //For each frame, check if any other key pressed later wants to be shown on this frame.

                    var amount = 0;

                    //If it's the last item of the list.
                    //if (outer == auxList.Count - 1 && amount <= model.KeyStrokesEarlierBy)
                    //    auxList[outer].KeyList.InsertRange(auxList[outer].KeyList.Count, auxList[0].KeyList);

                    //Check next items.
                    for (var inner = outer + 1; inner < auxList.Count - 1; inner++)
                    {
                        //For each frame, check if a next frame needs to show their key strokes.
                        amount += auxList[inner].Delay;

                        //If next item bleeds into this frame, insert on the list.
                        if (inner < auxList.Count - 1 && auxList[inner + 1].Delay <= model.KeyStrokesEarlierBy)
                            auxList[outer].KeyList.InsertRange(auxList[outer].KeyList.Count, auxList[inner].KeyList);

                        //Stops veryfying the previous frames if the delay sum is greater than the maximum.
                        if (amount >= model.KeyStrokesEarlierBy)
                            break;
                    }
                }
            }

            #endregion

            #region Expand the keystrokes

            if (model.KeyStrokesExtended)
            {
                //Checks from the end to the start, if there is a key stroke that needs to 'bleed'/'delayed' into the next frames.
                //I need to check from the end to the start so the keystrokes stays in position.
                for (var outer = auxList.Count - 1; outer > 0; outer--)
                {
                    var amount = 0;

                    if (outer == 1 && amount <= model.KeyStrokesDelay)
                    {
                        var listA = auxList[0].KeyList.TakeWhile((_, i) => !auxList[0].KeyList.Skip(i).SequenceEqual(auxList[1].KeyList.Take(auxList[0].KeyList.Count - i))).Concat(auxList[1].KeyList);

                        auxList[1].KeyList = new List<IKeyGesture>(listA);
                    }

                    //Check previous items.
                    for (var inner = outer - 1; inner >= 0; inner--)
                    {
                        //For each frame, check if a previous frame needs to show their key strokes.
                        amount += auxList[inner].Delay;

                        //If previous item bleeds into this frame, insert on the list.
                        if (inner > 0 && auxList[inner - 1].Delay <= model.KeyStrokesDelay)
                        {
                            var listA = auxList[inner].KeyList.TakeWhile((_, i) => !auxList[inner].KeyList.Skip(i).SequenceEqual(auxList[outer].KeyList.Take(auxList[inner].KeyList.Count - i))).Concat(auxList[outer].KeyList);

                            auxList[outer].KeyList = new List<IKeyGesture>(listA);
                        }

                        //Stops veryfying the previous frames if the delay sum is greater than the maximum.
                        if (amount >= model.KeyStrokesDelay)
                            break;
                    }
                }
            }

            #endregion

            //Pen used for drawing the outline of the text shape.
            var pen = new Pen(new SolidColorBrush(model.KeyStrokesOutlineColor), model.KeyStrokesOutlineThickness)
            {
                DashCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round
            };

            var count = 0;
            foreach (var frame in auxList)
            {
                if (_abortLoading)
                    return;

                if (!frame.KeyList.Any())
                {
                    UpdateProgress(count++);
                    continue;
                }

                #region Removes any duplicated modifier key

                var keyList = new List<IKeyGesture>();
                for (var i = 0; i < frame.KeyList.Count; i++)
                {
                    if (model.KeyStrokesIgnoreInjected && frame.KeyList[i].IsInjected)
                        continue;

                    //Ignore Control, Shift, Alt and Windows keys if not acting as modifiers.
                    if (model.KeyStrokesIgnoreNonModifiers && frame.KeyList[i].Key is >= Key.LeftShift and <= Key.RightAlt or Key.LWin or Key.RWin)
                        continue;

                    //If there's another key ahead on the same frame.
                    if (frame.KeyList.Count > i + 1)
                    {
                        //If this frame being added will be repeated next, ignore.
                        if (frame.KeyList[i + 1].Key == frame.KeyList[i].Key && frame.KeyList[i + 1].Modifiers == frame.KeyList[i].Modifiers)
                            continue;

                        //TODO: If there's a key between the current key and the one that is repeated, they are going to be shown.

                        //If this frame being added will be repeated within the next key presses as a modifier, ignore.
                        if (frame.KeyList[i].Key is Key.LeftCtrl or Key.RightCtrl && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Control) != 0)
                            continue;

                        if (frame.KeyList[i].Key is Key.LeftShift or Key.RightShift && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Shift) != 0)
                            continue;

                        if (frame.KeyList[i].Key is Key.LeftAlt or Key.RightAlt && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Alt) != 0)
                            continue;

                        if (frame.KeyList[i].Key is Key.LWin or Key.RWin && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Windows) != 0)
                            continue;
                    }

                    //Removes the previous modifier key, if a combination is next to it: "LeftCtrl Control + A" will be "Control + A". (This checks if the next modifier is not present as a current key).
                    if (i + 1 > frame.KeyList.Count - 1 || !(frame.KeyList[i].Key != Key.Left && frame.KeyList[i].Key != Key.Right && frame.KeyList[i + 1].Modifiers.ToString().Contains(frame.KeyList[i].Key.ToString().Remove("Left", "Right").Replace("Ctrl", "Control").TrimStart('L').TrimStart('R'))))
                        keyList.Add(frame.KeyList[i]);
                }

                if (keyList.Count == 0)
                {
                    UpdateProgress(count++);
                    continue;
                }

                #endregion

                #region Prepare the text

                var text = keyList.Select(x => "" + Native.Helpers.Other.GetSelectKeyText(x.Key, x.Modifiers, x.IsUppercase)).Aggregate((p, n) => p + model.KeyStrokesSeparator + n);

                if (string.IsNullOrEmpty(text))
                {
                    UpdateProgress(count++);
                    continue;
                }

                #endregion

                var image = frame.Path.SourceFrom();

                #region Check if margins and paddings are set properly

                if (image.Width - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2 <= 0 || image.Height - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2 <= 0)
                {
                    UpdateProgress(count++);
                    continue;
                }

                #endregion

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    //The FormattedText class helps in transforming the text to a shape.
                    var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface(model.KeyStrokesFontFamily, model.KeyStrokesFontStyle, model.KeyStrokesFontWeight, default), model.KeyStrokesFontSize,
                        new SolidColorBrush(model.KeyStrokesFontColor), null, TextFormattingMode.Ideal)
                    {
                        MaxTextWidth = image.Width - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2,
                        MaxTextHeight = image.Height - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2,
                    };

                    //TODO: Test with high dpi.

                    var geometry = formatted.BuildGeometry(new Point(0.5, 0.5));
                    var bounds = geometry.GetRenderBounds(pen);

                    var widthText = bounds.Width + model.KeyStrokesPadding * 2;
                    var heightText = bounds.Height + model.KeyStrokesPadding * 2 + 1; //Why 1?

                    var leftText =
                        model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Left ? model.KeyStrokesMargin - bounds.X :
                        model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Right ? image.Width - widthText - bounds.X - model.KeyStrokesMargin :
                        (image.Width - widthText - bounds.X) / 2d;

                    var topText =
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Top ? model.KeyStrokesMargin - bounds.Y :
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Bottom ? image.Height - heightText - bounds.Y - model.KeyStrokesMargin :
                        (image.Height - heightText) / 2d - bounds.Y;

                    var widthRectangle = model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Stretch ? image.Width - model.KeyStrokesMargin * 2 : widthText;
                    var heightRectangle = model.KeyStrokesVerticalAlignment == VerticalAlignment.Stretch ? image.Height - model.KeyStrokesMargin * 2 : Math.Max(heightText, model.KeyStrokesMinHeight);

                    //Center text when setting minimum height.
                    var minHeightAdjustment = model.KeyStrokesMinHeight > heightText && model.KeyStrokesVerticalAlignment != VerticalAlignment.Center && model.KeyStrokesVerticalAlignment != VerticalAlignment.Stretch ?
                        ((model.KeyStrokesMinHeight - heightText) / 2d) * (model.KeyStrokesVerticalAlignment == VerticalAlignment.Bottom ? -1 : 1) : 0;

                    var minHeightAdjustmentRectangle = model.KeyStrokesMinHeight > heightText && model.KeyStrokesVerticalAlignment == VerticalAlignment.Center ? ((model.KeyStrokesMinHeight - heightText) / 2d) : 0;

                    var leftRectangle =
                        model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Stretch ? model.KeyStrokesMargin :
                        leftText + bounds.X < model.KeyStrokesMargin ? model.KeyStrokesMargin : leftText + bounds.X;

                    var topRectangle =
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Stretch || model.KeyStrokesVerticalAlignment == VerticalAlignment.Top ? model.KeyStrokesMargin :
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Bottom ? image.Height - heightRectangle - model.KeyStrokesMargin :
                        topText + bounds.Y < model.KeyStrokesMargin ? model.KeyStrokesMargin : topText + bounds.Y - minHeightAdjustmentRectangle;

                    geometry.Transform = new TranslateTransform(Math.Round(leftText + model.KeyStrokesPadding, 0), Math.Round(topText + model.KeyStrokesPadding + minHeightAdjustment, 0));

                    //Draws everything in order, the image, the rectangle and the text.
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawRectangle(new SolidColorBrush(model.KeyStrokesBackgroundColor), null, new Rect(Math.Round(leftRectangle, 0), Math.Round(topRectangle, 0), Math.Round(widthRectangle, 0), Math.Round(heightRectangle, 0)));

                    //This code will draw the outline outside the text.
                    if (UserSettings.All.DrawOutlineOutside)
                    {
                        drawingContext.DrawGeometry(null, pen, geometry);
                        drawingContext.DrawGeometry(new SolidColorBrush(model.KeyStrokesFontColor), null, geometry);
                    }
                    else
                        drawingContext.DrawGeometry(new SolidColorBrush(model.KeyStrokesFontColor), pen, geometry);
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                GC.WaitForPendingFinalizers();
                GC.Collect(1);

                UpdateProgress(count++);
            }
        }

        private void BorderAsync(BorderViewModel model)
        {
            Dispatcher?.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            //If the borders are negative or this was called by an automatic task, select all frames.
            var frames = model.LeftThickness < 0 || model.TopThickness < 0 || model.RightThickness < 0 || model.BottomThickness < 0 || !model.IsManual ? Project.Frames : SelectedFrames();
            var scale = Math.Round(ZoomBoxControl.ImageDpi / 96d, 2); //ZoomBoxControl.ImageScale;

            //Since there could be a difference in the DPI of the UI vs the one from the image, I need to adjust the scale of the thickness.
            var leftThick = model.LeftThickness * ZoomBoxControl.ScaleDiff;
            var topThick = model.TopThickness * ZoomBoxControl.ScaleDiff;
            var rightThick = model.RightThickness * ZoomBoxControl.ScaleDiff;
            var bottomThick = model.BottomThickness * ZoomBoxControl.ScaleDiff;

            var count = 0;
            foreach (var frame in frames)
            {
                if (_abortLoading)
                    return;

                var image = frame.Path.SourceFrom();
                var drawingVisual = new DrawingVisual();

                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    #region Draws the white rectangle behind with full size

                    var marginHorizontal = Math.Abs((int)Math.Min(leftThick, 0) + Math.Min(rightThick, 0)); //Left and right negative margins as a positive number.
                    var marginVertical = Math.Abs((int)Math.Min(topThick, 0) + Math.Min(bottomThick, 0)); //Top and bottom negative margins as a positive number.

                    drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, image.Width + marginHorizontal, image.Height + marginVertical));

                    #endregion

                    #region Draws the image with the top-left margin

                    var marginLeft = (int)Math.Abs(Math.Min(leftThick, 0)); //Left negative margin as a positive number.
                    var marginTop = (int)Math.Abs(Math.Min(topThick, 0)); //Right negative margin as a positive number.

                    drawingContext.DrawImage(image, new Rect(marginLeft, marginTop, image.Width, image.Height));

                    #endregion

                    #region Draws the 4 lines

                    //The lines are centrally aligned, so they must be drawn at thickness / 2.
                    var brush = new SolidColorBrush(model.Color);

                    var height = image.Height + marginVertical;
                    var width = image.Width + marginLeft - (rightThick > 0 ? rightThick : 0); //image.Width + width / 2d - trueRight / 2d

                    //Left border.
                    var xLeft = Math.Abs(leftThick) / 2d;
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(leftThick)), new Point(xLeft, 0), new Point(xLeft, height));

                    //Right border.
                    var xRight = (leftThick < 0 ? leftThick * -1 : 0) + image.Width + (Math.Abs(rightThick) / 2d * (rightThick < 0 ? 1 : -1));
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(rightThick)), new Point(xRight, 0), new Point(xRight, height));

                    //Top border.
                    var xTop = Math.Abs(leftThick);
                    var yTop = Math.Abs(topThick) / 2d;
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(topThick)), new Point(xTop, yTop), new Point(width, yTop));

                    //Bottom border.
                    var yBottom = (topThick < 0 ? topThick * -1 : 0) + image.Height + (Math.Abs(bottomThick) / 2d * (bottomThick < 0 ? 1 : -1));
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(bottomThick)), new Point(xTop, yBottom), new Point(width, yBottom));

                    #endregion
                }

                var frameHeight = image.PixelHeight + (int)(Math.Round((topThick < 0 ? Math.Abs(topThick) : 0) + (bottomThick < 0 ? Math.Abs(bottomThick) : 0), 0) * scale);
                var frameWidth = image.PixelWidth + (int)(Math.Round((leftThick < 0 ? Math.Abs(leftThick) : 0) + (rightThick < 0 ? Math.Abs(rightThick) : 0), 0) * scale);

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(frameWidth, frameHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void ShadowAsync(ShadowViewModel model)
        {
            Dispatcher?.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var scale = Math.Round(ZoomBoxControl.ImageDpi / 96d, 2); //ZoomBoxControl.ImageScale;
            var blur = model.BlurRadius * ZoomBoxControl.ScaleDiff;
            var depth = model.Depth * ZoomBoxControl.ScaleDiff;

            var count = 0;
            foreach (var frame in Project.Frames)
            {
                if (_abortLoading)
                    return;

                var image = frame.Path.SourceFrom();
                var drawingVisual = new DrawingVisual();

                //Sizes:
                var frameHeight = 0;
                var frameWidth = 0;

                drawingVisual.Effect = new DropShadowEffect
                {
                    Color = model.Color,
                    BlurRadius = model.BlurRadius * ZoomBoxControl.ScaleDiff,
                    Opacity = model.Opacity,
                    Direction = model.Direction,
                    ShadowDepth = model.Depth * ZoomBoxControl.ScaleDiff,
                    RenderingBias = RenderingBias.Quality
                };

                //Draws image with shadow.
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    //https://upload.wikimedia.org/wikipedia/commons/thumb/f/fe/Sin_Cos_Tan_Cot_unit_circle.svg/1154px-Sin_Cos_Tan_Cot_unit_circle.svg.png
                    //The cosine of the direction gives the offset of the X axis based on the Width/2 of the point where the circle line crosses the line coming from the center of the circle.
                    //The sine of the direction gives the offset of the Y axis based on the Height/2 of the point where the circle line crosses the line coming from the center of the circle.

                    //• Cosine:
                    //Negative: to the left.
                    //Positive: to the right.
                    //• Sine:
                    //Positive: to the top.
                    //Negative: to the bottom.

                    //<- 180°, 3.14rad
                    //L: Blur + Depth
                    //T: Blur
                    //R: Math.Max(Blur - Depth, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.
                    //B: Blur

                    //-> 0°, 0rad
                    //L: Math.Max(Blur - Depth, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.
                    //T: Blur
                    //R: Blur + Depth
                    //B: Blur

                    //^> 45°, 0.78rad
                    //L: Math.Max(Blur - Depth * ratio, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.
                    //T: Blur + Depth * ratio
                    //R: Blur + Depth * ratio
                    //B: Math.Max(Blur - Depth * ratio, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.

                    //Converts the direction in degrees to radians.
                    var radians = Math.PI / 180.0 * model.Direction;
                    var offsetX = depth * Math.Cos(radians);
                    var offsetY = depth * Math.Sin(radians);

                    var offsetLeft = offsetX < 0 ? offsetX * -1 : 0;
                    var offsetTop = offsetY > 0 ? offsetY : 0;
                    var offsetRight = offsetX > 0 ? offsetX : 0;
                    var offsetBottom = offsetY < 0 ? offsetY * -1 : 0;

                    //Measure drop shadow space.
                    var marginLeft = offsetLeft > 0 ? offsetLeft + blur / 2d : Math.Max(blur / 2d - offsetLeft, 0); //- offsetX
                    var marginTop = offsetTop > 0 ? offsetTop + blur / 2d : Math.Max(blur / 2d - offsetTop, 0); //- offsetY
                    var marginRight = offsetRight > 0 ? offsetRight + blur / 2d : Math.Max(blur / 2d - offsetRight, 0); //+ offsetX
                    var marginBottom = offsetBottom > 0 ? offsetBottom + blur / 2d : Math.Max(blur / 2d - offsetBottom, 0); //+ offsetY

                    drawingContext.DrawImage(image, new Rect((int)marginLeft, (int)marginTop, image.Width, image.Height));

                    frameHeight = (int)((marginTop + image.Height + marginBottom) * scale);
                    frameWidth = (int)((marginLeft + image.Width + marginRight) * scale);
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var innerBmp = new RenderTargetBitmap(frameWidth, frameHeight, ZoomBoxControl.ImageDpi, ZoomBoxControl.ImageDpi, PixelFormats.Pbgra32);
                innerBmp.Render(drawingVisual);

                //Draws background and rendered image on top.
                drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    //Draws the background of the image.
                    drawingContext.DrawRectangle(new SolidColorBrush(model.BackgroundColor), null, new Rect(0, 0, innerBmp.Width, innerBmp.Height));

                    //Image, already with the shadow.
                    drawingContext.DrawImage(innerBmp, new Rect(0, 0, innerBmp.Width, innerBmp.Height));
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(frameWidth, frameHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void FlipRotate(FlipRotateType type)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingFlipRotate"), Project.Frames.Count);

            var frameList = type is FlipRotateType.RotateLeft90 or FlipRotateType.RotateRight90 ? Project.Frames : SelectedFrames();

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.Path.SourceFrom();

                Transform transform = null;

                switch (type)
                {
                    case FlipRotateType.FlipVertical:
                        transform = new ScaleTransform(1, -1, 0.5, 0.5);
                        break;
                    case FlipRotateType.FlipHorizontal:
                        transform = new ScaleTransform(-1, 1, 0.5, 0.5);
                        break;
                    case FlipRotateType.RotateLeft90:
                        transform = new RotateTransform(-90, 0.5, 0.5);
                        break;
                    case FlipRotateType.RotateRight90:
                        transform = new RotateTransform(90, 0.5, 0.5);
                        break;
                    default:
                        transform = new ScaleTransform(1, 1, 0.5, 0.5);
                        break;
                }

                var transBitmap = new TransformedBitmap(image, transform);

                // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(transBitmap));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void ReduceFrameCount(List<int> selection, int factor, int removeCount, ReduceDelayModes mode)
        {
            var removeList = new List<int>();

            //Gets the list of frames to be removed.
            for (var i = selection.Min() + factor - 1; i < selection.Min() + selection.Count - 1; i += factor + removeCount)
                removeList.AddRange(Util.Other.ListOfIndexes(i + 1, removeCount));

            //Only allow removing frames within the possible range.
            removeList = removeList.Where(x => x < Project.Frames.Count).ToList();

            var alterList = mode == ReduceDelayModes.Evenly ? Util.Other.ListOfIndexes(0, Project.Frames.Count).Where(w => !removeList.Contains(w)).ToList() :
                mode == ReduceDelayModes.Previous ? (from item in removeList where item - 1 >= 0 select item - 1).ToList() : //.Union(removeList)
                new List<int>(); //No other frame will be altered if the delay is not adjusted.

            if (alterList.Any())
                ActionStack.SaveState(ActionStack.EditAction.RemoveAndAlter, Project.Frames, removeList, alterList);
            else
                ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, removeList);

            var delayRemoved = 0;
            for (var i = removeList.Count - 1; i >= 0; i--)
            {
                var removeIndex = removeList[i];

                if (mode != ReduceDelayModes.DontAdjust)
                {
                    if (mode == ReduceDelayModes.Previous || factor == 1)
                    {
                        //Simply stacks the delay of the removed frames to the previous frame;
                        Project.Frames[removeIndex - 1].Delay += Project.Frames[removeIndex].Delay;
                    }
                    else if (mode == ReduceDelayModes.Evenly)
                    {
                        if (i == removeList.Count - 1 || removeList[i] + 1 == removeList[i + 1])
                        {
                            //Store the delay of the frames being removed.
                            delayRemoved += Project.Frames[removeIndex].Delay;
                        }
                        else
                        {
                            if (delayRemoved > 0)
                            {
                                //Calculate the size of the remaining section (this is the factor, the number of frames not being removed in each section).
                                var size = removeList[i + 1] - removeList[i] - 1;

                                //Spread evenly the accumulated delay among the remaining frames.
                                for (var r = removeList[i + 1] - 1; r > removeList[i]; r--)
                                    Project.Frames[r].Delay += delayRemoved / size; //Some information may be lost due to rounding.
                            }

                            //Start again the accumulation for this block of frames being removed.
                            delayRemoved = Project.Frames[removeIndex].Delay;
                        }
                    }
                }

                File.Delete(Project.Frames[removeIndex].Path);
                Project.Frames.RemoveAt(removeIndex);
            }
        }

        private int RemoveDuplicatesAsync(decimal similarity, DuplicatesRemovalModes removal, DuplicatesDelayModes delay)
        {
            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
                Cursor = Cursors.AppStarting;
            });

            var removeList = new List<int>();
            var alterList = new List<int>();

            ShowProgress(LocalizationHelper.Get("S.Editor.AnalyzingDuplicates"), Project.Frames.Count - 1);

            var similarFramePairs = Enumerable.Range(0, Project.Frames.Count - 1)
                .Select(i => { UpdateProgress(i + 1); return i; })
                .Select(i => (First: Project.Frames[i], Last: Project.Frames[i + 1]))
                .AsParallel()
                .Where(t => ImageMethods.CalculateDifference(t.First, t.Last) >= similarity);

            foreach (var (firstFrame, lastFrame) in similarFramePairs)
            {
                switch (removal)
                {
                    case DuplicatesRemovalModes.First:
                        removeList.Add(firstFrame.Index);
                        alterList.Add(lastFrame.Index);
                        break;
                    case DuplicatesRemovalModes.Last:
                        alterList.Add(firstFrame.Index);
                        removeList.Add(lastFrame.Index);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(removal));
                }
            }

            if (removeList.Count == 0)
            {
                //TODO: Nothing being removed. I need to warn the user.
                return Project.Frames.Count;
            }

            // ActionStack assumes the list is sorted.
            removeList.Sort();
            alterList.Sort();

            var count = 0;
            if (delay != DuplicatesDelayModes.DontAdjust)
            {
                ShowProgress(LocalizationHelper.Get("S.Editor.AdjustingDuplicatesDelay"), removeList.Count);

                //Gets the list of frames that will be altered (if the delay will be adjusted).
                var mode = removal == DuplicatesRemovalModes.First ? 1 : -1;

                ActionStack.SaveState(ActionStack.EditAction.RemoveAndAlter, Project.Frames, removeList, alterList);

                if (removal == DuplicatesRemovalModes.Last)
                {
                    for (var i = alterList.Count - 1; i >= 0; i--)
                    {
                        var index = alterList[i];

                        //Sum or average of the delays.
                        if (delay == DuplicatesDelayModes.Sum)
                            Project.Frames[index].Delay += Project.Frames[index - mode].Delay;
                        else
                            Project.Frames[index].Delay = (Project.Frames[index - mode].Delay + Project.Frames[index].Delay) / 2;

                        UpdateProgress(count++);
                    }
                }
                else
                {
                    foreach (var index in alterList)
                    {
                        //Sum or average of the delays.
                        if (delay == DuplicatesDelayModes.Sum)
                            Project.Frames[index].Delay += Project.Frames[index - mode].Delay;
                        else
                            Project.Frames[index].Delay = (Project.Frames[index - mode].Delay + Project.Frames[index].Delay) / 2;

                        UpdateProgress(count++);
                    }
                }
            }
            else
            {
                ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, removeList);
            }

            ShowProgress(LocalizationHelper.Get("S.Editor.DiscardingDuplicates"), removeList.Count);

            var removeFrames = removeList.Select(i => Project.Frames[i]).ToArray();

            foreach (var frame in removeFrames)
            {
                File.Delete(frame.Path);
                Project.Frames.Remove(frame);

                UpdateProgress(count++);
            }

            //Gets the minimum index being altered.
            return alterList.Concat(removeList).Min();
        }

        private int SmoothLoopAsync(decimal similarity, int threshold, SmoothLoopFromModes from)
        {
            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
                Cursor = Cursors.AppStarting;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.FindingLoop"), Project.Frames.Count - threshold);

            var start = from == SmoothLoopFromModes.Start ? threshold : Project.Frames.Count - 1;
            var end = from == SmoothLoopFromModes.Start ? Project.Frames.Count - 1 : threshold;
            var step = from == SmoothLoopFromModes.Start ? 1 : -1;

            var found = -1;
            var count = 0;
            var max = Math.Abs(start - end) + 1;

            while (count < max)
            {
                UpdateProgress(count++);

                if (ImageMethods.CalculateDifference(Project.Frames[0], Project.Frames[start]) >= similarity)
                {
                    found = start;
                    break;
                }

                start += step;
            }

            if (found == -1 || found == threshold - 1 || found == Project.Frames.Count - 1)
                return found;

            var removeList = Project.Frames.GetRange(found + 1, Project.Frames.Count - 1 - found).Select(s => s.Index).ToList();

            ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, removeList);

            ShowProgress(LocalizationHelper.Get("S.Editor.DiscardingLoop"), removeList.Count);

            var removeFrames = removeList.Select(i => Project.Frames[i]).ToArray();
            count = 0;

            foreach (var frame in removeFrames)
            {
                File.Delete(frame.Path);
                Project.Frames.Remove(frame);

                UpdateProgress(count++);
            }

            return Project.Frames.Count - 1;
        }

        private void DelayAsync(DelayViewModel model, bool forAll = false, bool ignoreUi = false)
        {
            var frameList = forAll ? Project.Frames : SelectedFrames();

            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
                Cursor = Cursors.AppStarting;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ChangingDelay"), frameList.Count);

            var count = 0;
            foreach (var frameInfo in frameList)
            {
                if (_abortLoading)
                    return;

                switch (model.Type)
                {
                    case DelayUpdateModes.Override:
                    {
                        frameInfo.Delay = model.NewDelay;
                        break;
                    }
                    case DelayUpdateModes.IncreaseDecrease:
                    {
                        frameInfo.Delay += model.IncreaseDecreaseDelay;

                        if (frameInfo.Delay < 10)
                            frameInfo.Delay = 10;
                        break;
                    }
                    default:
                    {
                        frameInfo.Delay = (int)Math.Round(frameInfo.Delay * model.Percent / 100m, 0);

                        if (frameInfo.Delay < 10)
                            frameInfo.Delay = 10;
                        break;
                    }
                }

                #region Update UI

                if (!ignoreUi)
                {
                    var index = Project.Frames.IndexOf(frameInfo);
                    Dispatcher.Invoke(() => _viewModel.Frames[index].Delay = frameInfo.Delay);
                }

                #endregion

                UpdateProgress(count++);
            }

            Project.Persist();
        }

        private int Fade(int selected, int frameCount, object optional)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingTransition"), Project.Frames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            //Calculate opacity increment. When fading to a color, it will add a frame with a 100% opacity at the end.
            var increment = 1F / (frameCount + (UserSettings.All.FadeToType == FadeModes.NextFrame ? 1 : 0));
            var previousName = Path.GetFileNameWithoutExtension(Project.Frames[selected].Path);
            var previousFolder = Path.GetDirectoryName(Project.Frames[selected].Path);

            #region Images

            var previousImage = Project.Frames[selected].Path.SourceFrom();
            var nextImage = UserSettings.All.FadeToType == FadeModes.NextFrame ? Project.Frames[Project.Frames.Count - 1 == selected ? 0 : selected + 1].Path.SourceFrom() :
                ImageMethods.CreateEmtpyBitmapSource(UserSettings.All.FadeToColor, previousImage.PixelWidth, previousImage.PixelHeight, previousImage.DpiX, PixelFormats.Indexed1);

            var nextBrush = new ImageBrush
            {
                ImageSource = nextImage,
                Stretch = Stretch.Uniform,
                TileMode = TileMode.None,
                Opacity = increment,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            #endregion

            #region Creates and Save each Transition Frame

            for (var index = 0; index < frameCount; index++)
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(previousImage, new Rect(0, 0, previousImage.Width, previousImage.Height));
                    drawingContext.DrawRectangle(nextBrush, null, new Rect(0, 0, nextImage.Width, nextImage.Height));
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(previousImage.PixelWidth, previousImage.PixelHeight, previousImage.DpiX, previousImage.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Increase the opacity for the next frame.
                nextBrush.Opacity += increment;

                //TODO: Fix filenaming.
                //TODO: This transition doesn't preserve any other frame info, such as keys pressed and mouse clicks.
                var fileName = Path.Combine(previousFolder, $"{previousName} T {index} {DateTime.Now:hh-mm-ss fff}.png");
                Project.Frames.Insert(selected + index + 1, new FrameInfo(fileName, UserSettings.All.FadeTransitionDelay));

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(fileName))
                    encoder.Save(stream);

                UpdateProgress(index);
            }

            #endregion

            return selected;
        }

        private int Slide(int selected, int frameCount, object optional)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingTransition"), Project.Frames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            var previousName = Path.GetFileNameWithoutExtension(Project.Frames[selected].Path);
            var previousFolder = Path.GetDirectoryName(Project.Frames[selected].Path);

            #region Images

            var previousImage = Project.Frames[selected].Path.SourceFrom();
            var nextImage = Project.Frames[(Project.Frames.Count - 1) == selected ? 0 : selected + 1].Path.SourceFrom();

            var nextBrush = new ImageBrush
            {
                ImageSource = nextImage,
                Stretch = Stretch.Uniform,
                TileMode = TileMode.None,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            #endregion

            //Calculate Translate Transform increment.
            var increment = previousImage.Width / (frameCount + 1);
            var transf = increment;

            //Calculate the Opacity increment.
            var alphaIncrement = 1F / (frameCount + 1);
            nextBrush.Opacity = alphaIncrement;

            #region Creates and Save each Transition Frame

            for (var index = 0; index < frameCount; index++)
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(previousImage, new Rect(0, 0, previousImage.Width, previousImage.Height));
                    drawingContext.DrawRectangle(nextBrush, null, new Rect(previousImage.Width - transf, 0, nextImage.Width, nextImage.Height));
                }

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap(previousImage.PixelWidth, previousImage.PixelHeight, previousImage.DpiX, previousImage.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Increase the translation and opacity for the next frame.
                transf += increment;
                nextBrush.Opacity += alphaIncrement;

                //TODO: Fix filenaming.
                //TODO: This transition doesn't preserve any other frame info, such as keys pressed and mouse clicks.
                var fileName = Path.Combine(previousFolder, $"{previousName} T {index} {DateTime.Now:hh-mm-ss fff}.png");
                Project.Frames.Insert(selected + index + 1, new FrameInfo(fileName, UserSettings.All.FadeTransitionDelay));

                // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(fileName))
                    encoder.Save(stream);

                UpdateProgress(index);
            }

            #endregion

            return selected;
        }

        private List<int> ObfuscateAsync(Rect rect, double screenScale, bool forAll = false)
        {
            var frameList = forAll ? Project.Frames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? Project.Frames.Select(x => Project.Frames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), frameList.Count);

            var size = frameList[0].Path.ScaledSize();

            rect = rect.Scale(screenScale).Limit(size.Width, size.Height);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.Path.SourceFrom();
                BitmapSource render;

                switch (UserSettings.All.ObfuscationMode)
                {
                    case ObfuscationModes.Blur:
                    {
                        render = ImageMethods.Blur(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height,
                            UserSettings.All.BlurLevel, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                    case ObfuscationModes.Darken:
                    {
                        render = ImageMethods.Lightness(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, true,
                            UserSettings.All.DarkenLevel, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                    case ObfuscationModes.Lighten:
                    {
                        render = ImageMethods.Lightness(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, false,
                            UserSettings.All.LightenLevel, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                    default:
                    {
                        render = ImageMethods.Pixelate(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height,
                            UserSettings.All.PixelSize, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.UseMedian, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                }

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(render));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }

            return selectedList;
        }

        private void MouseEventsAsync(MouseEventsViewModel model)
        {
            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var auxList = Project.Frames.CopyList();

            // Initialize brushes.
            var brushesByMouseButton = new Dictionary<MouseButtons, SolidColorBrush>();

            if (model.HighlightForegroundColor.A != 0)
            {
                var highlightSolidBrush = new SolidColorBrush(model.HighlightForegroundColor);
                highlightSolidBrush.Freeze();
                brushesByMouseButton.Add(MouseButtons.None, highlightSolidBrush);
            }

            if (model.LeftButtonForegroundColor.A != 0)
            {
                var leftClickSolidColorBrush = new SolidColorBrush(model.LeftButtonForegroundColor);
                leftClickSolidColorBrush.Freeze();
                brushesByMouseButton.Add(MouseButtons.Left, leftClickSolidColorBrush);
            }

            if (model.RightButtonForegroundColor.A != 0)
            {
                var rightClickSolidColorBrush = new SolidColorBrush(model.RightButtonForegroundColor);
                rightClickSolidColorBrush.Freeze();
                brushesByMouseButton.Add(MouseButtons.Right, rightClickSolidColorBrush);
            }

            if (model.MiddleButtonForegroundColor.A != 0)
            {
                var middleClickSolidColorBrush = new SolidColorBrush(model.MiddleButtonForegroundColor);
                middleClickSolidColorBrush.Freeze();
                brushesByMouseButton.Add(MouseButtons.Middle, middleClickSolidColorBrush);
            }

            var count = 0;
            foreach (var frame in auxList)
            {
                if (_abortLoading)
                    return;

                if (frame.ButtonClicked == MouseButtons.None || frame.CursorX == int.MinValue)
                {
                    UpdateProgress(count++);
                }

                SolidColorBrush brush = null;
                if (!brushesByMouseButton.TryGetValue(frame.ButtonClicked, out brush))
                {
                    continue;
                }

                var image = frame.Path.SourceFrom();
                var scale = Math.Round(image.DpiX / 96d, 2);
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawEllipse(brush, null, new Point(frame.CursorX / scale, frame.CursorY / scale), model.Width, model.Height);
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                GC.WaitForPendingFinalizers();
                GC.Collect(1);

                UpdateProgress(count++);
            }
        }

        #endregion
    }
}