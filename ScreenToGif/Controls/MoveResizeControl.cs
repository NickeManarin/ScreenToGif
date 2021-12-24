using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls;

public class MoveResizeControl : ContentControl
{
    #region Variables

    /// <summary>
    /// Resizing adorner uses Thumbs for visual elements.  
    /// The Thumbs have built-in mouse input handling.
    /// </summary>
    private Thumb _topLeft, _topRight, _bottomLeft, _bottomRight, _top, _bottom, _left, _right;

    /// <summary>
    /// The selection rectangle, used to drag the selection Rect elsewhere.
    /// </summary>
    private Border _border;

    /// <summary>
    /// The start point for the drag operation.
    /// </summary>
    private Point _startPoint;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty CanMoveProperty = DependencyProperty.Register("CanMove", typeof(bool), typeof(MoveResizeControl), new PropertyMetadata(true));

    public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register("CanResize", typeof(bool), typeof(MoveResizeControl), new PropertyMetadata(false));

    public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(Rect), typeof(MoveResizeControl), new PropertyMetadata(new Rect(-1, -1, 0, 0), Selected_PropertyChanged));

    public static readonly DependencyProperty LeftProperty = DependencyProperty.Register("Left", typeof(double), typeof(MoveResizeControl), new PropertyMetadata(-1d, Left_PropertyChanged));

    public static readonly DependencyProperty TopProperty = DependencyProperty.Register("Top", typeof(double), typeof(MoveResizeControl), new PropertyMetadata(-1d, Top_PropertyChanged));

    public static readonly DependencyProperty RestrictMovementProperty = DependencyProperty.Register("RestrictMovement", typeof(bool), typeof(MoveResizeControl), new PropertyMetadata(false));

    public static readonly DependencyProperty ContentScaleProperty = DependencyProperty.Register("ContentScale", typeof(double), typeof(MoveResizeControl), new PropertyMetadata(1d, ContentScale_PropertyChanged));

    #endregion

    #region Properties

    public bool CanMove
    {
        get => (bool) GetValue(CanMoveProperty);
        set => SetValue(CanMoveProperty, value);
    }

    public bool CanResize
    {
        get => (bool)GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    public Rect Selected
    {
        get => (Rect)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    public double Left
    {
        get => (double)GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    public double Top
    {
        get => (double)GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    public bool RestrictMovement
    {
        get => (bool)GetValue(RestrictMovementProperty);
        set => SetValue(RestrictMovementProperty, value);
    }

    public double ContentScale
    {
        get => (double)GetValue(ContentScaleProperty);
        set => SetValue(ContentScaleProperty, value);
    }

    #endregion

    static MoveResizeControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MoveResizeControl), new FrameworkPropertyMetadata(typeof(MoveResizeControl)));
    }

    #region Overrides

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _topLeft = Template.FindName("TopLeftThumb", this) as Thumb;
        _topRight = Template.FindName("TopRightThumb", this) as Thumb;
        _bottomLeft = Template.FindName("BottomLeftThumb", this) as Thumb;
        _bottomRight = Template.FindName("BottomRightThumb", this) as Thumb;

        _top = Template.FindName("TopThumb", this) as Thumb;
        _bottom = Template.FindName("BottomThumb", this) as Thumb;
        _left = Template.FindName("LeftThumb", this) as Thumb;
        _right = Template.FindName("RightThumb", this) as Thumb;

        _border = Template.FindName("SelectBorder", this) as Border;

        if (_topLeft == null || _topRight == null || _bottomLeft == null || _bottomRight == null ||
            _top == null || _bottom == null || _left == null || _right == null || _border == null)
            return;

        //Adjust the size of the element based on the content.
        //if (Math.Abs(Selected.Width) < 0.001 || Math.Abs(Selected.Height) < 0.001)
        //{
        //    var control = Content as FrameworkElement;

        //    control?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        //    control?.Arrange(new Rect(control.DesiredSize));

        //    if (control != null && Math.Abs(control.ActualHeight) > 0.001 && Math.Abs(control.ActualWidth) > 0.001)
        //        Selected = new Rect(Selected.X, Selected.Y, control.ActualWidth, control.ActualHeight);
        //}

        AdjustThumbs();

        //Add handlers for resizing • Corners.
        _topLeft.DragDelta += HandleTopLeft;
        _topRight.DragDelta += HandleTopRight;
        _bottomLeft.DragDelta += HandleBottomLeft;
        _bottomRight.DragDelta += HandleBottomRight;

        //Add handlers for resizing • Sides.
        _top.DragDelta += HandleTop;
        _bottom.DragDelta += HandleBottom;
        _left.DragDelta += HandleLeft;
        _right.DragDelta += HandleRight;

        //Drag to move.
        _border.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
        _border.MouseMove += Rectangle_MouseMove;
        _border.MouseLeftButtonUp += Rectangle_MouseLeftButtonUp;

        //Detect text updates.
        var textBlock = Content as TextBlock;

        //Size down too.
        if (textBlock != null)
            textBlock.LayoutUpdated += (sender, args) => AdjustContent();
    }

    protected override void OnChildDesiredSizeChanged(UIElement child)
    {
        AdjustContent();

        base.OnChildDesiredSizeChanged(child);
    }

    protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
    {
        AdjustContent();

        base.OnVisualChildrenChanged(visualAdded, visualRemoved);
    }

    #endregion

    #region Methods

    private void AdjustThumbs()
    {
        if (_topLeft == null)
            return;

        //Top left.
        Canvas.SetLeft(_topLeft, Selected.Left - _topLeft.Width / 2d);
        Canvas.SetTop(_topLeft, Selected.Top - _topLeft.Height / 2d);

        //Top right.
        Canvas.SetLeft(_topRight, Selected.Right - _topRight.Width / 2d);
        Canvas.SetTop(_topRight, Selected.Top - _topRight.Height / 2d);

        //Bottom left.
        Canvas.SetLeft(_bottomLeft, Selected.Left - _bottomLeft.Width / 2d);
        Canvas.SetTop(_bottomLeft, Selected.Bottom - _bottomLeft.Height / 2d);

        //Bottom right.
        Canvas.SetLeft(_bottomRight, Selected.Right - _bottomRight.Width / 2d);
        Canvas.SetTop(_bottomRight, Selected.Bottom - _bottomRight.Height / 2d);

        //Top.
        Canvas.SetLeft(_top, Selected.Left + Selected.Width / 2d - _top.Width / 2d);
        Canvas.SetTop(_top, Selected.Top - _top.Height / 2d);

        //Left.
        Canvas.SetLeft(_left, Selected.Left - _left.Width / 2d);
        Canvas.SetTop(_left, Selected.Top + Selected.Height / 2d - _left.Height / 2d);

        //Right.
        Canvas.SetLeft(_right, Selected.Right - _right.Width / 2d);
        Canvas.SetTop(_right, Selected.Top + Selected.Height / 2d - _right.Height / 2d);

        //Bottom.
        Canvas.SetLeft(_bottom, Selected.Left + Selected.Width / 2d - _bottom.Width / 2d);
        Canvas.SetTop(_bottom, Selected.Bottom - _bottom.Height / 2d);
    }

    public void AdjustContent()
    {
        var control = Content as FrameworkElement;

        if (control == null || !IsLoaded)
            return;

        control.LayoutTransform = new ScaleTransform(ContentScale, ContentScale, 0.5, 0.5);

        control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        control.Arrange(new Rect(control.DesiredSize));

        if (Math.Abs(control.ActualHeight) > 0.001 && Math.Abs(control.ActualWidth) > 0.001)
            Selected = new Rect(Selected.X, Selected.Y, control.ActualWidth * ContentScale, control.ActualHeight * ContentScale);
    }

    #endregion

    #region Events

    private static void Selected_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var control = o as MoveResizeControl;

        if (control == null)
            return;

        control.Left = control.Selected.Left;
        control.Top = control.Selected.Top;

        control.AdjustThumbs();
    }

    private static void Left_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var control = o as MoveResizeControl;

        if (control == null)
            return;

        control.Selected = new Rect(control.Left, control.Selected.Top, control.Selected.Width, control.Selected.Height);
    }

    private static void Top_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var control = o as MoveResizeControl;

        if (control == null)
            return;

        control.Selected = new Rect(control.Selected.Left, control.Top, control.Selected.Width, control.Selected.Height);
    }

    private static void ContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs d)
    {
        var control = o as MoveResizeControl;

        control?.AdjustContent();
    }

    private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);

        _border.CaptureMouse();

        e.Handled = true;
    }

    private void Rectangle_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_border.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;

        _border.MouseMove -= Rectangle_MouseMove;

        var currentPosition = e.GetPosition(this);

        var x = Selected.X + (currentPosition.X - _startPoint.X);
        var y = Selected.Y + (currentPosition.Y - _startPoint.Y);

        if (RestrictMovement)
        {
            if (x < 0)
                x = 0;

            if (y < 0)
                y = 0;

            if (x + Selected.Width > ActualWidth)
                x = ActualWidth - Selected.Width;

            if (y + Selected.Height > ActualHeight)
                y = ActualHeight - Selected.Height;
        }
        else
        {
            if (x < Selected.Width * -0.9)
                x = Selected.Width * -0.9;

            if (y < Selected.Height * -0.9)
                y = Selected.Height * -0.9;

            if (x + Selected.Width > ActualWidth + Selected.Width * 0.9)
                x = ActualWidth - Selected.Width + Selected.Width * 0.9;

            if (y + Selected.Height > ActualHeight + Selected.Height * 0.9)
                y = ActualHeight - Selected.Height + Selected.Height * 0.9;
        }

        Selected = new Rect(x, y, Selected.Width, Selected.Height);

        _startPoint = currentPosition;
        e.Handled = true;

        AdjustThumbs();

        _border.MouseMove += Rectangle_MouseMove;
    }

    private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_border.IsMouseCaptured)
            _border.ReleaseMouseCapture();

        AdjustThumbs();

        e.Handled = true;
    }

    ///<summary>
    ///Handler for resizing from the top-left.
    ///</summary>
    private void HandleTopLeft(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
        var left = Selected.Left - (width - Selected.Width);
        var height = Math.Max(Selected.Height - e.VerticalChange, 10);
        var top = Selected.Top - (height - Selected.Height);

        if (top < 0)
        {
            height -= top * -1;
            top = 0;
        }

        if (left < 0)
        {
            width -= left * -1;
            left = 0;
        }

        Selected = new Rect(left, top, width, height);

        AdjustThumbs();
    }

    /// <summary>
    ///  Handler for resizing from the top-right.
    /// </summary>
    private void HandleTopRight(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(Selected.Width + e.HorizontalChange, 10);
        var height = Math.Max(Selected.Height - e.VerticalChange, 10);
        var top = Selected.Top - (height - Selected.Height);

        if (top < 0)
        {
            height -= top * -1;
            top = 0;
        }

        if (Selected.Left + width > ActualWidth)
            width = ActualWidth - Selected.Left;

        Selected = new Rect(Selected.Left, top, width, height);

        AdjustThumbs();
    }

    /// <summary>
    ///  Handler for resizing from the bottom-left.
    /// </summary>
    private void HandleBottomLeft(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
        var left = Selected.Left - (width - Selected.Width);
        var height = Math.Max(Selected.Height + e.VerticalChange, 10);

        if (left < 0)
        {
            width -= left * -1;
            left = 0;
        }

        if (Selected.Left + width > ActualWidth)
            width = ActualWidth - Selected.Left;

        if (Selected.Top + height > ActualHeight)
            height = ActualHeight - Selected.Top;

        Selected = new Rect(left, Selected.Top, width, height);

        AdjustThumbs();
    }

    /// <summary>
    /// Handler for resizing from the bottom-right.
    /// </summary>
    private void HandleBottomRight(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(Selected.Width + e.HorizontalChange, 10);
        var height = Math.Max(Selected.Height + e.VerticalChange, 10);

        if (Selected.Left + width > ActualWidth)
            width = ActualWidth - Selected.Left;

        if (Selected.Top + height > ActualHeight)
            height = ActualHeight - Selected.Top;

        Selected = new Rect(Selected.Left, Selected.Top, width, height);

        AdjustThumbs();
    }

    /// <summary>
    /// Handler for resizing from the left-middle.
    /// </summary>
    private void HandleLeft(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
        var left = Selected.Left - (width - Selected.Width);

        if (left < 0)
        {
            width -= left * -1;
            left = 0;
        }

        Selected = new Rect(left, Selected.Top, width, Selected.Height);

        AdjustThumbs();
    }

    /// <summary>
    /// Handler for resizing from the top-middle.
    /// </summary>
    private void HandleTop(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var height = Math.Max(Selected.Height - e.VerticalChange, 10);
        var top = Selected.Top - (height - Selected.Height);

        if (top < 0)
        {
            height -= top * -1;
            top = 0;
        }

        Selected = new Rect(Selected.Left, top, Selected.Width, height);

        AdjustThumbs();
    }

    /// <summary>
    ///  Handler for resizing from the right-middle.
    /// </summary>
    private void HandleRight(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(Selected.Width + e.HorizontalChange, 10);

        if (Selected.Left + width > ActualWidth)
            width = ActualWidth - Selected.Left;

        Selected = new Rect(Selected.Left, Selected.Top, width, Selected.Height);

        AdjustThumbs();
    }

    /// <summary>
    /// Handler for resizing from the bottom-middle.
    /// </summary>
    private void HandleBottom(object sender, DragDeltaEventArgs e)
    {
        var hitThumb = sender as Thumb;

        if (hitThumb == null) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var height = Math.Max(Selected.Height + e.VerticalChange, 10);

        if (Selected.Top + height > ActualHeight)
            height = ActualHeight - Selected.Top;

        Selected = new Rect(Selected.Left, Selected.Top, Selected.Width, height);

        AdjustThumbs();
    }

    #endregion
}