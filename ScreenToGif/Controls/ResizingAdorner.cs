using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// The Resizing Adorner controls.
    /// </summary>
    public class ResizingAdorner : Adorner
    {
        #region Variables

        /// <summary>
        /// Resizing adorner uses Thumbs for visual elements.  
        /// The Thumbs have built-in mouse input handling.
        /// </summary>
        readonly Thumb _topLeft, _topRight, _bottomLeft, _bottomRight, _middleBottom, _middleTop, _leftMiddle, _rightMiddle;

        /// <summary>
        /// To store and manage the adorner's visual children.
        /// </summary>
        readonly VisualCollection _visualChildren;

        #endregion

        /// <summary>
        /// Initialize the ResizingAdorner.
        /// </summary>
        /// <param name="adornedElement">The element to be adorned.</param>
        public ResizingAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _visualChildren = new VisualCollection(this);

            //Call a helper method to initialize the Thumbs with a customized cursors.
            BuildAdornerCorner(ref _topLeft, Cursors.SizeNWSE);
            BuildAdornerCorner(ref _topRight, Cursors.SizeNESW);
            BuildAdornerCorner(ref _bottomLeft, Cursors.SizeNESW);
            BuildAdornerCorner(ref _bottomRight, Cursors.SizeNWSE);

            BuildAdornerCorner(ref _middleBottom, Cursors.SizeNS);
            BuildAdornerCorner(ref _middleTop, Cursors.SizeNS);
            BuildAdornerCorner(ref _leftMiddle, Cursors.SizeWE);
            BuildAdornerCorner(ref _rightMiddle, Cursors.SizeWE);

            //Add handlers for resizing • Corners
            _bottomLeft.DragDelta += HandleBottomLeft;
            _bottomRight.DragDelta += HandleBottomRight;
            _topLeft.DragDelta += HandleTopLeft;
            _topRight.DragDelta += HandleTopRight;

            //Add handlers for resizing • Sides
            _middleBottom.DragDelta += HandleBottomMiddle;
            _middleTop.DragDelta += HandleTopMiddle;
            _leftMiddle.DragDelta += HandleLeftMiddle;
            _rightMiddle.DragDelta += HandleRightMiddle;
        }

        #region DragDelta Event Handlers

        /// <summary>
        /// Handler for resizing from the bottom-right.
        /// </summary>
        private void HandleBottomRight(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
            adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);

            if (adornedElement.Width > parentElement.Width)
            {
                parentElement.Width = adornedElement.Width;
            }

            if (adornedElement.Height > parentElement.Height)
            {
                parentElement.Height = adornedElement.Height;
            }
        }

        /// <summary>
        ///  Handler for resizing from the top-right.
        /// </summary>
        private void HandleTopRight(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
            //adornedElement.Height = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);

            double heightOld = adornedElement.Height;
            double heightNew = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
            double topOld = Canvas.GetTop(adornedElement);
            adornedElement.Height = heightNew;
            Canvas.SetTop(adornedElement, topOld - (heightNew - heightOld));
        }

        /// <summary>
        ///  Handler for resizing from the top-left.
        /// </summary>
        private void HandleTopLeft(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            //adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            //adornedElement.Height = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);

            double widthOld = adornedElement.Width;
            double widthNew = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            double leftOld = Canvas.GetLeft(adornedElement);
            
            adornedElement.Width = widthNew;
            Canvas.SetLeft(adornedElement, leftOld - (widthNew - widthOld));

            double heightOld = adornedElement.Height;
            double heightNew = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
            double topOld = Canvas.GetTop(adornedElement);
            
            adornedElement.Height = heightNew;
            Canvas.SetTop(adornedElement, topOld - (heightNew - heightOld));
        }

        /// <summary>
        ///  Handler for resizing from the bottom-left.
        /// </summary>
        private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.
            //adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);

            double widthOld = adornedElement.Width;
            double widthNew = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            double leftOld = Canvas.GetLeft(adornedElement);

            adornedElement.Width = widthNew;
            Canvas.SetLeft(adornedElement, leftOld - (widthNew - widthOld));
        }

        /// <summary>
        /// Handler for resizing from the bottom-middle.
        /// </summary>
        private void HandleBottomMiddle(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger than the height of an adorner.
            adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);

            if (adornedElement.Height > parentElement.Height)
            {
                parentElement.Height = adornedElement.Height;
            }
        }

        /// <summary>
        /// Handler for resizing from the top-middle.
        /// </summary>
        private void HandleTopMiddle(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger than the height of an adorner.
            double heightOld = adornedElement.Height;
            double heightNew = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
            double topOld = Canvas.GetTop(adornedElement);
            
            adornedElement.Height = heightNew;
            Canvas.SetTop(adornedElement, topOld - (heightNew - heightOld));
        }

        /// <summary>
        /// Handler for resizing from the left-middle.
        /// </summary>
        private void HandleLeftMiddle(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger than the height of an adorner.
            double widthOld = adornedElement.Width;
            double widthNew = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            double leftOld = Canvas.GetLeft(adornedElement);

            adornedElement.Width = widthNew;
            Canvas.SetLeft(adornedElement, leftOld - (widthNew - widthOld));
        }

        /// <summary>
        ///  Handler for resizing from the right-middle.
        /// </summary>
        private void HandleRightMiddle(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize.
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger than the width of the adorner.
            adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///  Arrange the Adorners.
        /// </summary>
        /// <param name="finalSize">The final Size</param>
        /// <returns>The final size</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.  
            // These will be used to place the ResizingAdorner at the corners of the adorned element.  
            double desiredWidth = AdornedElement.DesiredSize.Width;
            double desiredHeight = AdornedElement.DesiredSize.Height;

            // adornerWidth & adornerHeight are used for placement as well.
            double adornerWidth = this.DesiredSize.Width;
            double adornerHeight = this.DesiredSize.Height;

            _topLeft.Arrange(new Rect(-adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
            _topRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
            _bottomLeft.Arrange(new Rect(-adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));
            _bottomRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));

            _middleBottom.Arrange(new Rect(0, desiredHeight / 2, adornerWidth, adornerHeight));
            _middleTop.Arrange(new Rect(0, -desiredHeight / 2, adornerWidth, adornerHeight));
            _leftMiddle.Arrange(new Rect(-desiredWidth / 2, 0, adornerWidth, adornerHeight));
            _rightMiddle.Arrange(new Rect(+desiredWidth / 2, 0, adornerWidth, adornerHeight));

            return finalSize;
        }

        /// <summary>
        /// Instantiates the corner Thumbs, setting the Cursor property, 
        /// some appearance properties, and add the elements to the visual tree.
        /// </summary>
        /// <param name="cornerThumb">The Thumb to Instantiate.</param>
        /// <param name="customizedCursor">The custom cursor.</param>
        private void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
        {
            if (cornerThumb != null) return;

            cornerThumb = new Thumb {Cursor = customizedCursor};
            cornerThumb.Height = cornerThumb.Width = 10;
            cornerThumb.Style = (Style)FindResource("ScrollBarThumbVertical");
            
            _visualChildren.Add(cornerThumb);
        }

        // This method ensures that the Widths and Heights are initialized.  Sizing to content produces
        // Width and Height values of Double.NaN.  Because this Adorner explicitly resizes, the Width and Height
        // need to be set first.  It also sets the maximum size of the adorned element.
        private void EnforceSize(FrameworkElement adornedElement)
        {
            if (adornedElement.Width.Equals(Double.NaN))
                adornedElement.Width = adornedElement.DesiredSize.Width;
            if (adornedElement.Height.Equals(Double.NaN))
                adornedElement.Height = adornedElement.DesiredSize.Height;

            FrameworkElement parent = adornedElement.Parent as FrameworkElement;

            if (parent != null)
            {
                adornedElement.MaxHeight = parent.ActualHeight;
                adornedElement.MaxWidth = parent.ActualWidth;
            }
        }

        // Override the VisualChildrenCount and GetVisualChild properties to interface with 
        // the adorner's visual collection.
        protected override int VisualChildrenCount
        {
            get { return _visualChildren.Count; }
        }

        /// <summary>
        /// Gets the VisualChildren at given position.
        /// </summary>
        /// <param name="index">The Index to look for.</param>
        /// <returns>The VisualChildren</returns>
        protected override Visual GetVisualChild(int index)
        {
            return _visualChildren[index];
        }

        #endregion
    }
}