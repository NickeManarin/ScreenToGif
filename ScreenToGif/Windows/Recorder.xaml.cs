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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenToGif.Controls;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.Enum;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Path = System.IO.Path;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using TextBox = System.Windows.Controls.TextBox;

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

        /// <summary>
        /// The maximum size of the recording. Also the maximum size of the window.
        /// </summary>
        private Point _sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);

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

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Recorder()
        {
            InitializeComponent();

            //Load
            _capture.Tick += _capture_Elapsed;
        }

        private void _capture_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            int left = 0;
            this.Dispatcher.Invoke(() => { left = (int)Left; });
            int top = 0;
            this.Dispatcher.Invoke(() => { top = (int)Top; });

            var lefttop = new Point(left + 7, top + 32);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, _size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _addDel.BeginInvoke(String.Format("{0}{1}.bmp", _pathTemp, _frameCount), new Bitmap(_bt), CallBack, null);

            this.Dispatcher.Invoke(() => this.Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
            GC.Collect(1);
        }

        private void RecordPause_Click(object sender, RoutedEventArgs e)
        {
            Record_Pause();
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

            if (_stage == Stage.Stopped)
            {
                #region To Record

                _capture.Interval = 1000 / NumericUpDown.Value;

                _listFrames = new List<string>();
                _listCursor = new List<CursorInfo>();

                #region If Fullscreen

                //TODO: Fullscreen recording.
                if (Settings.Default.FullScreen)
                {
                    //_sizeResolution = new Size(_sizeScreen);
                    _bt = new Bitmap(_sizeScreen.X, _sizeScreen.Y);

                    //HideWindowAndShowTrayIcon();
                }
                else
                {
                    _bt = new Bitmap((int)this.Width - 14, (int)this.Height - 65);
                }

                _gr = Graphics.FromImage(_bt);

                #endregion

                HeightTextBox.IsEnabled = false;
                WidthTextBox.IsEnabled = false;
                NumericUpDown.IsEnabled = false;
                RecordPauseButton.IsEnabled = false;
                this.IsRecording(true);
                this.Topmost = true;

                _addDel = AddFrames;

                #region Start

                if (Settings.Default.PreStart)
                {
                    this.Title = "Screen To Gif (2 seconds to go)"; //TODO: + Resources.TitleSecondsToGo;

                    _stage = Stage.PreStarting;
                    _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                    //timerPreStart.Start();
                }
                else
                {
                    if (Settings.Default.ShowCursor)
                    {
                        #region if show cursor

                        if (!Settings.Default.Snapshot)
                        {
                            #region Normal Recording

                            //_actHook.OnMouseActivity += MouseHookTarget;

                            if (!Settings.Default.FullScreen)
                            {
                                //To start recording right away, I call the tick before starting the timer,
                                //because the first tick will only occur after the delay.

                                //timerCapWithCursor_Tick(null, null);
                                //timerCapWithCursor.Start();
                            }
                            else
                            {
                                //timerCapWithCursorFull_Tick(null, null);
                                //timerCapWithCursorFull.Start();
                            }

                            _stage = Stage.Recording;
                            //RecordPauseButton.Content = Resources.Pause;
                            RecordPauseButton.Tag = "/ScreenToGif;component/Resources/Pause16x.png";
                            //RecordPauseButton.ImageAlign = ContentAlignment.MiddleLeft;

                            //AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region SnapShot Recording

                            //Set to snapshot mode, change the text of the record button to "Snap" and 
                            //every press of the button, takes a screenshot
                            _stage = Stage.Snapping;
                            RecordPauseButton.Tag = "/ScreenToGif;component/Resources/Snap16x.png";
                            //RecordPauseButton.Content = Resources.btnSnap;
                            //RecordPauseButton.ImageAlign = ContentAlignment.MiddleLeft;
                            this.Title = "Screen To Gif - "; //+ Resources.Con_SnapshotMode;

                            //AutoFitButtons();

                            #endregion
                        }

                        #endregion
                    }
                    else
                    {
                        #region If not

                        if (!Settings.Default.Snapshot)
                        {
                            #region Normal Recording

                            if (!Settings.Default.FullScreen)
                            {
                                //this.MinimizeBox = false;
                                //timerCapture_Tick(null, null);
                                //timerCapture.Start();
                            }
                            else
                            {
                                //timerCaptureFull_Tick(null, null);
                                //timerCaptureFull.Start();
                            }

                            _stage = Stage.Recording;
                            //RecordPauseButton.Content = Resources.Pause;
                            RecordPauseButton.Tag = "/ScreenToGif;component/Resources/Pause16x.png";
                            //RecordPauseButton.ImageAlign = ContentAlignment.MiddleLeft;

                            //AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region SnapShot Recording

                            _stage = Stage.Snapping;
                            //RecordPauseButton.Tag = Resources.Snap16x;
                            //RecordPauseButton.Content = Resources.btnSnap;
                            //RecordPauseButton.ImageAlign = ContentAlignment.MiddleLeft;
                            //this.Title = "Screen To Gif - " + Resources.Con_SnapshotMode;

                            //AutoFitButtons();

                            #endregion
                        }

                        #endregion
                    }
                }

                #endregion

                #endregion
            }
            else if (_stage == Stage.Recording)
            {
                #region To Pause

                //this.Title = Resources.TitlePaused;
                //RecordPauseButton.Content = Resources.btnRecordPause_Continue;
                RecordPauseButton.Tag = Tag = "/ScreenToGif;component/Resources/Record16x.png";
                //btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                _stage = Stage.Paused;

                //AutoFitButtons();

                //ModifyCaptureTimerAndChangeTrayIconVisibility(false);

                #endregion
            }
            else if (_stage == Stage.Paused)
            {
                #region To Record Again

                //this.Title = Resources.TitleRecording;
                //RecordPauseButton.Content = Resources.Pause;
                RecordPauseButton.Tag = "/ScreenToGif;component/Resources/Pause16x.png";
                //RecordPauseButton.ImageAlign = ContentAlignment.MiddleLeft;
                _stage = Stage.Recording;

                //AutoFitButtons();

                //ModifyCaptureTimerAndChangeTrayIconVisibility(true);

                #endregion
            }
            else if (_stage == Stage.Snapping)
            {

            }


            _size = new System.Drawing.Size((int)this.Width - 14, (int)this.Height - 65);

            _capture.Interval = 1000 / NumericUpDown.Value;
            _capture.Start();
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

        #region Sizing

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AdjustToSize();
            }
        }

        private void HeightTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AdjustToSize();
        }

        private void LightWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HeightTextBox.Text = this.Height.ToString();
            WidthTextBox.Text = this.Width.ToString();

            HeightTextBox.Value = (int)this.Height;
            WidthTextBox.Value = (int)this.Width;
        }

        private void SizeBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as NumericTextBox;

            if (textBox == null) return;

            textBox.Value = Convert.ToInt32(textBox.Text);
            textBox.Value = e.Delta > 0 ? textBox.Value + 1 : textBox.Value - 1;

            AdjustToSize();
        }

        private void AdjustToSize()
        {
            HeightTextBox.Value = Convert.ToInt32(HeightTextBox.Text);
            WidthTextBox.Value = Convert.ToInt32(WidthTextBox.Text);

            this.Width = WidthTextBox.Value;
            this.Height = HeightTextBox.Value;
        }

        #endregion
    }
}
