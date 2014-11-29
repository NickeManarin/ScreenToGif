using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenToGif.Controls;
using Path = System.IO.Path;
using Point = System.Drawing.Point;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Recorder.xaml
    /// </summary>
    public partial class Recorder : LightWindow
    {
        //TODO:
        //NumericUpDown: http://stackoverflow.com/questions/841293/where-is-the-wpf-numeric-updown-control
        //OuterGlow: http://windowglows.codeplex.com/
        //Maximizing Window with style "None": http://blogs.msdn.com/b/llobo/archive/2006/08/01/maximizing-window-_2800_with-windowstyle_3d00_none_2900_-considering-taskbar.aspx
        //WPF Localization: http://msdn.microsoft.com/en-us/library/ms788718.aspx
        //Numeric only: http://stackoverflow.com/questions/1268552/how-do-i-get-a-textbox-to-only-accept-numeric-input-in-wpf/1268648#1268648

        //WARNING: Simply ignore this code... It's just a test for now.

        private Point point;
        private System.Drawing.Size _size;

        #region Record Async

        /// <summary>
        /// Saves the Bitmap to the disk and adds the filename in the list of frames.
        /// </summary>
        /// <param name="filename">The final filename of the Bitmap.</param>
        /// <param name="bitmap">The Bitmap to save in the disk.</param>
        public delegate void AddFrame(string filename, Bitmap bitmap);

        private AddFrame _addDel;

        private void AddFrames(string filename, Bitmap bitmap)
        {
            _listFrames.Add(filename);
            bitmap.Save(filename);
            bitmap.Dispose();
        }

        private void CallBack(IAsyncResult r)
        {
            //if (!this.IsLoaded) return;

            _addDel.EndInvoke(r);
        }

        #endregion

        public Recorder()
        {
            InitializeComponent();

            //Load
            _capture.Tick += _capture_Elapsed;
        }

        private void _capture_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            //var lefttop = new Point((int)this.Left + 5, (int)this.Top + 5);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(point.X, point.Y, 0, 0, _size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _addDel.BeginInvoke(String.Format("{0}{1}.bmp", _pathTemp, _frameCount), new Bitmap(_bt), CallBack, null);

            this.Dispatcher.Invoke(() => this.Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
            GC.Collect(1);
        }

        private void RecordPause_Click(object sender, RoutedEventArgs e)
        {
            Record_Pause();

            _addDel = AddFrames;
            point = new Point((int)this.Left + 5, (int)this.Top + 5);
            _size = new System.Drawing.Size((int)this.Width, (int)this.Height);
            _bt = new Bitmap(_size.Width, _size.Height);
            _gr = Graphics.FromImage(_bt);

            _capture.Interval = NumericUpDown.Value;
            _capture.Start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _capture.Stop();
        }

        #region Functions

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void Record_Pause()
        {
            CreateTemp();


        }

        /// <summary>
        /// Creates the temp folder that holds all frames.
        /// </summary>
        private void CreateTemp()
        {
            #region Temp Folder

            if (!Directory.Exists(_pathTemp))
            {
                Directory.CreateDirectory(_pathTemp);
                Directory.CreateDirectory(_pathTemp + "Undo");
                Directory.CreateDirectory(_pathTemp + "Edit");
            }

            #endregion
        }

        #endregion

        private bool IsTextDisallowed(string text)
        {
            var regex = new Regex("[^0-9]+");
            return regex.IsMatch(text);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (String.IsNullOrEmpty(e.Text))
            {
                e.Handled = false;
                return;
            }

            if (IsTextDisallowed(e.Text))
            {
                e.Handled = false;
                return;
            }

            if (String.IsNullOrEmpty(e.Text))
            {
                e.Handled = false;
                return;
            }
        }

        private void PastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                var text = (String)e.DataObject.GetData(typeof(String));

                if (IsTextDisallowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
