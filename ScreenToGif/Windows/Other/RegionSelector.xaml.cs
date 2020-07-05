using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class RegionSelector : Window
    {
        public Monitor Monitor { get; set; }

        private Action<Rect> _selected;
        private Action<Monitor> _changed;
        private Action<Monitor> _gotHover;
        private Action _aborted;
        private double _scale = 1;


        public RegionSelector()
        {
            InitializeComponent();
        }


        public void Select(Monitor monitor, SelectControl.ModeType mode, Rect previousRegion, Action<Rect> selected, Action<Monitor> changed, Action<Monitor> gotHover, Action aborted)
        {
            //Resize to fit given window.
            Left = monitor.Bounds.Left;
            Top = monitor.Bounds.Top;
            Width = monitor.Bounds.Width;
            Height = monitor.Bounds.Height;

            Monitor = monitor;

            _scale = monitor.Dpi / 96d;
            _selected = selected;
            _changed = changed;
            _gotHover = gotHover;
            _aborted = aborted;

            SelectControl.Scale = monitor.Scale;
            SelectControl.ParentLeft = Left;
            SelectControl.ParentTop = Top;
            SelectControl.BackImage = CaptureBackground();
            SelectControl.Mode = mode;

            if (mode == Controls.SelectControl.ModeType.Region)
            {
                //Since each region selector is attached to a single screen, the selection must be translated.
                SelectControl.Selected = previousRegion.Translate(monitor.Bounds.Left * -1, monitor.Bounds.Top * -1);
                SelectControl.Windows.Clear();
            }
            else if (mode == Controls.SelectControl.ModeType.Window)
            {
                //Get only the windows that are located inside the given screen.
                var win = Native.EnumerateWindowsByMonitor(monitor);

                //Since each region selector is attached to a single screen, the list of positions must be translated.
                SelectControl.Windows = win.AdjustPosition(monitor.Bounds.Left, monitor.Bounds.Top);
            }
            else if (mode == Controls.SelectControl.ModeType.Fullscreen)
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
                var source = Dispatcher?.Invoke(() => PresentationSource.FromVisual(this));

                if (source?.CompositionTarget != null)
                    return Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);
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

        private BitmapSource CaptureBackground()
        {
            //A 7 pixel border is added to allow the crop by the magnifying glass.
            return Native.CaptureBitmapSource((int)Math.Round((Width + 14) * _scale), (int)Math.Round((Height + 14) * _scale),
                (int)Math.Round((Left - 7) * _scale), (int)Math.Round((Top - 7) * _scale));
        }


        private void SelectControl_MouseHovering(object sender, RoutedEventArgs e)
        {
            _gotHover.Invoke(Monitor);
        }

        private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs e)
        {
            SelectControl.IsPickingRegion = false;
            _selected.Invoke(SelectControl.Selected.Translate(Monitor.Bounds.Left, Monitor.Bounds.Top));
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
}