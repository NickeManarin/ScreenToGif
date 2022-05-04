using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.Controls.Shapes;

public sealed class Arrow : Shape
{
    #region Dependency Properties

    public static readonly DependencyProperty X1Property = DependencyProperty.Register("X1", typeof(double), typeof(Arrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty Y1Property = DependencyProperty.Register("Y1", typeof(double), typeof(Arrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty X2Property = DependencyProperty.Register("X2", typeof(double), typeof(Arrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty Y2Property = DependencyProperty.Register("Y2", typeof(double), typeof(Arrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty HeadWidthProperty = DependencyProperty.Register("HeadWidth", typeof(double), typeof(Arrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty HeadHeightProperty = DependencyProperty.Register("HeadHeight", typeof(double), typeof(Arrow), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure)); 

    #endregion

    #region Properties

    [TypeConverter(typeof(LengthConverter))]
    public double X1
    {
        get => (double)GetValue(X1Property);
        set => SetValue(X1Property, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public double Y1
    {
        get => (double)GetValue(Y1Property);
        set => SetValue(Y1Property, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public double X2
    {
        get => (double)GetValue(X2Property);
        set => SetValue(X2Property, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public double Y2
    {
        get => (double)GetValue(Y2Property);
        set => SetValue(Y2Property, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public double HeadWidth
    {
        get => (double)GetValue(HeadWidthProperty);
        set => SetValue(HeadWidthProperty, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public double HeadHeight
    {
        get => (double)GetValue(HeadHeightProperty);
        set => SetValue(HeadHeightProperty, value);
    }

    #endregion

    #region Overrides

    protected override Geometry DefiningGeometry
    {
        get
        {
            //var vector = new Point(X2, Y2) - new Point(X1, Y1);
            //var angle = Vector.AngleBetween(new Vector(1, 0), vector);
            var geometry = new StreamGeometry();
            var width = (double.IsNaN(Width) ? ActualWidth : Width) - StrokeThickness;
            var height = (double.IsNaN(Height) ? ActualHeight : Height) - StrokeThickness;

            using (var sgc = geometry.Open())
            {
                //TODO: Add StrokeThickness / 2d to top left
                sgc.BeginFigure(new Point(width * 0.6898, height * 0.4), true, true);
                sgc.LineTo(new Point(width * 0, height * 0.4), true, true);
                sgc.LineTo(new Point(width * 0, height * 0.65), true, true);
                sgc.LineTo(new Point(width * 0.6898, height * 0.65), true, true);
                sgc.LineTo(new Point(width * 0.3684, height * 1), true, true);
                sgc.LineTo(new Point(width * 0.6608, height * 1), true, true);
                sgc.LineTo(new Point(width * 1, height * 0.5), true, true);
                sgc.LineTo(new Point(width * 0.6608, height * 0), true, true);
                sgc.LineTo(new Point(width * 0.3684, height * 0), true, true);
            }

            //geometry.Transform = new RotateTransform(angle, (Math.Abs(X1) - Math.Abs(X2)) / 2, (Math.Abs(Y1) - Math.Abs(Y2)) / 2);
            geometry.Freeze();

            return geometry;
        }
    }

    #endregion
}