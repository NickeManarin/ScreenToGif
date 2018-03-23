﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ScreenToGif.ImageUtil;
using ScreenToGif.Util;
using ScreenToGif.Util.Model;
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
        //TODO: The main idea is to create a "record as you draw" feature, 
        //with the possibility to record keyframes by keyframes (Snapshot)
        //and show the previous drawn keyframe as a "ghost" to help the drawing
        //There will be some exceptions to the automatic recording, such as holding the Ctrl key, etc

        #region Variables


        #region Counters

        /// <summary>
        /// The delay of each frame took as snapshot.
        /// </summary>
        private int? _snapDelay = null;

        #endregion

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _dpi = 96d;

        private bool _isCtrlDown = false;

        #region Timer

        private Timer _capture = new Timer();

        #endregion

        #endregion

        #region Inicialization

        public Board(bool hideBackButton = true)
        {
            InitializeComponent();

            BackVisibility = hideBackButton ? Visibility.Collapsed : Visibility.Visible;

            //Load
            _capture.Tick += Normal_Elapsed;

            #region Temporary folder

            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                UserSettings.All.TemporaryFolder = Path.GetTempPath();

            Project = new ProjectInfo().CreateProjectFolder();

            #endregion
        }

        private void Board_OnLoaded(object sender, RoutedEventArgs e)
        {
            _dpi = this.Dpi();

            WidthIntegerBox.Scale = _dpi / 96d;
            HeightIntegerBox.Scale = _dpi / 96d;
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
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            //GC.Collect(1);
            //mutexLock.ReleaseMutex();
        }

        #endregion

        #region Discard Async

        private delegate void DiscardFrames();

        private DiscardFrames _discardFramesDel;

        private void Discard()
        {
            try
            {
                #region Remove all the files

                foreach (var frame in Project.Frames)
                {
                    try
                    {
                        File.Delete(frame.Path);
                    }
                    catch (Exception)
                    { }
                }

                try
                {
                    Directory.Delete(Project.FullPath, true);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Delete Temp Path");
                }

                #endregion

                Project.Frames.Clear();
            }
            catch (IOException io)
            {
                LogWriter.Log(io, "Error while trying to Discard the Recording");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the recording", ex.Message));
                LogWriter.Log(ex, "Error while trying to Discard the Recording");
            }
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                //Enables the controls that are disabled while recording;
                FpsNumericUpDown.IsEnabled = true;
                HeightIntegerBox.IsEnabled = true;
                WidthIntegerBox.IsEnabled = true;
                MainGrid.IsEnabled = true;

                Cursor = Cursors.Arrow;
                IsRecording = false;

                DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                //Removes the current drawings.
                MainInkCanvas.Strokes.Clear();

                //if (!Settings.Default.Snapshot)
                //{
                //Only display the Record text when not in snapshot mode. 
                Title = FindResource("Board.Title") as string;
                //}
                //else
                //{
                //    Stage = Stage.Snapping;
                //    EnableSnapshot_Executed(null, null);
                //}

                AutoFitButtons();
            });

            GC.Collect();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void RecordPause()
        {
            switch (Stage)
            {
                case Stage.Stopped:

                    #region To Record

                    _capture = new Timer { Interval = 1000 / FpsNumericUpDown.Value };
                    _snapDelay = null;

                    Project = new ProjectInfo().CreateProjectFolder();

                    HeightIntegerBox.IsEnabled = false;
                    WidthIntegerBox.IsEnabled = false;
                    FpsNumericUpDown.IsEnabled = false;

                    IsRecording = true;
                    Topmost = true;

                    FrameRate.Start(_capture.Interval);

                    #region Start

                    //if (!Settings.Default.Snapshot)
                    //{
                    #region Normal Recording

                    _capture.Tick += Normal_Elapsed;
                    //Normal_Elapsed(null, null);
                    _capture.Start();

                    Stage = Stage.Recording;

                    AutoFitButtons();

                    #endregion
                    //}
                    //else
                    //{
                    //    #region SnapShot Recording

                    //    Stage = Stage.Snapping;
                    //    //Title = "Board Recorder - " + Properties.Resources.Con_SnapshotMode;

                    //    AutoFitButtons();

                    //    #endregion
                    //}

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
                    Title = FindResource("Board.Title") as string;

                    AutoFitButtons();

                    FrameRate.Start(_capture.Interval);

                    _capture.Start();
                    break;

                #endregion

                case Stage.Snapping:

                    #region Take Screenshot (All possibles types)

                    _snapDelay = UserSettings.All.SnapshotDefaultDelay;

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

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && Project.Any)
                {
                    Close();
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !Project.Any)
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    Stage = Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    FpsNumericUpDown.IsEnabled = true;
                    HeightIntegerBox.IsEnabled = true;
                    WidthIntegerBox.IsEnabled = true;

                    IsRecording = false;
                    Topmost = true;

                    Title = FindResource("Board.Title") as string + " ■";

                    AutoFitButtons();

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                ErrorDialog.Ok(FindResource("Board.Title") as string, "Error while stopping", nll.Message, nll);
                LogWriter.Log(nll, "NullPointer on the Stop function");
            }
            catch (Exception ex)
            {
                ErrorDialog.Ok(FindResource("Board.Title") as string, "Error while stopping", ex.Message, ex);
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

                MinimizeVisibility = Visibility.Collapsed;
            }
            else
            {
                StopButton.Style = (Style)FindResource("Style.Button.Horizontal");

                MinimizeVisibility = Visibility.Visible;
            }
        }

        #endregion

        #region Timers

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            string fileName = $"{Project.FullPath}{FrameCount}.png";

            //TODO: GetRender fails to create useful image when the control has decimals values as size.

            var render = MainBorder.GetRender(_dpi); //TODO: Too heavy! Maybe just save the strokes? like layers?

            Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds(_snapDelay)));

            ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, render); });

            FrameCount++;
        }

        #endregion

        #region Sizing

        private void LightWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AutoFitButtons();
        }

        #endregion

        #region Upper Grid Events

        private void BoardTipColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.BoardColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.BoardColor = colorPicker.SelectedColor;
            }
        }

        #endregion

        #region Buttons

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _capture.Stop();
            FrameRate.Stop();
            FrameCount = 0;
            Stage = Stage.Stopped;

            MainGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Other Events

        private void Board_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString().Equals(UserSettings.All.StopShortcut.ToString()))
            {
                StopButton_Click(null, null);
            }

            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && !_isCtrlDown)
            {
                AutoRecordToggleButton.IsChecked = !(AutoRecordToggleButton.IsChecked ?? true);
                _isCtrlDown = true;
            }
        }

        private void Board_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                AutoRecordToggleButton.IsChecked = !(AutoRecordToggleButton.IsChecked ?? true);
                _isCtrlDown = false;
            }
        }

        private void MainInkCanvas_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Stage == Stage.Stopped || Stage == Stage.Paused) && AutoRecordToggleButton.IsChecked == true)
                RecordPause();

            if (DiscardButton.Visibility == Visibility.Collapsed)
                DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);
        }

        private void MainInkCanvas_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Stage == Stage.Recording)
                RecordPause();
        }

        private async void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                await Task.Factory.StartNew(() => Dispatcher.Invoke(DragMove));
        }

        private void Board_Deactivated(object sender, EventArgs e)
        {
            if (_isCtrlDown)
            {
                AutoRecordToggleButton.IsChecked = !(AutoRecordToggleButton.IsChecked ?? true);
                _isCtrlDown = false;
            }
        }

        private void LightWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Save Settings
            UserSettings.Save();

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