using System;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class CacheDialog : Window
    {
        #region Properties

        /// <summary>
        /// True if the removal process should ignore recent projects.
        /// </summary>
        public bool IgnoreRecent { get; set; }

        #endregion


        public CacheDialog()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayInfo();
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            DisplayInfo();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            IgnoreRecent = IgnoreRecentCheckBox.IsChecked == true;
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            DialogResult = false;
        }


        private void DisplayInfo()
        {
            InfoTextBlock.Text = IgnoreRecentCheckBox.IsChecked == true ? 
                LocalizationHelper.GetWithFormat("S.Options.Storage.Cache.IgnoreRecent.Yes", "Only the projects older than {0} days, that are not currently in use, will be removed.", UserSettings.All.AutomaticCleanUpDays) : 
                LocalizationHelper.Get("S.Options.Storage.Cache.IgnoreRecent.No");
        }
    }
}