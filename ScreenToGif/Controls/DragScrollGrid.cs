using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Controls;

/// <summary>
/// Scroll by Drag Grid
/// TODO: Make a grid that reacts to the drag sideway to increase or decrease a number.
/// </summary>
public class DragScrollGrid : Grid
{
    #region Variables

    private Point _lastPosition;

    public static readonly DependencyProperty IsDraggableProperty;

    #endregion

    #region Properties

    /// <summary>
    /// If true, will enable the value increase/decrease by sideway drag.
    /// </summary>
    public bool IsDraggable
    {
        get => (bool)GetValue(IsDraggableProperty);
        set => SetValue(IsDraggableProperty, value);
    }

    #endregion

    static DragScrollGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DragScrollGrid), new FrameworkPropertyMetadata(typeof(DragScrollGrid)));

        IsDraggableProperty = DependencyProperty.Register("IsDraggable", typeof(bool), typeof(DragScrollGrid), new PropertyMetadata(false));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        MouseDown += DragScrollGrid_MouseDown;
        MouseMove += DragScrollGrid_MouseMove;
        MouseUp += DragScrollGrid_MouseUp;
    }

    #region Events

    private void DragScrollGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Mouse.Capture(this);
        Cursor = Cursors.ScrollWE;
    }

    private void DragScrollGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!IsMouseCaptured) return;
        if ((int)_lastPosition.X == (int)e.GetPosition(this).X) return;

        if (_lastPosition.X > e.GetPosition(this).X)
        {
            //To the Left.
            //Value--;
        }

        //To the right.
        //Value++;
    }

    private void DragScrollGrid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    #endregion
}