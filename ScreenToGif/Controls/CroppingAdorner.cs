using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Point = System.Drawing.Point;

namespace ScreenToGif.Controls
{
    public class CroppingAdorner : Adorner
    {
        #region Private variables

        /// <summary>
        /// The size of the Thumb in pixels.
        /// </summary>
        private const double ThumbWidth = 10;

        /// <summary>
        /// Rectangle Shape, visual aid for the cropping selection.
        /// </summary>
        private readonly PuncturedRect _cropMask;

        /// <summary>
        /// Canvas that holds the Thumb collection.
        /// </summary>
        private readonly Canvas _thumbCanvas;

        /// <summary>
        /// Corner Thumbs used to change the crop selection.
        /// </summary>
        private readonly Thumb _thumbTopLeft, _thumbTopRight, _thumbBottomLeft, 
            _thumbBottomRight, _thumbTop, _thumbLeft, _thumbBottom, _thumbRight;

        /// <summary>
        /// Stores and manages the adorner's visual children.
        /// </summary>
        private readonly VisualCollection _visualCollection;

        /// <summary>
        /// Screen DPI.
        /// </summary>
        private static readonly double DpiX, DpiY;

        #endregion

        #region Routed Events

        public static readonly RoutedEvent CropChangedEvent = EventManager.RegisterRoutedEvent(
            "CropChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CroppingAdorner));

        public event RoutedEventHandler CropChanged
        {
            add { base.AddHandler(CropChangedEvent, value); }
            remove { base.RemoveHandler(CropChangedEvent, value); }
        }

        #endregion

        #region Dependency Properties

        public static DependencyProperty FillProperty = Shape.FillProperty.AddOwner(typeof(CroppingAdorner), 
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(110,0,0,0)), FillPropChanged));

        public static readonly DependencyProperty ClipRectangleProperty = DependencyProperty.Register("ClipRectangle", typeof(Rect), typeof(CroppingAdorner),
            new FrameworkPropertyMetadata(new Rect(0,0,0,0), ClipRectanglePropertyChanged));

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public Rect ClipRectangle
        {
            get
            {
                return _cropMask.Interior;
                //return (Rect)GetValue(ClipRectangleProperty);
            }
            set
            {
                SetValue(ClipRectangleProperty, value);
            }
        }

        private static void FillPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var crp = d as CroppingAdorner;

            if (crp != null)
            {
                crp._cropMask.Fill = (Brush)e.NewValue;
            }
        }

        private static void ClipRectanglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var crp = d as CroppingAdorner;

            if (crp == null) return;

            crp._cropMask.Interior = (Rect)e.NewValue;
            crp.SetThumbs(crp._cropMask.Interior);
            crp.RaiseEvent(new RoutedEventArgs(CropChangedEvent, crp));
        }

        #endregion

        #region Constructor

        static CroppingAdorner()
        {
            System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd((IntPtr)0);

            DpiX = g.DpiX;
            DpiY = g.DpiY;
        }

        public CroppingAdorner(UIElement adornedElement, Rect rcInit)
            : base(adornedElement)
        {
            _cropMask = new PuncturedRect
            {
                IsHitTestVisible = false,
                Interior = rcInit,
                Fill = Fill
            };

            _thumbCanvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _visualCollection = new VisualCollection(this)
            { _cropMask, _thumbCanvas};

            BuildCorner(ref _thumbTop, Cursors.SizeNS);
            BuildCorner(ref _thumbBottom, Cursors.SizeNS);
            BuildCorner(ref _thumbLeft, Cursors.SizeWE);
            BuildCorner(ref _thumbRight, Cursors.SizeWE);
            BuildCorner(ref _thumbTopLeft, Cursors.SizeNWSE);
            BuildCorner(ref _thumbTopRight, Cursors.SizeNESW);
            BuildCorner(ref _thumbBottomLeft, Cursors.SizeNESW);
            BuildCorner(ref _thumbBottomRight, Cursors.SizeNWSE);

            //Cropping handlers.
            _thumbBottomLeft.DragDelta += HandleBottomLeft;
            _thumbBottomRight.DragDelta += HandleBottomRight;
            _thumbTopLeft.DragDelta += HandleTopLeft;
            _thumbTopRight.DragDelta += HandleTopRight;
            _thumbTop.DragDelta += HandleTop;
            _thumbBottom.DragDelta += HandleBottom;
            _thumbRight.DragDelta += HandleRight;
            _thumbLeft.DragDelta += HandleLeft;

            //Clipping interior should be withing the bounds of the adorned element.
            FrameworkElement element = adornedElement as FrameworkElement;

            if (element != null)
            {
                element.SizeChanged += AdornedElement_SizeChanged;
            }
        }

        #endregion

        #region Thumb handlers

        private void HandleThumb(double drcL, double drcT, double drcW, double drcH, double dx, double dy)
        {
            Rect rcInterior = _cropMask.Interior;

            if (rcInterior.Width + drcW * dx < 0)
            {
                dx = -rcInterior.Width / drcW;
            }

            if (rcInterior.Height + drcH * dy < 0)
            {
                dy = -rcInterior.Height / drcH;
            }

            rcInterior = new Rect(
                rcInterior.Left + drcL * dx,
                rcInterior.Top + drcT * dy,
                rcInterior.Width + drcW * dx,
                rcInterior.Height + drcH * dy);

            if (rcInterior.Width < 10 || rcInterior.Height < 10)
                return;

            _cropMask.Interior = rcInterior;

            SetThumbs(_cropMask.Interior);
            RaiseEvent(new RoutedEventArgs(CropChangedEvent, this));
        }

        //Cropping from the bottom-left.
        private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(1, 0, -1, 1,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }

        //Cropping from the bottom-right.
        private void HandleBottomRight(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(0, 0, 1, 1,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }

        //Cropping from the top-right.
        private void HandleTopRight(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(0, 1, 1, -1,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }

        //Cropping from the top-left.
        private void HandleTopLeft(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(1, 1, -1, -1,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }

        //Cropping from the top.
        private void HandleTop(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(0, 1, 0, -1,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }

        //Cropping from the left.
        private void HandleLeft(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(1, 0, -1, 0,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }

        //Cropping from the right.
        private void HandleRight(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(0, 0, 1, 0,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }

        //Cropping from the bottom.
        private void HandleBottom(object sender, DragDeltaEventArgs args)
        {
            if (sender is Thumb)
            {
                HandleThumb(0, 0, 0, 1,
                    args.HorizontalChange,
                    args.VerticalChange);
            }
        }
        
        #endregion

        #region Other handlers

        private void AdornedElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;

            if (element == null)
                return;

            bool wasChanged = false;

            double intLeft = ClipRectangle.Left, intTop = ClipRectangle.Top,
                intWidth = ClipRectangle.Width, intHeight = ClipRectangle.Height;

            if (ClipRectangle.Left > element.RenderSize.Width)
            {
                intLeft = element.RenderSize.Width;
                intWidth = 0;
                wasChanged = true;
            }

            if (ClipRectangle.Top > element.RenderSize.Height)
            {
                intTop = element.RenderSize.Height;
                intHeight = 0;
                wasChanged = true;
            }

            if (ClipRectangle.Right > element.RenderSize.Width)
            {
                intWidth = Math.Max(0, element.RenderSize.Width - intLeft);
                wasChanged = true;
            }

            if (ClipRectangle.Bottom > element.RenderSize.Height)
            {
                intHeight = Math.Max(0, element.RenderSize.Height - intTop);
                wasChanged = true;
            }

            if (wasChanged)
            {
                ClipRectangle = new Rect(intLeft, intTop, intWidth, intHeight);
            }
        }

        #endregion

        #region Arranging

        protected override Size ArrangeOverride(Size finalSize)
        {
            var rcExterior = new Rect(0, 0, AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height);
            _cropMask.Exterior = rcExterior;

            var rcInterior = _cropMask.Interior;
            _cropMask.Arrange(rcExterior);

            SetThumbs(rcInterior);
            _thumbCanvas.Arrange(rcExterior);
            return finalSize;
        }

        #endregion

        #region Public Methods

        public BitmapSource CropImage()
        {
            Thickness margin = AdornerMargin();
            Rect rcInterior = _cropMask.Interior;

            Point pxFromSize = UnitsToPx(rcInterior.Width, rcInterior.Height);

            //CroppedBitmap indexes from the upper left of the margin whereas RenderTargetBitmap renders the
            //control exclusive of the margin.  Hence our need to take the margins into account here...

            Point pxFromPos = UnitsToPx(rcInterior.Left + margin.Left, rcInterior.Top + margin.Top);
            Point pxWhole = UnitsToPx(AdornedElement.RenderSize.Width + margin.Left, AdornedElement.RenderSize.Height + margin.Left);
            pxFromSize.X = Math.Max(Math.Min(pxWhole.X - pxFromPos.X, pxFromSize.X), 0);
            pxFromSize.Y = Math.Max(Math.Min(pxWhole.Y - pxFromPos.Y, pxFromSize.Y), 0);

            if (pxFromSize.X == 0 || pxFromSize.Y == 0)
            {
                return null;
            }

            Int32Rect rcFrom = new Int32Rect(pxFromPos.X, pxFromPos.Y, pxFromSize.X, pxFromSize.Y);

            RenderTargetBitmap rtb = new RenderTargetBitmap(pxWhole.X, pxWhole.Y, DpiX, DpiY, PixelFormats.Default);
            rtb.Render(AdornedElement);

            return new CroppedBitmap(rtb, rcFrom);
        }

        #endregion

        #region Private Methods

        private void SetThumbs(Rect rc)
        {
            SetPosition(_thumbBottomRight, rc.Right, rc.Bottom);
            SetPosition(_thumbTopLeft, rc.Left, rc.Top);
            SetPosition(_thumbTopRight, rc.Right, rc.Top);
            SetPosition(_thumbBottomLeft, rc.Left, rc.Bottom);
            SetPosition(_thumbTop, rc.Left + rc.Width / 2, rc.Top);
            SetPosition(_thumbBottom, rc.Left + rc.Width / 2, rc.Bottom);
            SetPosition(_thumbLeft, rc.Left, rc.Top + rc.Height / 2);
            SetPosition(_thumbRight, rc.Right, rc.Top + rc.Height / 2);
        }

        private Thickness AdornerMargin()
        {
            Thickness thick = new Thickness(0);

            if (AdornedElement is FrameworkElement)
            {
                thick = ((FrameworkElement)AdornedElement).Margin;
            }

            return thick;
        }

        private void BuildCorner(ref Thumb thumb, Cursor cursor)
        {
            if (thumb != null) return;

            thumb = new Thumb
            {
                Cursor = cursor,
                Style = (Style) FindResource("ScrollBarThumbVertical"),
                Width = ThumbWidth,
                Height = ThumbWidth
            };
            
            _thumbCanvas.Children.Add(thumb);
        }

        private Point UnitsToPx(double x, double y)
        {
            return new Point((int)(x * DpiX / 96), (int)(y * DpiY / 96));
        }

        private void SetPosition(Thumb thumb, double x, double y)
        {
            Canvas.SetTop(thumb, y - ThumbWidth / 2);
            Canvas.SetLeft(thumb, x - ThumbWidth / 2);
        }

        #endregion

        #region Visual Tree Override

        // Override the VisualChildrenCount and GetVisualChild properties to interface with 
        // the adorner's visual collection.
        protected override int VisualChildrenCount => _visualCollection.Count;

        protected override Visual GetVisualChild(int index)
        { return _visualCollection[index]; }

        #endregion
    }
}
