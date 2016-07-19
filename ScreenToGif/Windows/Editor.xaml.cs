using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.Writers;
using ScreenToGif.Windows.Other;
using ScreenToGif.ImageUtil.Decoder;
using ScreenToGif.Util.Parameters;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Windows
{
    public partial class Editor : Window
    {
        #region Properties

        public static readonly DependencyProperty FilledListProperty = DependencyProperty.Register("FilledList", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty NotPreviewingProperty = DependencyProperty.Register("NotPreviewing", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register("IsLoading", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// True if there is a value inside the list of frames.
        /// </summary>
        public bool FilledList
        {
            get { return (bool)GetValue(FilledListProperty); }
            set { SetValue(FilledListProperty, value); }
        }

        /// <summary>
        /// True if not in preview mode.
        /// </summary>
        public bool NotPreviewing
        {
            get { return (bool)GetValue(NotPreviewingProperty); }
            set { SetValue(NotPreviewingProperty, value); }
        }

        /// <summary>
        /// True if loading frames.
        /// </summary>
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        #endregion

        #region Variables

        /// <summary>
        /// The List of Frames.
        /// </summary>
        public List<FrameInfo> ListFrames { get; set; }

        //public ObservableCollection<FrameInfo> ListFrames2 { get; set; }

        /// <summary>
        /// The clipboard.
        /// </summary>
        public List<FrameInfo> ClipboardFrames { get; set; }

        /// <summary>
        /// Last selected frame index. Used to track users last selection and decide which frame to show.
        /// </summary>
        private int LastSelected { get; set; } = -1;

        /// <summary>
        /// True if the user was selecting frames using the FirstFrame/Previous/Next/LastFrame commands.
        /// </summary>
        private bool WasChangingSelection { get; set; }

        private readonly System.Windows.Forms.Timer _timerPreview = new System.Windows.Forms.Timer();

        #endregion

        #region Initialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Editor()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += System_DisplaySettingsChanged;
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;

            ScrollSynchronizer.SetScrollGroup(ZoomBoxControl.GetScrollViewer(), "Canvas");
            ScrollSynchronizer.SetScrollGroup(MainScrollViewer, "Canvas");

            #region Window Positioning

            if (Math.Abs(Settings.Default.EditorLeft - -1) < 0.5)
                Settings.Default.EditorLeft = (SystemParameters.VirtualScreenWidth - Width) / 2;
            if (Math.Abs(Settings.Default.EditorTop - -1) < 0.5)
                Settings.Default.EditorTop = (SystemParameters.VirtualScreenHeight - Height) / 2;

            if (Settings.Default.EditorLeft > SystemParameters.VirtualScreenWidth)
                Settings.Default.EditorLeft = SystemParameters.VirtualScreenWidth - 50;
            if (Settings.Default.EditorTop > SystemParameters.VirtualScreenHeight)
                Settings.Default.EditorTop = SystemParameters.VirtualScreenHeight - 50;

            #endregion

            //ListFrames2 = new ObservableCollection<FrameInfo>(ListFrames);

            //if (ListFrames2 != null)
            //{
            //    ActionStack.Prepare(ListFrames2[0].ImageLocation);

            //    FrameListView.ItemsSource = ListFrames2;
            //    return;
            //}

            if (ListFrames != null)
            {
                ShowProgress(FindResource("Editor.Preparing").ToString(), ListFrames.Count, true);

                Cursor = Cursors.AppStarting;
                IsLoading = true;

                ActionStack.Prepare(ListFrames[0].ImageLocation);

                _loadFramesDel = Load;
                _loadFramesDel.BeginInvoke(LoadCallback, null);
                return;
            }

            #region Open With...

            if (Argument.FileNames.Any())
            {
                #region Validation

                var extensionList = Argument.FileNames.Select(Path.GetExtension).ToList();

                var media = new[] { "jpg", "gif", "bmp", "png", "avi", "mp4", "wmv" };

                var projectCount = extensionList.Count(x => !String.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
                var mediaCount = extensionList.Count(x => !String.IsNullOrEmpty(x) && media.Contains(x));

                //TODO: Change this validation if multiple files import is implemented. 
                //Later I need to implement another validation for multiple video files.
                //TODO: Multiple file importing.

                if (projectCount + mediaCount > 1)
                {
                    Dispatcher.Invoke(() => EditorStatusBand.Warning(FindResource("Editor.InvalidLoadingFiles").ToString()));
                    return;
                }

                if (projectCount > 0)
                {
                    Dispatcher.Invoke(() => EditorStatusBand.Warning(FindResource("Editor.InvalidLoadingProjects").ToString()));
                    return;
                }

                #endregion

                _importFramesDel = ImportFrom;
                _importFramesDel.BeginInvoke(Argument.FileNames[0], CreateTempPath(), ImportFromCallback, null);
            }

            #endregion

            WelcomeTextBlock.Text = Humanizer.Welcome();
        }

        #endregion

        #region Frame Selection

        private void FrameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region If nothing selected

            if (FrameListView.SelectedIndex == -1)
            {
                DelayNumericUpDown.ValueChanged -= NumericUpDown_OnValueChanged;
                DelayNumericUpDown.Value = 0;
                DelayNumericUpDown.ValueChanged += NumericUpDown_OnValueChanged;

                ZoomBoxControl.ImageSource = null;
                return;
            }

            if (LastSelected == -1 || _timerPreview.Enabled || WasChangingSelection || LastSelected >= FrameListView.Items.Count || (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0))
                LastSelected = FrameListView.SelectedIndex;

            WasChangingSelection = false;

            #endregion

            #region If deselected (and nothing else was selected or replaced)

            if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0)
            {
                //Based on the last selection, after deselecting a frame, the next frame that should be shown is the nearest one.
                //If the deselected frame is the one being shown.
                if (e.RemovedItems.Cast<FrameListBoxItem>().Any(x => x.FrameNumber == LastSelected))
                {
                    var selectedList = FrameListView.SelectedItems.Cast<FrameListBoxItem>();

                    //Gets the nearest selected frame, from the last deselected point.
                    var closest = selectedList.Aggregate((x, y) => Math.Abs(x.FrameNumber - LastSelected) < Math.Abs(y.FrameNumber - LastSelected) ? x : y);

                    ZoomBoxControl.ImageSource = ListFrames[closest.FrameNumber].ImageLocation;
                    FrameListView.ScrollIntoView(closest);

                    DelayNumericUpDown.ValueChanged -= NumericUpDown_OnValueChanged;
                    DelayNumericUpDown.Value = ListFrames[closest.FrameNumber].Delay;
                    DelayNumericUpDown.ValueChanged += NumericUpDown_OnValueChanged;
                }

                //GC.Collect(1);
                return;
            }

            #endregion

            #region If selected

            var selected = (FrameListBoxItem)FrameListView.Items[LastSelected];

            if (selected != null)
            {
                if (selected.IsSelected)
                {
                    ZoomBoxControl.ImageSource = ListFrames[selected.FrameNumber].ImageLocation;
                    FrameListView.ScrollIntoView(selected);

                    DelayNumericUpDown.ValueChanged -= NumericUpDown_OnValueChanged;
                    DelayNumericUpDown.Value = ListFrames[selected.FrameNumber].Delay;
                    DelayNumericUpDown.ValueChanged += NumericUpDown_OnValueChanged;
                }

                //GC.Collect(1);
                return;
            }

            #endregion
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as FrameListBoxItem;

            if (item != null)
                LastSelected = item.FrameNumber;

            //GC.Collect(1);
        }

        #endregion

        #region File Tab

        #region New/Open

        private void NewRecording_CanExecute(object sender, CanExecuteRoutedEventArgs e)
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

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            if (result.HasValue && !result.Value && recorder.ExitArg == ExitAction.Recorded && recorder.ListFrames != null)
            {
                ActionStack.Clear();
                ActionStack.Prepare(recorder.ListFrames[0].ImageLocation);

                LoadNewFrames(recorder.ListFrames);
            }

            Encoder.Restore();
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;
        }

        private void NewWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            if (result.HasValue && !result.Value && webcam.ExitArg == ExitAction.Recorded && webcam.ListFrames != null)
            {
                ActionStack.Clear();
                ActionStack.Prepare(webcam.ListFrames[0].ImageLocation);

                LoadNewFrames(webcam.ListFrames);
            }
        }

        private void NewBoardRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var board = new Board();
            var result = board.ShowDialog();

            if (result.HasValue && !result.Value && board.ExitArg == ExitAction.Recorded && board.ListFrames != null)
            {
                ActionStack.Clear();
                ActionStack.Prepare(board.ListFrames[0].ImageLocation);

                LoadNewFrames(board.ListFrames);
            }
        }

        private void NewAnimation_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.NewAnimation, ResMessage("Editor.File.Blank"), "Vector.File.New", ApplyNewImageButton_Click);
        }

        private void NewAnimationBackgroundColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.NewImageColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.NewImageColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyNewImageButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            #region FileName

            string pathTemp = Path.Combine(Path.GetTempPath(), @"ScreenToGif\Recording", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

            if (!Directory.Exists(pathTemp))
                Directory.CreateDirectory(pathTemp);

            var fileName = Path.Combine(pathTemp, "0.bmp");

            #endregion

            ActionStack.Clear();
            ActionStack.Prepare(pathTemp);

            #region Create and Save Image

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var scale = this.Scale();

                var bitmapSource = ImageMethods.CreateEmtpyBitmapSource(Settings.Default.NewImageColor,
                    (int)(Settings.Default.NewImageWidth * scale),
                    (int)(Settings.Default.NewImageHeight * scale), this.Dpi(), PixelFormats.Indexed1);
                var bitmapFrame = BitmapFrame.Create(bitmapSource);

                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(bitmapFrame);
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            GC.Collect();

            #endregion

            ClosePanel();

            #region Adds to the List

            var frame = new FrameInfo(fileName, 66);

            LoadNewFrames(new List<FrameInfo> { frame });

            #endregion
        }

        private void NewFromMediaProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = FindResource("Editor.OpenMediaProject").ToString(),
                Filter = "All supported files (*.bmp, *.jpg, *.png, *.gif, *.mp4, *.wmv, *.avi, *.stg, *.zip)|*.bmp;*.jpg;*.png;*.gif;*.mp4;*.wmv;*.avi;*.stg;*.zip|" +
                         "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi|" +
                         "ScreenToGif Project (*.stg, *.zip) |*.stg;*.zip",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //DiscardProject_Executed(null, null);

                _importFramesDel = ImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileName, CreateTempPath(), ImportFromCallback, null);
            }
        }

        #endregion

        #region Insert

        private void Insert_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 0 && FrameListView.SelectedIndex != -1 && !IsLoading && !e.Handled;
        }

        private void InsertRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            WindowState = WindowState.Minimized;
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();

                Encoder.Restore();
                WindowState = WindowState.Normal;
                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex) { Owner = this };
            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //ActionStack.Did(ListFrames);
                ListFrames = insert.ActualList;
                LoadSelectedStarter(0);
            }

            #endregion

            Encoder.Restore();
            WindowState = WindowState.Normal;
        }

        private void InsertWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var recorder = new Webcam();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex);
            insert.Owner = this;

            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //ActionStack.Did(ListFrames);
                ListFrames = insert.ActualList;
                LoadSelectedStarter(0);
            }

            #endregion
        }

        private void InsertFromMedia_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = FindResource("Editor.OpenMedia").ToString(),
                Filter = "All supported files (*.bmp, *.jpg, *.png, *.gif, *.mp4, *.wmv, *.avi)|*.bmp;*.jpg;*.png;*.gif;*.mp4;*.wmv;*.avi|" +
                         "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ActionStack.Did(ListFrames);

                _importFramesDel = InsertImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileName, CreateTempPath(), InsertImportFromCallback, null);
            }
        }

        #endregion

        #region File

        private void File_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Any() && !IsLoading && !e.Handled;
        }



        private void SaveAsGif_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            try
            {
                string fileName = Util.Other.FileName("gif");

                if (String.IsNullOrEmpty(fileName)) return;

                Encoder.AddItem(ListFrames.CopyToEncode(), fileName, this.Scale());
            }
            catch (Exception ex)
            {
                Dialog.Ok("Error While Saving", "Error while saving the animation", ex.Message);
                LogWriter.Log(ex, "Error while trying to save an animation.");
            }
        }

        private void SaveAsVideo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            try
            {
                string fileName = Util.Other.FileName("avi");

                if (String.IsNullOrEmpty(fileName)) return;

                Encoder.AddItem(ListFrames.CopyToEncode(), fileName, this.Scale());
            }
            catch (Exception ex)
            {
                Dialog.Ok("Error While Saving", "Error while saving the animation", ex.Message);
                LogWriter.Log(ex, "Error while trying to save an animation.");
            }
        }

        private void SaveAsProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            string fileName = Util.Other.FileName("stg", ListFrames.Count);

            if (String.IsNullOrEmpty(fileName)) return;

            _saveProjectDel = SaveProject;
            _saveProjectDel.BeginInvoke(fileName, SaveProjectCallback, null);
        }

        private void DiscardProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            #region Prepare UI

            ClosePanel();

            ActionGrid.BeginStoryboard(FindResource("HidePanelStoryboard") as Storyboard);

            FrameListView.SelectionChanged -= FrameListView_SelectionChanged;
            FrameListView.SelectedIndex = -1;

            FrameListView.Items.Clear();
            ZoomBoxControl.Clear();

            #endregion

            if (ListFrames == null || ListFrames.Count == 0) return;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(ListFrames, DiscardCallback, null);
        }

        #endregion

        #endregion

        #region View Tab

        private void Playback_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 1 && !IsLoading && ActionGrid.Width < 220;
        }

        private void FirstFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;
            FrameListView.SelectedIndex = 0;
        }

        private void PreviousFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;

            if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == 0)
            {
                FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                return;
            }

            //Show previous frame.
            FrameListView.SelectedIndex--;
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayPause();
        }

        private void NextFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;

            if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == FrameListView.Items.Count - 1)
            {
                FrameListView.SelectedIndex = 0;
                return;
            }

            //Show next frame.
            FrameListView.SelectedIndex++;
        }

        private void LastFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;
            FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
        }


        private void Zoom_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 0 && !IsLoading && !OverlayGrid.IsVisible && FrameListView.SelectedIndex != -1;
        }

        private void Zoom100_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomBoxControl.Zoom = 1.0;
        }

        private void FitImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            #region Get the sizes

            var height = ListFrames.First().ImageLocation.SourceFrom().Height;
            var width = ListFrames.First().ImageLocation.SourceFrom().Width;
            var viewHeight = MainScrollViewer.ActualHeight; //Replaced ZoomBoxControl.
            var viewWidth = MainScrollViewer.ActualWidth;

            #endregion

            #region Calculate the Zoom

            var zoomHeight = 1D;
            var zoomWidth = 1D;

            if (width > viewWidth)
            {
                zoomWidth = viewWidth / width;
            }

            if (height > viewHeight)
            {
                zoomHeight = viewHeight / height;
            }

            #endregion

            #region Apply the zoom

            if (zoomHeight > 0 && zoomHeight < zoomWidth)
                ZoomBoxControl.Zoom = zoomHeight;
            else if (zoomWidth > 0 && zoomWidth < zoomHeight)
                ZoomBoxControl.Zoom = zoomWidth;
            else
                ZoomBoxControl.Zoom = 1;

            #endregion
        }

        private void ShowEncoderButton_Click(object sender, RoutedEventArgs e)
        {
            //Encoder.Start(this.Scale());

            var test = new Board();
            test.ShowDialog();
        }

        #endregion

        #region Edit Tab

        #region Action Stack

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanUndo() && !IsLoading && !e.Handled;
        }

        private void Reset_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanUndo() && !IsLoading && !e.Handled;
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanRedo() && !IsLoading && !e.Handled;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            ListFrames = ActionStack.Undo(ListFrames.CopyList());
            LoadNewFrames(ListFrames, false);
        }

        private void Reset_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            ListFrames = ActionStack.Reset(ListFrames.CopyList());
            LoadNewFrames(ListFrames, false);
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            ListFrames = ActionStack.Redo(ListFrames.CopyList());
            LoadNewFrames(ListFrames, false);
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

            if (FrameListView.SelectedItems.Count == FrameListView.Items.Count)
            {
                Dialog.Ok(FindResource("Editor.Clipboard.InvalidCut.Title").ToString(),
                    FindResource("Editor.Clipboard.InvalidCut.Instruction").ToString(),
                    FindResource("Editor.Clipboard.InvalidCut.Message").ToString(), Dialog.Icons.Info);
                CutButton.IsEnabled = true;
                return;
            }

            #endregion

            var index = FrameListView.SelectedItems.OfType<FrameListBoxItem>().OrderBy(x => x.FrameNumber).First().FrameNumber;
            ActionStack.Did(ListFrames, index);

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
            var list = selected.Select(item => ListFrames[item.FrameNumber]).ToList();

            FrameListView.SelectedIndex = -1;

            if (!Util.Clipboard.Cut(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to cut selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");

                Undo_Executed(null, null);

                return;
            }

            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => ListFrames.RemoveAt(x.FrameNumber));
            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => FrameListView.Items.Remove(x));

            AdjustFrameNumbers(selected[0].FrameNumber);
            SelectNear(selected[0].FrameNumber);

            #region Item

            var imageItem = new ImageListBoxItem();
            imageItem.Author = DateTime.Now.ToString("hh:mm:ss");

            if (selected.Count > 1)
            {
                imageItem.Tag = String.Format("Frames: {0}", String.Join(", ", selected.Select(x => x.FrameNumber)));
                imageItem.Image = FindResource("Vector.ImageStack") as Canvas;
                imageItem.Content = String.Format("{0} Images", list.Count);
            }
            else
            {
                imageItem.Tag = String.Format("Frame: {0}", selected[0].FrameNumber);
                imageItem.Image = FindResource("Vector.Image") as Canvas;
                imageItem.Content = String.Format("{0} Image", list.Count);
            }

            #endregion

            ClipboardListView.Items.Add(imageItem);
            ClipboardListView.SelectedIndex = ClipboardListView.Items.Count - 1;

            ShowPanel(PanelType.Clipboard, FindResource("Editor.Edit.Clipboard").ToString(), "Vector.Paste");
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
            var list = selected.Select(item => ListFrames[item.FrameNumber]).ToList();

            if (!Util.Clipboard.Copy(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to copy selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");
                return;
            }

            #region Item

            var imageItem = new ImageListBoxItem();
            imageItem.Tag = String.Format("Frames: {0}", String.Join(", ", selected.Select(x => x.FrameNumber)));
            imageItem.Author = DateTime.Now.ToString("hh:mm:ss");

            if (list.Count > 1)
            {
                imageItem.Image = FindResource("Vector.ImageStack") as Canvas;
                imageItem.Content = String.Format("{0} Images", list.Count);
            }
            else
            {
                imageItem.Image = FindResource("Vector.Image") as Canvas;
                imageItem.Content = String.Format("{0} Image", list.Count);
            }

            #endregion

            ClipboardListView.Items.Add(imageItem);
            ClipboardListView.SelectedIndex = ClipboardListView.Items.Count - 1;

            ShowPanel(PanelType.Clipboard, FindResource("Editor.Edit.Clipboard").ToString(), "Vector.Paste");
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && Util.Clipboard.Items.Count > 0 &&
                           ClipboardListView.SelectedItem != null;
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var index = FrameListView.SelectedItems.OfType<FrameListBoxItem>().Last().FrameNumber;
            index = PasteBeforeRadioButton.IsChecked.HasValue && PasteBeforeRadioButton.IsChecked.Value
                    ? index
                    : index + 1;

            ActionStack.Did(ListFrames, index);

            var clipData = Util.Clipboard.Paste(ClipboardListView.SelectedIndex, 0);

            ListFrames.InsertRange(index, clipData);

            ClosePanel();

            LoadSelectedStarter(index, ListFrames.Count - 1);
        }

        private void ShowClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelType.Clipboard, FindResource("Editor.Edit.Clipboard").ToString(), "Vector.Paste");
        }


        private void ClipBoardSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ClipboardListView.SelectedItem != null && !IsLoading;
        }

        private void ExploreClipBoard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var selected = Util.Clipboard.Items[ClipboardListView.SelectedIndex];

                Process.Start(Path.GetDirectoryName(selected[0].ImageLocation));
            }
            catch (Exception ex)
            {
                Dialog.Ok("Browse Folder Error", "Impossible to browse clipboard folder.", ex.Message);
                LogWriter.Log(ex, "Browse Clipboard Folder");
            }
        }

        private void RemoveClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Util.Clipboard.Remove(ClipboardListView.SelectedIndex);
            ClipboardListView.Items.RemoveAt(ClipboardListView.SelectedIndex);
        }

        #endregion

        #region Frames

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading;
        }

        private void DeletePrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading &&
                FrameListView.SelectedIndex > 0;
        }

        private void DeleteNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading &&
                FrameListView.SelectedIndex < FrameListView.Items.Count - 1;
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            try
            {
                #region Validation

                if (ListFrames.Count == FrameListView.SelectedItems.Count)
                {
                    if (Dialog.Ask(FindResource("Editor.Remove.Title").ToString(),
                        FindResource("Editor.Remove.Instruction").ToString(),
                        FindResource("Editor.Remove.Message").ToString(), Dialog.Icons.Question))
                    {
                        DiscardProject_Executed(null, null);
                    }

                    return;
                }

                #endregion

                ActionStack.Did(ListFrames);

                var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
                var selectedOrdered = selected.OrderByDescending(x => x.FrameNumber).ToList();
                var list = selectedOrdered.Select(item => ListFrames[item.FrameNumber]).ToList();

                FrameListView.SelectedItem = null;

                list.ForEach(x => File.Delete(x.ImageLocation));
                selectedOrdered.ForEach(x => ListFrames.RemoveAt(x.FrameNumber));
                selectedOrdered.ForEach(x => FrameListView.Items.Remove(x));

                AdjustFrameNumbers(selectedOrdered.Last().FrameNumber);
                SelectNear(selectedOrdered.Last().FrameNumber);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error While Trying to Delete Frames");

                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
            }
        }

        private void DeletePrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            for (int index = FrameListView.SelectedIndex - 1; index >= 0; index--)
            {
                DeleteFrame(index);
            }

            AdjustFrameNumbers(0);
            SelectNear(0);
        }

        private void DeleteNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            int countList = FrameListView.Items.Count - 1; //So we have a fixed value.

            ActionStack.Did(ListFrames, FrameListView.SelectedIndex);

            for (int i = countList; i > FrameListView.SelectedIndex; i--) //From the end to the middle.
            {
                DeleteFrame(i);
            }

            SelectNear(FrameListView.Items.Count - 1);
        }

        #endregion

        #region Reordering

        private void Reordering_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading &&
            FrameListView.Items.Count > 1;
        }

        private void Reverse_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            ListFrames.Reverse();

            LoadSelectedStarter(0);
        }

        private void Yoyo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            ListFrames = Util.Other.Yoyo(ListFrames);
            LoadSelectedStarter(0);
        }

        private void MoveLeft_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            //TODO: Review this code.

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>());
            var selectedOrdered = selected.OrderBy(x => x.FrameNumber).ToList();

            var listIndex = selectedOrdered.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selectedOrdered)
            {
                #region Index

                var oldindex = listIndex[selectedOrdered.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index - 1;

                if (index == 0)
                    newIndex = FrameListView.Items.Count - 1;

                if (oldindex - 1 == index)
                {
                    continue;
                }

                #endregion

                #region Move

                var auxItem = ListFrames[index];

                FrameListView.Items.RemoveAt(index);
                ListFrames.RemoveAt(index);

                FrameListView.Items.Insert(newIndex, item);
                ListFrames.Insert(newIndex, auxItem);

                #endregion
            }

            #region Update Count

            var indexUpdate = listIndex.First() == 0 ? 0 : listIndex.First() - 1;

            AdjustFrameNumbers(indexUpdate);

            #endregion

            #region Select Frames

            foreach (var item in selected)
            {
                FrameListView.SelectedItems.Add(item);
            }

            #endregion

            #endregion
        }

        private void MoveRight_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>());
            var selectedOrdered = selected.OrderByDescending(x => x.FrameNumber).ToList();

            var listIndex = selectedOrdered.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selectedOrdered)
            {
                #region Index

                var oldindex = listIndex[selectedOrdered.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index + 1;

                if (index == FrameListView.Items.Count - 1)
                    newIndex = 0;

                if (oldindex + 1 == index)
                {
                    continue;
                }

                #endregion

                #region Move

                var auxItem = ListFrames[index];

                FrameListView.Items.RemoveAt(index);
                ListFrames.RemoveAt(index);

                FrameListView.Items.Insert(newIndex, item);
                ListFrames.Insert(newIndex, auxItem);

                #endregion
            }

            #region Update Count

            var indexUpdate = listIndex.Last();

            if (listIndex.First() == FrameListView.Items.Count - 1)
                indexUpdate = 0;

            AdjustFrameNumbers(indexUpdate);

            #endregion

            #region Select Frames

            foreach (var item in selected)
            {
                FrameListView.SelectedItems.Add(item);
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Image Tab

        private void Image_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading;
        }

        #region Size and Position

        private void Resize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WidthResizeNumericUpDown.ValueChanged -= WidthResizeNumericUpDown_ValueChanged;
            HeightResizeNumericUpDown.ValueChanged -= HeightResizeNumericUpDown_ValueChanged;

            #region Info

            var image = ListFrames[0].ImageLocation.SourceFrom();
            CurrentDpiLabel.Content = DpiNumericUpDown.Value = (int)Math.Round(image.DpiX, MidpointRounding.AwayFromZero);
            CurrentWidthLabel.Content = WidthResizeNumericUpDown.Value = (int)Math.Round(image.Width, MidpointRounding.AwayFromZero);
            CurrentHeightLabel.Content = HeightResizeNumericUpDown.Value = (int)Math.Round(image.Height, MidpointRounding.AwayFromZero);

            #endregion

            #region Resize Attributes

            double gcd = Util.Other.Gcd(image.Height, image.Width);

            _widthRatio = image.Width / gcd;
            _heightRatio = image.Height / gcd;

            #endregion

            WidthResizeNumericUpDown.ValueChanged += WidthResizeNumericUpDown_ValueChanged;
            HeightResizeNumericUpDown.ValueChanged += HeightResizeNumericUpDown_ValueChanged;

            ShowPanel(PanelType.Resize, FindResource("Editor.Image.Resize").ToString(), "Vector.Resize", ApplyResizeButton_Click);
        }

        private double _imageWidth;
        private double _imageHeight;
        private double _widthRatio = -1;
        private double _heightRatio = -1;

        private void KeepAspectCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            #region Resize Attributes

            double gcd = Util.Other.Gcd(HeightResizeNumericUpDown.Value, WidthResizeNumericUpDown.Value);

            _widthRatio = WidthResizeNumericUpDown.Value / gcd;
            _heightRatio = HeightResizeNumericUpDown.Value / gcd;

            #endregion

            _imageHeight = HeightResizeNumericUpDown.Value;
            _imageWidth = WidthResizeNumericUpDown.Value;
        }

        private void WidthResizeNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (KeepAspectCheckBox.IsChecked.HasValue && !KeepAspectCheckBox.IsChecked.Value)
                return;

            _imageWidth = WidthResizeNumericUpDown.Value;

            HeightResizeNumericUpDown.ValueChanged -= HeightResizeNumericUpDown_ValueChanged;
            _imageHeight = Math.Round(_heightRatio * _imageWidth / _widthRatio);
            HeightResizeNumericUpDown.Value = (int)_imageHeight;
            HeightResizeNumericUpDown.ValueChanged += HeightResizeNumericUpDown_ValueChanged;
        }

        private void HeightResizeNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (KeepAspectCheckBox.IsChecked.HasValue && !KeepAspectCheckBox.IsChecked.Value)
                return;

            WidthResizeNumericUpDown.ValueChanged -= WidthResizeNumericUpDown_ValueChanged;
            _imageWidth = Math.Round(_widthRatio * HeightResizeNumericUpDown.Value / _heightRatio);
            WidthResizeNumericUpDown.Value = (int)_imageWidth;
            WidthResizeNumericUpDown.ValueChanged += WidthResizeNumericUpDown_ValueChanged;
        }

        private void ApplyResizeButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            var size = ListFrames[0].ImageLocation.ScaledSize();

            if (size.Width == WidthResizeNumericUpDown.Value && size.Height == HeightResizeNumericUpDown.Value &&
                (int)Math.Round(ListFrames[0].ImageLocation.DpiOf()) == DpiNumericUpDown.Value)
            {
                EditorStatusBand.Warning(FindResource("Editor.Resize.Warning").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;

            _resizeFramesDel = Resize;
            _resizeFramesDel.BeginInvoke(WidthResizeNumericUpDown.Value, HeightResizeNumericUpDown.Value,
                DpiNumericUpDown.Value, ResizeCallback, null);

            ClosePanel();
        }


        private void Crop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Crop, ResMessage("Editor.Image.Crop"), "Vector.Crop", ApplyCropButton_Click);
        }

        private CroppingAdorner _cropAdorner;
        private FrameworkElement _currentElement = null;
        private bool _resizing = false;

        private void AddCropToElement(FrameworkElement fel)
        {
            if (_currentElement != null)
            {
                RemoveCropElements();
            }

            var rcInterior = new Rect(
                fel.Width * 0.2,
                fel.Height * 0.2,
                fel.Width * 0.6,
                fel.Height * 0.6);

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
            AdornerLayer aly = AdornerLayer.GetAdornerLayer(_currentElement);
            aly.Remove(_cropAdorner);

            _currentElement = null;
            _cropAdorner.CropChanged -= CropChanged;
            _cropAdorner = null;
        }

        private void CropChanged(Object sender, RoutedEventArgs rea)
        {
            RefreshCropImage();

            _resizing = true;

            TopCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Top;
            LeftCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Left;
            BottomCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Bottom;
            RightCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Right;

            CropSizeLabel.Content = $"{(int)_cropAdorner.ClipRectangle.Width} × {(int)_cropAdorner.ClipRectangle.Height}";

            _resizing = false;
        }

        private void RefreshCropImage()
        {
            if (_cropAdorner == null) return;

            var rect = new Int32Rect((int)_cropAdorner.ClipRectangle.X, (int)_cropAdorner.ClipRectangle.Y, (int)_cropAdorner.ClipRectangle.Width, (int)_cropAdorner.ClipRectangle.Height);

            if (rect.HasArea)
                CropImage.Source = ListFrames[LastSelected].ImageLocation.CropFrom(rect);
        }

        private void CropValue_Changed(object sender, EventArgs e)
        {
            if (_cropAdorner == null)
                return;

            if (_resizing)
                return;

            var top = TopCropNumericUpDown.Value;
            var left = LeftCropNumericUpDown.Value;
            var bottom = BottomCropNumericUpDown.Value;
            var right = RightCropNumericUpDown.Value;

            _cropAdorner.ClipRectangle = new Rect(new System.Windows.Point(left, top), new System.Windows.Point(right, bottom));
        }

        private void ApplyCropButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            var rect = new Int32Rect((int)_cropAdorner.ClipRectangle.X, (int)_cropAdorner.ClipRectangle.Y, (int)_cropAdorner.ClipRectangle.Width, (int)_cropAdorner.ClipRectangle.Height);

            if (!rect.HasArea)
            {
                EditorStatusBand.Warning(FindResource("Editor.Crop.Warning").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;

            _cropFramesDel = Crop;
            _cropFramesDel.BeginInvoke(rect, CropCallback, null);

            RemoveCropElements();
            ClosePanel();
        }


        private void FlipRotate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FlipRotate, ResMessage("Editor.Image.FlipRotate"), "Vector.FlipHorizontal", ApplyFlipRotateButton_Click);
        }

        private void ApplyFlipRotateButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;

            var type = FlipHorizontalRadioButton.IsChecked.Value
                ? FlipRotateType.FlipHorizontal : FlipVerticalRadioButton.IsChecked.Value
                ? FlipRotateType.FlipVertical : RotateLeftRadioButton.IsChecked.Value ?
                  FlipRotateType.RotateLeft90 : FlipRotateType.RotateRight90;

            _flipRotateFramesDel = FlipRotate;
            _flipRotateFramesDel.BeginInvoke(type, FlipRotateCallback, null);

            ClosePanel();
        }

        #endregion

        #region Text

        private void Caption_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Caption, ResMessage("Editor.Image.Caption"), "Vector.Caption", ApplyCaptionButton_Click);
        }

        private void CaptionFontColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.CaptionFontColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.CaptionFontColor = colorPicker.SelectedColor;
            }
        }

        private void CaptionOutlineColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.CaptionOutlineColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.CaptionOutlineColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyCaptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CaptionTextBox.Text.Length == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.Caption.WarningNoText").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Caption.WarningSelection").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = CaptionOverlayGrid.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void FreeText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeText, ResMessage("Editor.Image.FreeText"), "Vector.FreeText", ApplyFreeTextButton_Click);
        }

        private void FreeTextTextBlock_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (element == null) return;

            element.CaptureMouse();
            e.Handled = true;
        }

        private void FreeTextTextBlock_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (element == null) return;

            element.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void FreeTextTextBlock_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.Captured == null) return;
            if (!Mouse.Captured.Equals(sender)) return;

            var element = sender as FrameworkElement;

            if (element == null) return;

            var newPoint = e.GetPosition(FreeTextOverlayCanvas);

            //Maximum axis -2000 to 2000.
            if (newPoint.X > 2000)
                newPoint.X = 2000;
            if (newPoint.X < -2000)
                newPoint.X = -2000;

            if (newPoint.Y > 2000)
                newPoint.Y = 2000;
            if (newPoint.Y < -2000)
                newPoint.Y = -2000;

            Canvas.SetTop(element, newPoint.Y);
            Canvas.SetLeft(element, newPoint.X);
        }

        private void FreeTextFontColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.FreeTextFontColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.FreeTextFontColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyFreeTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeTextTextBox.Text.Length == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.Caption.WarningNoText").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.FreeText.WarningSelection").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = FreeTextOverlayCanvas.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void TitleFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.TitleFrame, ResMessage("Editor.Image.TitleFrame"), "Vector.TitleFrame", ApplyTitleFrameButton_Click);
        }

        private void TitleFrameFontColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.TitleFrameFontColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.TitleFrameFontColor = colorPicker.SelectedColor;
            }
        }

        private void TitleFrameBackgroundColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.TitleFrameBackgroundColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.TitleFrameBackgroundColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyTitleFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.TitleFrame.WarningSelection").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = TitleFrameOverlayGrid.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _titleFrameDel = TitleFrame;
            _titleFrameDel.BeginInvoke(render, FrameListView.SelectedIndex, dpi, TitleFrameCallback, null);

            ClosePanel();
        }

        #endregion

        #region Overlay

        private void FreeDrawing_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeDrawing, ResMessage("Editor.Image.FreeDrawing"), "Vector.FreeDrawing", ApplyFreeDrawingButton_Click);
        }

        private void FreeDrawingColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.FreeDrawingColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.FreeDrawingColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyFreeDrawingButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeDrawingInkCanvas.Strokes.Count == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.FreeDrawing.WarningNoDrawing").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.FreeDrawing.WarningSelection").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = FreeDrawingInkCanvas.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            FreeDrawingInkCanvas.Strokes.Clear();

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private AdornerLayer _adornerLayer = null;
        private void Watermark_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Watermark, ResMessage("Editor.Image.Watermark"), "Vector.Watermark", ApplyWatermarkButton_Click);
        }

        private void SelectWatermark_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = ResMessage("Editor.Watermark.Select"),
                Filter = "Image (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.WatermarkFilePath = ofd.FileName;
            }
        }

        private void ApplyWatermarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Default.WatermarkFilePath) || !File.Exists(Settings.Default.WatermarkFilePath))
            {
                EditorStatusBand.Warning(FindResource("Editor.Watermark.WarningNoImage").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Watermark.WarningSelection").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = WatermarkOverlayGrid.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void Border_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Border, ResMessage("Editor.Image.Border"), "Vector.Border", ApplyBorderButton_Click);
        }

        private void BorderColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.BorderColor);
            colorPicker.Owner = this;
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.BorderColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyBorderButton_Click(object sender, RoutedEventArgs e)
        {
            if (BorderOverlayBorder.BorderThickness == new Thickness(0, 0, 0, 0))
            {
                EditorStatusBand.Warning(FindResource("Editor.Border.WarningThickness").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Border.WarningSelection").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = BorderOverlayBorder.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void Cinemagraph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Cinemagraph, ResMessage("Editor.Image.Cinemagraph"), "Vector.Cinemagraph", ApplyCinemagraphButton_Click);
        }

        private void ApplyCinemagraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (CinemagraphInkCanvas.Strokes.Count == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.Cinemagraph.WarningNoDrawing").ToString());
                return;
            }

            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();

            #region Get the Strokes and Clip the Image

            var image = ListFrames[0].ImageLocation.SourceFrom();
            var rectangle = new RectangleGeometry(new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(image.PixelWidth, image.PixelHeight)));
            Geometry geometry = Geometry.Empty;

            foreach (Stroke stroke in CinemagraphInkCanvas.Strokes)
            {
                geometry = Geometry.Combine(geometry, stroke.GetGeometry(), GeometryCombineMode.Union, null);
            }

            geometry = Geometry.Combine(geometry, rectangle, GeometryCombineMode.Xor, null);

            var clippedImage = new System.Windows.Controls.Image
            {
                Height = image.PixelHeight,
                Width = image.PixelWidth,
                Source = image,
                Clip = geometry
            };
            clippedImage.Measure(scaledSize);
            clippedImage.Arrange(new Rect(scaledSize));

            var imageRender = clippedImage.GetRender(dpi, scaledSize);

            #endregion

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(imageRender, dpi, true, OverlayCallback, null);

            ClosePanel();
        }

        #endregion

        #endregion

        #region Select Tab

        private void Selection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && FrameListView != null && FrameListView.HasItems && !IsLoading;
        }

        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            FrameListView.SelectAll();
        }

        private void GoTo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var go = new GoTo(ListFrames.Count - 1);
            go.Owner = this;
            var result = go.ShowDialog();

            if (result.HasValue && result.Value)
            {
                FrameListView.SelectedIndex = go.Selected;
            }
        }

        private void InverseSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            foreach (ListViewItem item in FrameListView.Items)
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        private void DeselectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            FrameListView.SelectedIndex = -1;
        }

        #endregion

        #region Transition

        private void Transition_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && FrameListView?.SelectedItems != null && !IsLoading && FrameListView.SelectedIndex != -1;
        }

        private void Fade_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Fade, ResMessage("Editor.Fade.Title"), "Vector.Fade", ApplyFadeButtonButton_Click);
        }

        private void ApplyFadeButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Fade.WarningSelection").ToString());
                return;
            }

            Cursor = Cursors.AppStarting;

            ActionStack.Did(ListFrames);

            _transitionDel = Fade;
            _transitionDel.BeginInvoke(FrameListView.SelectedIndex, (int)FadeSlider.Value, null, TransitionCallback, null);

            ClosePanel();
        }

        private void Slide_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Slide, ResMessage("Editor.Slide.Title"), "Vector.Slide", ApplySlideButtonButton_Click);
        }

        private void ApplySlideButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Slide.WarningSelection").ToString());
                return;
            }

            Cursor = Cursors.AppStarting;

            ActionStack.Did(ListFrames);

            _transitionDel = Slide;
            _transitionDel.BeginInvoke(FrameListView.SelectedIndex, (int)SlideSlider.Value, SlideFrom.Right, TransitionCallback, null);

            ClosePanel();
        }

        #endregion

        #region Playback Tab

        private void NumericUpDown_OnValueChanged(object sender, EventArgs e)
        {
            if (ListFrames == null) return;
            if (ListFrames.Count == 0) return;
            if (FrameListView.SelectedIndex == -1) return;
            if ((int)DelayNumericUpDown.Value < 10)
                DelayNumericUpDown.Value = 10;

            //TODO: Add to the ActionStack

            ListFrames[FrameListView.SelectedIndex].Delay = (int)DelayNumericUpDown.Value;

            ((FrameListBoxItem)FrameListView.Items[FrameListView.SelectedIndex]).Delay = (int)DelayNumericUpDown.Value;
        }

        private void OverrideDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.OverrideDelay, ResMessage("Editor.Playback.OverrideDelay"), "Vector.OverrideDelay", ApplyOverrideDelayButton_Click);
        }

        private void ApplyOverrideDelayButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add to the ActionStack without having to add the images...
            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;

            _delayFramesDel = Delay;
            _delayFramesDel.BeginInvoke(DelayChangeType.Override, NewDelayNumericUpDown.Value, DelayCallback, null);

            ClosePanel();
        }

        private void ChangeDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.ChangeDelay, ResMessage("Editor.Playback.ChangeDelay"), "Vector.ChangeDelay", ApplyChangeDelayButton_Click);
        }

        private void ApplyChangeDelayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChangeDelayNumericUpDown.Value == 0)
            {
                ClosePanel();
                return;
            }

            //TODO: Add to the ActionStack without having to add the images...
            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;

            _delayFramesDel = Delay;
            _delayFramesDel.BeginInvoke(DelayChangeType.IncreaseDecrease, ChangeDelayNumericUpDown.Value, DelayCallback, null);

            ClosePanel();
        }

        #endregion

        #region Options Tab

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            var options = new Options();
            options.ShowDialog();
        }

        private void FeedbackButton_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            var feed = new Feedback { Owner = this };
            feed.ShowDialog();
        }

        #endregion

        #region Other Events

        #region Frame ListView

        private void ListFramesSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView.SelectedIndex != -1 && !IsLoading;
        }

        private void OpenImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Process.Start(ListFrames[FrameListView.SelectedIndex].ImageLocation);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open Image");
                Dialog.Ok("Open Image", "Impossible to open image.", ex.Message);
            }
        }

        #endregion

        #region ZoomBox

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
                e.Handled = true;
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt)
            {
                #region Translate the Element (Scroll)

                if (sender.GetType() == typeof(ScrollViewer))
                {
                    switch (Keyboard.Modifiers)
                    {
                        case ModifierKeys.Alt:

                            double verDelta = e.Delta > 0 ? -10.5 : 10.5;
                            MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + verDelta);

                            break;
                        case ModifierKeys.Shift:

                            double horDelta = e.Delta > 0 ? -10.5 : 10.5;
                            MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + horDelta);

                            break;
                    }

                    return;
                }

                #endregion

                e.Handled = false;
                return;
            }


            if (e.Delta > 0)
            {
                if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == FrameListView.Items.Count - 1)
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
                    FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                    return;
                }

                //Show previous frame.
                FrameListView.SelectedIndex--;
            }
        }

        #endregion

        private void TimerPreview_Tick(object sender, EventArgs e)
        {
            _timerPreview.Tick -= TimerPreview_Tick;

            if (ListFrames.Count - 1 == FrameListView.SelectedIndex)
            {
                FrameListView.SelectedIndex = 0;
            }
            else
            {
                FrameListView.SelectedIndex++;
            }

            if (ListFrames[FrameListView.SelectedIndex].Delay == 0)
                ListFrames[FrameListView.SelectedIndex].Delay = 10;

            //Sets the interval for this frame. If this frame has 500ms, the next frame will take 500ms to show.
            _timerPreview.Interval = ListFrames[FrameListView.SelectedIndex].Delay;
            _timerPreview.Tick += TimerPreview_Tick;

            GC.Collect(2);
        }

        #region Drag and Drop

        private void Control_DragEnter(object sender, DragEventArgs e)
        {
            Pause();

            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void Control_Drop(object sender, DragEventArgs e)
        {
            Pause();

            var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (fileNames == null) return;
            if (fileNames.Length == 0) return;

            #region Validation

            var extensionList = fileNames.Select(Path.GetExtension).ToList();

            var media = new[] { ".jpg", ".gif", ".bmp", ".png", ".avi", ".mp4", ".wmv" };

            var projectCount = extensionList.Count(x => !String.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
            var mediaCount = extensionList.Count(x => !String.IsNullOrEmpty(x) && media.Contains(Path.GetExtension(x)));

            //TODO: Change this validation if multiple files import is implemented. 
            //Later I need to implement another validation for multiple video files.
            //TODO: Multiple file importing.
            if (projectCount + mediaCount > 1)
            {
                Dialog.Ok(ResMessage("Editor.DragDrop.InvalidFiles.Title"),
                    ResMessage("Editor.DragDrop.InvalidFiles.Instruction"),
                    ResMessage("Editor.DragDrop.InvalidFiles.Message"), Dialog.Icons.Warning);
                return;
            }

            if (projectCount > 0)
            {
                Dialog.Ok(ResMessage("Editor.DragDrop.Invalid.Title"),
                    ResMessage("Editor.DragDrop.InvalidProject.Instruction"),
                    ResMessage("Editor.DragDrop.InvalidProject.Message"), Dialog.Icons.Warning);
                return;
            }

            if (mediaCount == 0)
            {
                //TODO: Warning
                return;
            }

            #endregion

            #region Importing Options

            if (Path.GetExtension(fileNames[0]).Equals("stg") || Path.GetExtension(fileNames[0]).Equals("zip"))
            {
                DiscardProject_Executed(null, null);
            }

            //If inserted into new recording or forced into new one.
            if (ListFrames == null || ListFrames.Count == 0 || e.KeyStates == DragDropKeyStates.ControlKey)
                _importFramesDel = ImportFrom;
            else
                _importFramesDel = InsertImportFrom;

            #endregion

            _importFramesDel.BeginInvoke(fileNames[0], CreateTempPath(), ImportFromCallback, null);
        }

        #endregion

        private void Window_Activated(object sender, EventArgs e)
        {
            //TODO: Check with High dpi.
            if (Settings.Default.EditorExtendChrome)
                Glass.ExtendGlassFrame(this, new Thickness(0, 100, 0, 0)); //26
            else
                Glass.RetractGlassFrame(this);

            RibbonTabControl.UpdateVisual();
        }

        private void EditorWindow_Deactivated(object sender, EventArgs e)
        {
            RibbonTabControl.UpdateVisual(false);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //What if there's any processing happening?

            Pause();

            Settings.Default.Save();

            Encoder.TryClose();

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged -= System_DisplaySettingsChanged;
            SystemParameters.StaticPropertyChanged -= SystemParameters_StaticPropertyChanged;
        }

        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Pause();
                GC.Collect();
            }
        }

        private void System_DisplaySettingsChanged(object sender, EventArgs e)
        {
            //TODO: If a monitor is removed, or resolution changes, update the position of the window.
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "WindowGlassColor")
            {
                RibbonTabControl.UpdateVisual();
            }
        }

        #endregion

        #region Private Methods

        #region Load

        #region Async Loading

        private delegate bool LoadFrames();

        private LoadFrames _loadFramesDel;

        /// <summary>
        /// Loads the new frames and clears the old ones.
        /// </summary>
        /// <param name="listFrames">The new list of frames.</param>
        private void LoadNewFrames(List<FrameInfo> listFrames, bool clear = true)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;

            FrameListView.Items.Clear();
            ZoomBoxControl.Zoom = 1;

            #region Discard

            if (ListFrames != null && ListFrames.Any() && clear)
            {
                _discardFramesDel = Discard;
                _discardFramesDel.BeginInvoke(ListFrames, DiscardAndLoadCallback, null);

                ListFrames = listFrames;

                return;
            }

            #endregion

            ListFrames = listFrames;

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);
        }

        private bool Load()
        {
            try
            {
                ShowProgress(DispatcherResMessage("Editor.LoadingFrames"), ListFrames.Count);

                var color = System.Drawing.Color.FromArgb(Settings.Default.ClickColor.A, Settings.Default.ClickColor.R,
                    Settings.Default.ClickColor.G, Settings.Default.ClickColor.B);

                foreach (FrameInfo frame in ListFrames)
                {
                    #region Cursor Merge

                    if (Settings.Default.ShowCursor && frame.CursorInfo != null)
                    {
                        try
                        {
                            using (var imageTemp = frame.ImageLocation.From())
                            {
                                using (var graph = Graphics.FromImage(imageTemp))
                                {
                                    #region Mouse Clicks

                                    if (frame.CursorInfo.Clicked && Settings.Default.MouseClicks)
                                    {
                                        //TODO: Center the aura to the x,y 0,0 point.
                                        var rectEllipse = new Rectangle((int)frame.CursorInfo.Position.X - 5,
                                            (int)frame.CursorInfo.Position.Y - 5,
                                            frame.CursorInfo.Image.Width - 5,
                                            frame.CursorInfo.Image.Height - 5);

                                        graph.DrawEllipse(new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.FromArgb(120, color)), frame.CursorInfo.Image.Width), rectEllipse);
                                    }

                                    #endregion

                                    var rect = new Rectangle(
                                        (int)frame.CursorInfo.Position.X,
                                        (int)frame.CursorInfo.Position.Y,
                                        frame.CursorInfo.Image.Width,
                                        frame.CursorInfo.Image.Height);

                                    graph.DrawImage(frame.CursorInfo.Image, rect);
                                    graph.Flush();
                                }

                                imageTemp.Save(frame.ImageLocation);
                            }

                            frame.CursorInfo.Image.Dispose();
                            frame.CursorInfo = null;
                        }
                        catch (Exception) { }
                    }

                    #endregion

                    var itemInvoked = Dispatcher.Invoke(() =>
                    {
                        var item = new FrameListBoxItem
                        {
                            FrameNumber = ListFrames.IndexOf(frame),
                            Image = frame.ImageLocation,
                            Delay = frame.Delay
                        };

                        return item;
                    });

                    Dispatcher.InvokeAsync(() =>
                    {
                        FrameListView.Items.Add(itemInvoked);

                        UpdateProgress(itemInvoked.FrameNumber);
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading");
                return false;
            }
        }

        private void LoadCallback(IAsyncResult ar)
        {
            var result = _loadFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;

                if (ListFrames.Count > 0)
                    FilledList = true;

                FrameListView.SelectedIndex = 0;
                FrameListView.Focus();

                HideProgress();

                WelcomeGrid.BeginStoryboard(FindResource("HideWelcomeBorderStoryboard") as Storyboard, HandoffBehavior.Compose);

                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        #region Async Selective Loading

        private delegate bool LoadSelectedFrames(int start, int? end);

        private LoadSelectedFrames _loadSelectedFramesDel;

        private void LoadSelectedStarter(int start, int? end = null)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;
            ShowProgress(ResMessage("Editor.UpdatingFrames"), ListFrames.Count, true);

            _loadSelectedFramesDel = LoadSelected;
            _loadSelectedFramesDel.BeginInvoke(start, end, LoadSelectedCallback, null);
        }

        private bool LoadSelected(int start, int? end)
        {
            end = end ?? ListFrames.Count - 1;
            UpdateProgress(0);

            try
            {
                //For each changed frame.
                for (int index = start; index <= end; index++)
                {
                    //Check if within limits.
                    if (index <= FrameListView.Items.Count - 1)
                    {
                        #region Edit the existing frame

                        Dispatcher.Invoke(() =>
                        {
                            FrameListBoxItem frame = (FrameListBoxItem)FrameListView.Items[index];

                            frame.FrameNumber = index;
                            frame.Delay = ListFrames[index].Delay;
                            frame.Image = null; //To update the image.
                            frame.Image = ListFrames[index].ImageLocation;
                            frame.UpdateLayout();
                            frame.InvalidateVisual();
                        });

                        #endregion
                    }
                    else
                    {
                        #region Create another frame

                        Dispatcher.Invoke(() =>
                        {
                            var item = new FrameListBoxItem
                            {
                                FrameNumber = index,
                                Image = ListFrames[index].ImageLocation,
                                Delay = ListFrames[index].Delay
                            };

                            FrameListView.Items.Add(item);
                        });

                        #endregion
                    }

                    UpdateProgress(index);
                }

                if (ListFrames.Count > 0)
                    Dispatcher.Invoke(() => { FilledList = true; });

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading Error");
                return false;
            }
        }

        private void LoadSelectedCallback(IAsyncResult ar)
        {
            bool result = _loadSelectedFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;
                HideProgress();

                if (LastSelected != -1)
                {
                    ZoomBoxControl.ImageSource = null;
                    ZoomBoxControl.ImageSource = ListFrames[LastSelected].ImageLocation;

                    FrameListView.ScrollIntoView(FrameListView.Items[LastSelected]);
                }

                FrameListView.Focus();

                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        #region Async Import

        private delegate void ImportFrames(string fileName, string pathTemp);

        private ImportFrames _importFramesDel;

        private List<FrameInfo> InsertInternal(string fileName, string pathTemp)
        {
            List<FrameInfo> listFrames;

            try
            {
                switch (fileName.Split('.').Last())
                {
                    case "stg":
                    case "zip":

                        listFrames = ImportFromProject(fileName, pathTemp);
                        break;

                    case "gif":

                        listFrames = ImportFromGif(fileName, pathTemp);
                        break;

                    case "mp4":
                    case "wmv":
                    case "avi":

                        listFrames = ImportFromVideo(fileName, pathTemp); //TODO: Remake. Show status.
                        break;

                    default:

                        listFrames = ImportFromImage(fileName, pathTemp);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Import Error");
                return null;
            }

            return listFrames;
        }

        private void ImportFrom(string fileName, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(DispatcherResMessage("Editor.PreparingImport"), 100);

            var listFrames = InsertInternal(fileName, pathTemp);

            if (listFrames == null || listFrames.Count == 0)
            {
                Dispatcher.Invoke(delegate
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;

                    if (ListFrames?.Count > 0)
                        FilledList = false;

                    HideProgress();

                    CommandManager.InvalidateRequerySuggested();
                });

                return;
            }

            ActionStack.Clear();
            ActionStack.Prepare(listFrames[0].ImageLocation);

            Dispatcher.Invoke(() => LoadNewFrames(listFrames));
        }

        private void ImportFromCallback(IAsyncResult ar)
        {
            _importFramesDel.EndInvoke(ar);

            GC.Collect();
        }

        private void InsertImportFrom(string fileName, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(DispatcherResMessage("Editor.PreparingImport"), 100);

            var auxList = InsertInternal(fileName, pathTemp);

            Dispatcher.Invoke(() =>
            {
                #region Insert

                var insert = new Insert(ListFrames, auxList, FrameListView.SelectedIndex) { Owner = this };
                var result = insert.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    ListFrames = insert.ActualList;
                    LoadSelectedStarter(FrameListView.SelectedIndex, ListFrames.Count - 1); //Check
                }

                #endregion
            });
        }

        private void InsertImportFromCallback(IAsyncResult ar)
        {
            _importFramesDel.EndInvoke(ar);

            GC.Collect();

            //TODO: Check if this thing is needed.
            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;

                FrameListView.Focus();
                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        private List<FrameInfo> ImportFromProject(string sourceFileName, string pathTemp)
        {
            try
            {
                //Extract to the folder.
                ZipFile.ExtractToDirectory(sourceFileName, pathTemp);

                if (!File.Exists(Path.Combine(pathTemp, "List.sb")))
                    throw new FileNotFoundException("Impossible to open project.", "List.sb");

                //Read as text.
                var serial = File.ReadAllText(Path.Combine(pathTemp, "List.sb"));

                //Deserialize to a List.
                var list = Serializer.DeserializeFromString<List<FrameInfo>>(serial);

                //Shows the ProgressBar
                ShowProgress("Importing Frames", list.Count);

                int count = 0;
                foreach (var frame in list)
                {
                    //Change the file path to the current one.
                    frame.ImageLocation = Path.Combine(pathTemp, Path.GetFileName(frame.ImageLocation));

                    count++;
                    UpdateProgress(count);
                }

                return list;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Import Project");
                return new List<FrameInfo>();
            }
        }

        private List<FrameInfo> ImportFromGif(string sourceFileName, string pathTemp)
        {
            ShowProgress(DispatcherResMessage("Editor.ImportingFrames"), 50, true);

            GifFile gifMetadata;
            var listFrames = new List<FrameInfo>();

            var decoder = ImageMethods.GetDecoder(sourceFileName, out gifMetadata) as GifBitmapDecoder;

            ShowProgress(DispatcherResMessage("Editor.ImportingFrames"), decoder.Frames.Count);

            if (decoder.Frames.Count > 0)
            {
                var fullSize = ImageMethods.GetFullSize(decoder, gifMetadata);
                int index = 0;

                BitmapSource baseFrame = null;
                foreach (var rawFrame in decoder.Frames)
                {
                    var metadata = ImageMethods.GetFrameMetadata(decoder, gifMetadata, index);

                    var bitmapSource = ImageMethods.MakeFrame(fullSize, rawFrame, metadata, baseFrame);

                    #region Disposal Method

                    switch (metadata.DisposalMethod)
                    {
                        case FrameDisposalMethod.None:
                        case FrameDisposalMethod.DoNotDispose:
                            baseFrame = bitmapSource;
                            break;
                        case FrameDisposalMethod.RestoreBackground:
                            if (ImageMethods.IsFullFrame(metadata, fullSize))
                            {
                                baseFrame = null;
                            }
                            else
                            {
                                baseFrame = ImageMethods.ClearArea(bitmapSource, metadata);
                            }
                            break;
                        case FrameDisposalMethod.RestorePrevious:
                            // Reuse same base frame
                            break;
                    }

                    #endregion

                    #region Each Frame

                    var fileName = Path.Combine(pathTemp, index + ".bmp");

                    using (var stream = new FileStream(fileName, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(stream);
                        stream.Close();
                    }

                    //It should not throw a overflow exception because of the maximum value for the miliseconds.
                    var frame = new FrameInfo(fileName, (int)metadata.Delay.TotalMilliseconds);
                    listFrames.Add(frame);

                    UpdateProgress(index);

                    GC.Collect(1);

                    #endregion

                    index++;
                }
            }

            #region Old Way to Save the Image to the Recording Folder

            //var decoder = new GifBitmapDecoder(new Uri(sourceFileName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            //int count = 0;

            //foreach (BitmapFrame bitmapFrame in decoder.Frames)
            //{
            //    var fileName = String.Format("{0}{1}.bmp", pathTemp, count);

            //    using (var stream = new FileStream(fileName, FileMode.Create))
            //    {
            //        BitmapEncoder encoder = new BmpBitmapEncoder();
            //        encoder.Frames.Add(BitmapFrame.Create(bitmapFrame));
            //        encoder.Save(stream);
            //        stream.Close();
            //    }

            //    var frame = new FrameInfo(fileName, 66);
            //    listFrames.Add(frame);

            //    count++;
            //}

            #endregion

            return listFrames;
        }

        private List<FrameInfo> ImportFromImage(string sourceFileName, string pathTemp)
        {
            var fileName = Path.Combine(pathTemp, 0 + DateTime.Now.ToString("hh-mm-ss") + ".bmp");

            #region Save the Image to the Recording Folder

            var bitmap = new BitmapImage(new Uri(sourceFileName));

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                stream.Close();
            }

            GC.Collect();

            #endregion

            return new List<FrameInfo> { new FrameInfo(fileName, 66) };
        }

        private List<FrameInfo> ImportFromVideo(string fileName, string pathTemp)
        {
            int delay = 66;

            var frameList = Dispatcher.Invoke(() =>
            {
                var videoSource = new VideoSource(fileName) { Owner = this };
                var result = videoSource.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    delay = videoSource.Delay;
                    return videoSource.FrameList;
                }

                return null;
            });

            if (frameList == null) return null;

            ShowProgress(DispatcherResMessage("Editor.ImportingFrames"), frameList.Count);

            #region Saves the Frames to the Disk

            var frameInfoList = new List<FrameInfo>();
            int count = 0;

            foreach (BitmapFrame frame in frameList)
            {
                var frameName = Path.Combine(pathTemp, count + DateTime.Now.ToString("hh-mm-ss") + ".bmp");

                using (var stream = new FileStream(frameName, FileMode.Create))
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(frame);
                    encoder.Save(stream);
                    stream.Close();
                }

                var frameInfo = new FrameInfo(frameName, delay);
                frameInfoList.Add(frameInfo);

                GC.Collect();
                count++;

                UpdateProgress(count);
            }

            #endregion

            return frameInfoList;
        }

        #endregion

        #region Playback

        private void PlayPause()
        {
            if (_timerPreview.Enabled)
            {
                _timerPreview.Tick -= TimerPreview_Tick;
                _timerPreview.Stop();

                NotPreviewing = true;
                PlayButton.Text = ResMessage("Editor.View.Play");
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");

                PlayMenuItem.Header = ResMessage("Editor.View.Play");
                PlayMenuItem.Image = (Canvas)FindResource("Vector.Play");
            }
            else
            {
                NotPreviewing = false;
                PlayButton.Text = ResMessage("Editor.View.Pause");
                PlayButton.Content = FindResource("Vector.Pause");
                PlayPauseButton.Content = FindResource("Vector.Pause");

                PlayMenuItem.Header = ResMessage("Editor.View.Pause");
                PlayMenuItem.Image = (Canvas)FindResource("Vector.Pause");

                #region Starts playing the next frame

                if (ListFrames.Count - 1 == FrameListView.SelectedIndex)
                {
                    FrameListView.SelectedIndex = 0;
                }
                else
                {
                    FrameListView.SelectedIndex++;
                }

                #endregion

                if (ListFrames[FrameListView.SelectedIndex].Delay == 0)
                    ListFrames[FrameListView.SelectedIndex].Delay = 10;

                _timerPreview.Interval = ListFrames[FrameListView.SelectedIndex].Delay;
                _timerPreview.Tick += TimerPreview_Tick;
                _timerPreview.Start();
            }
        }

        private void Pause()
        {
            if (_timerPreview.Enabled)
            {
                _timerPreview.Tick -= TimerPreview_Tick;
                _timerPreview.Stop();

                NotPreviewing = true;
                PlayButton.Text = ResMessage("Editor.View.Play");
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");

                PlayMenuItem.Header = ResMessage("Editor.View.Play");
                PlayMenuItem.Image = (Canvas)FindResource("Vector.Play");
            }
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

                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            }, DispatcherPriority.Loaded);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                StatusProgressBar.IsIndeterminate = false;
                StatusProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                StatusGrid.Visibility = Visibility.Hidden;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            });
        }

        #endregion

        private void SelectNear(int index)
        {
            if (FrameListView.Items.Count - 1 < index)
            {
                FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                return;
            }

            FrameListView.SelectedIndex = index;
            FrameListView.ScrollIntoView(FrameListView.SelectedItem);
        }

        private void AdjustFrameNumbers(int startIndex)
        {
            for (int index = startIndex; index < FrameListView.Items.Count; index++)
            {
                ((FrameListBoxItem)FrameListView.Items[index]).FrameNumber = index;
            }
        }

        private void ShowPanel(PanelType type, string title, string vector, Action<object, RoutedEventArgs> apply = null)
        {
            //ActionGrid.BeginStoryboard(FindResource("HidePanelStoryboard") as Storyboard);

            #region Hide all Grids

            foreach (UIElement child in ActionInternalGrid.Children)
            {
                child.Visibility = Visibility.Collapsed;
            }

            #endregion

            #region Overlay

            //TODO: Better
            if (ListFrames != null && ListFrames.Count > 0)
            {
                var image = ListFrames[0].ImageLocation.SourceFrom();
                CaptionOverlayGrid.Width = image.Width;
                CaptionOverlayGrid.Height = image.Height;
            }

            #endregion

            #region Commons

            ActionTitleLabel.Content = title;
            ActionViewBox.Child = FindResource(vector) as Canvas;

            Util.Other.RemoveRoutedEventHandlers(ApplyButton, ButtonBase.ClickEvent);

            if (apply != null)
            {
                ApplyButton.Text = ResMessage("Action.Apply");
                ApplyButton.Content = FindResource("Vector.Ok") as Canvas;
                ApplyButton.Click += apply.Invoke;
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
                case PanelType.SaveAs:
                    ApplyButton.Text = ResMessage("Action.Save");
                    ApplyButton.Content = FindResource("Vector.Save") as Canvas;
                    SaveGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.NewAnimation:
                    NewGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Clipboard:
                    ClipboardGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Resize:
                    ResizeGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.FlipRotate:
                    FlipRotateGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Crop:
                    #region Crop

                    CropGrid.Visibility = Visibility.Visible;

                    AddCropToElement(CropAreaGrid);
                    RefreshCropImage();

                    BottomCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height - (CaptionOverlayGrid.Height * .1));
                    TopCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height * .1);

                    RightCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width - (CaptionOverlayGrid.Width * .1));
                    LeftCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width * .1);

                    #endregion
                    break;
                case PanelType.Caption:
                    CaptionGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.FreeText:
                    FreeTextGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.TitleFrame:
                    TitleFrameGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.FreeDrawing:
                    FreeDrawingGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Watermark:
                    #region Watermark

                    //TODO:
                    //Set the position of the image based on the latest position, if the position -+ size is enough to showthe image.
                    //_adornerLayer = AdornerLayer.GetAdornerLayer(WatermarkImage);
                    //_adornerLayer.Add(new ResizingAdorner(WatermarkImage));

                    WatermarkGrid.Visibility = Visibility.Visible;

                    #endregion
                    break;
                case PanelType.Border:
                    BorderGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.OverrideDelay:
                    OverrideDelayGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.ChangeDelay:
                    ChangeDelayGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Cinemagraph:
                    CinemagraphGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Fade:
                    FadeGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Slide:
                    SlideGrid.Visibility = Visibility.Visible;
                    break;
            }

            #endregion

            if (ActionGrid.Width < 5)
                ActionGrid.BeginStoryboard(FindResource("ShowPanelStoryboard") as Storyboard, HandoffBehavior.SnapshotAndReplace);

            #region Overlay Grid

            if (OverlayGrid.Opacity < 1 && (int)type < 0)
            {
                OverlayGrid.BeginStoryboard(FindResource("ShowOverlayGridStoryboard") as Storyboard, HandoffBehavior.Compose);
                ZoomBoxControl.Zoom = 1.0;
            }
            else if (OverlayGrid.Opacity > 0 && (int)type > 0)
                OverlayGrid.BeginStoryboard(FindResource("HideOverlayGridStoryboard") as Storyboard, HandoffBehavior.Compose);

            #endregion

            CommandManager.InvalidateRequerySuggested();
        }

        private void ClosePanel()
        {
            EditorStatusBand.Hide();

            ActionGrid.BeginStoryboard(FindResource("HidePanelStoryboard") as Storyboard);
            OverlayGrid.BeginStoryboard(FindResource("HideOverlayGridStoryboard") as Storyboard);
        }

        private List<int> SelectedFramesIndex()
        {
            return FrameListView.SelectedItems.OfType<FrameListBoxItem>().Select(x => FrameListView.Items.IndexOf(x)).ToList();
        }

        #endregion

        #region Async Project

        private delegate void SaveProjectDelegate(string fileName);

        private SaveProjectDelegate _saveProjectDel;

        private void SaveProject(string fileName)
        {
            ShowProgress(DispatcherResMessage("Editor.ExportingRecording"), ListFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Export as Project

            try
            {
                string serial = Serializer.SerializeToString(ListFrames);

                if (serial == null)
                    throw new Exception("Object serialization failed.");

                string tempDirectory = Path.GetDirectoryName(ListFrames.First().ImageLocation);

                var dir = Directory.CreateDirectory(Path.Combine(tempDirectory, "Export"));

                File.WriteAllText(Path.Combine(dir.FullName, "List.sb"), serial);

                int count = 0;
                foreach (FrameInfo frameInfo in ListFrames)
                {
                    File.Copy(frameInfo.ImageLocation, Path.Combine(dir.FullName, Path.GetFileName(frameInfo.ImageLocation)));
                    UpdateProgress(count++);
                }

                ZipFile.CreateFromDirectory(dir.FullName, fileName);
                Directory.Delete(dir.FullName, true);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Exporting Recording as a Project");
                Dispatcher.Invoke(() => Dialog.Ok("Error While Saving", "Error while Saving as Project", ex.Message));
            }

            #endregion
        }

        private void SaveProjectCallback(IAsyncResult ar)
        {
            _saveProjectDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;

                HideProgress();

                CommandManager.InvalidateRequerySuggested();
            });

            GC.Collect();
        }

        #endregion

        #region Async Discard

        private delegate void DiscardFrames(List<FrameInfo> removeFrames);

        private DiscardFrames _discardFramesDel;

        private void Discard(List<FrameInfo> removeFrames)
        {
            ShowProgress(DispatcherResMessage("Editor.DiscardingFrames"), removeFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            try
            {
                int count = 0;
                foreach (FrameInfo frame in removeFrames)
                {
                    File.Delete(frame.ImageLocation);

                    UpdateProgress(count++);
                }

                string path = Path.GetDirectoryName(removeFrames[0].ImageLocation);
                var folderList = Directory.EnumerateDirectories(path).ToList();

                ShowProgress(DispatcherResMessage("Editor.DiscardingFolders"), folderList.Count);

                count = 0;
                foreach (string folder in folderList)
                {
                    if (!folder.Contains("Encode "))
                        Directory.Delete(folder, true);

                    UpdateProgress(count++);
                }

                removeFrames.Clear();
                ActionStack.Clear();
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

            HideProgress();
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                WelcomeGrid.BeginStoryboard(FindResource("ShowWelcomeBorderStoryboard") as Storyboard, HandoffBehavior.Compose);

                FilledList = false;
                IsLoading = false;
                WelcomeTextBlock.Text = Humanizer.Welcome();

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
            });

            GC.Collect();
        }

        private void DiscardAndLoadCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                FilledList = false;

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
            });

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);

            GC.Collect();
        }

        #endregion

        #region Async Resize

        private delegate void ResizeFrames(int width, int height, double dpi);

        private ResizeFrames _resizeFramesDel;

        private void Resize(int width, int height, double dpi)
        {
            ShowProgress(DispatcherResMessage("Editor.ResizingFrames"), ListFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            int count = 0;
            foreach (FrameInfo frame in ListFrames)
            {
                var png = new BmpBitmapEncoder();
                png.Frames.Add(ImageMethods.ResizeImage((BitmapImage)frame.ImageLocation.SourceFrom(), width, height, 0, dpi));

                using (Stream stm = File.OpenWrite(frame.ImageLocation))
                {
                    png.Save(stm);
                }

                UpdateProgress(count++);
            }
        }

        private void ResizeCallback(IAsyncResult ar)
        {
            _resizeFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(0, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Crop

        private delegate void CropFrames(Int32Rect rect);

        private CropFrames _cropFramesDel;

        private void Crop(Int32Rect rect)
        {
            ShowProgress(DispatcherResMessage("Editor.CroppingFrames"), ListFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            int count = 0;
            foreach (FrameInfo frame in ListFrames)
            {
                var png = new BmpBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(frame.ImageLocation.CropFrom(rect)));

                using (Stream stm = File.OpenWrite(frame.ImageLocation))
                {
                    png.Save(stm);
                }

                UpdateProgress(count++);
            }
        }

        private void CropCallback(IAsyncResult ar)
        {
            _cropFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(0, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Merge Frames

        private delegate List<int> OverlayFrames(RenderTargetBitmap render, double dpi, bool forAll = false);

        private OverlayFrames _overlayFramesDel;

        private List<int> Overlay(RenderTargetBitmap render, double dpi, bool forAll = false)
        {
            var frameList = forAll ? ListFrames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? ListFrames.Select(x => ListFrames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(DispatcherResMessage("Editor.ApplyingOverlay"), frameList.Count);

            int count = 0;
            foreach (FrameInfo frame in frameList)
            {
                var image = frame.ImageLocation.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawImage(render, new Rect(0, 0, render.Width, render.Height));
                }

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, dpi, dpi, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                // Creates a BmpBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(frame.ImageLocation))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }

            return selectedList;
        }

        private void OverlayCallback(IAsyncResult ar)
        {
            var selected = _overlayFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(selected.Min(), selected.Max());
            });
        }

        #endregion

        #region Async Title Frame

        private delegate int TitleFrameAction(RenderTargetBitmap render, int selected, double dpi);

        private TitleFrameAction _titleFrameDel;

        private int TitleFrame(RenderTargetBitmap render, int selected, double dpi)
        {
            ShowProgress(DispatcherResMessage("Editor.CreatingTitleFrame"), 1, true);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Save Image

            var name = Path.GetFileNameWithoutExtension(ListFrames[selected].ImageLocation);
            var folder = Path.GetDirectoryName(ListFrames[selected].ImageLocation);
            var fileName = Path.Combine(folder, String.Format("{0} TF {1}.bmp", name, DateTime.Now.ToString("hh-mm-ss")));

            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(render));

            // Saves the image into a file using the encoder
            using (Stream stream = File.Create(fileName))
                encoder.Save(stream);

            GC.Collect();

            #endregion

            //Adds to the List
            ListFrames.Insert(selected, new FrameInfo(fileName, Settings.Default.TitleFrameDelay));

            return selected;
        }

        private void TitleFrameCallback(IAsyncResult ar)
        {
            var selected = _titleFrameDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(selected, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Flip/Rotate

        private delegate void FlipRotateFrames(FlipRotateType type);

        private FlipRotateFrames _flipRotateFramesDel;

        private void FlipRotate(FlipRotateType type)
        {
            ShowProgress(DispatcherResMessage("Editor.ApplyingFlipRotate"), ListFrames.Count);

            var frameList = type == FlipRotateType.RotateLeft90 ||
                type == FlipRotateType.RotateRight90 ? ListFrames : SelectedFrames();

            Dispatcher.Invoke(() => IsLoading = true);

            int count = 0;
            foreach (FrameInfo frame in frameList)
            {
                var image = frame.ImageLocation.SourceFrom();

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

                // Creates a BmpBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(transBitmap));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(frame.ImageLocation))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void FlipRotateCallback(IAsyncResult ar)
        {
            _flipRotateFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(0, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Delay

        private delegate void DelayFrames(DelayChangeType type, int delay);

        private DelayFrames _delayFramesDel;

        private void Delay(DelayChangeType type, int delay)
        {
            var frameList = SelectedFrames();

            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
                Cursor = Cursors.AppStarting;
            });

            ShowProgress(DispatcherResMessage("Editor.ChangingDelay"), frameList.Count);

            int count = 0;
            foreach (FrameInfo frameInfo in frameList)
            {
                if (type == DelayChangeType.Override)
                {
                    frameInfo.Delay = delay;
                }
                else
                {
                    frameInfo.Delay += delay;

                    if (frameInfo.Delay < 10)
                        frameInfo.Delay = 10;
                }

                #region Update UI

                var index = ListFrames.IndexOf(frameInfo);
                Dispatcher.Invoke(() => ((FrameListBoxItem)FrameListView.Items[index]).Delay = frameInfo.Delay);

                #endregion

                UpdateProgress(count++);
            }
        }

        private void DelayCallback(IAsyncResult ar)
        {
            _delayFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.Arrow;

                HideProgress();
                IsLoading = false;

                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        #region Async Transitions

        private delegate int Transition(int selected, int frameCount, object optional);

        private Transition _transitionDel;

        private int Fade(int selected, int frameCount, object optional)
        {
            ShowProgress(DispatcherResMessage("Editor.ApplyingTransition"), ListFrames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            //Calculate opacity increment.
            var increment = 1F / (frameCount + 1);
            var previousName = Path.GetFileNameWithoutExtension(ListFrames[selected].ImageLocation);
            var previousFolder = Path.GetDirectoryName(ListFrames[selected].ImageLocation);

            #region Images

            var previousImage = ListFrames[selected].ImageLocation.SourceFrom();
            var nextImage = ListFrames[(ListFrames.Count - 1) == selected ? 0 : selected + 1].ImageLocation.SourceFrom();

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

            for (int index = 0; index < frameCount; index++)
            {
                var drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(previousImage, new Rect(0, 0, previousImage.Width, previousImage.Height));
                    drawingContext.DrawRectangle(nextBrush, null, new Rect(0, 0, nextImage.Width, nextImage.Height));
                }

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap(previousImage.PixelWidth, previousImage.PixelHeight, previousImage.DpiX, previousImage.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Increase the opacity for the next frame.
                nextBrush.Opacity += increment;

                //TODO: Fix filenaming.
                string fileName = Path.Combine(previousFolder, String.Format("{0} T {1} {2}.bmp", previousName, index, DateTime.Now.ToString("hh-mm-ss")));
                ListFrames.Insert(selected + index + 1, new FrameInfo(fileName, 66));

                // Creates a BmpBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(fileName))
                    encoder.Save(stream);

                UpdateProgress(index);
            }

            #endregion

            return selected;
        }

        private int Slide(int selected, int frameCount, object optional)
        {
            ShowProgress(DispatcherResMessage("Editor.ApplyingTransition"), ListFrames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            var previousName = Path.GetFileNameWithoutExtension(ListFrames[selected].ImageLocation);
            var previousFolder = Path.GetDirectoryName(ListFrames[selected].ImageLocation);

            #region Images

            var previousImage = ListFrames[selected].ImageLocation.SourceFrom();
            var nextImage = ListFrames[(ListFrames.Count - 1) == selected ? 0 : selected + 1].ImageLocation.SourceFrom();

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

            for (int index = 0; index < frameCount; index++)
            {
                var drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
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
                string fileName = Path.Combine(previousFolder, String.Format("{0} T {1} {2}.bmp", previousName, index, DateTime.Now.ToString("hh-mm-ss")));
                ListFrames.Insert(selected + index + 1, new FrameInfo(fileName, 66));

                // Creates a BmpBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(fileName))
                    encoder.Save(stream);

                UpdateProgress(index);
            }

            #endregion


            return selected;
        }

        private void TransitionCallback(IAsyncResult ar)
        {
            var selected = _transitionDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(selected, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Other

        private static string CreateTempPath()
        {
            #region Temp Path

            string pathTemp = Path.GetTempPath() + $@"ScreenToGif\Recording\{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}\";

            if (!Directory.Exists(pathTemp))
                Directory.CreateDirectory(pathTemp);

            #endregion

            return pathTemp;
        }

        private void DeleteFrame(int index)
        {
            //Delete the File from the disk.
            File.Delete(ListFrames[index].ImageLocation);

            //Remove from the list.
            ListFrames.RemoveAt(index);
            FrameListView.Items.RemoveAt(index);
        }

        private List<FrameInfo> SelectedFrames()
        {
            var selectedIndexList = Dispatcher.Invoke(() => SelectedFramesIndex());
            return ListFrames.Where(x => selectedIndexList.Contains(ListFrames.IndexOf(x))).ToList();
        }

        private string ResMessage(string key)
        {
            return FindResource(key).ToString().Replace("\\n", " ");
        }

        private string DispatcherResMessage(string key)
        {
            return Dispatcher.Invoke(() => FindResource(key).ToString().Replace("\n", " "));
        }

        #endregion

        #endregion

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            #region Validation

            //Filename empty, (if not overwrite, check if exists), etc

            #endregion

            #region Parameters

            var fileName = Path.Combine(Settings.Default.DefaultOutput, Settings.Default.LatestFilename);

            Parameters param;

            if (GifRadioButton.IsChecked.HasValue && GifRadioButton.IsChecked.Value)
            {
                param = new GifParameters
                {
                    Type = Export.Gif,
                    EncoderType = NewEncoderRadioButton.IsChecked == true ? GifEncoderType.ScreenToGif :
                        LegacyEncoderRadioButton.IsChecked == true ? GifEncoderType.Legacy : GifEncoderType.PaintNet,
                    DetectUnchangedPixels = Settings.Default.DetectUnchanged,
                    DummyColor = Settings.Default.PaintTransparent ? Settings.Default.TransparentColor : new Color?(),
                    Quality = Settings.Default.Quality,
                    RepeatCount = Settings.Default.Looped ? (Settings.Default.RepeatForever ? 0 : Settings.Default.RepeatCount) : -1,
                    Filename = fileName + ".gif"
                };
            }
            else
            {
                //images, framerate, fps, output
                //images: %d.bmp 
                //framerate = -vf ""zoompan = d = 25 + '50*eq(in,3)' + '100*eq(in,5)'""
                var command = "-i \"{0}\" {1} -r 15 \"{2}\"";

                param = new VideoParameters
                {
                    Type = Export.Video,
                    VideoEncoder = FfmpegEncoderRadioButton.IsChecked == true ? VideoEncoderType.Ffmpg : VideoEncoderType.AviStandalone,
                    Quality = (uint)AviQualitySlider.Value,
                    Command = command,
                    Filename = fileName + (FfmpegEncoderRadioButton.IsChecked == true ? ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag : ".avi")
                };
            }

            #endregion

            Encoder.AddItem(ListFrames.CopyToEncode(), param, this.Scale());
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            if (!Util.Other.IsFfmpegPresent())
                SystemEncoderRadioButton.IsChecked = true;

            ShowPanel(PanelType.SaveAs, ResMessage("Editor.File.Save"), "Vector.Save", SaveAsButton_Click);
        }

        private void TransparentColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorSelector(Settings.Default.TransparentColor, false) { Owner = this };
            var result = colorDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.TransparentColor = colorDialog.SelectedColor;
            }
        }

        private void ChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                FileName = Settings.Default.LatestFilename,
                InitialDirectory = Settings.Default.DefaultOutput,
                DefaultExt = GifRadioButton.IsChecked == true ? ".gif" : ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag.ToString(),
                Filter = GifRadioButton.IsChecked == true ? "Gif animation (.gif)|*.gif" : "Avi video (.avi)|*.avi|Mp4 video (.mp4)|*.mp4|WebM video|*.webm|Wmv video|*.wmv",
            };

            var result = sfd.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            Settings.Default.DefaultOutput = Path.GetDirectoryName(sfd.FileName);
            Settings.Default.LatestFilename = Path.GetFileNameWithoutExtension(sfd.FileName);
        }

        private void FfmpegEncoderRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (Util.Other.IsFfmpegPresent())
            {
                EncoderStatusBand.Hide();
                return;
            }

            EncoderStatusBand.Warning("FFmpeg not present. Add it to the Path environment variables or set the location on Settings."); //TODO: Localize.
            SystemEncoderRadioButton.IsChecked = true;
        }

        private void IncreaseNumber_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileNumber(1);
        }

        private void DecreaseNumber_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileNumber(-1);
        }

        public void ChangeFileNumber(int change)
        {
            //If there's no filename declared, show the default one.
            if (string.IsNullOrWhiteSpace(Settings.Default.LatestFilename))
            {
                Settings.Default.LatestFilename = FindResource("SaveAs.File.Animation") as string;
                return;
            }

            int index = Settings.Default.LatestFilename.Length;
            int start = -1, end = -1;

            //Detects the last number in a string.
            foreach (char c in Settings.Default.LatestFilename.Reverse())
            {
                if (char.IsNumber(c))
                {
                    if (end == -1)
                        end = index;

                    start = index - 1;
                }
                else if (start == index)
                    break;

                index--;
            }

            //If there's no number.
            if (end == -1)
            {
                Settings.Default.LatestFilename += $" ({change})";
                return;
            }

            //If iy's a negative number, include the signal.
            if (start > 0 && Settings.Default.LatestFilename.Substring(start - 1, 1).Equals("-"))
                start--;

            //Cut, convert, merge.
            int number;
            if (int.TryParse(Settings.Default.LatestFilename.Substring(start, end - start), out number))
            {
                int offset = start + number.ToString().Length;

                Settings.Default.LatestFilename = Settings.Default.LatestFilename.Substring(0, start) + (number + change) +
                    Settings.Default.LatestFilename.Substring(offset, Settings.Default.LatestFilename.Length - end);
            }
        }

        private void FileHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var extension = GifRadioButton.IsChecked == true ? ".gif" : (FfmpegEncoderRadioButton.IsChecked == true
                    ? ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag
                    : ".avi");

                Process.Start(Path.Combine(OutputFolderTextBox.Text, OutputFilenameTextBox.Text + extension));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open file");
            }
        }

        private void FilenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            var extension = GifRadioButton.IsChecked == true ? ".gif" : (FfmpegEncoderRadioButton.IsChecked == true
                ? ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag
                : ".avi");

            var exists = File.Exists(Path.Combine(OutputFolderTextBox.Text, OutputFilenameTextBox.Text + extension));

            FileExistsGrid.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
