using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.Controls;

internal class CircularProgressBar : ProgressBar
{
    #region Variables and properties

    private Path _pathRoot;
    private PathFigure _pathFigure;
    private ArcSegment _arcSegment;

    public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register(nameof(Percentage), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(OnPercentageChanged));
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(5D, OnPropertyChanged));
    public static readonly DependencyProperty SegmentColorProperty = DependencyProperty.Register(nameof(SegmentColor), typeof(Brush), typeof(CircularProgressBar), new PropertyMetadata(Brushes.Red));
    public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(nameof(Radius), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(25D, OnPropertyChanged));
    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(nameof(Angle), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(120D, OnPropertyChanged));
    public static readonly DependencyProperty IsInvertedProperty = DependencyProperty.Register(nameof(IsInverted), typeof(bool), typeof(CircularProgressBar), new PropertyMetadata(false, OnPropertyChanged));

    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public Brush SegmentColor
    {
        get => (Brush)GetValue(SegmentColorProperty);
        set => SetValue(SegmentColorProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double Percentage
    {
        get => (double)GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public bool IsInverted
    {
        get => (bool)GetValue(IsInvertedProperty);
        set => SetValue(IsInvertedProperty, value);
    }

    #endregion

    static CircularProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CircularProgressBar), new FrameworkPropertyMetadata(typeof(CircularProgressBar)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        ValueChanged += CircularProgressBar_ValueChanged;

        _pathRoot = Template.FindName("PathRoot", this) as Path;
        _pathFigure = Template.FindName("PathFigure", this) as PathFigure;
        _arcSegment = Template.FindName("ArcSegment", this) as ArcSegment;

        if (Math.Abs(Percentage) < 0.001)
        {
            if (IsInverted)
                Percentage = Math.Abs(100F * (Value - 1) / (Maximum - Minimum) - 100F);
            else
                Percentage = (100F * Value) / (Maximum - Minimum);
        }

        Angle = (Percentage * 360) / 100;
        RenderArc();
    }

    #region Events

    private void CircularProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsInverted)
        {
            Percentage = Math.Abs((100F * (Value - 1)) / (Maximum - Minimum) - 100F);
            return;
        }

        Percentage = (100F * Value) / (Maximum - Minimum);
    }

    private static void OnPercentageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is CircularProgressBar circle)
            circle.Angle = (circle.Percentage * 360) / 100;
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var circle = sender as CircularProgressBar;
        circle?.RenderArc();
    }

    #endregion

    #region Methods

    public void RenderArc()
    {
        var startPoint = new Point(Radius, 0);
        var endPoint = ComputeCartesianCoordinate(Angle, Radius);
        endPoint.X += Radius;
        endPoint.Y += Radius;

        if (_pathRoot != null)
        {
            _pathRoot.Width = Radius * 2 + StrokeThickness;
            _pathRoot.Height = Radius * 2 + StrokeThickness;
            _pathRoot.Margin = new Thickness(StrokeThickness, StrokeThickness, 0, 0);
        }

        var largeArc = Angle > 180.0;

        var outerArcSize = new Size(Radius, Radius);

        if (_pathFigure != null)
            _pathFigure.StartPoint = startPoint;

        if (Math.Abs(startPoint.X - Math.Round(endPoint.X)) < 0.001 && Math.Abs(startPoint.Y - Math.Round(endPoint.Y)) < 0.001)
            endPoint.X -= 0.01;

        if (_arcSegment != null)
        {
            _arcSegment.Point = endPoint;
            _arcSegment.Size = outerArcSize;
            _arcSegment.IsLargeArc = largeArc;
        }
    }

    private Point ComputeCartesianCoordinate(double angle, double radius)
    {
        //Convert to radians.
        var angleRad = (Math.PI / 180.0) * (angle - 90);

        var x = radius * Math.Cos(angleRad);
        var y = radius * Math.Sin(angleRad);

        return new Point(x, y);
    }

    #endregion
}