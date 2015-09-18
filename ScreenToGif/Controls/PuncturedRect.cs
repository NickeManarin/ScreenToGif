using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// PuncturedRectangle class.
    /// </summary>
    public class PuncturedRect : Shape
	{
		#region Dependency properties

		public static readonly DependencyProperty InteriorProperty =
			DependencyProperty.Register("Interior", typeof(Rect), typeof(FrameworkElement),
				new FrameworkPropertyMetadata(new Rect(0, 0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender, null, new CoerceValueCallback(CoerceRectInterior), false), null);

		private static object CoerceRectInterior(DependencyObject d, object value)
		{
			PuncturedRect pr = (PuncturedRect)d;
			Rect rcExterior = pr.Exterior;
			Rect rcProposed = (Rect)value;

			double left = Math.Max(rcProposed.Left, rcExterior.Left);
			double top = Math.Max(rcProposed.Top, rcExterior.Top);
			double width = Math.Min(rcProposed.Right, rcExterior.Right) - left;
			double height = Math.Min(rcProposed.Bottom, rcExterior.Bottom) - top;

			rcProposed = new Rect(left, top, width, height);
			return rcProposed;
		}

		public Rect Interior
		{
			get { return (Rect)GetValue(InteriorProperty); }
			set { SetValue(InteriorProperty, value); }
		}

        public static readonly DependencyProperty ExteriorProperty =
			DependencyProperty.Register("Exterior", typeof(Rect), typeof(FrameworkElement),
				new FrameworkPropertyMetadata(new Rect(0, 0, double.MaxValue, double.MaxValue),
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange |
					FrameworkPropertyMetadataOptions.AffectsParentMeasure |
					FrameworkPropertyMetadataOptions.AffectsParentArrange |
					FrameworkPropertyMetadataOptions.AffectsRender,
					null, null, false), null);

		public Rect Exterior
		{
			get { return (Rect)GetValue(ExteriorProperty); }
			set { SetValue(ExteriorProperty, value); }
		}

		#endregion

		#region Constructors

        //public PuncturedRect() : this(new Rect(0, 0, 0, 0), new Rect()) { }

        //public PuncturedRect(Rect exterior, Rect interior)
        //{
        //    Interior = interior;
        //    Exterior = exterior;
        //}

		#endregion

		#region Override

		protected override Geometry DefiningGeometry
		{
			get
			{
				PathGeometry pthgExt = new PathGeometry();
				PathFigure pthfExt = new PathFigure();
				pthfExt.StartPoint = Exterior.TopLeft;
				pthfExt.Segments.Add(new LineSegment(Exterior.TopRight, false));
				pthfExt.Segments.Add(new LineSegment(Exterior.BottomRight, false));
				pthfExt.Segments.Add(new LineSegment(Exterior.BottomLeft, false));
				pthfExt.Segments.Add(new LineSegment(Exterior.TopLeft, false));
				pthgExt.Figures.Add(pthfExt);

				Rect rectIntSect = Rect.Intersect(Exterior, Interior);
				PathGeometry pthgInt = new PathGeometry();
				PathFigure pthfInt = new PathFigure();
				pthfInt.StartPoint = rectIntSect.TopLeft;
				pthfInt.Segments.Add(new LineSegment(rectIntSect.TopRight, false));
				pthfInt.Segments.Add(new LineSegment(rectIntSect.BottomRight, false));
				pthfInt.Segments.Add(new LineSegment(rectIntSect.BottomLeft, false));
				pthfInt.Segments.Add(new LineSegment(rectIntSect.TopLeft, false));
				pthgInt.Figures.Add(pthfInt);

				CombinedGeometry cmbg = new CombinedGeometry(GeometryCombineMode.Exclude, pthgExt, pthgInt);
				return cmbg;
			}
		}

		#endregion
	}
}
