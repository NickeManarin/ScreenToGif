﻿using System;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Wraps a WinForm NotifyIcon and adds a ScreenToGif logo to it.
    /// </summary>
    internal class TrayIcon : IDisposable
    {
        #region Member variables

        public EventHandler NotifyIconClicked;

        private readonly NotifyIcon _notifyIcon = new NotifyIcon();

        #endregion Member variables

        #region Constructor

        public TrayIcon()
        {
            _notifyIcon.Icon = Resources.Logo;
            _notifyIcon.Click += OnNotifyIconClicked;
            _notifyIcon.Visible = false;
        }

        #endregion Constructor

        #region Public methods

        public void ShowTrayIcon()
        {
            _notifyIcon.Visible = true;
        }

        public void HideTrayIcon()
        {
            _notifyIcon.Visible = false;
        }

        public ContextMenu ContextMenu
        {
            get => _notifyIcon.ContextMenu; 
            set => _notifyIcon.ContextMenu = value;
        }

        #endregion Public methods

        #region Private methods

        private void OnNotifyIconClicked(object sender, EventArgs e)
        {
            if (null != NotifyIconClicked)
            {
                NotifyIconClicked(this, new EventArgs());
            }
        }

        #endregion Private methods

        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }
}