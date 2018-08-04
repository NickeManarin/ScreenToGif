using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using ScreenToGif.Cloud.Imgur;
using ScreenToGif.Controls;
using ScreenToGif.Model;
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
    public partial class Options : Window, INotification
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

        /// <summary>
        /// .
        /// </summary>
        private ObservableCollection<DefaultTaskModel> _effectList;

        #endregion

        public Options()
        {
            InitializeComponent();

#if UWP
                PaypalLabel.Visibility = Visibility.Collapsed;
#endif
        }

        public Options(int index)
        {
            InitializeComponent();

            SelectTab(index);

#if UWP
                PaypalLabel.Visibility = Visibility.Collapsed;
#endif
        }

        #region App Settings

        private void NotificationIconCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (App.NotifyIcon != null)
                App.NotifyIcon.Visibility = UserSettings.All.ShowNotificationIcon ? Visibility.Visible : Visibility.Collapsed;
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

            var sizeW = UserSettings.All.GridSize.Width;
            var sizeH = UserSettings.All.GridSize.Height;

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

        #region Automated Tasks

        private void TasksPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var list = UserSettings.All.AutomatedTasksList?.Cast<DefaultTaskModel>().ToList() ?? new List<DefaultTaskModel>();

            TasksDataGrid.ItemsSource = _effectList = new ObservableCollection<DefaultTaskModel>(list);
        }

        private void MoveUp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DefaultsPanel.IsVisible && TasksDataGrid.SelectedIndex > 0;
        }

        private void MoveDown_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DefaultsPanel.IsVisible && TasksDataGrid.SelectedIndex < TasksDataGrid.Items.Count - 1;
        }

        private void Remove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DefaultsPanel.IsVisible && TasksDataGrid.SelectedIndex != -1;
        }

        private void Add_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DefaultsPanel.IsVisible;
        }

        private void MoveUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedIndex = TasksDataGrid.SelectedIndex;
            var selected = _effectList[selectedIndex];

            _effectList.RemoveAt(selectedIndex);
            _effectList.Insert(selectedIndex - 1, selected);
            TasksDataGrid.SelectedItem = selected;

            UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
        }

        private void MoveDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedIndex = TasksDataGrid.SelectedIndex;
            var selected = _effectList[selectedIndex];

            _effectList.RemoveAt(selectedIndex);
            _effectList.Insert(selectedIndex + 1, selected);
            TasksDataGrid.SelectedItem = selected;

            UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
        }

        private void Add_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var eff = new AutomatedTask();
            var result = eff.ShowDialog();

            if (result != true)
                return;

            _effectList.Add(eff.CurrentTask);
            UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
        }

        private void Edit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var index = TasksDataGrid.SelectedIndex;
            var selected = _effectList[TasksDataGrid.SelectedIndex].ShallowCopy();

            var eff = new AutomatedTask { CurrentTask = selected, IsEditing = true };
            var result = eff.ShowDialog();

            if (result != true)
                return;

            _effectList[TasksDataGrid.SelectedIndex] = eff.CurrentTask;
            TasksDataGrid.Items.Refresh();
            TasksDataGrid.SelectedIndex = index;

            UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
        }

        private void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var index = TasksDataGrid.SelectedIndex;
            _effectList.RemoveAt(TasksDataGrid.SelectedIndex);

            //Automatically selects the closest item from the position of the one that was removed.
            TasksDataGrid.SelectedIndex = _effectList.Count == 0 ? -1 : _effectList.Count <= index ? _effectList.Count - 1 : index;

            UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
        }

        private void TasksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EditCommandBinding.Command.CanExecute(sender))
                EditCommandBinding.Command.Execute(sender);
        }

        private void TasksDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && EditCommandBinding.Command.CanExecute(sender))
            {
                EditCommandBinding.Command.Execute(sender);
                e.Handled = true;
            }

            //if (e.Key == Key.Space)
            //{
            //    var selected = TasksDataGrid.SelectedItem as DefaultTaskModel;

            //    if (selected != null)
            //    {
            //        selected.CanUndo = !selected.CanUndo;
            //        e.Handled = true;

            //        UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
            //    }
            //}
        }

        private void ExtendedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
        }

        #endregion

        #region Shortcuts

        private void ShortcutsPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Global.IgnoreHotKeys = ShortcutsPanel.IsVisible;
        }

        private void Globals_OnKeyChanged(object sender, KeyChangedEventArgs e)
        {
            Recorders_OnKeyChanged(sender, e);

            if (e.Cancel)
                return;

            //Unregister old shortcut.
            HotKeyCollection.Default.Remove(e.PreviousModifiers, e.PreviousKey);

            //Registers all shortcuts and updates the input gesture text.
            App.RegisterShortcuts();
        }

        private void Recorders_OnKeyChanged(object sender, KeyChangedEventArgs e)
        {
            if (!(sender is KeyBox box))
                return;

            var list = new List<Tuple<Key, ModifierKeys>>
            {
                new Tuple<Key, ModifierKeys>(UserSettings.All.RecorderShortcut, UserSettings.All.RecorderModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.BoardRecorderShortcut, UserSettings.All.BoardRecorderModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.WebcamRecorderShortcut, UserSettings.All.WebcamRecorderModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.EditorShortcut, UserSettings.All.EditorModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.OptionsShortcut, UserSettings.All.OptionsModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.ExitShortcut, UserSettings.All.ExitModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.StartPauseShortcut, UserSettings.All.StartPauseModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.StopShortcut, UserSettings.All.StopModifiers),
                new Tuple<Key, ModifierKeys>(UserSettings.All.DiscardShortcut, UserSettings.All.DiscardModifiers)
            };

            //If this new shortcut is already in use.
            if (box.MainKey != Key.None && list.Count(c => c.Item1 == box.MainKey && c.Item2 == box.ModifierKeys) > 1)
            {
                box.MainKey = e.PreviousKey;
                box.ModifierKeys = e.PreviousModifiers;
                e.Cancel = true;
            }
        }

        #endregion

        #region Language

        private void LanguagePanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //To avoid being called during startup of the window and to avoid being called twice after selection changes.
            if (!IsLoaded || e.AddedItems.Count == 0) return;

            try
            {
                LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);
            }
            catch (Exception ex)
            {
                ErrorDialog.Ok(LocalizationHelper.Get("Title.Options"), "Error while stopping", ex.Message, ex);
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

        private void TempPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (TempPanel.Visibility != Visibility.Visible)
                return;

            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                UserSettings.All.TemporaryFolder = Path.GetTempPath();

            _tempDel = CheckTemp;
            _tempDel.BeginInvoke(e, CheckTempCallBack, null);

            NotificationUpdated();

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
                AppDataPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough, new Pen(Brushes.DarkSlateGray, 1),
                    0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

                AppDataPathTextBlock.SetResourceReference(ToolTipProperty, "TempFiles.NotExists");
            }

            //Local.
            if (!File.Exists(LocalPathTextBlock.Text))
            {
                LocalPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough, new Pen(Brushes.DarkSlateGray, 1),
                    0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

                LocalPathTextBlock.SetResourceReference(ToolTipProperty, "TempFiles.NotExists");
            }

            #endregion
        }

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
                    TempSeparator.TextRight = LocalizationHelper.Get("TempFiles.FilesAndFolders.None");
                    return;
                }

                _folderList = await Task.Factory.StartNew(() => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Select(x => new DirectoryInfo(x)).ToList());

                if (Dialog.Ask("ScreenToGif", LocalizationHelper.Get("TempFiles.KeepRecent"), LocalizationHelper.Get("TempFiles.KeepRecent.Info")))
                    _folderList = await Task.Factory.StartNew(() => _folderList.Where(w => (DateTime.Now - w.CreationTime).Days > (UserSettings.All.AutomaticCleanUpDays > 0 ? UserSettings.All.AutomaticCleanUpDays : 5)).ToList());

                foreach (var folder in _folderList)
                {
                    if (MutexList.IsInUse(folder.Name))
                        continue;

                    Directory.Delete(folder.FullName, true);
                }

                _folderList = await Task.Factory.StartNew(() => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Select(x => new DirectoryInfo(x)).ToList());
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while cleaning the Temp folder");
            }
            finally
            {
                App.MainViewModel.CheckDiskSpace();
            }

            TempSeparator.TextRight = string.Format(LocalizationHelper.Get("TempFiles.FilesAndFolders.Count") ?? "{0} folders and {1} files", _folderList.Count.ToString("##,##0"),
                _folderList.Sum(folder => Directory.EnumerateFiles(folder.FullName).Count()).ToString("##,##0"));

            ClearTempButton.IsEnabled = _folderList.Any();
        }


        private void CreateLocalSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLoaded && !File.Exists(LocalPathTextBlock.Text);
        }

        private void RemoveLocalSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLoaded && File.Exists(LocalPathTextBlock.Text);
        }

        private void RemoveAppDataSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLoaded && File.Exists(AppDataPathTextBlock.Text);
        }

        private void OpenAppDataSettings_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                    Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml"));
                else
                    Process.Start("explorer.exe", $"/select,\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml")}\"");
            }
            catch (Exception ex)
            {
                Dialog.Ok("Open AppData Settings Folder", "Impossible to open where the AppData settings is located", ex.Message);
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

        private void OpenLocalSettings_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                    Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml"));
                else
                    Process.Start("explorer.exe", $"/select,\"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml")}\"");
            }
            catch (Exception ex)
            {
                Dialog.Ok("Open AppData Local Folder", "Impossible to open where the Local settings file is located", ex.Message);
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
                _tempDel.EndInvoke(r);

                Dispatcher.Invoke(() =>
                {
                    App.MainViewModel.CheckDiskSpace();

                    TempSeparator.TextRight = string.Format(LocalizationHelper.Get("TempFiles.FilesAndFolders.Count") ?? "{0} folders and {1} files", _folderList.Count.ToString("##,##0"), _fileCount.ToString("##,##0"));

                    ClearTempButton.IsEnabled = _folderList.Any();
                });
            }
            catch (Exception)
            { }
        }

        #endregion

        #endregion

        #region Cloud Services

        private void ImgurHyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusBand.Hide();
                Process.Start(Imgur.GetGetAuthorizationAdress());
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Creating the link and opening a Imgur related page");
                StatusBand.Error(LocalizationHelper.Get("S.Upload.Imgur.Auth.NotPossible"));
            }
        }

        private async void ImgurAuthorizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserSettings.All.ImgurOAuthToken))
            {
                StatusBand.Warning(LocalizationHelper.Get("S.Upload.Imgur.Auth.Missing"));
                return;
            }

            try
            {
                ImgurExpander.IsEnabled = false;
                StatusBand.Hide();

                if (await Imgur.GetAccessToken())
                {
                    UserSettings.All.ImgurOAuthToken = null;
                    StatusBand.Info(LocalizationHelper.Get("S.Upload.Imgur.Auth.Completed"));
                }
                else
                    StatusBand.Warning(LocalizationHelper.Get("S.Upload.Imgur.Auth.Error"));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Authorizing access - Imgur");
                ErrorDialog.Ok("ScreenToGif - Options", "It was not possible to authorize the app", "It was not possible to authorize the app. Check if you provided the correct token and if you have an internet connection.", ex);
            }

            ImgurExpander.IsEnabled = true;
            UpdateImgurStatus();
            UpdateAlbumList();
        }

        private async void ImgurRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserSettings.All.ImgurRefreshToken))
            {
                StatusBand.Warning(LocalizationHelper.Get("S.Upload.Imgur.Refresh.None"));
                return;
            }

            try
            {
                ImgurExpander.IsEnabled = false;
                StatusBand.Hide();

                if (await Imgur.RefreshToken())
                    StatusBand.Info(LocalizationHelper.Get("S.Upload.Imgur.Auth.Completed"));
                else
                    StatusBand.Warning(LocalizationHelper.Get("S.Upload.Imgur.Auth.Error"));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Refreshing authorization - Imgur");
                ErrorDialog.Ok("ScreenToGif - Options", "It was not possible to authorize the app", "It was not possible to authorize the app. Check if you provided the correct token and if you have an internet connection.", ex);
            }

            ImgurExpander.IsEnabled = true;
            UpdateImgurStatus();
            UpdateAlbumList();
        }

        private void ImgurClearButton_Click(object sender, RoutedEventArgs e)
        {
            UserSettings.All.ImgurOAuthToken = null;
            UserSettings.All.ImgurAccessToken = null;
            UserSettings.All.ImgurRefreshToken = null;
            UserSettings.All.ImgurExpireDate = null;
            UserSettings.All.ImgurAlbumList = null;
            UserSettings.All.ImgurSelectedAlbum = null;
            ImgurAlbumComboBox.ItemsSource = null;

            StatusBand.Info(LocalizationHelper.Get("S.Upload.Imgur.Removed"));
            UpdateImgurStatus();
            UpdateAlbumList();
        }

        private void YandexOauth_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(UserSettings.All.LanguageCode.StartsWith("ru") ? e.Uri.AbsoluteUri.Replace("yandex.com", "yandex.ru") : e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open Hyperlink");
            }
        }

        private void UpdateImgurStatus()
        {
            ImgurTextBlock.Text = UserSettings.All.ImgurAccessToken == null || !UserSettings.All.ImgurExpireDate.HasValue ? LocalizationHelper.Get("S.Upload.Imgur.NotAuthorized") :
                UserSettings.All.ImgurExpireDate < DateTime.UtcNow ? string.Format(LocalizationHelper.Get("S.Upload.Imgur.Expired"), UserSettings.All.ImgurExpireDate.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture)) :
                    string.Format(LocalizationHelper.Get("S.Upload.Imgur.Valid"), UserSettings.All.ImgurExpireDate.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture));
        }

        private async void UpdateAlbumList(bool offline = false)
        {
            if (!offline && !await Imgur.IsAuthorized())
                return;

            var list = offline && UserSettings.All.ImgurAlbumList != null ? UserSettings.All.ImgurAlbumList.Cast<ImgurAlbumData>().ToList() : offline ? null : await Imgur.GetAlbums();

            if (list == null)
            {
                list = new List<ImgurAlbumData>();

                if (!offline)
                    StatusBand.Error(LocalizationHelper.Get("S.Upload.Imgur.Error.AlbumLoad"));
            }

            if (!offline || list.All(a => a.Id != "♥♦♣♠"))
                list.Insert(0, new ImgurAlbumData { Id = "♥♦♣♠", Title = LocalizationHelper.Get("S.Upload.Imgur.AskMe") });

            ImgurAlbumComboBox.ItemsSource = list;

            if (ImgurAlbumComboBox.SelectedIndex == -1)
                ImgurAlbumComboBox.SelectedIndex = 0;
        }

        #endregion

        #region Extras

        private void ExtrasGrid_Loaded(object sender, RoutedEventArgs e)
        {
            CheckTools();
        }

        private async void FfmpegImageCard_Click(object sender, RoutedEventArgs e)
        {
            CheckTools();

            if (!string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation) && File.Exists(UserSettings.All.FfmpegLocation))
            {
                Native.ShowFileProperties(Path.GetFullPath(UserSettings.All.FfmpegLocation));
                return;
            }

            #region Save as

            var output = UserSettings.All.FfmpegLocation ?? "";

            if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                output = "";

            //It's only a relative path if not null/empty and there's no root folder declared.
            var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
            var notAlt = !string.IsNullOrWhiteSpace(output) && output.Contains(Path.DirectorySeparatorChar);

            var name = Path.GetFileNameWithoutExtension(output) ?? "";
            var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";
            var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var sfd = new SaveFileDialog
            {
                FileName = string.IsNullOrWhiteSpace(name) ? "ffmpeg" : name,
                InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
                Filter = "FFmpeg executable (.exe)|*.exe",
                DefaultExt = ".exe"
            };

            var result = sfd.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            UserSettings.All.FfmpegLocation = sfd.FileName;

            //Converts to a relative path again.
            if (isRelative && !string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation))
            {
                var selected = new Uri(UserSettings.All.FfmpegLocation);
                var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                //This app even returns you the correct slashes/backslashes.
                UserSettings.All.FfmpegLocation = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            #endregion

            #region Download

            ExtrasGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;
            FfmpegImageCard.Status = ExtrasStatus.Processing;
            FfmpegImageCard.Description = FindResource("Extras.Downloading") as string;

            try
            {
                //Save to a temp folder.
                var temp = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                using (var client = new WebClient { Proxy = WebHelper.GetProxy() })
                    await client.DownloadFileTaskAsync(new Uri(string.Format("https://ffmpeg.zeranoe.com/builds/win{0}/static/ffmpeg-latest-win{0}-static.zip", Environment.Is64BitProcess ? "64" : "32")), temp);

                using (var zip = ZipFile.Open(temp, ZipArchiveMode.Read))
                {
                    var entry = zip.Entries.FirstOrDefault(x => x.Name.Contains("ffmpeg.exe"));

                    if (File.Exists(UserSettings.All.FfmpegLocation))
                        File.Delete(UserSettings.All.FfmpegLocation);

                    entry?.ExtractToFile(UserSettings.All.FfmpegLocation, true);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while downloading FFmpeg");
                ErrorDialog.Ok("Downloading FFmpeg", "It was not possible to download FFmpeg", ex.Message, ex);
            }
            finally
            {
                ExtrasGrid.IsEnabled = true;
                Cursor = Cursors.Arrow;
                CheckTools();
            }

            #endregion
        }

        private async void GifskiImageCard_Click(object sender, RoutedEventArgs e)
        {
            CheckTools();

            if (!string.IsNullOrWhiteSpace(UserSettings.All.GifskiLocation) && File.Exists(UserSettings.All.GifskiLocation))
            {
                Native.ShowFileProperties(Path.GetFullPath(UserSettings.All.GifskiLocation));
                return;
            }

            #region Save as

            var output = UserSettings.All.GifskiLocation ?? "";

            if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                output = "";

            //It's only a relative path if not null/empty and there's no root folder declared.
            var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
            var notAlt = !string.IsNullOrWhiteSpace(output) && output.Contains(Path.DirectorySeparatorChar);

            var name = Path.GetFileNameWithoutExtension(output) ?? "";
            var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";
            var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var sfd = new SaveFileDialog
            {
                FileName = string.IsNullOrWhiteSpace(name) ? "gifski" : name,
                InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
                Filter = "Gifski library (.dll)|*.dll",
                DefaultExt = ".dll"
            };

            var result = sfd.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            UserSettings.All.GifskiLocation = sfd.FileName;

            //Converts to a relative path again.
            if (isRelative && !string.IsNullOrWhiteSpace(UserSettings.All.GifskiLocation))
            {
                var selected = new Uri(UserSettings.All.GifskiLocation);
                var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                //This app even returns you the correct slashes/backslashes.
                UserSettings.All.GifskiLocation = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            #endregion

            #region Download

            ExtrasGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;
            GifskiImageCard.Status = ExtrasStatus.Processing;
            GifskiImageCard.Description = FindResource("Extras.Downloading") as string;

            try
            {
                //Save to a temp folder.
                var temp = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                using (var client = new WebClient { Proxy = WebHelper.GetProxy() })
                    await client.DownloadFileTaskAsync(new Uri("https://github.com/NickeManarin/ScreenToGif-Website/raw/master/downloads/Gifski.zip", UriKind.Absolute), temp);
                //await client.DownloadFileTaskAsync(new Uri("http://screentogif.com/downloads/Gifski.zip", UriKind.Absolute), temp);

                using (var zip = ZipFile.Open(temp, ZipArchiveMode.Read))
                {
                    var entry = zip.Entries.FirstOrDefault(x => x.Name.Contains("gifski.dll"));

                    if (File.Exists(UserSettings.All.GifskiLocation))
                        File.Delete(UserSettings.All.GifskiLocation);

                    entry?.ExtractToFile(UserSettings.All.GifskiLocation, true);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while downloading Gifski");
                ErrorDialog.Ok("Downloading Gifski", "It was not possible to download Gifski", ex.Message, ex);
            }
            finally
            {
                ExtrasGrid.IsEnabled = true;
                Cursor = Cursors.Arrow;
                CheckTools();
            }

            #endregion
        }

        private void LocationTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CheckTools();
        }

        private void SelectFfmpeg_Click(object sender, RoutedEventArgs e)
        {
            var output = UserSettings.All.FfmpegLocation ?? "";

            if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                output = "";

            //It's only a relative path if not null/empty and there's no root folder declared.
            var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
            var notAlt = !string.IsNullOrWhiteSpace(output) && (UserSettings.All.FfmpegLocation ?? "").Contains(Path.DirectorySeparatorChar);

            var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";
            var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var ofd = new OpenFileDialog
            {
                FileName = "ffmpeg",
                Filter = "FFmpeg executable (*.exe)|*.exe", //TODO: Localize.
                Title = LocalizationHelper.Get("Extras.FfmpegLocation.Select"),
                InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
                DefaultExt = ".exe"
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            UserSettings.All.FfmpegLocation = ofd.FileName;

            //Converts to a relative path again.
            if (isRelative && !string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation))
            {
                var selected = new Uri(UserSettings.All.FfmpegLocation);
                var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                //This app even returns you the correct slashes/backslashes.
                UserSettings.All.FfmpegLocation = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            CheckTools();
        }

        private void SelectGifski_Click(object sender, RoutedEventArgs e)
        {
            var output = UserSettings.All.GifskiLocation ?? "";

            if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                output = "";

            //It's only a relative path if not null/empty and there's no root folder declared.
            var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
            var notAlt = !string.IsNullOrWhiteSpace(output) && (UserSettings.All.GifskiLocation ?? "").Contains(Path.DirectorySeparatorChar);

            var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";
            var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var ofd = new OpenFileDialog
            {
                FileName = "gifski",
                Filter = "Gifski library (*.dll)|*.dll", //TODO: Localize.
                Title = LocalizationHelper.Get("Extras.GifskiLocation.Select"),
                InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
                DefaultExt = ".dll"
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            UserSettings.All.GifskiLocation = ofd.FileName;

            //Converts to a relative path again.
            if (isRelative && !string.IsNullOrWhiteSpace(UserSettings.All.GifskiLocation))
            {
                var selected = new Uri(UserSettings.All.FfmpegLocation);
                var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                //This app even returns you the correct slashes/backslashes.
                UserSettings.All.GifskiLocation = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            CheckTools();
        }

        private void ExtrasHyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Erro while trying to navigate to the license website.");
            }
        }

        private void CheckTools()
        {
            if (!IsLoaded)
                return;

            try
            {
                if (Util.Other.IsFfmpegPresent())
                {
                    var info = new FileInfo(UserSettings.All.FfmpegLocation);
                    info.Refresh();

                    FfmpegImageCard.Status = ExtrasStatus.Ready;
                    FfmpegImageCard.Description = string.Format(TryFindResource("Extras.Ready") as string ?? "{0}", Humanizer.BytesToString(info.Length));
                }
                else
                {
                    FfmpegImageCard.Status = ExtrasStatus.Available;
                    FfmpegImageCard.Description = string.Format(TryFindResource("Extras.Download") as string ?? "{0}", "~ 43,7 MB");
                }

                if (Util.Other.IsGifskiPresent())
                {
                    var info = new FileInfo(UserSettings.All.GifskiLocation);
                    info.Refresh();

                    GifskiImageCard.Status = ExtrasStatus.Ready;
                    GifskiImageCard.Description = string.Format(TryFindResource("Extras.Ready") as string ?? "{0}", Humanizer.BytesToString(info.Length));
                }
                else
                {
                    GifskiImageCard.Status = ExtrasStatus.Available;
                    GifskiImageCard.Description = string.Format(TryFindResource("Extras.Download") as string ?? "{0}", "~ 1 MB");
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Checking the existance of external tools.");
            }
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

        private void BitcoinCashHyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText("1HN81cAwDo16tRtiYfkzvzFqikQUimM3S8");
        }

        private void MoneroHyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText("44yC9CkwHVfKPsKxg5RcA67GZEqiQH6QoBYtRKwkhDaE3tvRpiw1E5i6GShZYNsDq9eCtHnq49SrKjF4DG7NwjqWMoMueD4");
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

        private void ExtraSupportHyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://www.screentogif.com/donate");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Openning the donation website");

                ErrorDialog.Ok(FindResource("Title.Options") as string, "Error openning the donation website", ex.Message, ex);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProxyPasswordBox.Password = WebHelper.Unprotect(UserSettings.All.ProxyPassword);

            UpdateImgurStatus();
            UpdateAlbumList(true);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Global.IgnoreHotKeys = false;

            RenderOptions.ProcessRenderMode = UserSettings.All.DisableHardwareAcceleration ? RenderMode.SoftwareOnly : RenderMode.Default;

            UserSettings.All.ProxyPassword = WebHelper.Protect(ProxyPasswordBox.Password);
            UserSettings.Save();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            UserSettings.Save();

            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        #endregion

        public void NotificationUpdated()
        {
            if (!string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder) && Global.AvailableDiskSpace < 2000000000)
                LowSpaceTextBlock.Visibility = Visibility.Visible;
            else
                LowSpaceTextBlock.Visibility = Visibility.Collapsed;
        }

        internal void SelectTab(int index)
        {
            if (index <= -1 || index >= OptionsStackPanel.Children.Count - 1) return;

            if (OptionsStackPanel.Children[index] is RadioButton radio)
                radio.IsChecked = true;
        }
    }
}