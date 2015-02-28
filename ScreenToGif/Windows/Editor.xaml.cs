using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Util;

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

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Editor()
        {
            InitializeComponent();

            //TODO: Recording logic.
            //Load all frames into the snapshot ListView.
            //Present a frame in the ImageBox
            //Enable controls that were disabled because no list was open.
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ListFrames != null)
            {
                foreach (FrameInfo frame in ListFrames)
                {
                    var item = new FrameListBoxItem
                    {
                        FrameNumber = ListFrames.IndexOf(frame),
                        Image = frame.ImageLocation,
                        Delay = frame.Delay
                    };

                    item.PreviewMouseLeftButtonDown += Item_PreviewMouseLeftButtonDown;

                    FrameListView.Items.Add(item);
                }
            }
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as FrameListBoxItem;

            if (item != null)
                ImageViewer.Source = new BitmapImage(new Uri(ListFrames[item.FrameNumber].ImageLocation));
        }

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

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            //TODO: Hide the Editor and show the Recorder.
            //By closing the recorder, return here. Obviously.
            //Not sure if clear variables before or after the return of the Recorder.
            //It may be a little heavier to have a range of variables of the editor up in the memory.

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
            var webcam = new Webcam();

            this.Hide();

            var result = webcam.ShowDialog();

            this.Show();

            if (result.HasValue && result == true)
            {
                //TODO: Clear all variables if Ok.
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

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var path = new MiniPath();
            path.ShowDialog();
        }


    }
}
