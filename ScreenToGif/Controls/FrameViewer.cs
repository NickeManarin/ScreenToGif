using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ScreenToGif.Controls;

/// <summary>
/// Frame viewer that works with a WriteableBitmap.
/// </summary>
public class FrameViewer : Control
{
    //UI
    //Image scale difference with screen scale.
    //Zoom.
    //Mouse and keyboard events.
    //Check if rendering works.
        
    #region Properties

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(WriteableBitmap), typeof(FrameViewer), 
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, Source_PropertyChanged));

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(FrameViewer), 
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender, Zoom_PropertyChanged));


    /// <summary>
    /// The source image.
    /// </summary>
    [Description("The source image.")]
    public WriteableBitmap Source
    {
        get => (WriteableBitmap)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// The zoom level of the image.
    /// </summary>
    [Description("The zoom level of the image.")]
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetCurrentValue(ZoomProperty, value);
    }



    #endregion

    static FrameViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FrameViewer), new FrameworkPropertyMetadata(typeof(FrameViewer)));
    }


    private static void Source_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        //When the image source changes, the UI needs to be adjusted somewhow.
    }

    private static void Zoom_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

    }
}