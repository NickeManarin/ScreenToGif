﻿using System;
using System.Diagnostics;
using System.Windows.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.UserControls
{
    public partial class ProgressPanel : UserControl
    {
        public ProgressPanel()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, $"Error while trying to navigate to a given URI: '{e?.Uri?.AbsoluteUri}'.");
            }
        }
    }
}