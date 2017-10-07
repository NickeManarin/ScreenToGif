using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;
using ScreenToGif.Windows.Other;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using DialogResultWinForms = System.Windows.Forms.DialogResult;
using Label = System.Windows.Controls.Label;
using Localization = ScreenToGif.Windows.Other.Localization;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ScreenToGif.Windows
{
    public partial class Options : Window
    {
        #region Variables

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private List<DirectoryInfo> _folderList = new List<DirectoryInfo>();

        /// <summary>
        /// The file count of the Temp folder.
        /// </summary>
        private int _fileCount;

        #endregion

        public Options()
        {
            InitializeComponent();
        }

        #region App Settings

        private void ClickColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorSelector(UserSettings.All.ClickColor)
            {
                Owner = this
            };

            var result = colorDialog.ShowDialog();

            if (result.HasValue && result.Value)
                UserSettings.All.ClickColor = colorDialog.SelectedColor;
        }

        private void StartKeyBox_OnKeyChanged(object sender, KeyChangedEventArgs e)
        {
            if (UserSettings.All.StartPauseShortcut == UserSettings.All.StopShortcut && UserSettings.All.StartPauseModifiers == UserSettings.All.StopModifiers ||
                UserSettings.All.StartPauseShortcut == UserSettings.All.DiscardShortcut && UserSettings.All.StartPauseModifiers == UserSettings.All.DiscardModifiers)
            {
                UserSettings.All.StartPauseShortcut = e.PreviousKey;
                UserSettings.All.StartPauseModifiers = e.PreviousModifiers;
                e.Cancel = true;
            }
        }

        private void StopKeyBox_OnKeyChanged(object sender, KeyChangedEventArgs e)
        {
            if (UserSettings.All.StopShortcut == UserSettings.All.StartPauseShortcut && UserSettings.All.StopModifiers == UserSettings.All.StartPauseModifiers ||
                UserSettings.All.StopShortcut == UserSettings.All.DiscardShortcut && UserSettings.All.StopModifiers == UserSettings.All.DiscardModifiers)
            {
                UserSettings.All.StopShortcut = e.PreviousKey;
                UserSettings.All.StopModifiers = e.PreviousModifiers;
                e.Cancel = true;
            }
        }

        private void DiscardKeyBox_OnKeyChanged(object sender, KeyChangedEventArgs e)
        {
            if (UserSettings.All.DiscardShortcut == UserSettings.All.StartPauseShortcut && UserSettings.All.DiscardModifiers == UserSettings.All.StartPauseModifiers ||
                UserSettings.All.DiscardShortcut == UserSettings.All.StopShortcut && UserSettings.All.DiscardModifiers == UserSettings.All.StopModifiers)
            {
                UserSettings.All.DiscardShortcut = e.PreviousKey;
                UserSettings.All.DiscardModifiers = e.PreviousModifiers;
                e.Cancel = true;
            }
        }

        #endregion

        #region Interface

        private void InterfacePanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            //Editor
            GridWidthIntegerBox.Value = (int)UserSettings.All.GridSize.Width;
            GridHeightIntegerBox.Value = (int)UserSettings.All.GridSize.Height;

            CheckScheme(false);
            CheckSize(false);

            //Recorder
            CheckRecorderScheme(false);

            //Board
            //GridWidth2TextBox.Value = (int)Settings.Default.BoardGridSize.Width;
            //GridHeight2TextBox.Value = (int)Settings.Default.BoardGridSize.Height;

            //CheckBoardScheme(false);
            //CheckBoardSize(false);
        }

        private void ColorSchemesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckScheme();
        }

        private void ColorBorder_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            var color = ((SolidColorBrush)border.Background).Color;

            var colorPicker = new ColorSelector(color) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                border.Background = new SolidColorBrush(colorPicker.SelectedColor);

                if (border.Tag.Equals("Editor"))
                    CheckScheme(false);
                else if (border.Tag.Equals("Recorder"))
                    CheckRecorderScheme(false);
                else
                    CheckBoardScheme(false);
            }
        }

        private void BoardColorSchemesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckBoardScheme();
        }

        private void RecorderSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckRecorderScheme();
        }

        private void CheckScheme(bool schemePicked = true)
        {
            #region Colors

            var veryLightEven = Color.FromArgb(255, 245, 245, 245);
            var veryLightOdd = Color.FromArgb(255, 240, 240, 240);

            var lightEven = Color.FromArgb(255, 255, 255, 255);
            var lightOdd = Color.FromArgb(255, 211, 211, 211);

            var mediumEven = Color.FromArgb(255, 153, 153, 153);
            var mediumOdd = Color.FromArgb(255, 102, 102, 102);

            var darkEven = Color.FromArgb(255, 102, 102, 102);
            var darkOdd = Color.FromArgb(255, 51, 51, 51);

            #endregion

            if (schemePicked)
            {
                #region If ComboBox Selected

                switch (ColorSchemesComboBox.SelectedIndex)
                {
                    case 0:
                        EvenColorBorder.Background = new SolidColorBrush(veryLightEven);
                        OddColorBorder.Background = new SolidColorBrush(veryLightOdd);
                        break;
                    case 1:
                        EvenColorBorder.Background = new SolidColorBrush(lightEven);
                        OddColorBorder.Background = new SolidColorBrush(lightOdd);
                        break;
                    case 2:
                        EvenColorBorder.Background = new SolidColorBrush(mediumEven);
                        OddColorBorder.Background = new SolidColorBrush(mediumOdd);
                        break;
                    case 3:
                        EvenColorBorder.Background = new SolidColorBrush(darkEven);
                        OddColorBorder.Background = new SolidColorBrush(darkOdd);
                        break;
                }

                return;

                #endregion
            }

            #region If Color Picked

            var evenColor = ((SolidColorBrush)EvenColorBorder.Background).Color;
            var oddColor = ((SolidColorBrush)OddColorBorder.Background).Color;

            if (evenColor.Equals(veryLightEven) && oddColor.Equals(veryLightOdd))
            {
                ColorSchemesComboBox.SelectedIndex = 0;
            }
            else if (evenColor.Equals(lightEven) && oddColor.Equals(lightOdd))
            {
                ColorSchemesComboBox.SelectedIndex = 1;
            }
            else if (evenColor.Equals(mediumEven) && oddColor.Equals(mediumOdd))
            {
                ColorSchemesComboBox.SelectedIndex = 2;
            }
            else if (evenColor.Equals(darkEven) && oddColor.Equals(darkOdd))
            {
                ColorSchemesComboBox.SelectedIndex = 3;
            }
            else
            {
                ColorSchemesComboBox.SelectedIndex = 5;
            }

            #endregion
        }

        private void CheckRecorderScheme(bool schemePicked = true)
        {
            #region Colors

            var veryLightBack = Color.FromArgb(255, 255, 255, 255);
            var veryLightFore = Color.FromArgb(255, 0, 0, 0);

            var lightBack = Color.FromArgb(255, 245, 245, 245);
            var lightFore = Color.FromArgb(255, 0, 0, 0);

            var mediumBack = Color.FromArgb(255, 211, 211, 211);
            var mediumFore = Color.FromArgb(255, 0, 0, 0);

            #endregion

            if (schemePicked)
            {
                #region If ComboBox Selected

                switch (RecorderSchemeComboBox.SelectedIndex)
                {
                    case 0:
                        RecorderBackgroundBorder.Background = new SolidColorBrush(veryLightBack);
                        RecorderForegroundBorder.Background = new SolidColorBrush(veryLightFore);
                        break;
                    case 1:
                        RecorderBackgroundBorder.Background = new SolidColorBrush(lightBack);
                        RecorderForegroundBorder.Background = new SolidColorBrush(lightFore);
                        break;
                    case 2:
                        RecorderBackgroundBorder.Background = new SolidColorBrush(mediumBack);
                        RecorderForegroundBorder.Background = new SolidColorBrush(mediumFore);
                        break;
                }

                return;

                #endregion
            }

            #region If Color Picked

            var backColor = ((SolidColorBrush)RecorderBackgroundBorder.Background).Color;
            var foreColor = ((SolidColorBrush)RecorderForegroundBorder.Background).Color;

            if (backColor.Equals(veryLightBack) && foreColor.Equals(veryLightFore))
            {
                RecorderSchemeComboBox.SelectedIndex = 0;
            }
            else if (backColor.Equals(lightBack) && foreColor.Equals(lightFore))
            {
                RecorderSchemeComboBox.SelectedIndex = 1;
            }
            else if (backColor.Equals(mediumBack) && foreColor.Equals(mediumFore))
            {
                RecorderSchemeComboBox.SelectedIndex = 2;
            }
            else
            {
                RecorderSchemeComboBox.SelectedIndex = 4;
            }

            #endregion
        }

        private void CheckBoardScheme(bool schemePicked = true)
        {
            #region Colors

            var background = Color.FromArgb(255, 255, 255, 255);
            var veryLightEven = Color.FromArgb(255, 255, 255, 255);
            var veryLightOdd = Color.FromArgb(255, 255, 255, 255);

            var lightEven = Color.FromArgb(255, 211, 211, 211);
            var lightOdd = Color.FromArgb(255, 211, 211, 211);

            var mediumEven = Color.FromArgb(255, 102, 102, 102);
            var mediumOdd = Color.FromArgb(255, 102, 102, 102);

            var darkEven = Color.FromArgb(255, 51, 51, 51);
            var darkOdd = Color.FromArgb(255, 51, 51, 51);

            #endregion

            //if (schemePicked)
            //{
            //    #region If ComboBox Selected

            //    switch (ColorSchemes2ComboBox.SelectedIndex)
            //    {
            //        case 0:
            //            BackgroundBorder.Background = new SolidColorBrush(background);
            //            EvenColor2Border.Background = new SolidColorBrush(veryLightEven);
            //            OddColor2Border.Background = new SolidColorBrush(veryLightOdd);
            //            break;
            //        case 1:
            //            BackgroundBorder.Background = new SolidColorBrush(background);
            //            EvenColor2Border.Background = new SolidColorBrush(lightEven);
            //            OddColor2Border.Background = new SolidColorBrush(lightOdd);
            //            break;
            //        case 2:
            //            BackgroundBorder.Background = new SolidColorBrush(background);
            //            EvenColor2Border.Background = new SolidColorBrush(mediumEven);
            //            OddColor2Border.Background = new SolidColorBrush(mediumOdd);
            //            break;
            //        case 3:
            //            BackgroundBorder.Background = new SolidColorBrush(background);
            //            EvenColor2Border.Background = new SolidColorBrush(darkEven);
            //            OddColor2Border.Background = new SolidColorBrush(darkOdd);
            //            break;
            //    }

            //    return;

            //    #endregion
            //}

            //#region If Color Picked

            //var backColor = ((SolidColorBrush)BackgroundBorder.Background).Color;
            //var evenColor = ((SolidColorBrush)EvenColor2Border.Background).Color;
            //var oddColor = ((SolidColorBrush)OddColor2Border.Background).Color;

            //if (!backColor.Equals(background))
            //{
            //    ColorSchemes2ComboBox.SelectedIndex = 5;
            //}
            //else if (evenColor.Equals(veryLightEven) && oddColor.Equals(veryLightOdd))
            //{
            //    ColorSchemes2ComboBox.SelectedIndex = 0;
            //}
            //else if (evenColor.Equals(lightEven) && oddColor.Equals(lightOdd))
            //{
            //    ColorSchemes2ComboBox.SelectedIndex = 1;
            //}
            //else if (evenColor.Equals(mediumEven) && oddColor.Equals(mediumOdd))
            //{
            //    ColorSchemes2ComboBox.SelectedIndex = 2;
            //}
            //else if (evenColor.Equals(darkEven) && oddColor.Equals(darkOdd))
            //{
            //    ColorSchemes2ComboBox.SelectedIndex = 3;
            //}
            //else
            //{
            //    ColorSchemes2ComboBox.SelectedIndex = 5;
            //}

            //#endregion
        }

        #region Grid Size

        private void GridSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = sender as ComboBox;

            if (source?.Tag.Equals("Editor") ?? true)
                CheckSize();
            else
                CheckBoardSize();
        }

        private void GridSizeBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GridSizeContextMenu.PlacementTarget = GridSizeBorder;
            GridSizeContextMenu.IsOpen = true;
        }

        private void GridSize2Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //GridSize2ContextMenu.PlacementTarget = GridSize2Border;
            //GridSize2ContextMenu.IsOpen = true;
        }

        private void CheckSize(bool sizePicked = true)
        {
            if (sizePicked)
            {
                #region If ComboBox Selected

                switch (GridSizeComboBox.SelectedIndex)
                {
                    case 0:
                        UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(10, 10));
                        break;
                    case 1:
                        UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(15, 15));
                        break;
                    case 2:
                        UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(20, 20));
                        break;
                    case 3:
                        UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(25, 25));
                        break;
                    case 4:
                        UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(30, 30));
                        break;
                    case 5:
                        UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(50, 50));
                        break;
                    case 6:
                        UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(100, 100));
                        break;
                }

                return;

                #endregion
            }

            #region If Settings Loaded

            double sizeW = UserSettings.All.GridSize.Width;
            double sizeH = UserSettings.All.GridSize.Height;

            if (sizeW != sizeH)
            {
                GridSizeComboBox.SelectedIndex = 8;
                return;
            }

            if (sizeW == 10)
            {
                GridSizeComboBox.SelectedIndex = 0;
            }
            else if (sizeW == 15)
            {
                GridSizeComboBox.SelectedIndex = 1;
            }
            else if (sizeW == 20)
            {
                GridSizeComboBox.SelectedIndex = 2;
            }
            else if (sizeW == 25)
            {
                GridSizeComboBox.SelectedIndex = 3;
            }
            else if (sizeW == 30)
            {
                GridSizeComboBox.SelectedIndex = 4;
            }
            else if (sizeW == 50)
            {
                GridSizeComboBox.SelectedIndex = 5;
            }
            else if (sizeW == 100)
            {
                GridSizeComboBox.SelectedIndex = 6;
            }
            else
            {
                GridSizeComboBox.SelectedIndex = 8;
            }

            #endregion
        }

        private void GridSizeIntegerBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as IntegerBox;

            if (textBox == null)
                return;

            if (textBox.Value < 1)
                textBox.Text = "10";

            if (string.Equals("Editor", textBox.Tag))
                AdjustToSize();
            else
                AdjustToSizeBoard();
        }

        private void GridSizeIntegerBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as IntegerBox;

            if (textBox == null)
                return;

            if (string.Equals("Editor", textBox.Tag))
                AdjustToSize();
            else
                AdjustToSizeBoard();
        }

        private void AdjustToSize()
        {
            try
            {
                UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(GridWidthIntegerBox.Value, GridHeightIntegerBox.Value));

                CheckSize(false);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Adjusting the Grid Size");
            }
        }

        private void CheckBoardSize(bool sizePicked = true)
        {
            //if (sizePicked)
            //{
            //    #region If ComboBox Selected

            //    switch (GridSize2ComboBox.SelectedIndex)
            //    {
            //        case 0:
            //            Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(10, 10));
            //            break;
            //        case 1:
            //            Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(15, 15));
            //            break;
            //        case 2:
            //            Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(20, 20));
            //            break;
            //        case 3:
            //            Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(25, 25));
            //            break;
            //        case 4:
            //            Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(30, 30));
            //            break;
            //        case 5:
            //            Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(50, 50));
            //            break;
            //        case 6:
            //            Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(100, 100));
            //            break;
            //    }

            //    return;

            //    #endregion
            //}

            //#region If Settings Loaded

            //double sizeW = Settings.Default.BoardGridSize.Width;
            //double sizeH = Settings.Default.BoardGridSize.Height;

            //if (sizeW != sizeH)
            //{
            //    GridSize2ComboBox.SelectedIndex = 8;
            //    return;
            //}

            //if (sizeW == 10)
            //{
            //    GridSize2ComboBox.SelectedIndex = 0;
            //}
            //else if (sizeW == 15)
            //{
            //    GridSize2ComboBox.SelectedIndex = 1;
            //}
            //else if (sizeW == 20)
            //{
            //    GridSize2ComboBox.SelectedIndex = 2;
            //}
            //else if (sizeW == 25)
            //{
            //    GridSize2ComboBox.SelectedIndex = 3;
            //}
            //else if (sizeW == 30)
            //{
            //    GridSize2ComboBox.SelectedIndex = 4;
            //}
            //else if (sizeW == 50)
            //{
            //    GridSize2ComboBox.SelectedIndex = 5;
            //}
            //else if (sizeW == 100)
            //{
            //    GridSize2ComboBox.SelectedIndex = 6;
            //}
            //else
            //{
            //    GridSize2ComboBox.SelectedIndex = 8;
            //}

            //#endregion
        }

        private void AdjustToSizeBoard()
        {
            try
            {
                //Settings.Default.BoardGridSize = new Rect(new Point(0, 0), new Point(GridWidth2TextBox.Value, GridHeight2TextBox.Value));

                CheckSize(false);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Adjusting the Grid Size");
            }
        }

        #endregion

        #endregion

        #region Language

        private void LanguagePanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            try
            {
                LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);
            }
            catch (Exception ex)
            {
                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error while stopping", ex.Message, ex);
                LogWriter.Log(ex, "Error while trying to set the language.");
            }
        }

        private void TranslateHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/NickeManarin/ScreenToGif/wiki/Localization");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open the latest resource available");
            }
        }

        private void TranslateOfflineHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                Filter = "Resource Dictionary (*.xaml)|*.xaml",
                Title = "Save Resource Dictionary",
                FileName = "StringResources.en"
            };

            var result = sfd.ShowDialog();

            if (result.HasValue && result.Value)
                LocalizationHelper.SaveDefaultResource(sfd.FileName);
        }

        private void ImportHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var local = new Localization { Owner = this };
            local.ShowDialog();
        }

        private void EmailHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("mailto:nicke@outlook.com.br");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open MailTo");
            }
        }

        #endregion

        #region Temp Files

        private void ChooseLogsLocation_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog { ShowNewFolderButton = true };

            if (!string.IsNullOrWhiteSpace(UserSettings.All.LogsFolder))
                folderDialog.SelectedPath = UserSettings.All.LogsFolder;

            if (folderDialog.ShowDialog() == DialogResultWinForms.OK)
                UserSettings.All.LogsFolder = folderDialog.SelectedPath;
        }

        private void ChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog { ShowNewFolderButton = true };

            if (!string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                folderDialog.SelectedPath = UserSettings.All.TemporaryFolder;

            if (folderDialog.ShowDialog() == DialogResultWinForms.OK)
                UserSettings.All.TemporaryFolder = folderDialog.SelectedPath;
        }

        #region Async

        private delegate void TempDelegate(DependencyPropertyChangedEventArgs e);

        private TempDelegate _tempDel;

        private void CheckTemp(DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue) return;

            _folderList = new List<DirectoryInfo>();

            var path = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording");

            if (!Directory.Exists(path)) return;

            _folderList = Directory.GetDirectories(path).Select(x => new DirectoryInfo(x)).ToList();

            _fileCount = _folderList.Sum(folder => Directory.EnumerateFiles(folder.FullName).Count());
        }

        private void CheckTempCallBack(IAsyncResult r)
        {
            try
            {
                //Error: It may throw an error when BeginInvoke before the end of the last one.
                _tempDel.EndInvoke(r);

                Dispatcher.Invoke(() =>
                {
                    FolderCountLabel.Text = _folderList.Count.ToString("###,###");
                    FileCountLabel.Text = _fileCount.ToString("###,###");
                    ClearTempButton.IsEnabled = _folderList.Any();
                });
            }
            catch (Exception)
            { }
        }

        #endregion

        private void TempPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (TempPanel.Visibility != Visibility.Visible)
                return;

            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                UserSettings.All.TemporaryFolder = Path.GetTempPath();

            _tempDel = CheckTemp;
            _tempDel.BeginInvoke(e, CheckTempCallBack, null);

            #region Settings

            //Paths.
            AppDataPathTextBlock.Text = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");
            LocalPathTextBlock.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            //Remove all text decorations (Strikethrough).
            AppDataPathTextBlock.TextDecorations.Clear();
            LocalPathTextBlock.TextDecorations.Clear();

            //Clear the tooltips.
            AppDataPathTextBlock.ClearValue(ToolTipProperty);
            LocalPathTextBlock.ClearValue(ToolTipProperty);

            //AppData.
            if (!File.Exists(AppDataPathTextBlock.Text))
            {
                AppDataPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough,
                    new Pen(Brushes.DarkSlateGray, 1),
                    0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

                AppDataPathTextBlock.SetResourceReference(ToolTipProperty, "TempFiles.NotExists");
            }

            //Local.
            if (!File.Exists(LocalPathTextBlock.Text))
            {
                LocalPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough,
                    new Pen(Brushes.DarkSlateGray, 1),
                    0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

                LocalPathTextBlock.SetResourceReference(ToolTipProperty, "TempFiles.NotExists");
            }

            #endregion
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Process.Start(path);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while trying to open the Temp Folder.");
            }
        }

        private async void ClearTempButton_Click(object sender, RoutedEventArgs e)
        {
            ClearTempButton.IsEnabled = false;

            try
            {
                var path = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording");

                if (!Directory.Exists(path))
                {
                    _folderList.Clear();
                    FolderCountLabel.Text = "0";
                    FileCountLabel.Text = "0";
                    return;
                }

                #region Update the Information

                //Directory.GetDirectories(Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording")).Select(s => new DirectoryInfo(s)).Where(w => (DateTime.Now - w.CreationTime).Days > 5).ToList();
                _folderList = await Task.Factory.StartNew(() => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Select(x => new DirectoryInfo(x)).ToList());

                #endregion

                if (Dialog.Ask("ScreenToGif", FindResource("TempFiles.KeepRecent") as string, FindResource("TempFiles.KeepRecent.Info") as string))
                    _folderList = await Task.Factory.StartNew(() => _folderList.Where(w => (DateTime.Now - w.CreationTime).Days > 5).ToList());

                foreach (var folder in _folderList)
                {
                    //var project = Path.Combine(folder.FullName, "Project.json");

                    //if (File.Exists(project) && (new FileInfo(project).LastWriteTime - DateTime.Now).Days < 5)
                    //    continue;

                    Directory.Delete(folder.FullName, true);
                }

                #region Update the Information

                _folderList = Directory.GetDirectories(path).Select(x => new DirectoryInfo(x)).ToList();

                #endregion
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while cleaning the Temp folder");
            }

            FolderCountLabel.Text = _folderList.Count.ToString("###,###");
            FileCountLabel.Text = _folderList.Sum(folder => Directory.EnumerateFiles(folder.FullName).Count()).ToString("###,###");
            ClearTempButton.IsEnabled = _folderList.Any();
        }


        private void CreateLocalSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !File.Exists(LocalPathTextBlock.Text);
        }

        private void RemoveLocalSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = File.Exists(LocalPathTextBlock.Text);
        }

        private void RemoveAppDataSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = File.Exists(AppDataPathTextBlock.Text);
        }

        private void CreateLocalSettings_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                UserSettings.CreateLocalSettings();

                LocalPathTextBlock.TextDecorations.Clear();
                LocalPathTextBlock.ClearValue(ToolTipProperty);
            }
            catch (Exception ex)
            {
                Dialog.Ok("Create Local Settings", "Impossible to create local settings", ex.Message);
            }
        }

        private void RemoveLocalSettings_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                UserSettings.RemoveLocalSettings();

                LocalPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough,
                    new Pen(Brushes.DarkSlateGray, 1), 0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

                LocalPathTextBlock.SetResourceReference(ToolTipProperty, "TempFiles.NotExists");
            }
            catch (Exception ex)
            {
                Dialog.Ok("Remove Local Settings", "Impossible to remove local settings", ex.Message);
            }
        }

        private void RemoveAppDataSettings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                UserSettings.RemoveAppDataSettings();

                AppDataPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough,
                    new Pen(Brushes.DarkSlateGray, 1), 0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

                AppDataPathTextBlock.SetResourceReference(ToolTipProperty, "TempFiles.NotExists");
            }
            catch (Exception ex)
            {
                Dialog.Ok("Remove AppData Settings", "Impossible to remove AppData settings", ex.Message);
            }
        }

        #endregion

        #region Extras

        private void SelectFfmpeg_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                FileName = "ffmpeg.exe",
                Filter = "FFmpeg executable (*.exe)|*.exe",
                Title = FindResource("Extras.FfmpegLocation.Select") as string
            };

            //TODO: Localize.

            //Current location.
            if (!string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation) && !UserSettings.All.FfmpegLocation.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            {
                var directory = Path.GetDirectoryName(UserSettings.All.FfmpegLocation);

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    ofd.InitialDirectory = directory;
            }

            if (ofd.ShowDialog(this).Value)
                UserSettings.All.FfmpegLocation = ofd.FileName;
        }

        #endregion

        #region Donate

        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Openning the Donation website");

                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error openning the donation website", ex.Message, ex);
            }
        }

        private void DonateEuroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Openning the Donation website");

                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error openning the donation website", ex.Message, ex);
            }
        }

        private void DonateOtherButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var label = CurrencyComboBox.SelectedValue as Label;

                var currency = label?.Content.ToString().Substring(0, 3) ?? "USD";

                Process.Start($"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code={currency}&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Openning the Donation website");

                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error openning the donation website", ex.Message, ex);
            }
        }

        private void PatreonHyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://www.patreon.com/nicke");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Openning the Patreon website");

                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error openning the patreon website", ex.Message, ex);
            }
        }

        private void SteamHyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://steamcommunity.com/id/nickesm/wishlist");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Openning the Steam website");

                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error openning the steam website", ex.Message, ex);
            }
        }

        private void BitcoinHyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://www.coinbase.com/nicke");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Openning the CoinBase website");

                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error openning the coinbase website", ex.Message, ex);
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
                LogWriter.Log(ex, "Open Hyperlink");
            }
        }

        #endregion

        #region Other

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UserSettings.Save();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            UserSettings.Save();

            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        #endregion
    }
}