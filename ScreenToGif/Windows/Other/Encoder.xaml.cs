using ScreenToGif.Controls;
using ScreenToGif.Util;
using System;
using System.Windows;
using System.Windows.Input;

namespace ScreenToGif.Windows.Other;

public partial class Encoder : Window, IEncoding
{
    #region Variables

    /// <summary>
    /// The static Encoder window.
    /// </summary>
    private static Encoder _encoder = null;

    /// <summary>
    /// The latest state of the window. Used when hiding the window to show the recorder.
    /// </summary>
    private static WindowState _lastState = WindowState.Normal;

    /// <summary>
    /// True if this is the encoder window.
    /// </summary>
    public bool IsEncoderWindow { get; } = true;

    /// <summary>
    /// True if this window is available for use.
    /// </summary>
    public static bool IsAvailable => _encoder != null;

    #endregion

    public Encoder()
    {
        InitializeComponent();
    }

    #region Events

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        //Remove all ItemViews of this window from the manager list.
        NotificationBox?.RemoveAllViews();

        _encoder = null;
        GC.Collect();

        //Display the encodings in the editor.
        EncodingManager.MoveEncodingsToPopups();
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        NotificationBox.CheckIfFileExist();
    }

    private void ClearAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = NotificationBox?.HasAnyActiveEncoding == true;
    }

    private void ClearAll_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        EncodingManager.RemoveFinishedEncodings();
    }

    #endregion


    public EncoderListViewItem EncodingAdded(int id)
    {
        return _encoder?.NotificationBox.AddEncoding(id);
    }

    public void EncodingUpdated(int? id = null, bool onlyStatus = false)
    {
        if (onlyStatus)
            return;

        _encoder?.NotificationBox.UpdateEncoding(id);
    }

    public EncoderListViewItem EncodingRemoved(int id)
    {
        return _encoder?.NotificationBox.RemoveEncoding(id);
    }


    #region Public Static

    /// <summary>
    /// Shows the Encoder window.
    /// </summary>
    /// <param name="scale">Screen scale.</param>
    public static void Start(double scale)
    {
        #region If already started

        if (_encoder != null)
        {
            if (_encoder.WindowState == WindowState.Minimized)
                Restore();

            _encoder.Activate();
            return;
        }

        #endregion

        _encoder = new Encoder();

        var screen = ScreenHelper.GetScreen(_encoder);

        //Lower Right corner.
        _encoder.Left = screen.WorkingArea.Width / scale - _encoder.Width;
        _encoder.Top = screen.WorkingArea.Height / scale - _encoder.Height;

        _encoder.Show();
    }

    /// <summary>
    /// Minimizes the Encoder window.
    /// </summary>
    public static void Minimize()
    {
        if (_encoder == null)
            return;

        _lastState = _encoder.WindowState;

        _encoder.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Minimizes the Encoder window.
    /// </summary>
    public static void Restore()
    {
        if (_encoder == null)
            return;

        _encoder.WindowState = _lastState;
    }

    /// <summary>
    /// Tries to close the Window if there's no encoding active.
    /// </summary>
    public static void TryClose()
    {
        if (_encoder == null)
            return;

        if (_encoder.NotificationBox.HasAnyActiveEncoding)
            _encoder.Close();
    }

    #endregion
}