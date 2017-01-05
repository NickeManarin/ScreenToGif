using System;
using System.Windows;

namespace ScreenToGif.Controls
{
    public class GhostWindow : Window
    {
        #region Dependency Properties



        #endregion

        #region Properties



        #endregion

        static GhostWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GhostWindow), new FrameworkPropertyMetadata(typeof(GhostWindow)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            
        }

        //Start:
        //Fill all screens with 20% white background.
        //Change cursor, follow mouse (to display info about position and size)
        //When dragging, create punctured rect.
        //After releasing the mouse capture, show resize adorner and recording controls.
    }
}
