using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models;
using ScreenToGif.Domain.Models.Native;
using ScreenToGif.Util;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Windows.Other;

public partial class RegionSelector : Window
{
    public Monitor Monitor { get; set; }

    private Action<Monitor, Rect> _selected;
    private Action<Monitor> _changed;
    private Action<Monitor> _gotHover;
    private Action _aborted;
    private double _scale = 1;


    public RegionSelector()
    {
        InitializeComponent();
    }


    public void Select(Monitor monitor, ModeType mode, Rect previousRegion, Action<Monitor, Rect> selected, Action<Monitor> changed, Action<Monitor> gotHover, Action aborted)
    {
        //Resize to fit given window.
        Left = monitor.Bounds.Left;
        Top = monitor.Bounds.Top;
        Width = monitor.Bounds.Width;
        Height = monitor.Bounds.Height;
            
        Monitor = monitor;

        _scale = monitor.Scale;
        _selected = selected;
        _changed = changed;
        _gotHover = gotHover;
        _aborted = aborted;

        SelectControl.Width = monitor.Bounds.Width;
        SelectControl.Height = monitor.Bounds.Height;
        SelectControl.Scale = monitor.Scale;
        SelectControl.ParentLeft = Left;
        SelectControl.ParentTop = Top;
        SelectControl.Mode = mode;

        if (mode == ModeType.Region)
        {
            SelectControl.BackImage = CaptureBackground();

            if (UserSettings.All.SelectionImprovement)
            {
                AllowsTransparency = false;
                Background = new ImageBrush(CaptureBackground(false));
            }

            //Since each region selector is attached to a single screen, the selection must be translated.
            SelectControl.Selected = previousRegion.Translate(monitor.Bounds.Left * -1, monitor.Bounds.Top * -1);
            SelectControl.Windows.Clear();
        }
        else if (mode == ModeType.Window)
        {
            //Get only the windows that are located inside the given screen.
            var win = Native.Helpers.Windows.EnumerateWindowsByMonitor(monitor);

            //Since each region selector is attached to a single screen, the list of positions must be translated.
            SelectControl.Windows = win.AdjustPosition(monitor.Bounds.Left, monitor.Bounds.Top);
        }
        else if (mode == ModeType.Fullscreen)
        {
            //Each selector is the whole screen.
            SelectControl.Windows = new List<DetectedRegion>
            {
                new DetectedRegion(monitor.Handle, new Rect(new Size(monitor.Bounds.Width, monitor.Bounds.Height)), monitor.Name)
            };
        }

        //Call the selector to select the region.
        SelectControl.IsPickingRegion = true;
        Show();

        this.MoveToScreen(monitor, true);
    }

    public void ClearSelection()
    {
        SelectControl.Retry();
    }

    public void ClearHoverEffects()
    {
        SelectControl.HideZoom();
    }

    public void CancelSelection()
    {
        Close();
    }


    private double GetScreenDpi()
    {
        try
        {
            var source = Dispatcher?.Invoke<PresentationSource>(() => PresentationSource.FromVisual(this));

            if (source?.CompositionTarget != null)
                return Dispatcher.Invoke<double>(() => source.CompositionTarget.TransformToDevice.M11);
            else
                return 1;
        }
        catch (Exception)
        {
            return 1;
        }
        finally
        {
            GC.Collect(1);
        }
    }

    private BitmapSource CaptureBackground(bool addPadding = true)
    {
        //A 7 pixel offset is added to allow the crop by the magnifying glass.
        if (addPadding)
            return Native.Helpers.Capture.CaptureScreenAsBitmapSource((int)Math.Round((Width + 14 + 1) * _scale), (int)Math.Round((Height + 14 + 1) * _scale),
                (int)Math.Round((Left - 7) * _scale), (int)Math.Round((Top - 7) * _scale));

        return Native.Helpers.Capture.CaptureScreenAsBitmapSource((int)Math.Round(Width * _scale), (int)Math.Round(Height * _scale),
            (int)Math.Round(Left * _scale), (int)Math.Round(Top * _scale));
    }

    private void SelectControl_MouseHovering(object sender, RoutedEventArgs e)
    {
        _gotHover.Invoke(Monitor);
    }

    private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs e)
    {
        SelectControl.IsPickingRegion = false;
        _selected.Invoke(Monitor, SelectControl.Selected.Translate(Monitor.Bounds.Left, Monitor.Bounds.Top)); //NonExpandedSelection
        Close();
    }

    private void SelectControl_SelectionChanged(object sender, RoutedEventArgs e)
    {
        _changed.Invoke(Monitor);
    }

    private void SelectControl_SelectionCanceled(object sender, RoutedEventArgs e)
    {
        SelectControl.IsPickingRegion = false;
        _aborted.Invoke();
        Close();
    }
}