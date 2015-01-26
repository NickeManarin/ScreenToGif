using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenToGif.Util.Writers;
using ScreenToGif.Webcam.DirectX;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Webcam.xaml
    /// </summary>
    public partial class Webcam : Window
    {
        #region Variables

        private Capture _capture = null;
        private Filters _filters;

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Webcam()
        {
            InitializeComponent();
        }

        #region Functions

        /// <summary>
        /// Loads the list of video devices.
        /// </summary>
        private void LoadVideoDevices()
        {
            _filters = new Filters();

            if (_filters.VideoInputDevices.Count == 0)
            {
                VideoDevicesComboBox.IsEnabled = false;
                RecordButton.IsEnabled = false;
                StopButton.IsEnabled = false;

                VideoCanvas.Visibility = Visibility.Hidden;
                NoVideoLabel.Visibility = Visibility.Visible;

                return;
            }

            for (int i = 0; i < _filters.VideoInputDevices.Count; i++)
            {
                VideoDevicesComboBox.Items.Add(_filters.VideoInputDevices[i].Name);
            }

            //Selects the first video device.
            VideoDevicesComboBox.SelectedIndex = 0;

            VideoDevicesComboBox.IsEnabled = true;
            RecordButton.IsEnabled = true;
            StopButton.IsEnabled = true;
        }

        #endregion

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVideoDevices();
        }

        private void VideoDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Get current devices and dispose of capture object
                // because the video device can only be changed
                // by creating a new Capture object.
                Filter videoDevice = null;

                // To change the video device, a dispose is needed.
                if (_capture != null)
                {
                    _capture.Dispose();
                    _capture = null;
                }

                // Get new video device
                videoDevice = (VideoDevicesComboBox.SelectedIndex > -1 ? _filters.VideoInputDevices[VideoDevicesComboBox.SelectedIndex] : null);

                // Create capture object
                if (videoDevice != null)
                {
                    _capture = new Capture(videoDevice) { PreviewWindow = this };

                    _capture.StartPreview();

                    //this.Height = _capture.Height;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Video device not supported");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_capture != null)
            {
                _capture.StopPreview();
                _capture.Dispose();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RecordButton.IsEnabled = false;

            //Clear the combo box.
            VideoDevicesComboBox.Items.Clear();

            //Check again for video devices.
            LoadVideoDevices();
        }

        #endregion

        #region Old Code

        //IntPtr deviceHandle;

        //public const uint WM_CAP = 0x400;
        //public const uint WM_CAP_DRIVER_CONNECT = 0x40a;
        //public const uint WM_CAP_DRIVER_DISCONNECT = 0x40b;
        //public const uint WM_CAP_EDIT_COPY = 0x41e;
        //public const uint WM_CAP_SET_PREVIEW = 0x432;
        //public const uint WM_CAP_SET_OVERLAY = 0x433;
        //public const uint WM_CAP_SET_PREVIEWRATE = 0x434;
        //public const uint WM_CAP_SET_SCALE = 0x435;
        //public const uint WS_CHILD = 0x40000000;
        //public const uint WS_VISIBLE = 0x10000000;

        //[DllImport("avicap32.dll")]
        //public extern static IntPtr capGetDriverDescription(ushort index, StringBuilder name, int nameCapacity, StringBuilder description,
        //            int descriptionCapacity);

        //[DllImport("avicap32.dll")]
        //public extern static IntPtr capCreateCaptureWindow(string title, uint style, int x, int y, int width, int height, IntPtr window,
        //            int id);

        //[DllImport("user32.dll")]
        //public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        //[DllImport("user32.dll")]
        //public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        //public void Attach()
        //{
        //    deviceHandle = capCreateCaptureWindow("WebCap", WS_VISIBLE | WS_CHILD, 0, 0, (int)this.ActualWidth - 150, (int)this.ActualHeight, new WindowInteropHelper(this).Handle, 0);

        //    if (SendMessage(deviceHandle, WM_CAP_DRIVER_CONNECT, (IntPtr)0, (IntPtr)0).ToInt32() > 0)
        //    {
        //        SendMessage(deviceHandle, WM_CAP_SET_SCALE, (IntPtr)(-1), (IntPtr)0);
        //        SendMessage(deviceHandle, WM_CAP_SET_PREVIEWRATE, (IntPtr)0x42, (IntPtr)0);
        //        SendMessage(deviceHandle, WM_CAP_SET_PREVIEW, (IntPtr)(-1), (IntPtr)0);
        //        SetWindowPos(deviceHandle, new IntPtr(0), 0, 0, (int)this.ActualWidth, (int)this.ActualHeight, 6);
        //    }
        //}

        #endregion
    }
}
