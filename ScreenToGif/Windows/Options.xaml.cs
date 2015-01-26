using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using ScreenToGif.Util.Writers;
using Application = System.Windows.Application;
using Path = System.IO.Path;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        #region Variables

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private readonly string _pathTemp = Path.GetTempPath() + @"ScreenToGif\Recording\";

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private List<string> _listFolders = new List<string>();

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Options()
        {
            InitializeComponent();
        }

        #region Gif Settings

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ImageButton_Click(null, null);
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorSelector(Properties.Settings.Default.TransparentColor, false);
            colorDialog.Owner = this;
            var result = colorDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Properties.Settings.Default.TransparentColor = colorDialog.SelectedColor;
            }
        }

        #endregion

        #region About

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                //TODO:
            }
        }

        #endregion

        #region Temp Files

        private void TempPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                _listFolders = new List<string>();

                if (Directory.Exists(_pathTemp))
                {
                    var date = new DateTime();
                    _listFolders = Directory.GetDirectories(_pathTemp).Where(x =>
                        x.Split('\\').Last().Length == 19 && DateTime.TryParse(x.Split('\\').Last().Substring(0, 10), out date)).ToList();
                }

                FolderCountLabel.Content = _listFolders.Count();
                FileCountLabel.Content = _listFolders.Sum(folder => Directory.EnumerateFiles(folder).Count());
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_pathTemp))
                {
                    Directory.CreateDirectory(_pathTemp);
                }

                Process.Start(_pathTemp);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while trying to open the Temp Folder.");
            }
        }

        private void ClearTempButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_pathTemp))
                {
                    _listFolders.Clear();
                    FolderCountLabel.Content = _listFolders.Count;
                    return;
                }

                #region Update the Information

                var date = new DateTime();
                _listFolders = Directory.GetDirectories(_pathTemp).Where(x =>
                    x.Split('\\').Last().Length == 19 && DateTime.TryParse(x.Split('\\').Last().Substring(0, 10), out date)).ToList();

                FolderCountLabel.Content = _listFolders.Count;

                #endregion

                foreach (string folder in _listFolders)
                {
                    //TODO: Detects if there is a STG instance using one of this folders...
                    Directory.Delete(folder, true);
                }

                #region Update the Information

                _listFolders = Directory.GetDirectories(_pathTemp).Where(x =>
                    x.Split('\\').Last().Length == 19 && DateTime.TryParse(x.Split('\\').Last().Substring(0, 10), out date)).ToList();

                FolderCountLabel.Content = _listFolders.Count;
                FileCountLabel.Content = _listFolders.Sum(folder => Directory.EnumerateFiles(folder).Count());

                #endregion
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while cleaning Temp");
            }
        }

        #endregion

        #region Other

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //TODO: Load all settings.
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();

            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Save all settings.

            Properties.Settings.Default.Save();
        }

        #endregion
    }
}
