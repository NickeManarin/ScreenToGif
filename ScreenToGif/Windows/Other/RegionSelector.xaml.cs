using System;
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
        private Action _aborted;
        private double _scale = 1;

        public RegionSelector()
        {
            InitializeComponent();
        }

        public void Select(Monitor monitor, SelectControl2.ModeType mode, Rect previousRegion, Action<Rect> selected, Action<Monitor> changed, Action aborted)
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
            _aborted = aborted;

            SelectControl.BackImage = CaptureBackground();
            SelectControl.Mode = mode;

            if (mode == SelectControl2.ModeType.Region)
            {
                //Since each region selector is attached to a single screen, the selection must be translated.
                if (!previousRegion.IsEmpty)
                    previousRegion.Offset(monitor.Bounds.Left * -1, monitor.Bounds.Right * -1);

                SelectControl.Selected = previousRegion;
                SelectControl.Windows.Clear();
            }
            else if (mode == SelectControl2.ModeType.Window)
            {
                var win = Native.EnumerateWindows();

                //Take into consederation each screen dpi.
                //Adjust the position of the window.

                SelectControl.Windows = win.AdjustPosition(monitor.Bounds.Left, monitor.Bounds.Top);
            }

            //Call the selector to select the region.
            SelectControl.IsPickingRegion = true;
            Show();
        }

        public void ClearSelection()
        {
            SelectControl.Retry();
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

        private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs e)
        {
            SelectControl.IsPickingRegion = false;
            _selected.Invoke(SelectControl.Selected);
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