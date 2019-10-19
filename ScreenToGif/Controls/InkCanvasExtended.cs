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

        private Image InkingImage { get; }

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
            InkingImage.Source = new DrawingImage(new DrawingGroup());

            // The following two HostVisuals are children of the _mainContainerVisual in this thread,
            // and these won't normally get composed into the UI thread from the inking thread's
            // element tree until a stroke is complete.
            var myArgs = new[]
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
            System.Windows.Threading.Dispatcher lDispatcher = lDynamicDispProperty.GetValue(lRenderingThread, null) as System.Windows.Threading.Dispatcher;
            if (lRenderingThread == null)
            {
                return;
            }

            // We invoke the inking thread to get the visual targets from the real time host visuals, then
            // use BFS to grab all the DrawingGroups as frozen into a new DrawingGroup, which we return
            // as frozen to the UI thread.
            DrawingGroup inkDrawingGroup = lDispatcher.Invoke(DispatcherPriority.Send,
                (DispatcherOperationCallback)delegate (object args)
                {
                    // We've got the field references from the other thread now, so we just get their
                    // VisualTarget properties, which is where we'll make a magical RootVisual call.
                    object[] lObjects = args as object[];
                    PropertyInfo lVtProperty = lObjects[0].GetType().GetProperty("VisualTarget", BindingFlags.Instance |
                        BindingFlags.NonPublic |
                        BindingFlags.Public);
                    VisualTarget VisualTarget1 = lVtProperty.GetValue(lObjects[0], null) as VisualTarget;
                    VisualTarget VisualTarget2 = lVtProperty.GetValue(lObjects[1], null) as VisualTarget;

                    // The RootVisual builds the visual when property get is called. We then
                    // all all its descendent DrawingGroups to this DrawingGroup that we are
                    // just using as a collection to return from the thread.
                    DrawingGroup drawingGroups = new DrawingGroup();
                    VisualTarget1?.RootVisual?.visualToFrozenDrawingGroup(drawingGroups);
                    VisualTarget1?.RootVisual?.visualToFrozenDrawingGroup(drawingGroups);
                    return drawingGroups.GetAsFrozen();
                },
                myArgs) as DrawingGroup;


            // This is a little jenky, but we need to set the image margins otherwise 
            // the drawing content will be cropped and aligned top-left despite inking 
            // very far from origin. Because we set image to an empty drawingImage 
            // earlier, we don't need to worry about things visibly jumping on screen.
            // Important note: Negatives are okay. If we set X,Y = (0,0), then the 
            // DrawingGroup.Bounds.TopLeft = (-3,0) [because user inked off the canvas
            // for instance], then the point starting at (-3,0) will be shifted to (0,0)
            // and our entire drawing will be moved over by 3 pixels, so here we just 
            // set the margin to whatever *negative* number the TopLeft has in that case,
            // and we care only about going over the actual width/height (e.g. infinity)
            var bounds = inkDrawingGroup.Bounds.TopLeft;
            var w = System.Math.Min(bounds.X, this.ActualWidth);
            var h = System.Math.Min(bounds.Y, this.ActualHeight);
            InkingImage.Margin = new System.Windows.Thickness(w, h, 0, 0);

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