using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.Controls
{
    class CircularProgressBar : ProgressBar
    {
        #region Variables

        private Path _pathRoot;
        private PathFigure _pathFigure;
        private ArcSegment _arcSegment;

        public static readonly DependencyProperty PercentageProperty;
        public static readonly DependencyProperty StrokeThicknessProperty;
        public static readonly DependencyProperty SegmentColorProperty;
        public static readonly DependencyProperty RadiusProperty;
        public static readonly DependencyProperty AngleProperty;
        public static readonly DependencyProperty IsInvertedProperty;

        #endregion

        #region Properties

        public Double Radius
        {
            get { return (Double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        public Brush SegmentColor
        {
            get { return (Brush)GetValue(SegmentColorProperty); }
            set { SetValue(SegmentColorProperty, value); }
        }

        public Double StrokeThickness
        {
            get { return (Double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public Double Percentage
        {
            get { return (double)GetValue(PercentageProperty); }
            set { SetValue(PercentageProperty, value); }
        }

        public Double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public bool IsInverted
        {
            get { return (bool)GetValue(IsInvertedProperty); }
            set { SetValue(IsInvertedProperty, value); }
        }

        #endregion

        static CircularProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CircularProgressBar), new FrameworkPropertyMetadata(typeof(CircularProgressBar)));

            PercentageProperty = DependencyProperty.Register("Percentage", typeof(Double), typeof(CircularProgressBar), new PropertyMetadata(OnPercentageChanged));
            StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(Double), typeof(CircularProgressBar), new PropertyMetadata(5D, OnPropertyChanged));
            SegmentColorProperty = DependencyProperty.Register("SegmentColor", typeof(Brush), typeof(CircularProgressBar), new PropertyMetadata(new SolidColorBrush(Colors.Red)));
            RadiusProperty = DependencyProperty.Register("Radius", typeof(Double), typeof(CircularProgressBar), new PropertyMetadata(25D, OnPropertyChanged));
            AngleProperty = DependencyProperty.Register("Angle", typeof(Double), typeof(CircularProgressBar), new PropertyMetadata(120D, OnPropertyChanged));

            IsInvertedProperty = DependencyProperty.Register("IsInverted", typeof(bool), typeof(CircularProgressBar), new PropertyMetadata(false, OnPropertyChanged));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ValueChanged += CircularProgressBar_ValueChanged;

            _pathRoot = Template.FindName("PathRoot", this) as Path;
            _pathFigure = Template.FindName("PathFigure", this) as PathFigure;
            _arcSegment = Template.FindName("ArcSegment", this) as ArcSegment;

            if (Percentage == 0)
            {
                if (IsInverted)
                    Percentage = Math.Abs((100F * (Value - 1)) / (Maximum - Minimum) - 100F);
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
            CircularProgressBar circle = sender as CircularProgressBar;
            circle.Angle = (circle.Percentage * 360) / 100;
        }

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CircularProgressBar circle = sender as CircularProgressBar;
            circle.RenderArc();
        }

        #endregion

        #region Methods

        public void RenderArc()
        {
            Point startPoint = new Point(Radius, 0);
            Point endPoint = ComputeCartesianCoordinate(Angle, Radius);
            endPoint.X += Radius;
            endPoint.Y += Radius;

            if (_pathRoot != null)
            {
                _pathRoot.Width = Radius * 2 + StrokeThickness;
                _pathRoot.Height = Radius * 2 + StrokeThickness;
                _pathRoot.Margin = new Thickness(StrokeThickness, StrokeThickness, 0, 0);
            }

            bool largeArc = Angle > 180.0;

            Size outerArcSize = new Size(Radius, Radius);

            if (_pathFigure != null)
                _pathFigure.StartPoint = startPoint;

            if (startPoint.X == Math.Round(endPoint.X) && startPoint.Y == Math.Round(endPoint.Y))
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
            //Convert to radians
            double angleRad = (Math.PI / 180.0) * (angle - 90);

            double x = radius * Math.Cos(angleRad);
            double y = radius * Math.Sin(angleRad);

            return new Point(x, y);
        }

        #endregion
    }
}
