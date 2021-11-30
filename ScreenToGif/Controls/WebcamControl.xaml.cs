using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Util;
using ScreenToGif.Webcam.DirectX;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Controls;

public partial class WebcamControl : UserControl
{
    #region Variables

    public CaptureWebcam Capture { get; set; }

    public Filter VideoDevice { get; set; }

    #endregion

    #region Properties

    public int VideoWidth => Capture?.Width ?? -1;

    public int VideoHeight => Capture?.Height ?? -1;

    #endregion

    public WebcamControl()
    {
        InitializeComponent();
    }

    #region Private Methods

    private bool IsInDesignMode()
    {
        return DesignerProperties.GetIsInDesignMode(this);
    }

    private double Scale()
    {
        var source = PresentationSource.FromVisual(this);

        if (source?.CompositionTarget != null)
            return source.CompositionTarget.TransformToDevice.M11;

        return 1d;
    }

    #endregion

    #region Public Methods

    public void Refresh()
    {
        try
        {
            //To change the video device, a dispose is needed.
            if (Capture != null)
            {
                Capture.Dispose();
                Capture = null;
            }

            //Create capture object.
            if (VideoDevice != null)
            {
                Capture = new CaptureWebcam(VideoDevice) { PreviewWindow = this, Scale = Scale() };
                Capture.StartPreview();

                //Width = Height * ((double)Capture.Width / (double)Capture.Height);
            }
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "It was not possible to access the webcam feed.");
            ErrorDialog.Ok("ScreenToGif", "It was not possible to access the webcam's feed", e.Message, e);
        }
    }

    public void Unload()
    {
        if (Capture != null)
        {
            Capture.StopPreview();
            Capture.Dispose();
        }

        VideoDevice = null;

        GC.Collect();
    }

    #endregion

    #region Events

    private void WebcamControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        //Don't show the feed if in design mode.
        if (IsInDesignMode())
            return;

        Refresh();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Unload();
    }

    #endregion
}