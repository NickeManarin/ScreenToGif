using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : Window
    {
        #region Properties

        public static readonly DependencyProperty FilledListProperty = DependencyProperty.Register("FilledList", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty NotPreviewingProperty = DependencyProperty.Register("NotPreviewing", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));

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

        #endregion

        #region Variables

        /// <summary>
        /// The List of Frames.
        /// </summary>
        public List<FrameInfo> ListFrames { get; set; }

        /// <summary>
        /// True if loading frames.
        /// </summary>
        public bool IsLoading { get; set; }

        /// <summary>
        /// The clipboard.
        /// </summary>
        public List<FrameInfo> ClipboardFrames { get; set; }

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
            if (ListFrames != null)
            {
                Cursor = Cursors.AppStarting;
                FrameListView.IsEnabled = false;
                RibbonTabControl.IsEnabled = false;

                ActionStack.Prepare(ListFrames[0].ImageLocation);

                _loadFramesDel = Load;
                _loadFramesDel.BeginInvoke(LoadCallback, null);
            }

            KeyUp += Editor_KeyUp;
        }

        #endregion

        #region Frame Selection

        private void FrameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[e.AddedItems.Count - 1] as FrameListBoxItem;

                if (item != null)
                {
                    var index = item.FrameNumber == 0 ?
                        0 : ListFrames.Count >= item.FrameNumber ?
                        item.FrameNumber : ListFrames.Count - 1;

                    ZoomBoxControl.ImageSource = ListFrames[index].ImageLocation;
                    FrameListView.ScrollIntoView(item);

                    FpsNumericUpDown.ValueChanged -= NumericUpDown_OnValueChanged;
                    FpsNumericUpDown.Value = ListFrames[index].Delay;
                    FpsNumericUpDown.ValueChanged += NumericUpDown_OnValueChanged;
                }

                GC.Collect(1);
                return;
            }

            if (e.RemovedItems.Count <= 0) return;

            var removedItem = e.RemovedItems[e.RemovedItems.Count - 1] as FrameListBoxItem;

            //Error: -1?
            if (removedItem != null)
            {
                var index = removedItem.FrameNumber == 0 ?
                    0 : ListFrames.Count >= removedItem.FrameNumber ?
                        removedItem.FrameNumber - 1 : ListFrames.Count - 1;

                ZoomBoxControl.ImageSource = ListFrames[index].ImageLocation;
                FrameListView.ScrollIntoView(removedItem);

                FpsNumericUpDown.ValueChanged -= NumericUpDown_OnValueChanged;
                FpsNumericUpDown.Value = ListFrames[index].Delay;
                FpsNumericUpDown.ValueChanged += NumericUpDown_OnValueChanged;
            }

            GC.Collect(1);
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as FrameListBoxItem;

            if (item != null)
                ZoomBoxControl.ImageSource = ListFrames[item.FrameNumber].ImageLocation;

            GC.Collect(1);
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
            Hide();
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            if (result.HasValue && recorder.ExitArg == ExitAction.Recorded && recorder.ListFrames != null)
            {
                DiscardProject_Executed(null, null);

                ActionStack.Clear();
                ActionStack.Prepare(recorder.ListFrames[0].ImageLocation);

                LoadNewFrames(recorder.ListFrames);
            }

            Encoder.Restore();
            ShowDialog();
        }

        private void NewWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            Hide();

            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            if (result.HasValue && !result.Value)
            {
                if (webcam.ExitArg == ExitAction.Recorded)
                {
                    DiscardProject_Executed(null, null);

                    ActionStack.Clear();
                    ActionStack.Prepare(webcam.ListFrames[0].ImageLocation);

                    LoadNewFrames(webcam.ListFrames);
                }
            }

            ShowDialog();
        }

        private void NewAnimation_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.NewAnimation, "New Animation", "Vector.File.New");
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
                var bitmapSource = ImageMethods.CreateEmtpyBitmapSource(Settings.Default.NewImageColor,
                    Settings.Default.NewImageWidth, Settings.Default.NewImageHeight, PixelFormats.Indexed1);
                var bitmapFrame = BitmapFrame.Create(bitmapSource);

                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(bitmapFrame);
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            GC.Collect();

            #endregion

            DiscardProject_Executed(null, null);

            #region Adds to the List

            var frame = new FrameInfo(fileName, 66);

            ListFrames = new List<FrameInfo> { frame };
            LoadNewFrames(ListFrames);

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
                Title = "Open a Media (Image or Video) or a Project File",
                Filter = "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi|" +
                         "ScreenToGif Project (*.stg) |*.stg",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                DiscardProject_Executed(null, null);

                _importFramesDel = ImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileName, CreateTempPath(), ImportFromCallback, null);
            }
        }

        #endregion

        #region Insert

        private void Insert_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 0 && !IsLoading && !e.Handled;
        }

        private void InsertRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            Hide();
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();
                Encoder.Restore();
                Show();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex) { Owner = this };
            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ActionStack.Did(ListFrames);
                LoadNewFrames(insert.ActualList);
            }

            #endregion

            Encoder.Restore();
            ShowDialog();
        }

        private void InsertWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            Hide();
            Encoder.Minimize();

            var recorder = new Webcam();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();
                Encoder.Restore();
                Show();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex);
            insert.Owner = this;

            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ActionStack.Did(ListFrames);
                LoadNewFrames(insert.ActualList);
            }

            #endregion

            Encoder.Restore();
            ShowDialog();
        }

        private void InsertFromMedia_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = "Open a Media (Image or Video)",
                Filter = "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ActionStack.Did(ListFrames);

                _importFramesDel = InsertImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileName, CreateTempPath(), ImportFromCallback, null);
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
                var ofd = new SaveFileDialog();
                ofd.AddExtension = true;
                ofd.Filter = "Gif Animation (*.gif)|*.gif";
                ofd.Title = "Save Animation As Gif"; //TODO: Better description.

                var result = ofd.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return;

                Encoder.AddItem(ListFrames.CopyToEncode(), ofd.FileName, this.Dpi());
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
                var ofd = new SaveFileDialog();
                ofd.AddExtension = true;
                ofd.Filter = "Avi Video (*.avi)|*.avi";
                ofd.Title = "Save Animation as Video";

                var result = ofd.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return;

                Encoder.AddItem(ListFrames.CopyToEncode(), ofd.FileName, this.Dpi());
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

            #region Export as Project

            try
            {
                #region Save Dialog

                var saveDialog = new SaveFileDialog();
                saveDialog.AddExtension = true;
                //saveDialog.DefaultExt = ".stg";
                saveDialog.FileName = String.Format("Project - {0} Frames [{1: hh-mm-ss}]", ListFrames.Count, DateTime.Now);
                saveDialog.Filter = "*.stg|(ScreenToGif Project)|*.zip|(Zip Archive)";
                saveDialog.Title = "Select the File Location";

                var result = saveDialog.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return;

                #endregion

                string serial = Serializer.SerializeToString(ListFrames);

                if (serial == null)
                    throw new Exception("Object serialization failed.");

                string tempDirectory = Path.GetDirectoryName(ListFrames.First().ImageLocation);

                var dir = Directory.CreateDirectory(Path.Combine(tempDirectory, "Export"));

                File.WriteAllText(Path.Combine(dir.FullName, "List.sb"), serial);

                foreach (FrameInfo frameInfo in ListFrames)
                {
                    File.Copy(frameInfo.ImageLocation, Path.Combine(dir.FullName, Path.GetFileName(frameInfo.ImageLocation)));
                }

                ZipFile.CreateFromDirectory(dir.FullName, saveDialog.FileName + (saveDialog.FilterIndex == 1 ? ".stg" : ".zip"));
            }
            catch (Exception ex)
            {
                Dialog.Ok("Error While Saving", "Error while Saving as Project", ex.Message);
                LogWriter.Log(ex, "Exporting Recording as a Project");
            }

            #endregion
        }

        private void DiscardProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionGrid.BeginStoryboard(FindResource("HidePanelStoryboard") as Storyboard);

            if (ListFrames == null || ListFrames.Count == 0) return;

            try
            {
                FrameListView.SelectionChanged -= FrameListView_SelectionChanged;
                FrameListView.SelectedIndex = -1;

                FrameListView.Items.Clear();
                ZoomBoxControl.Clear();

                foreach (FrameInfo frame in ListFrames)
                {
                    File.Delete(frame.ImageLocation);
                }

                string path = Path.GetDirectoryName(ListFrames[0].ImageLocation);
                var folderList = Directory.EnumerateDirectories(path);

                foreach (string folder in folderList)
                {
                    if (!folder.StartsWith("Enc"))
                        Directory.Delete(folder, true);
                }

                ListFrames.Clear();
                ActionStack.Clear();

                FilledList = false;

                WelcomeGrid.BeginStoryboard(FindResource("ShowWelcomeBorderStoryboard") as Storyboard, HandoffBehavior.Compose);

                FrameListView.Visibility = Visibility.Collapsed;
                WelcomeTextBlock.Text = "..."; //TODO: Show tips.

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
            }
            catch (Exception ex)
            {
                Dialog.Ok("Discard Error", "Error while trying to discard the project", ex.Message);
                LogWriter.Log(ex, "Error while trying to Discard the Project");
            }

            GC.Collect();
        }

        #endregion

        #endregion

        #region View Tab

        private void Playback_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 1 && ActionGrid.Width < 220;
        }

        private void FirstFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            FrameListView.SelectedIndex = 0;
        }

        private void PreviousFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

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

            FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
        }


        private void Zoom_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 0 && !OverlayGrid.IsVisible;
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
            var viewHeight = ZoomBoxControl.ActualHeight;
            var viewWidth = ZoomBoxControl.ActualWidth;

            #endregion

            #region Calculate the Zoom

            var zoomHeight = 0D;
            var zoomWidth = 0D;

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
            Encoder.Start(this.Dpi());



            var test = new Board();
            test.ShowDialog();
        }

        #endregion

        #region Edit Tab

        #region Action Stack

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanUndo() && !e.Handled;
        }

        private void Reset_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanUndo() && !e.Handled;
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanRedo() && !e.Handled;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ListFrames = ActionStack.Undo(ListFrames.CopyList());
            LoadNewFrames(ListFrames);
        }

        private void Reset_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ListFrames = ActionStack.Reset(ListFrames.CopyList());
            LoadNewFrames(ListFrames);
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ListFrames = ActionStack.Redo(ListFrames.CopyList());
            LoadNewFrames(ListFrames);
        }

        #endregion

        #region ClipBoard

        private void ClipBoard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !e.Handled;
        }

        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            #region Validation

            if (FrameListView.SelectedItems.Count == FrameListView.Items.Count)
            {
                Dialog.Ok("Cut Operation", "You can't cut all frames.", "The recording needs at least one frame.", Dialog.Icons.Info);
                CutButton.IsEnabled = true;
                return;
            }

            #endregion

            ActionStack.Did(ListFrames);

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
            var list = selected.Select(item => ListFrames[item.FrameNumber]).ToList();

            FrameListView.SelectedItem = null;

            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => ListFrames.RemoveAt(x.FrameNumber));
            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => FrameListView.Items.Remove(x));

            list = list.CopyToClipboard(true); //Maybe add to a Clipboard class helper that handles all requests?
            ClipboardListView.DataContext = list;

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

            //var imageList = new List<ImageListBoxItem>() { imageItem };
            ClipboardListView.Items.Clear();
            ClipboardListView.Items.Add(imageItem);

            ShowPanel(PanelType.Clipboard, "Clipboard", "Vector.Paste");
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();

            #region Validation

            if (!selected.Any())
            {
                Dialog.Ok("Copy Operation", "No frames selected.", "You need to select at least one frame.", Dialog.Icons.Info);
                return;
            }

            #endregion

            var list = selected.Select(item => ListFrames[item.FrameNumber]).ToList();

            list = list.CopyToClipboard(); //Maybe add to a Clipboard class helper that handles all requests?

            ClipboardListView.DataContext = list;

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

            //var imageList = new List<ImageListBoxItem>() { imageItem };
            ClipboardListView.Items.Clear();
            ClipboardListView.Items.Add(imageItem);

            ShowPanel(PanelType.Clipboard, "Clipboard", "Vector.Paste");
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var clipData = ClipboardListView.DataContext as List<FrameInfo>;

            #region Validation

            if (clipData == null || clipData.Count == 0)
            {
                Dialog.Ok("Paste Operation", "There is no frames to paste.", "You need at least one frame to paste.", Dialog.Icons.Info);
                return;
            }

            #endregion

            ActionStack.Did(ListFrames);

            var index = FrameListView.SelectedItems.OfType<FrameListBoxItem>().Last().FrameNumber;

            index = PasteBeforeRadioButton.IsChecked.HasValue && PasteBeforeRadioButton.IsChecked.Value
                ? index
                : index + 1;

            ListFrames.InsertRange(index, clipData.CopyBackFromClipboard(index));

            LoadNewFrames(ListFrames); //TODO: Replace this one
        }

        private void ShowClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelType.Clipboard, "Clipboard", "Vector.Paste");
        }

        #endregion

        #region Frames

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null;
        }

        private void DeletePrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null &&
                        FrameListView.SelectedIndex < FrameListView.Items.Count - 1;
        }

        private void DeleteNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null &&
            FrameListView.SelectedIndex > 0;
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            try
            {
                #region Validation

                if (ListFrames.Count == FrameListView.SelectedItems.Count)
                {
                    if (Dialog.Ask("Remove All", "Do you want to remove all frames?", "You are trying to remove all frames. \n\rYou can't undo this operation.", Dialog.Icons.Question))
                    {
                        DiscardProject_Executed(null, null);
                    }

                    return;
                }

                #endregion

                ActionStack.Did(ListFrames);

                var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
                var list = selected.Select(item => ListFrames[item.FrameNumber]).ToList();

                FrameListView.SelectedItem = null;

                list.ForEach(x => File.Delete(x.ImageLocation));
                selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => ListFrames.RemoveAt(x.FrameNumber));
                selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => FrameListView.Items.Remove(x));

                AdjustFrameNumbers(selected[0].FrameNumber);
                SelectNear(selected[0].FrameNumber);
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

            ActionStack.Did(ListFrames);

            for (int i = countList; i > FrameListView.SelectedIndex; i--) //From the end to the middle.
            {
                DeleteFrame(i);
            }

            SelectNear(FrameListView.Items.Count - 1);
        }

        #endregion

        #region Reordering

        private void Reverse_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null &&
            FrameListView.Items.Count > 1;
        }

        private void Yoyo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null &&
                FrameListView.Items.Count > 1;
        }

        private void MoveLeftRight_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null &&
                FrameListView.Items.Count > 1;
        }

        private void Reverse_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            ListFrames.Reverse();

            LoadNewFrames(ListFrames);
        }

        private void Yoyo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            LoadNewFrames(Util.Other.Yoyo(ListFrames));
        }

        private void MoveLeft_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>()).OrderBy(x => x.FrameNumber).ToList();
            FrameListView.SelectedItems.Clear();

            var listIndex = selected.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selected)
            {
                #region Index

                var oldindex = listIndex[selected.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index - 1;

                if (index == 0)
                    newIndex = FrameListView.Items.Count - 1;

                if (oldindex - 1 == index)
                {
                    FrameListView.SelectedItems.Add(item);
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

                FrameListView.SelectedItems.Add(item);
            }

            //TODO: Replace with AdjustFrameNumbers
            #region Count Frames

            foreach (var frame in FrameListView.Items.OfType<FrameListBoxItem>())
            {
                frame.FrameNumber = FrameListView.Items.IndexOf(frame);
            }

            #endregion

            #endregion
        }

        private void MoveRight_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>()).OrderByDescending(x => x.FrameNumber).ToList();
            FrameListView.SelectedItems.Clear();

            var listIndex = selected.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selected)
            {
                #region Index

                var oldindex = listIndex[selected.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index + 1;

                if (index == FrameListView.Items.Count - 1)
                    newIndex = 0;

                if (oldindex + 1 == index)
                {
                    FrameListView.SelectedItems.Add(item);
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

                FrameListView.SelectedItems.Add(item);
            }

            #region Count Frames

            foreach (var frame in FrameListView.Items.OfType<FrameListBoxItem>())
            {
                frame.FrameNumber = FrameListView.Items.IndexOf(frame);
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Image Tab

        private void Image_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null;
        }

        #region Size and Position

        private void Resize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WidthResizeNumericUpDown.ValueChanged -= WidthResizeNumericUpDown_ValueChanged;
            HeightResizeNumericUpDown.ValueChanged -= HeightResizeNumericUpDown_ValueChanged;

            #region Info

            var image = ListFrames[0].ImageLocation.SourceFrom();
            CurrentDpiLabel.Content = DpiNumericUpDown.Value = (int)image.DpiX;
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

            ShowPanel(PanelType.Resize, "Resize", "Vector.Resize");
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

            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;
            EnableDisable(false);

            _resizeFramesDel = Resize;
            _resizeFramesDel.BeginInvoke((int)WidthResizeNumericUpDown.Value, (int)HeightResizeNumericUpDown.Value, (int)DpiNumericUpDown.Value, ResizeCallback, null);

            ClosePanel();
        }


        private void Crop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Crop, "Crop", "Vector.Crop");
        }

        private void ApplyCropButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;
            EnableDisable(false);

            _cropFramesDel = Crop;
            _cropFramesDel.BeginInvoke(new Int32Rect(LeftCropNumericUpDown.Value, TopCropNumericUpDown.Value,
                RightCropNumericUpDown.Value - LeftCropNumericUpDown.Value, BottomCropNumericUpDown.Value - TopCropNumericUpDown.Value),
                CropCallback, null);

            ClosePanel();
        }


        private void FlipRotate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            Cursor = Cursors.AppStarting;

            _flipRotateFramesDel = FlipRotate;
            _flipRotateFramesDel.BeginInvoke(e.Parameter as string, FlipRotateCallback, null);
        }

        #endregion

        #region Text

        private void Caption_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Caption, "Caption", "Vector.Caption");
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
            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var render = CaptionOverlayGrid.GetRender(dpi);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, OverlayCallback, null);

            ClosePanel();
        }


        private void FreeText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeText, "Free Text", "Vector.FreeText");
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
            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var render = FreeTextOverlayCanvas.GetRender(dpi);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, OverlayCallback, null);

            ClosePanel();
        }


        private void TitleFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.TitleFrame, "Title Frame", "Vector.TitleFrame");
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
            #region Create and Save Image

            var fileName = ListFrames[0].ImageLocation.Replace(".bmp", "TF.bmp");
            var dpi = ListFrames[0].ImageLocation.DpiOf();

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                #region Parameters

                var render = TitleFrameOverlayGrid.GetRender(dpi);
                var width = Math.Round(TitleFrameOverlayGrid.ActualWidth, MidpointRounding.AwayFromZero);
                var height = Math.Round(TitleFrameOverlayGrid.ActualHeight, MidpointRounding.AwayFromZero);

                var bitmapSource = ImageMethods.CreateEmtpyBitmapSource(Settings.Default.NewImageColor, (int)width, (int)height, PixelFormats.Indexed1);

                #endregion

                #region Merge

                var drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(bitmapSource, new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                    drawingContext.DrawImage(render, new Rect(0, 0, render.Width, render.Height));
                }

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                #endregion

                var bitmapFrame = BitmapFrame.Create(bmp);

                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(bitmapFrame);
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            GC.Collect();

            #endregion

            #region Adds to the List

            var frame = new FrameInfo(fileName, 500);

            ListFrames.Insert(FrameListView.SelectedIndex, frame);
            LoadNewFrames(ListFrames);

            #endregion

            ClosePanel();
        }

        #endregion

        #region Overlay

        private void FreeDrawing_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeDrawing, "Free Drawing", "Vector.FreeDrawing");
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
            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var render = FreeDrawingInkCanvas.GetRender(dpi);

            Cursor = Cursors.AppStarting;

            FreeDrawingInkCanvas.Strokes.Clear();

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, OverlayCallback, null);

            ClosePanel();
        }


        private void Watermark_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Watermark, "Watermark", "Vector.Watermark");
        }

        private void SelectWatermark_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = "Select an Image",
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
            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var render = WatermarkOverlayGrid.GetRender(dpi);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, OverlayCallback, null);

            ClosePanel();
        }


        private void Border_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Border, "Border", "Vector.Border");
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
            ActionStack.Did(ListFrames);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var render = BorderOverlayBorder.GetRender(dpi);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, OverlayCallback, null);

            ClosePanel();
        }

        #endregion

        #endregion

        #region Options Tab

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new Options();
            options.ShowDialog();
        }

        #endregion

        #region Other Events

        private void Editor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
                e.Handled = true;
        }

        private void timerPreview_Tick(object sender, EventArgs e)
        {
            _timerPreview.Tick -= timerPreview_Tick;

            //Sets the interval for this frame. If this frame has 500ms, the next frame will take 500ms to show.
            _timerPreview.Interval = ListFrames[FrameListView.SelectedIndex].Delay;

            if (ListFrames.Count - 1 == FrameListView.SelectedIndex)
            {
                FrameListView.SelectedIndex = 0;
            }
            else
            {
                FrameListView.SelectedIndex++;
            }

            _timerPreview.Tick += timerPreview_Tick;

            GC.Collect(2);
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt)
            {
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

        #region Drag and Drop

        private void Control_Drop(object sender, DragEventArgs e)
        {
            Pause();

            var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (fileNames == null) return;

            foreach (string name in fileNames)
            {
                //TODO: Import via Drag/Drop
                //...Into a new Recording if ListFrames.Count == 0
                //...Into the current recording and position if ListFrames.Count > 0
            }
        }

        private void Control_DragEnter(object sender, DragEventArgs e)
        {
            Pause();

            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        #endregion

        private void EditorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Pause();

            Settings.Default.Save();
        }

        #endregion

        #region Private Methods

        #region Load

        #region Async Loading

        private delegate bool LoadFrames();

        private LoadFrames _loadFramesDel = null;

        /// <summary>
        /// Loads the new frames and clears the old ones.
        /// </summary>
        /// <param name="listFrames">The new list of frames.</param>
        private void LoadNewFrames(List<FrameInfo> listFrames)
        {
            Cursor = Cursors.AppStarting;

            EnableDisable(false);

            ListFrames = listFrames;

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);
        }

        private bool Load()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    FrameListView.Items.Clear();
                    FrameListView.UpdateLayout();
                    ZoomBoxControl.Visibility = Visibility.Visible;

                    WelcomeGrid.BeginStoryboard(FindResource("HideWelcomeBorderStoryboard") as Storyboard, HandoffBehavior.Compose);
                });

                ShowProgress("Loading Frames", ListFrames.Count);

                if (ListFrames != null)
                {
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
                                            //Draws the ellipse first, to get behind the cursor.
                                            var rectEllipse = new Rectangle(
                                                (int)frame.CursorInfo.Position.X - (frame.CursorInfo.Image.Width / 2),
                                                (int)frame.CursorInfo.Position.Y - (frame.CursorInfo.Image.Height / 2),
                                                    frame.CursorInfo.Image.Width - 10,
                                                    frame.CursorInfo.Image.Height - 10);

                                            graph.DrawEllipse(new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Yellow), 3), rectEllipse);
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
                            itemInvoked.PreviewMouseLeftButtonDown += Item_PreviewMouseLeftButtonDown;

                            FrameListView.Items.Add(itemInvoked);

                            UpdateProgress(itemInvoked.FrameNumber);
                        });
                    }

                    if (ListFrames.Count > 0)
                        Dispatcher.Invoke(() => { FilledList = true; });
                }

                HideProgress();

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading Error");
                return false;
            }
        }

        private void LoadCallback(IAsyncResult ar)
        {
            bool result = _loadFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(delegate
            {
                this.Cursor = Cursors.Arrow;

                EnableDisable();
                CommandManager.InvalidateRequerySuggested();

                if (!result)
                {
                    //TODO: Expect errors
                    return;
                }
            });
        }

        #endregion

        #region Async Import

        private delegate bool ImportFrames(string fileName, string pathTemp);

        private ImportFrames _importFramesDel = null;

        private List<FrameInfo> InsertInternal(string fileName, string pathTemp)
        {
            List<FrameInfo> listFrames;

            try
            {
                switch (fileName.Split('.').Last())
                {
                    case "stg":

                        listFrames = ImportFromProject(fileName, pathTemp);
                        break;

                    case "gif":

                        listFrames = ImportFromGif(fileName, pathTemp); //TODO: Remake.
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

        private bool ImportFrom(string fileName, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;

                EnableDisable(false);
            });

            #endregion

            ShowProgress("Preparing to Import", 100);

            ListFrames = InsertInternal(fileName, pathTemp);

            return Load();
        }

        private bool InsertImportFrom(string fileName, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;

                EnableDisable(false);
            });

            #endregion

            ShowProgress("Preparing to Import", 100);

            var auxList = InsertInternal(fileName, pathTemp);

            Dispatcher.Invoke(() =>
            {
                #region Insert

                var insert = new Insert(ListFrames, auxList, FrameListView.SelectedIndex) { Owner = this };
                var result = insert.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    LoadNewFrames(insert.ActualList);
                }

                #endregion
            });

            return true;
        }

        private void ImportFromCallback(IAsyncResult ar)
        {
            bool result = _importFramesDel.EndInvoke(ar);

            GC.Collect();

            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                CommandManager.InvalidateRequerySuggested();

                EnableDisable();

                if (!result)
                {
                    //TODO: Expect errors
                    return;
                }
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
                LogWriter.Log(ex, "Error - Import Project");
                return new List<FrameInfo>();
            }
        }

        private List<FrameInfo> ImportFromGif(string sourceFileName, string pathTemp)
        {
            var gifDecoder = new GifDecoder();
            gifDecoder.Read(sourceFileName);

            var list = ImageMethods.GetFrames(sourceFileName);

            var listFrames = new List<FrameInfo>();

            int frameCount = gifDecoder.GetFrameCount();

            ShowProgress("Importing Frames", frameCount);

            for (int index = 0; index < frameCount; index++)
            {
                #region Each Frame

                var fileName = Path.Combine(pathTemp, index + ".bmp");

                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    //var frameAux = gifDecoder.GetFrame(index);
                    var frameAux = list[index];
                    frameAux.Save(stream, ImageFormat.Png);
                    frameAux.Dispose();

                    stream.Flush();
                    stream.Close();
                }

                var frame = new FrameInfo(fileName, gifDecoder.GetDelay(index));
                listFrames.Add(frame);

                UpdateProgress(index);

                GC.Collect(1);

                #endregion
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
            var fileName = Path.Combine(pathTemp, 0 + ".bmp");

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

            ShowProgress("Importing Frames", frameList.Count);

            #region Saves the Frames to the Disk

            var frameInfoList = new List<FrameInfo>();
            int count = 0;

            foreach (BitmapFrame frame in frameList)
            {
                var frameName = Path.Combine(pathTemp, count + ".bmp");

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
                _timerPreview.Tick -= timerPreview_Tick;
                _timerPreview.Stop();

                NotPreviewing = false;
                PlayButton.Text = Properties.Resources.Con_PlayPreview;
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");
            }
            else
            {
                NotPreviewing = true;
                PlayButton.Text = Properties.Resources.Con_StopPreview;
                PlayButton.Content = FindResource("Vector.Pause");
                PlayPauseButton.Content = FindResource("Vector.Pause");

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

                _timerPreview.Tick += timerPreview_Tick;
                _timerPreview.Start();
            }
        }

        private void Pause()
        {
            if (_timerPreview.Enabled)
            {
                _timerPreview.Tick -= timerPreview_Tick;
                _timerPreview.Stop();

                NotPreviewing = false;
                PlayButton.Text = Properties.Resources.Con_PlayPreview;
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");
            }
        }

        #endregion

        #region UI

        private void ShowProgress(string description, int maximum)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Content = description;
                StatusProgressBar.Maximum = maximum;
                StatusProgressBar.Value = 0;
                StatusGrid.Visibility = Visibility.Visible;
            }, DispatcherPriority.Loaded);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                StatusProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                StatusGrid.Visibility = Visibility.Hidden;
            });
        }

        private void SelectNear(int index)
        {
            if (FrameListView.Items.Count - 1 < index)
            {
                FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                return;
            }

            FrameListView.SelectedIndex = index;
        }

        private void AdjustFrameNumbers(int startIndex)
        {
            for (int index = startIndex; index < FrameListView.Items.Count; index++)
            {
                ((FrameListBoxItem)FrameListView.Items[index]).FrameNumber = index;
            }
        }

        private void ShowPanel(PanelType type, String title, String vector)
        {
            //ActionGrid.BeginStoryboard(FindResource("HidePanelStoryboard") as Storyboard);

            #region Hide all Grids

            foreach (UIElement child in ActionStackPanel.Children)
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

            #region Type

            switch (type)
            {
                case PanelType.NewAnimation:
                    NewGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Clipboard:
                    ClipboardGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Resize:
                    ResizeGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Crop:
                    #region Crop

                    BottomCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height - (CaptionOverlayGrid.Height * .1));
                    TopCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height * .1);

                    RightCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width - (CaptionOverlayGrid.Width * .1));
                    LeftCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width * .1);

                    CropGrid.Visibility = Visibility.Visible;

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
                    WatermarkGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Border:
                    BorderGrid.Visibility = Visibility.Visible;
                    break;
            }

            #endregion

            #region Title

            ActionTitleLabel.Content = title;
            ActionViewBox.Child = FindResource(vector) as Canvas;

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
        }

        private void ClosePanel()
        {
            ActionGrid.BeginStoryboard(FindResource("HidePanelStoryboard") as Storyboard);
            OverlayGrid.BeginStoryboard(FindResource("HideOverlayGridStoryboard") as Storyboard);
        }

        private void EnableDisable(bool enable = true)
        {
            //TODO: Check booleans.
            if (enable)
            {
                RibbonTabControl.IsEnabled = true;
                FrameListView.IsEnabled = true;
                ActionGrid.IsEnabled = true;
                FrameListView.Visibility = Visibility.Visible;

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
                FrameListView.SelectedIndex = 0;

                return;
            }

            RibbonTabControl.IsEnabled = false;
            FrameListView.IsEnabled = false;
            ActionGrid.IsEnabled = false;
            FrameListView.Visibility = Visibility.Collapsed;

            FrameListView.SelectionChanged -= FrameListView_SelectionChanged;
            FrameListView.SelectedItem = null;
            ZoomBoxControl.ImageSource = null;
        }

        private List<int> SelectedFramesIndex()
        {
            return FrameListView.SelectedItems.OfType<FrameListBoxItem>().Select(x => FrameListView.Items.IndexOf(x)).ToList();
        }

        #endregion

        #region Async Resize

        private delegate void ResizeFrames(int width, int height, int dpi);

        private ResizeFrames _resizeFramesDel = null;

        private void Resize(int width, int height, int dpi)
        {
            ShowProgress("Resizing Frames", ListFrames.Count);

            int count = 0;
            foreach (FrameInfo frame in ListFrames)
            {
                var png = new BmpBitmapEncoder();
                png.Frames.Add(ImageMethods.ResizeImage(frame.ImageLocation.SourceFrom(), width, height, 0, dpi));

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
                LoadNewFrames(ListFrames);
            });
        }

        #endregion

        #region Async Crop

        private delegate void CropFrames(Int32Rect rect);

        private CropFrames _cropFramesDel = null;

        private void Crop(Int32Rect rect)
        {
            ShowProgress("Cropping Frames", ListFrames.Count);

            int count = 0;
            foreach (FrameInfo frame in ListFrames)
            {
                var png = new BmpBitmapEncoder();
                png.Frames.Add(ImageMethods.CropImage(frame.ImageLocation.SourceFrom(), rect));

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
                LoadNewFrames(ListFrames);
            });
        }

        #endregion

        #region Async Merge Frames

        private delegate void OverlayFrames(ImageSource render, double dpi);

        private OverlayFrames _overlayFramesDel = null;

        private void Overlay(ImageSource render, double dpi)
        {
            ShowProgress("Applying Overlay to Frames", ListFrames.Count);

            var frameList = SelectedFrames();

            Dispatcher.Invoke(() => EnableDisable(false));

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
                var bmp = new RenderTargetBitmap((int)Math.Round(image.Width), (int)Math.Round(image.Height), dpi, dpi, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                // Creates a BmpBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(frame.ImageLocation))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void OverlayCallback(IAsyncResult ar)
        {
            _overlayFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadNewFrames(ListFrames);
            });
        }

        #endregion

        #region Async Flip/Rotate

        private delegate void FlipRotateFrames(string param);

        private FlipRotateFrames _flipRotateFramesDel = null;

        private void FlipRotate(string param)
        {
            ShowProgress("Applying Flip/Rotate to Frames", ListFrames.Count);

            var frameList = param.StartsWith("Rotate") ? ListFrames : SelectedFrames();

            Dispatcher.Invoke(() => EnableDisable(false));

            int count = 0;
            foreach (FrameInfo frame in frameList)
            {
                var image = frame.ImageLocation.SourceFrom();

                Transform transform = null;

                switch (param)
                {
                    case "FlipVertical":
                        transform = new ScaleTransform(1, -1, 0.5, 0.5);
                        break;
                    case "FlipHorizontal":
                        transform = new ScaleTransform(-1, 1, 0.5, 0.5);
                        break;
                    case "RotateLeft90":
                        transform = new RotateTransform(-90, 0.5, 0.5);
                        break;
                    case "RotateRight90":
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
                LoadNewFrames(ListFrames);
            });
        }

        #endregion

        #region Other

        private static string CreateTempPath()
        {
            #region Temp Path

            string pathTemp = Path.GetTempPath() + String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

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

        #endregion

        #endregion

        //TODO
        private void NumericUpDown_OnValueChanged(object sender, EventArgs e)
        {
            if (ListFrames == null) return;
            if (ListFrames.Count == 0) return;

            ListFrames[FrameListView.SelectedIndex].Delay = (int)FpsNumericUpDown.Value;

            //TODO: Update the FrameListView item delay
        }
    }
}
