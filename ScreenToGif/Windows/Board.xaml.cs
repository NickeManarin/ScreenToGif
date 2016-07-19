using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.ImageUtil;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Writers;
using ScreenToGif.Windows.Other;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Board recorder, a "record as you draw" feature.
    /// </summary>
    public partial class Board
    {
        //TODO: The main ideia is to create a "record as you draw" feature, 
        //with the possibility to record keyframes by keyframes (Snapshot)
        //and show the previous drawn keyframe as a "ghost" to help the drawing
        //There will be some exceptions to the automatic recording, such as holding the Ctrl key, etc

        #region Variables

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        #region Flags

        /// <summary>
        /// True if the BackButton should be hidden.
        /// </summary>
        private readonly bool _hideBackButton;

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage { get; set; }

        /// <summary>
        /// The action to be executed after closing this Window.
        /// </summary>
        public ExitAction ExitArg = ExitAction.Return;

        #endregion

        #region Counters

        /// <summary>
        /// The delay of each frame took as snapshot.
        /// </summary>
        private int? _snapDelay = null;

        #endregion

        /// <summary>
        /// Lists of cursors.
        /// </summary>
        public List<FrameInfo> ListFrames = new List<FrameInfo>();

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private readonly string _pathTemp;

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _dpi = 96d;

        #region Timer

        private Timer _capture = new Timer();

        #endregion

        #endregion

        #region Inicialization

        public Board(bool hideBackButton = false)
        {
            InitializeComponent();

            _hideBackButton = hideBackButton;

            //Load
            _capture.Tick += Normal_Elapsed;

            #region Temporary folder

            if (string.IsNullOrWhiteSpace(Settings.Default.TemporaryFolder))
            {
                Settings.Default.TemporaryFolder = Path.GetTempPath();
            }

            _pathTemp = Path.Combine(Settings.Default.TemporaryFolder, "ScreenToGif", "Recording", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

            #endregion
        }

        private void Board_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_hideBackButton)
            {
                HideBackButton();
            }

            _dpi = this.Dpi();
        }

        #endregion

        #region Record Async

        /// <summary>
        /// Saves the Bitmap to the disk.
        /// </summary>
        /// <param name="fileName">The final filename of the Bitmap.</param>
        /// <param name="bitmap">The Bitmap to save in the disk.</param>
        private void AddFrames(string fileName, BitmapSource bitmap)
        {
            //var mutexLock = new Mutex(false, bitmap.GetHashCode().ToString());
            //mutexLock.WaitOne();

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            //GC.Collect(1);
            //mutexLock.ReleaseMutex();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void RecordPause()
        {
            Extras.CreateTemp(_pathTemp);

            switch (Stage)
            {
                case Stage.Stopped:

                    #region To Record

                    _capture = new Timer {Interval = 1000/(int) FpsNumericUpDown.Value};
                    _snapDelay = null;

                    ListFrames = new List<FrameInfo>();

                    HeightTextBox.IsEnabled = false;
                    WidthTextBox.IsEnabled = false;
                    FpsNumericUpDown.IsEnabled = false;

                    IsRecording(true);
                    Topmost = true;

                    FrameRate.Start(_capture.Interval);

                    #region Start

                    if (!Settings.Default.Snapshot)
                    {
                        #region Normal Recording

                        _capture.Tick += Normal_Elapsed;
                        //Normal_Elapsed(null, null);
                        _capture.Start();

                        Stage = Stage.Recording;

                        AutoFitButtons();

                        #endregion
                    }
                    else
                    {
                        #region SnapShot Recording

                        Stage = Stage.Snapping;
                        //Title = "Board Recorder - " + Properties.Resources.Con_SnapshotMode;

                        AutoFitButtons();

                        #endregion
                    }

                    break;

                    #endregion

                    #endregion

                case Stage.Recording:

                    #region To Pause

                    Stage = Stage.Paused;
                    Title = FindResource("Recorder.Paused").ToString();

                    AutoFitButtons();

                    _capture.Stop();

                    FrameRate.Stop();
                    break;

                    #endregion

                case Stage.Paused:

                    #region To Record Again

                    Stage = Stage.Recording;
                    Title = "Board Recorder";

                    AutoFitButtons();

                    FrameRate.Start(_capture.Interval);

                    _capture.Start();
                    break;

                    #endregion

                case Stage.Snapping:

                    #region Take Screenshot (All possibles types)

                    _snapDelay = Settings.Default.SnapshotDefaultDelay;

                    Normal_Elapsed(null, null);

                    break;

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
                FrameCount = 0;

                _capture.Stop();
                FrameRate.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && ListFrames.Any())
                {
                    #region Stop

                    ExitArg = ExitAction.Recorded;
                    DialogResult = false;

                    #endregion
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !ListFrames.Any())
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    Stage = Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    FpsNumericUpDown.IsEnabled = true;
                    HeightTextBox.IsEnabled = true;
                    WidthTextBox.IsEnabled = true;

                    IsRecording(false);
                    Topmost = true;

                    Title = "Board Recorder ■";

                    AutoFitButtons();

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                var errorViewer = new ExceptionViewer(nll);
                errorViewer.ShowDialog();
                LogWriter.Log(nll, "NullPointer on the Stop function");
            }
            catch (Exception ex)
            {
                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
                LogWriter.Log(ex, "Error on the Stop function");
            }
        }

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (LowerGrid.ActualWidth < 250)
            {
                StopButton.Style = (Style)FindResource("Style.Button.NoText");

                HideMinimizeAndMaximize(true);
            }
            else
            {
                StopButton.Style = (Style)FindResource("Style.Button.Horizontal");

                HideMinimizeAndMaximize(false);
            }
        }

        #endregion

        #region Timers

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            string fileName = $"{_pathTemp}{FrameCount}.bmp";

            var render = MainBorder.GetRender(_dpi); //TODO: Too heavy!

            ListFrames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, render); });

            FrameCount++;
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
            HeightTextBox.Value = (int)Math.Round(Height, MidpointRounding.AwayFromZero);
            WidthTextBox.Value = (int)Math.Round(Width, MidpointRounding.AwayFromZero);

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

        #region Upper Grid Events

        private void BoardTipColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(Settings.Default.BoardColor) {Owner = this};
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.BoardColor = colorPicker.SelectedColor;
            }
        }

        #endregion

        #region Buttons

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;

            var options = new Options();
            options.ShowDialog(); //TODO: If recording started, maybe disable some properties.

            Topmost = true;
        }

        #endregion

        #region Other Events

        private void MainInkCanvas_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Stage == Stage.Stopped || Stage == Stage.Paused) && Keyboard.Modifiers != ModifierKeys.Control)
                RecordPause();
        }

        private void MainInkCanvas_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Stage == Stage.Recording)
                RecordPause();
        }

        private void LightWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region Save Settings

            Settings.Default.LastFps = Convert.ToInt32(FpsNumericUpDown.Value);
            Settings.Default.Width = (int)Width;
            Settings.Default.Height = (int)Height;

            Settings.Default.Save();

            #endregion

            if (Stage != (int)Stage.Stopped)
            {
                _capture.Stop();
                _capture.Dispose();
            }

            GC.Collect();
        }

        #endregion
    }
}
