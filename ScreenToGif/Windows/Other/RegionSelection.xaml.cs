using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Native;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Windows.Other;

public partial class RegionSelection : Window
{
    #region Properties

    public Rect Rect { get; set; }
        
    public ModeType? Mode { get; set; }

    public bool IsStatic { get; set; }
        
    public Monitor Monitor { get; set; }

    public double Dpi { get; set; }

    public double Scale { get; set; }

    private Point _previousPoint;
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Right;
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Bottom;

    #endregion

    #region Custom event

    public static readonly RoutedEvent PositionChangedEvent = EventManager.RegisterRoutedEvent(nameof(PositionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RegionSelection));
    public static readonly RoutedEvent DragStartedEvent = EventManager.RegisterRoutedEvent(nameof(DragStarted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RegionSelection));
    public static readonly RoutedEvent DragEndedEvent = EventManager.RegisterRoutedEvent(nameof(DragEnded), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RegionSelection));

    public event RoutedEventHandler PositionChanged
    {
        add => AddHandler(PositionChangedEvent, value);
        remove => RemoveHandler(PositionChangedEvent, value);
    }

    public event RoutedEventHandler DragStarted
    {
        add => AddHandler(DragStartedEvent, value);
        remove => RemoveHandler(DragStartedEvent, value);
    }

    public event RoutedEventHandler DragEnded
    {
        add => AddHandler(DragEndedEvent, value);
        remove => RemoveHandler(DragEndedEvent, value);
    }

    private void RaisePositionChanged()
    {
        if (PositionChangedEvent == null || !IsLoaded)
            return;

        RaiseEvent(new RoutedEventArgs(PositionChangedEvent));
    }

    private void RaiseDragStarted()
    {
        if (DragStartedEvent == null || !IsLoaded)
            return;

        RaiseEvent(new RoutedEventArgs(DragStartedEvent));
    }

    private void RaiseDragEnded()
    {
        if (DragEndedEvent == null || !IsLoaded)
            return;

        RaiseEvent(new RoutedEventArgs(DragEndedEvent));
    }

    #endregion

    public RegionSelection()
    {
        InitializeComponent();

        RenderOptions.SetEdgeMode(SelectionRectangle, EdgeMode.Unspecified);
    }


    private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        Scale = e.NewDpi.DpiScaleX;
        Dpi = e.NewDpi.PixelsPerInchX;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        Opacity = WindowState == WindowState.Minimized ? 0 : 1;
    }

    private void Thumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsStatic || sender is not Border border)
            return;

        border.CaptureMouse();
        _previousPoint = PointToScreen(e.GetPosition(this));
        RaiseDragStarted();

        e.Handled = true;
    }

    private void Thumb_MouseMove(object sender, MouseEventArgs e)
    {
        if (IsStatic || !(sender is Border border) || !border.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
            return;

        var currentPosition = PointToScreen(e.GetPosition(this));

        //Detect how much the mouse cursor was moved.
        var x = Rect.X + (currentPosition.X - _previousPoint.X) / Scale;
        var y = Rect.Y + (currentPosition.Y - _previousPoint.Y) / Scale;

        //Limit the drag to the current screen.
        if (x < Monitor.Bounds.X - 1)
            x = Monitor.Bounds.X - 1;

        if (y < Monitor.Bounds.Y - 1)
            y = Monitor.Bounds.Y - 1;

        if (x + Rect.Width > Monitor.Bounds.Right + 1)
            x = Monitor.Bounds.Right + 1 - Rect.Width;

        if (y + Rect.Height > Monitor.Bounds.Bottom + 1)
            y = Monitor.Bounds.Bottom + 1 - Rect.Height;

        //Is there any way to prevent mouse going towards the edges when the region is already touching it?

        //Move the selection.
        Rect = new Rect(x, y, Rect.Width, Rect.Height);
        DisplaySelection(false);
        RaisePositionChanged();

        _previousPoint = currentPosition;
        e.Handled = true;
    }

    private void Thumb_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsStatic || !(sender is Border border) || !border.IsMouseCaptured)
            return;

        border.ReleaseMouseCapture();
        DisplayThumbs();
        RaiseDragEnded();
    }


    public void Select(ModeType? mode, Rect region, Monitor monitor = null)
    {
        //TODO: Configurable border color.

        //When the region switches monitors, move the selection to the new monitor, so that the scale of the UI changes.
        //This solves the issue where the UI would move to the wrong position.
        if (monitor != null)
        {
            //If the new region is in another screen, move the panel to the new screen first, to adjust the UI to the screen DPI.
            if (Monitor?.Handle != monitor.Handle || Monitor?.Scale != monitor.Scale)
            {
                if (double.IsNaN(Left) || double.IsNaN(Top))
                    Show();

                this.MoveToScreen(monitor);
            }

            Monitor = monitor;
        }
        else
        {
            //TODO: Maybe get the monitor which intersects the most with the region.
            Monitor = MonitorHelper.FromPoint((int) region.X, (int) region.Y);
        }

        Mode = mode ?? Mode;
        Rect = region;
        IsStatic = !Mode.HasValue || Mode == ModeType.Fullscreen || !UserSettings.All.EnableSelectionPanning;
        Opacity = !Mode.HasValue || Mode == ModeType.Fullscreen ? 0 : 1;

        DisplaySelection();
        DisplayThumbs();
        Show();

        Scale = this.Scale();
        Dpi = Scale * 96d;
    } 

    private void DisplaySelection(bool ignoreThumbs = true)
    {
        Left = (Rect.Left - (ignoreThumbs || _horizontalAlignment == HorizontalAlignment.Right ? 0 : HorizontalBorder.ActualWidth)) / (this.Scale() / Monitor.Scale);
        Top = (Rect.Top - (ignoreThumbs || _verticalAlignment == VerticalAlignment.Bottom ? 0 : VerticalBorder.ActualHeight)) / (this.Scale() / Monitor.Scale);

        SelectionRectangle.Width = Rect.Width;
        SelectionRectangle.Height = Rect.Height;
    }

    private void DisplayThumbs()
    {
        if (IsStatic)
        {
            HorizontalBorder.Visibility = Visibility.Collapsed;
            CornerBorder.Visibility = Visibility.Collapsed;
            VerticalBorder.Visibility = Visibility.Collapsed;
            return;
        }

        //Detect the space left on all 4 sides.
        var leftSpace = Rect.X - Monitor.Bounds.X;
        var topSpace = Rect.Y - Monitor.Bounds.Y;
        var rightSpace = Monitor.Bounds.Right - Rect.Right;
        var bottomSpace = Monitor.Bounds.Bottom - Rect.Bottom;

        //Display the thumb to the left if there's space on the left and not enough space on the right.
        //Display the thumb to the top if there's space on the top and not enough space on the bottom.
        _horizontalAlignment = rightSpace < 10 && leftSpace > 10 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        _verticalAlignment = bottomSpace < 10 && topSpace > 10 ? VerticalAlignment.Top : VerticalAlignment.Bottom;

        #region Position the thumbs

        //Visibility as hidden, to have the size available.
        if (_horizontalAlignment != HorizontalAlignment.Right)
        {
            HorizontalBorder.Visibility = Visibility.Hidden;
            HorizontalBorder.Refresh();
        }

        if (_verticalAlignment != VerticalAlignment.Bottom)
        {
            VerticalBorder.Visibility = Visibility.Hidden;
            VerticalBorder.Refresh();
        }
            
        //Offset.
        Left = (Rect.Left - (_horizontalAlignment == HorizontalAlignment.Right ? 0 : HorizontalBorder.ActualWidth)) / (this.Scale() / Monitor.Scale);
        Top = (Rect.Top - (_verticalAlignment == VerticalAlignment.Bottom ? 0 : VerticalBorder.ActualHeight)) / (this.Scale() / Monitor.Scale); 

        //Grid positioning.
        Grid.SetRow(HorizontalBorder, 1);
        Grid.SetColumn(HorizontalBorder, _horizontalAlignment == HorizontalAlignment.Right ? 2 : 0);

        Grid.SetRow(CornerBorder, _verticalAlignment == VerticalAlignment.Bottom ? 2 : 0);
        Grid.SetColumn(CornerBorder, _horizontalAlignment == HorizontalAlignment.Right ? 2 : 0);

        Grid.SetRow(VerticalBorder, _verticalAlignment == VerticalAlignment.Bottom ? 2 : 0);
        Grid.SetColumn(VerticalBorder, 1);

        //Alignment.
        VerticalBorder.HorizontalAlignment = _horizontalAlignment;
        HorizontalBorder.VerticalAlignment = _verticalAlignment;

        //Corners.
        HorizontalBorder.CornerRadius = new CornerRadius
        {
            TopLeft = _verticalAlignment == VerticalAlignment.Bottom && _horizontalAlignment == HorizontalAlignment.Left ? 15 : 0,
            TopRight = _verticalAlignment == VerticalAlignment.Bottom && _horizontalAlignment == HorizontalAlignment.Right ? 15 : 0,
            BottomRight = _verticalAlignment == VerticalAlignment.Top && _horizontalAlignment == HorizontalAlignment.Right ? 15 : 0,
            BottomLeft = _verticalAlignment == VerticalAlignment.Top && _horizontalAlignment == HorizontalAlignment.Left ? 15 : 0
        };
        CornerBorder.CornerRadius = new CornerRadius
        {
            TopLeft = _verticalAlignment == VerticalAlignment.Top && _horizontalAlignment == HorizontalAlignment.Left ? 15 : 0,
            TopRight = _verticalAlignment == VerticalAlignment.Top && _horizontalAlignment == HorizontalAlignment.Right ? 15 : 0,
            BottomRight = _verticalAlignment == VerticalAlignment.Bottom && _horizontalAlignment == HorizontalAlignment.Right ? 15 : 0,
            BottomLeft = _verticalAlignment == VerticalAlignment.Bottom && _horizontalAlignment == HorizontalAlignment.Left ? 15 : 0
        };
        VerticalBorder.CornerRadius = new CornerRadius
        {
            TopLeft = _verticalAlignment == VerticalAlignment.Top && _horizontalAlignment == HorizontalAlignment.Right ? 15 : 0,
            TopRight = _verticalAlignment == VerticalAlignment.Top && _horizontalAlignment == HorizontalAlignment.Left ? 15 : 0,
            BottomRight = _verticalAlignment == VerticalAlignment.Bottom && _horizontalAlignment == HorizontalAlignment.Left ? 15 : 0,
            BottomLeft = _verticalAlignment == VerticalAlignment.Bottom && _horizontalAlignment == HorizontalAlignment.Right ? 15 : 0
        };

        //Borders.
        HorizontalBorder.BorderThickness = new Thickness
        {
            Left = _horizontalAlignment == HorizontalAlignment.Left ? 1 : 0,
            Top = _verticalAlignment == VerticalAlignment.Bottom ? 1 : 0,
            Right = _horizontalAlignment == HorizontalAlignment.Right ? 1 : 0,
            Bottom = _verticalAlignment == VerticalAlignment.Top ? 1 : 0
        };
        CornerBorder.BorderThickness = new Thickness
        {
            Left = _horizontalAlignment == HorizontalAlignment.Left ? 1 : 0,
            Top = _verticalAlignment == VerticalAlignment.Top ? 1 : 0,
            Right = _horizontalAlignment == HorizontalAlignment.Right ? 1 : 0,
            Bottom = _verticalAlignment == VerticalAlignment.Bottom ? 1 : 0
        };
        VerticalBorder.BorderThickness = new Thickness
        {
            Left = _horizontalAlignment == HorizontalAlignment.Right ? 1 : 0,
            Top = _verticalAlignment == VerticalAlignment.Top ? 1 : 0,
            Right = _horizontalAlignment == HorizontalAlignment.Left ? 1 : 0,
            Bottom = _verticalAlignment == VerticalAlignment.Bottom ? 1 : 0
        };

        //Tooltips.
        ToolTipService.SetPlacement(HorizontalBorder, _horizontalAlignment == HorizontalAlignment.Right ? PlacementMode.Right : PlacementMode.Left);
        ToolTipService.SetPlacement(CornerBorder, _horizontalAlignment == HorizontalAlignment.Right ? PlacementMode.Right : PlacementMode.Left);
        ToolTipService.SetPlacement(VerticalBorder, _verticalAlignment == VerticalAlignment.Bottom ? PlacementMode.Bottom : PlacementMode.Top);

        //Visibility.
        HorizontalBorder.Visibility = Visibility.Visible;
        CornerBorder.Visibility = Visibility.Visible;
        VerticalBorder.Visibility = Visibility.Visible;

        #endregion
    }

    public void DisplayGuidelines()
    {
        GuidelinesGrid.Visibility = Visibility.Visible;
    }

    public void HideGuidelines()
    {
        GuidelinesGrid.Visibility = Visibility.Collapsed;
        Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
    }

    public void ClearSelection()
    {
        Close();
    }
}