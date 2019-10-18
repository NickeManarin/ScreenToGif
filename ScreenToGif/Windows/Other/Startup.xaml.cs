﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Startup : INotification
    {
        public Startup()
        {
            InitializeComponent();
        }

        #region Events

        private void Startup_Initialized(object sender, EventArgs e)
        {
            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning())
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion
        }

        private void Startup_Loaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged += System_DisplaySettingsChanged;

            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning())
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion

            NotificationUpdated();
        }

        private void Update_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Global.UpdateModel != null;
        }

        private void Update_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            App.MainViewModel?.UpdateAction();
        }

        private void System_DisplaySettingsChanged(object sender, EventArgs e)
        {
            UpdatePositioning(false);
        }

        private void Startup_Closing(object sender, CancelEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= System_DisplaySettingsChanged;

            //Manually get the position/size of the window, so it's possible opening multiple instances.
            UserSettings.All.StartupTop = Top;
            UserSettings.All.StartupLeft = Left;
            UserSettings.All.StartupWidth = Width;
            UserSettings.All.StartupHeight = Height;
            UserSettings.All.StartupWindowState = WindowState;
            UserSettings.Save();
        }

        #endregion

        #region Methods

        private bool UpdatePositioning(bool onLoad = true)
        {
            var top = onLoad ? UserSettings.All.StartupTop : Top;
            var left = onLoad ? UserSettings.All.StartupLeft : Left;
            var width = onLoad ? UserSettings.All.StartupWidth : Width;
            var height = onLoad ? UserSettings.All.StartupHeight : Height;
            var state = onLoad ? UserSettings.All.StartupWindowState : WindowState;

            //If the position was never set, let it center on screen. 
            if (double.IsNaN(top) && double.IsNaN(left))
                return false;

            //The catch here is to get the closest monitor from current Top/Left point. 
            var monitors = Monitor.AllMonitorsScaled(this.Scale());
            var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary);

            if (closest == null)
                return false;

            //To much to the Left.
            if (closest.WorkingArea.Left > left + width - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > top + height - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < left + 100)
                left = closest.WorkingArea.Right - width;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < top + 100)
                top = closest.WorkingArea.Bottom - height;

            Top = top;
            Left = left;
            Width = width;
            Height = height;
            WindowState = state;

            return true;
        }

        public void NotificationUpdated()
        {
            if (Global.UpdateModel == null)
            {
                UpdateTextBlock.Visibility = Visibility.Collapsed;
                return;
            }

            UpdateRun.Text = string.Format(LocalizationHelper.Get("NewRelease") ?? "New release available • {0}", Global.UpdateModel.Version.ToStringShort());
            UpdateTextBlock.Visibility = Visibility.Visible;

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion
    }
}