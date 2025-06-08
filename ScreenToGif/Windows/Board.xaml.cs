using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ScreenToGif.Domain.Enums;
using ScreenToGif.ImageUtil;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows.Other;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows;

/// <summary>
/// Board recorder, a "record as you draw" feature.
/// </summary>
public partial class Board
{
    #region Variables

    /// <summary>
    /// The DPI of the current screen.
    /// </summary>
    private double _dpi = 96d;

    private bool _isCtrlDown = false;

    #region Timer

    private Timer _capture = new Timer();

    #endregion

    #endregion

    public Board()
    {
        InitializeComponent();

        _capture.Tick += Normal_Elapsed;
    }

    private void Board_Loaded(object sender, RoutedEventArgs e)
    {
        _dpi = this.Dpi();

        WidthIntegerBox.Scale = _dpi / 96d;
        HeightIntegerBox.Scale = _dpi / 96d;
        
        Arguments.ClearAutomationArgs();
    }

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

    private void Discard()
    {
        try
        {
            #region Remove all the files

            if (Project == null)
                return;

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

    #endregion

    /// <summary>
    /// Method that starts or pauses the recording
    /// </summary>
    private void RecordPause()
    {
        switch (Stage)
        {
            case RecorderStages.Stopped:
            {
                _capture = new Timer { Interval = 1000 / FpsNumericUpDown.Value };

                Project?.Clear();
                Project = new ProjectInfo().CreateProjectFolder(ProjectByType.BoardRecorder);

                HeightIntegerBox.IsEnabled = false;
                WidthIntegerBox.IsEnabled = false;
                FpsNumericUpDown.IsEnabled = false;

                IsRecording = true;
                Topmost = true;

                FrameRate.Start(_capture.Interval);
                
                _capture.Tick += Normal_Elapsed;
                _capture.Start();

                Stage = RecorderStages.Recording;

                AutoFitButtons();

                break;
            }

            case RecorderStages.Recording:
            {
                Stage = RecorderStages.Paused;
                Title = LocalizationHelper.Get("S.Recorder.Paused");

                AutoFitButtons();

                _capture.Stop();

                FrameRate.Stop();
                break;
            }

            case RecorderStages.Paused:
            {
                Stage = RecorderStages.Recording;
                Title = LocalizationHelper.Get("S.Board.Title");

                AutoFitButtons();

                FrameRate.Start(_capture.Interval);

                _capture.Start();
                break;
            }
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

            if (Stage != RecorderStages.Stopped && Stage != RecorderStages.PreStarting && Project.Any)
            {
                Close();
            }
            else if (Stage is RecorderStages.PreStarting && !Project.Any)
            {
                Stage = RecorderStages.Stopped;

                //Enables the controls that are disabled while recording;
                FpsNumericUpDown.IsEnabled = true;
                HeightIntegerBox.IsEnabled = true;
                WidthIntegerBox.IsEnabled = true;

                IsRecording = false;
                Topmost = true;

                Title = LocalizationHelper.Get("S.Board.Title") + " ■";

                AutoFitButtons();
            }
        }
        catch (NullReferenceException nll)
        {
            ErrorDialog.Ok(LocalizationHelper.Get("S.Board.Title"), "Error while stopping", nll.Message, nll);
            LogWriter.Log(nll, "NullPointer on the Stop function");
        }
        catch (Exception ex)
        {
            ErrorDialog.Ok(LocalizationHelper.Get("S.Board.Title"), "Error while stopping", ex.Message, ex);
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

    private void Normal_Elapsed(object sender, EventArgs e)
    {
        var fileName = $"{Project.FullPath}{FrameCount}.png";

        //TODO: GetRender fails to create useful image when the control has decimals values as size.

        var render = MainBorder.GetRender(_dpi); //TODO: Too heavy! Maybe just save the strokes? like layers?

        Project.Frames.Add(new FrameInfo(fileName, FrameRate.GetMilliseconds()));

        ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, render); });

        FrameCount++;
    }

    private void LightWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        AutoFitButtons();
    }

    private async void DiscardButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserSettings.All.NotifyRecordingDiscard && !Dialog.Ask(LocalizationHelper.Get("S.Recorder.Discard.Title"),
                LocalizationHelper.Get("S.Recorder.Discard.Instruction"), LocalizationHelper.Get("S.Recorder.Discard.Message"), false))
            return;

        _capture.Stop();
        FrameRate.Stop();
        FrameCount = 0;
        Stage = RecorderStages.Stopped;

        MainGrid.IsEnabled = false;
        Cursor = Cursors.AppStarting;

        await Task.Run(Discard);

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

        Title = LocalizationHelper.Get("S.Board.Title");

        AutoFitButtons();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        Stop();
    }

    private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = Stage != RecorderStages.Recording && Stage != RecorderStages.PreStarting;
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
    
    private void Board_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key.ToString().Equals(UserSettings.All.StopShortcut.ToString()))
            StopButton_Click(null, null);

        if (e.Key is Key.LeftCtrl or Key.RightCtrl && !_isCtrlDown)
        {
            AutoRecordToggleButton.IsChecked = !(AutoRecordToggleButton.IsChecked ?? true);
            _isCtrlDown = true;
        }
    }

    private void Board_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl)
        {
            AutoRecordToggleButton.IsChecked = !(AutoRecordToggleButton.IsChecked ?? true);
            _isCtrlDown = false;
        }
    }

    private void MainInkCanvas_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Stage is RecorderStages.Stopped or RecorderStages.Paused && AutoRecordToggleButton.IsChecked == true)
            RecordPause();

        if (DiscardButton.Visibility == Visibility.Collapsed)
            DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);
    }

    private void MainInkCanvas_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (Stage == RecorderStages.Recording)
            RecordPause();
    }

    private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed)
            DragMove();
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

        if (Stage != RecorderStages.Stopped)
        {
            _capture.Stop();
            _capture.Dispose();
        }

        GC.Collect();
    }
}