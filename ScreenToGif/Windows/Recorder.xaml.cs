using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Drawing.Point;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Recorder.xaml
    /// </summary>
    public partial class Recorder : LightWindow
    {
        #region Inicialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Recorder(bool hideBackButton = false)
        {
            InitializeComponent();

            _hideBackButton = hideBackButton;

            //Load
            _capture.Tick += Normal_Elapsed;

            //Config Timers - Todo: organize
            _preStartTimer.Tick += PreStart_Elapsed;
            _preStartTimer.Interval = 1000;

            //TrayIcon event.
            _trayIcon.NotifyIconClicked += NotifyIconClicked;

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook();
                _actHook.KeyDown += KeyHookTarget;
                _actHook.Start(true, true); //true for the mouse, true for the keyboard.
            }
            catch (Exception) { }

            #endregion
        }

        private void Recorder_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_hideBackButton)
            {
                HideBackButton();
            }
        }

        #endregion

        #region Hooks

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            //TODO: I need a better way of comparing the keys.
            if (e.Key.ToString().Equals(Settings.Default.StartPauseKey.ToString()))
            {
                RecordPauseButton_Click(null, null);
            }
            else if (e.Key.ToString().Equals(Settings.Default.StopKey.ToString()))
            {
                StopButton_Click(null, null);
            }
        }

        /// <summary>
        /// MouseHook event method, detects the mouse clicks.
        /// </summary>
        private void MouseHookTarget(object sender, CustomMouseEventArgs keyEventArgs)
        {
            _recordClicked = keyEventArgs.Button == MouseButton.Left;
        }

        #endregion

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
            bitmap.Save(filename);
            bitmap.Dispose();
        }

        private void CallBack(IAsyncResult r)
        {
            //if (!this.IsLoaded) return;

            _addDel.EndInvoke(r);
        }

        #endregion

        #region Timers

        private void PreStart_Elapsed(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                Title = String.Format("Screen To Gif ({0} {1}", _preStartCount, Properties.Resources.TitleSecondsToGo);
                _preStartCount--;
            }
            else
            {
               _preStartTimer.Stop();
                RecordPauseButton.IsEnabled = true;

                if (Settings.Default.ShowCursor)
                {
                    #region If Show Cursor

                    if (!Settings.Default.Snapshot)
                    {
                        _actHook.OnMouseActivity += MouseHookTarget;

                        if (!Settings.Default.FullScreen)
                        {
                            IsRecording(true);
                            _capture.Tick += Cursor_Elapsed;
                            Cursor_Elapsed(null, null);
                            _capture.Start();
                        }
                        else
                        {
                            _capture.Tick += FullCursor_Elapsed;
                            FullCursor_Elapsed(null, null);
                            _capture.Start();
                        }

                        Stage = Stage.Recording;
                        RecordPauseButton.Text = Properties.Resources.Pause;
                        RecordPauseButton.Content = (Canvas)FindResource("Pause");
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                        AutoFitButtons();
                    }
                    else
                    {
                        Stage = Stage.Snapping;
                        RecordPauseButton.Content = (Canvas)FindResource("CameraIcon");
                        RecordPauseButton.Text = Properties.Resources.btnSnap;
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                        Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                        AutoFitButtons();
                    }

                    #endregion
                }
                else
                {
                    #region If Not

                    if (!Settings.Default.Snapshot)
                    {
                        if (!Settings.Default.FullScreen)
                        {
                            IsRecording(true);
                            _capture.Tick += Normal_Elapsed;
                            Normal_Elapsed(null, null);
                            _capture.Start();
                        }
                        else
                        {
                            _capture.Tick += Full_Elapsed;
                            Full_Elapsed(null, null);
                            _capture.Start();
                        }

                        Stage = Stage.Recording;
                        RecordPauseButton.Text = Properties.Resources.Pause;
                        RecordPauseButton.Content = (Canvas)FindResource("Pause");
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                        AutoFitButtons();
                    }
                    else
                    {
                        Stage = Stage.Snapping;
                        RecordPauseButton.Content = (Canvas)FindResource("CameraIcon");
                        RecordPauseButton.Text = Properties.Resources.btnSnap;
                        RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                        Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                        AutoFitButtons();
                    }

                    #endregion
                }
            }
        }

        //TODO: Make all capture modes.
        private void Normal_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            int left = 0, top = 0;
            Dispatcher.Invoke(() =>
            {
                left = (int)Left;
                top = (int)Top;
            });

            var lefttop = new Point(left + 9, top + 34); //was 7 32
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, _size, CopyPixelOperation.SourceCopy);

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);
            ListFrames.Add(new FrameInfo(fileName, _capture.Interval));

            _addDel.BeginInvoke(fileName, new Bitmap(_bt), CallBack, null);

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
            GC.Collect(1);
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            int left = 0, top = 0;
            Dispatcher.Invoke(() =>
            {
                left = (int)Left;
                top = (int)Top;
            });

            var lefttop = new Point(left + 9, top + 34);

            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, _size, CopyPixelOperation.SourceCopy);

            string fileName = String.Format("{0}{1}.bmp", _pathTemp, _frameCount);
            ListFrames.Add(new FrameInfo(fileName, _capture.Interval, new CursorInfo(CaptureCursor.CaptureImageCursor(ref _posCursor), OutterGrid.PointFromScreen(_posCursor), _recordClicked)));

            _addDel.BeginInvoke(fileName, new Bitmap(_bt), CallBack, null);

            Dispatcher.Invoke(() => Title = String.Format("Screen To Gif • {0}", _frameCount));

            _frameCount++;
            GC.Collect(1);
        }

        private void Full_Elapsed(object sender, EventArgs e)
        {

        }

        private void FullCursor_Elapsed(object sender, EventArgs e)
        {
            
        }

        #endregion

        #region Click Events

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            Record_Pause();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = false;

            //TODO: If recording started, maybe disable some properties.
            var options = new Options();
            options.ShowDialog();

            Topmost = true;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void Record_Pause()
        {
            CreateTemp();

            if (Stage == Stage.Stopped)
            {
                #region To Record

                _capture = new Timer();
                _capture.Interval = 1000 / NumericUpDown.Value;

                ListFrames = new List<FrameInfo>();

                #region If Fullscreen

                if (Settings.Default.FullScreen)
                {
                    //TODO: Fullscreen recording.
                    //_sizeResolution = new Size(_sizeScreen);
                    _bt = new Bitmap((int)_sizeScreen.X, (int)_sizeScreen.Y);

                    HideWindowAndShowTrayIcon();
                }
                else
                {
                    _bt = new Bitmap((int)Width - 18, (int)Height - 69);
                }

                #endregion

                _gr = Graphics.FromImage(_bt);

                HeightTextBox.IsEnabled = false;
                WidthTextBox.IsEnabled = false;
                NumericUpDown.IsEnabled = false;

                IsRecording(true);
                Topmost = true;

                _addDel = AddFrames;
                _size = new System.Drawing.Size((int)Width - 18, (int)Height - 69);

                #region Start

                if (Settings.Default.PreStart)
                {
                    Title = "Screen To Gif (2 " + Properties.Resources.TitleSecondsToGo;
                    RecordPauseButton.IsEnabled = false;

                    Stage = Stage.PreStarting;
                    _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                    _preStartTimer.Start();
                }
                else
                {
                    if (Settings.Default.ShowCursor)
                    {
                        #region If Show Cursor

                        if (!Settings.Default.Snapshot)
                        {
                            #region Normal Recording

                            if (!Settings.Default.FullScreen)
                            {
                                //To start recording right away, I call the tick before starting the timer,
                                //because the first tick will only occur after the delay.
                                _capture.Tick += Cursor_Elapsed;
                                Cursor_Elapsed(null, null);
                                _capture.Start();
                            }
                            else
                            {
                                _capture.Tick += FullCursor_Elapsed;
                                FullCursor_Elapsed(null, null);
                                _capture.Start();
                            }

                            Stage = Stage.Recording;
                            RecordPauseButton.Text = Properties.Resources.Pause;
                            RecordPauseButton.Content = (Canvas)FindResource("Pause");
                            RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                            AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region SnapShot Recording

                            //Set to Snapshot Mode, change the text of the record button to "Snap" and 
                            //every press of the button, takes a screenshot
                            Stage = Stage.Snapping;
                            RecordPauseButton.Content = (Canvas)FindResource("CameraIcon");
                            RecordPauseButton.Text = Properties.Resources.btnSnap;
                            RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                            Title = "Screen To Gif - " +  Properties.Resources.Con_SnapshotMode; 

                            AutoFitButtons();

                            #endregion
                        }

                        #endregion
                    }
                    else
                    {
                        #region If Not

                        if (!Settings.Default.Snapshot)
                        {
                            #region Normal Recording

                            _actHook.OnMouseActivity += MouseHookTarget;

                            if (!Settings.Default.FullScreen)
                            {
                                _capture.Tick += Normal_Elapsed;
                                Normal_Elapsed(null, null);
                                _capture.Start();
                            }
                            else
                            {
                                _capture.Tick += Full_Elapsed;
                                Full_Elapsed(null, null);
                                _capture.Start();
                            }

                            Stage = Stage.Recording;
                            RecordPauseButton.Text = Properties.Resources.Pause;
                            RecordPauseButton.Content = (Canvas)FindResource("Pause");
                            RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                            AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region SnapShot Recording

                            Stage = Stage.Snapping;
                            RecordPauseButton.Content = (Canvas)FindResource("CameraIcon");
                            RecordPauseButton.Text = Properties.Resources.btnSnap;
                            RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                            Title = "Screen To Gif - " + Properties.Resources.Con_SnapshotMode;

                            AutoFitButtons();

                            #endregion
                        }

                        #endregion
                    }
                }

                #endregion

                #endregion
            }
            else if (Stage == Stage.Recording)
            {
                #region To Pause

                Stage = Stage.Paused;
                RecordPauseButton.Text = Properties.Resources.btnRecordPause_Continue;
                RecordPauseButton.Content = (Canvas)FindResource("RecordDark");
                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                Title = Properties.Resources.TitlePaused;

                AutoFitButtons();

                _capture.Stop();
                //ModifyCaptureTimerAndChangeTrayIconVisibility(false);

                #endregion
            }
            else if (Stage == Stage.Paused)
            {
                #region To Record Again

                Stage = Stage.Recording;
                RecordPauseButton.Text = Properties.Resources.Pause;
                RecordPauseButton.Content = (Canvas)FindResource("Pause");
                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                Title = Properties.Resources.TitleRecording;

                AutoFitButtons();

                _capture.Start();
                //ModifyCaptureTimerAndChangeTrayIconVisibility(true);

                #endregion
            }
            else if (Stage == Stage.Snapping)
            {
                #region Take Screenshot (All possibles types)

                if (Settings.Default.ShowCursor)
                {
                    if (Settings.Default.FullScreen)
                    {
                        FullCursor_Elapsed(null, null);
                    }
                    else
                    {
                        Cursor_Elapsed(null, null);
                    }
                }
                else
                {
                    if (Settings.Default.FullScreen)
                    {
                        Full_Elapsed(null, null);
                    }
                    else
                    {
                        Normal_Elapsed(null, null);
                    }
                }

                #endregion
            }
        }

        /// <summary>
        /// Stops the recording or the Pre-Start countdown.
        /// </summary>
        private void Stop()
        {
            try
            {
                _frameCount = 0; 

                _capture.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && ListFrames.Any())
                {
                    #region If not Already Stoped nor Pre Starting and FrameCount > 0, Stops

                    //TODO: Stop the keyboard and mouse watcher.
                    //TODO: Do async the merge of the cursor with the image and the resize of full screen recordings.
                    //Or maybe just open the editor and do that there.
                    //Close this window and return the list of frames.

                    ExitArg = ExitAction.Recorded;
                    DialogResult = false;

                    #endregion
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !ListFrames.Any())
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    Stage = Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    NumericUpDown.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    HeightTextBox.IsEnabled = true;
                    WidthTextBox.IsEnabled = true;

                    IsRecording(false);
                    Topmost = true;

                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Record;
                    RecordPauseButton.Content = (Canvas)FindResource("RecordDark");
                    RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                    Title = Properties.Resources.TitleStoped;

                    AutoFitButtons();

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                var errorViewer = new ExceptionViewer(nll);
                errorViewer.ShowDialog();
                LogWriter.Log(nll, "NullPointer in the Stop function");
            }
            catch (Exception ex)
            {
                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
                LogWriter.Log(ex, "Error in the Stop function");
            }
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

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (LowerGrid.ActualWidth < 250)
            {
                if (RecordPauseButton.HorizontalContentAlignment != HorizontalAlignment.Left) return;

                RecordPauseButton.Text = String.Empty;
                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Center;
                StopButton.Text = String.Empty;
                StopButton.HorizontalContentAlignment = HorizontalAlignment.Center;

                HideMinimizeAndMaximize(true);
            }
            else
            {
                if (RecordPauseButton.HorizontalContentAlignment != HorizontalAlignment.Center) return;

                if (Stage == Stage.Recording)
                    RecordPauseButton.Text = Properties.Resources.Pause;
                else if (Stage == Stage.Paused)
                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Continue;
                else if (Settings.Default.Snapshot)
                    RecordPauseButton.Text = Properties.Resources.btnSnap;
                else
                    RecordPauseButton.Text = Properties.Resources.btnRecordPause_Record;

                RecordPauseButton.HorizontalContentAlignment = HorizontalAlignment.Left;
                StopButton.Text = Properties.Resources.Label_Stop;
                StopButton.HorizontalContentAlignment = HorizontalAlignment.Left;

                HideMinimizeAndMaximize(false);
            }
        }

        #endregion
        
        #region NotifyIcon Methods

        private void ChangeTrayIconVisibility(bool isTrayIconEnabled)
        {
            if (Settings.Default.FullScreen)
            {
                if (isTrayIconEnabled)
                {
                    HideWindowAndShowTrayIcon();
                }
                else
                {
                    ShowWindowAndHideTrayIcon();
                }
            }
        }

        private void HideWindowAndShowTrayIcon()
        {
            _trayIcon.ShowTrayIcon();
            Visibility = Visibility.Hidden;
        }

        private void ShowWindowAndHideTrayIcon()
        {
            _trayIcon.HideTrayIcon();
            Visibility = Visibility.Visible;
        }

        private void NotifyIconClicked(object sender, EventArgs eventArgs)
        {
            Visibility = Visibility.Visible;
            Record_Pause();
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
            HeightTextBox.Value = (int)Height;
            WidthTextBox.Value = (int)Width;

            AutoFitButtons();
        }

        private void SizeBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as NumericTextBox;

            if (textBox == null) return;

            textBox.Value = e.Delta > 0 ? textBox.Value + 1 : textBox.Value - 1;

            AdjustToSize();
        }

        private void AdjustToSize()
        {
            HeightTextBox.Value = Convert.ToInt32(HeightTextBox.Text) + 69; //was 65
            WidthTextBox.Value = Convert.ToInt32(WidthTextBox.Text) + 18; //was 16

            Width = WidthTextBox.Value;
            Height = HeightTextBox.Value;
        }

        #endregion

        private void LightWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region Save Settings

            Settings.Default.LastFps = Convert.ToInt32(NumericUpDown.Value);
            Settings.Default.Width = (int)Width;
            Settings.Default.Height = (int)Height;

            Settings.Default.Save();

            #endregion

            try
            {
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            if (Stage != (int)Stage.Stopped)
            {
                _capture.Stop();
                _capture.Dispose();
            }

            _trayIcon.Dispose();

            //TODO: Clean resources?
        }
    }
}
