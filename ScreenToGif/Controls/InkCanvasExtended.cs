using ScreenToGif.ImageUtil;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScreenToGif.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// InkCanvasControl class extending the InkCanvas class
    /// </summary>
    public class InkCanvasExtended : InkCanvas
    {
        /// <summary>
        /// Base Constructor pluse creation of the Inking Overlay
        /// </summary>
        public InkCanvasExtended()
            : base()
        {
            // We add a child image to use as an inking overlay because we need 
            // to compose the inking thread's real-time data with the UI data
            // that contains already-captured strokes.
            InkingImage = new Image();
            this.Children.Add(InkingImage);
            // Inking overlay should be in front, since it is a "newer" pre-stroke
            // element and once its stroke is brought over to the UI thread it will
            // be in front of previous strokes. 
            Canvas.SetZIndex(InkingImage, 99);
            
        }
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
        /// Overlay Image that receives DrawingGroup contents from the real-time inking thread upon UpdateInkOverlay()
        /// </summary>
        private Image InkingImage { get; }

        /// <summary>
        /// Tiny transparent corner point to prevent whitespace before DrawingGroup.TopLeft being cropped by Image control
        /// </summary>
        private readonly GeometryDrawing TransparentCornerPoint =
            new GeometryDrawing(
                new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)),
                new Pen(Brushes.White, 0.1),
                new LineGeometry(new Point(0, 0), new Point(0.1,0.1))
            ).GetAsFrozen() as GeometryDrawing;

        /// <summary>
        /// Re-initializes InkingImage (the overlay that receives DrawingGroups from the inking thread)
        /// </summary>
        public void ResetInkOverlay()
        {
            InkingImage.Source = new DrawingImage(new DrawingGroup());
        }

        /// <summary>
        /// Updates the inking overlay from the real-time inking thread
        /// </summary>
        /// <returns></returns>
        public void UpdateInkOverlay()
        {
            // Set the source to nothing at first so we don't see things in the wrong place
            // momentarily when adjusting the margins later on. 
            // Also good for when returning when no inking exists, but could do immediately
            // before the return in that case.
            ResetInkOverlay();

            // The following two HostVisuals are children of the _mainContainerVisual in this thread,
            // and these won't normally get composed into the UI thread from the inking thread's
            // element tree until a stroke is complete.
            var rawInkHostVisuals = new[]
            {
                typeof(DynamicRenderer).GetField("_rawInkHostVisual1", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this.DynamicRenderer ),
                typeof(DynamicRenderer).GetField("_rawInkHostVisual2", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this.DynamicRenderer )
            };

            // We just need to get a handle on the _renderingThread (inking thread), and its ThreadDispatcher.
            var lRenderingThread = typeof(DynamicRenderer).GetField("_renderingThread", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this.DynamicRenderer);
            if(lRenderingThread == null)
            {
                return;
            }
            PropertyInfo lDynamicDispProperty = lRenderingThread.GetType().GetProperty("ThreadDispatcher", BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);

            // The ThreadDispatcher derives from System.Windows.Threading.Dispatcher, 
            // which is a little simpler than using more reflection to get the derived type.
            Dispatcher dispatcher = lDynamicDispProperty.GetValue(lRenderingThread, null) as Dispatcher;
            if (lRenderingThread == null)
            {
                return;
            }

            // We invoke the inking thread to get the visual targets from the real time host visuals, then
            // use BFS to grab all the DrawingGroups as frozen into a new DrawingGroup, which we return
            // as frozen to the UI thread.
            DrawingGroup inkDrawingGroup = dispatcher.Invoke(DispatcherPriority.Send,
                (DispatcherOperationCallback)delegate (object rawInkHVs)
                {
                    // We've got the field references from the other thread now, so we just get their
                    // VisualTarget properties, which is where we'll make a magical RootVisual call.
                    object[] lObjects = rawInkHVs as object[];
                    PropertyInfo vtProperty = lObjects[0].GetType().GetProperty("VisualTarget", BindingFlags.Instance |
                        BindingFlags.NonPublic |
                        BindingFlags.Public);
                    VisualTarget visualTarget1 = vtProperty.GetValue(lObjects[0], null) as VisualTarget;
                    VisualTarget visualTarget2 = vtProperty.GetValue(lObjects[1], null) as VisualTarget;

                    // The RootVisual builds the visual when property get is called. We then
                    // all all its descendent DrawingGroups to this DrawingGroup that we are
                    // just using as a collection to return from the thread.
                    DrawingGroup drawingGroups = new DrawingGroup();

                    // This is a little jenky:
                    // We add a point in the top left so the DrawingGroup will be 
                    // appropriately offset from the top left of the Image container.
                    drawingGroups.Children.Add(TransparentCornerPoint);

                    // We try to add both the visualTagets (in case both are active)
                    visualTarget1?.RootVisual?.visualToFrozenDrawingGroup(drawingGroups);
                    visualTarget2?.RootVisual?.visualToFrozenDrawingGroup(drawingGroups);

                    return drawingGroups.GetAsFrozen();
                },
                rawInkHostVisuals) as DrawingGroup;

            // At this point, just update the image source with the drawing image.
            InkingImage.Source = new DrawingImage(inkDrawingGroup);
        }
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

}