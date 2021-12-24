using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace ScreenToGif.Controls;

/// <inheritdoc />
/// <summary>
/// InkCanvasControl class extending the InkCanvas class
/// </summary>
public class InkCanvasExtended : InkCanvas
{
    /// <summary>
    /// Gets or set the eraser shape
    /// </summary>
    public new StylusShape EraserShape
    {
        get => (StylusShape) GetValue(EraserShapeProperty);
        set => SetValue(EraserShapeProperty, value);
    }

    // Using a DependencyProperty as the backing store for EraserShape.  
    // This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EraserShapeProperty = DependencyProperty.Register("EraserShape", typeof (StylusShape), typeof (InkCanvasExtended), 
        new UIPropertyMetadata(new RectangleStylusShape(10, 10), OnEraserShapePropertyChanged));

    /// <summary>
    /// Event to handle the property change
    /// </summary>
    /// <param name="d">dependency object</param>
    /// <param name="e">event args</param>
    private static void OnEraserShapePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is InkCanvasExtended canvas))
            return;

        canvas.EraserShape = (StylusShape) e.NewValue;
        canvas.RenderTransform = new MatrixTransform();
    }
}