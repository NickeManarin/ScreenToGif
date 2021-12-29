using System;
using System.Windows;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Windows.Other;

public partial class CacheDialog : Window
{
    #region Properties

    private bool DisplayOptions { get; set; }
    
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
        if (!DisplayOptions)
        {
            OptionsGrid.Visibility = Visibility.Collapsed;
            return;
        }

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

    /// <summary>
    /// Shows a Yes/No dialog.
    /// </summary>
    /// <param name="displayOptions">True if the options should be displayed.</param>
    /// <param name="ignoreRecent">True if the recent projects should be ignored.</param>
    /// <returns>True if Yes</returns>
    public static bool Ask(bool displayOptions, out bool ignoreRecent)
    {
        var dialog = new CacheDialog
        {
            DisplayOptions = displayOptions
        };
        var result = dialog.ShowDialog();
        ignoreRecent = dialog.IgnoreRecent;

        return result.HasValue && result.Value;
    }
}