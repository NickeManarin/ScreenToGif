using System;
using System.Windows;
using ScreenToGif.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class RegionSelection : Window
    {
        #region Properties

        public Rect Rect { get; set; }

        public double Dpi { get; set; }

        public double Scale { get; set; }

        #endregion

        public RegionSelection()
        {
            InitializeComponent();
        }

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            Scale = e.NewDpi.DpiScaleX;
            Dpi = e.NewDpi.PixelsPerInchX;
        }


        public void Select(Rect region, Monitor monitor = null, bool isFullscreen = false)
        {
            //When the region switches monitors, move the selection to the new monitor, so that the scale of the UI changes.
            //This solves the issue where the UI would move to the wrong position.
            if (monitor != null)
            {
                Left = monitor.Bounds.Left;
                Top = monitor.Bounds.Top;
            }

            //Resize to fit given window.
            Left = region.Left;
            Top = region.Top;
            Width = region.Width;
            Height = region.Height;

            Show();
            
            Scale = this.Scale();
            Dpi = Scale * 96d;
            Opacity = isFullscreen ? 0 : 1;
        } 

        public void ClearSelection()
        {
            Close();
        }
    }
}