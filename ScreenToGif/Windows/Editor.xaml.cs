using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.Writers;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : Window
    {
        #region Variables

        /// <summary>
        /// The List of Frames.
        /// </summary>
        public List<FrameInfo> ListFrames { get; set; }

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
                this.Cursor = Cursors.AppStarting;
                FrameListView.IsEnabled = false;
                RibbonTabControl.IsEnabled = false;

                _loadFramesDel = LoadFramesAsync;
                _loadFramesDel.BeginInvoke(LoadFramesCallback, null);
            }

            KeyUp += Editor_KeyUp;
        }

        void Editor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
                e.Handled = true;
        }

        #region Async Loading

        private delegate bool LoadFrames();

        private LoadFrames _loadFramesDel = null;

        private bool LoadFramesAsync()
        {
            try
            {
                foreach (FrameInfo frame in ListFrames)
                {
                    FrameListView.Dispatcher.Invoke(delegate
                    {
                        var item = new FrameListBoxItem
                        {
                            FrameNumber = ListFrames.IndexOf(frame),
                            Image = frame.ImageLocation,
                            Delay = frame.Delay
                        };

                        item.PreviewMouseLeftButtonDown += Item_PreviewMouseLeftButtonDown;

                        FrameListView.Items.Add(item);
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading Error");
                return false;
            }
        }

        private void LoadFramesCallback(IAsyncResult ar)
        {
            bool result = _loadFramesDel.EndInvoke(ar);

            this.Dispatcher.Invoke(delegate
            {
                FrameListView.SelectionChanged += FrameListView_SelectionChanged;

                FrameListView.SelectedIndex = 0;

                //Re-enable the disable controls.
                this.Cursor = Cursors.Arrow;
                FrameListView.IsEnabled = true;
                RibbonTabControl.IsEnabled = true;
            });
        }

        #endregion

        #endregion

        #region Frame Selection

        private void FrameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[e.AddedItems.Count - 1] as FrameListBoxItem;

                if (item != null)
                    ZoomBoxControl.ImageSource = new BitmapImage(new Uri(ListFrames[item.FrameNumber].ImageLocation));

                GC.Collect(1);
                return;
            }

            if (e.RemovedItems.Count > 0)
            {
                var item = e.RemovedItems[e.RemovedItems.Count - 1] as FrameListBoxItem;

                if (item != null)
                    ZoomBoxControl.ImageSource = new BitmapImage(new Uri(ListFrames[(item.FrameNumber - 1) < 0 ? 0 : item.FrameNumber - 1].ImageLocation));

                GC.Collect(1);
            }
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as FrameListBoxItem;

            if (item != null)
                ZoomBoxControl.ImageSource = new BitmapImage(new Uri(ListFrames[item.FrameNumber].ImageLocation));

            GC.Collect(1);
        }

        #endregion

        #region File Tab

        private void NewAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            var newAnim = new Create();
            var result = newAnim.ShowDialog();

            if (result.HasValue && result == true)
            {
                //TODO: Clear all variables if Ok.
            }
        }

        private void NewRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            //Not sure if clear variables before or after the return of the Recorder.
            //It may be a little heavier to have a range of variables of the editor up in the memory.

            Encoder.Restore();
            this.Show();
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.AddExtension = true;
            ofd.CheckFileExists = true;
            ofd.Filter = "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif";
            ofd.Title = "Open one image to insert";

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //ofd.FileName;
                //TODO: Clear Variables, open the selected image.
                //TODO: From a video source: http://www.betterthaneveryone.com/archive/2009/10/02/882.aspx
            }
        }

        private void NewWebcamRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            this.Show();

            if (result.HasValue && result == true)
            {
                //TODO: Clear all variables if Ok.
            }
        }

        //Separator

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new SaveFileDialog();
            ofd.AddExtension = true;
            ofd.Filter = "Gif Animation (*.gif)|*.gif";
            ofd.Title = "Save animation as..."; //TODO: Better description.

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //BUG: ListFrame is being updated...

                Encoder.AddItem(ListFrames.CopyList(), ofd.FileName);
            }

            //Encoder.AddItem(ListFrames, @"C:\Users\Nicke Manarin\Downloads\bestpractices.png");
        }

        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.AddExtension = true;
            saveDialog.DefaultExt = ".stg";
            saveDialog.FileName = String.Format("Project - {0} Frames.stg", ListFrames.Count);
            saveDialog.Filter = "*.stg|(ScreenToGif Project)";
            saveDialog.Title = "Select the File Location";

            var result = saveDialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            try
            {
                string serial = Serializer.SerializeToString(ListFrames);

                if(serial == null)
                    throw new Exception("Object serialization failed.");

                //Deserialize Example:
                //Serializer.DeserializeFromString<List<FrameInfo>>(serial)
                //Unzip
                //http://www.codeguru.com/csharp/.net/zip-and-unzip-files-programmatically-in-c.htm

                string tempDirectory = Path.GetDirectoryName(ListFrames.First().ImageLocation);

                var dir = Directory.CreateDirectory(Path.Combine(tempDirectory, "Export"));

                File.WriteAllText(Path.Combine(dir.FullName, "List.sb"), serial);

                foreach (FrameInfo frameInfo in ListFrames)
                {
                    File.Copy(frameInfo.ImageLocation, Path.Combine(dir.FullName, Path.GetFileName(frameInfo.ImageLocation)));
                }

                ZipFile.CreateFromDirectory(dir.FullName, saveDialog.FileName);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Exporting Recording as a Project");
            }
        }

        #endregion

        #region Options Tab

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new Options();
            options.ShowDialog();
        }

        #endregion

        #region Other Events

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift ||
                Keyboard.Modifiers == ModifierKeys.Alt)
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

        #endregion

        //Test
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var path = new MiniPath();
            path.ShowDialog();
        }

        private void ExceptionTestButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException("Not yet implemented", new TimeZoneNotFoundException("Not found, hahaha", new ArithmeticException()));
        }
    }
}
