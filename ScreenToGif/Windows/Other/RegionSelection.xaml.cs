using System;
using System.Windows;
using ScreenToGif.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class RegionSelection : Window
    {
        public Rect Rect { get; set; }

        public RegionSelection()
        {
            InitializeComponent();
        }

        public void Select(Rect region)
        {
            //Resize to fit given window.
            Left = region.Left;
            Top = region.Top;
            Width = region.Width;
            Height = region.Height;

            Show();
        } 

        public void ClearSelection()
        {
            Close();
        }
    }
}