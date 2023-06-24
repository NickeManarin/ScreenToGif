using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;
using ScreenToGif.Util.InterProcessChannel;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.ExportPresets;
using ScreenToGif.ViewModel.Tasks;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.Windows.Other;
using Localization = ScreenToGif.Windows.Other.Localization;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ScreenToGif.Windows;

public partial class Options : Window, INotification
{
    #region Constants and variables

    internal const int ApplicationIndex = 0;
    internal const int RecorderIndex = 1;
    internal const int InterfaceIndex = 2;
    internal const int AutomatedTasksIndex = 3;
    internal const int ShortcutsIndex = 4;
    internal const int LanguageIndex = 5;
    internal const int TempFilesIndex = 6;
    internal const int UploadIndex = 7;
    internal const int ExtrasIndex = 8;
    internal const int DonateIndex = 9;
    internal const int AboutIndex = 10;

    /// <summary>
    /// Used to decide if a size check is necessary, when a new path is detected.
    /// </summary>
    private string _previousPath = "";

    /// <summary>
    /// True when the cache folder is being checked.
    /// </summary>
    private bool _isBusy = false;

    /// <summary>
    /// The Path of the cache folder.
    /// </summary>
    private List<DirectoryInfo> _folderList = new();

    /// <summary>
    /// The file count of the cache folder.
    /// </summary>
    private int _fileCount;

    /// <summary>
    /// The size in bytes of the cache folder.
    /// </summary>
    private long _cacheSize;

    /// <summary>
    /// List of tasks.
    /// </summary>
    private ObservableCollection<BaseTaskViewModel> _effectList;

    /// <summary>
    /// List of upload presets.
    /// </summary>
    private ObservableCollection<UploadPreset> _uploadList;

    /// <summary>
    /// The latest size of the grid before being altered.
    /// </summary>
    private Rect _latestGridSize = Rect.Empty;

    /// <summary>
    /// Flag used to avoid multiple calls on the startup mode change.
    /// </summary>
    private bool _ignoreStartup;

    #endregion

    public Options()
    {
        InitializeComponent();

#if FULL_MULTI_MSIX_STORE
        UpdatesCheckBox.Visibility = Visibility.Collapsed;
        CheckForUpdatesLabel.Visibility = Visibility.Collapsed;
        StoreTextBlock.Visibility = Visibility.Visible;
#elif FULL_MULTI_MSIX
        PortableUpdateCheckBox.Visibility = Visibility.Collapsed;
        AdminUpdateCheckBox.Visibility = Visibility.Collapsed;
#endif
    }

    public Options(int index) : this()
    {
        SelectTab(index);
    }

    #region App Settings

    private void ApplicationPanel_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StartupModeGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;
            _ignoreStartup = true;

            //Detect if this app is set to start with windows.
            var sub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            var key = sub?.GetValue("ScreenToGif");
            var name = ProcessHelper.GetEntryAssemblyPath();

            if (key == null || key as string != name)
            {
                //If the key does not exists or its content does not point to the same executable, it means that this app will not run when the user logins.
                StartManuallyCheckBox.IsChecked = true;
            }
            else
            {
                //If the key exists and its content point to the same executable, it means that this app will run when the user logins.
                StartAutomaticallyCheckBox.IsChecked = true;
            }

            //Detect other version of this app?

            StartupModeGrid.IsEnabled = true;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to detect if the app is starting when the user logins");
            StartupModeGrid.IsEnabled = false;
        }
        finally
        {
            _ignoreStartup = false;
            Cursor = Cursors.Arrow;
        }
    }

    private void Instance_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        //With this inter process server, this instance can listen to arguments sent by other instances.
        if (UserSettings.All.SingleInstance)
            InstanceSwitcherChannel.RegisterServer(App.InstanceSwitch_Received);
        else
            InstanceSwitcherChannel.UnregisterServer();
    }

    private void AppThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        try
        {
            if (AppThemeComboBox.SelectedValue is not AppThemes selected)
                throw new Exception("No theme was selected.");

            ThemeHelper.SelectTheme(selected);

            App.NotifyIcon?.RefreshVisual();
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while selecting the app's theme.");
            ExceptionDialog.Ok(ex, Title, "Error while selecting the app's theme", ex.Message);
        }
    }

    private void StartAutomaticallyCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_ignoreStartup)
                return;

            Cursor = Cursors.AppStarting;
            _ignoreStartup = true;

            var sub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var name = ProcessHelper.GetEntryAssemblyPath();

            if (string.IsNullOrWhiteSpace(name) || sub == null)
            {
                StatusBand.Error(LocalizationHelper.Get("S.Options.App.Startup.Mode.Warning"));
                throw new Exception("Impossible to set the app to run on startup. " + name + (sub == null ? ", null" : ""));
            }

            //Add the value in the registry so that the application runs at startup.
            sub.SetValue("ScreenToGif", name);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to set the app to run on startup.");
        }
        finally
        {
            _ignoreStartup = false;
            Cursor = Cursors.Arrow;
        }
    }

    private void StartAutomaticallyCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_ignoreStartup)
                return;

            Cursor = Cursors.AppStarting;
            _ignoreStartup = true;

            var sub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var name = ProcessHelper.GetEntryAssemblyPath();

            if (string.IsNullOrWhiteSpace(name) || sub == null)
            {
                StatusBand.Error(LocalizationHelper.Get("S.Options.App.Startup.Mode.Warning"));
                throw new Exception("Impossible to set the app to not run on startup. " + name + (sub == null ? ", null" : ""));
            }

            //Remove the value from the registry so that the application doesn't start automatically.
            sub.DeleteValue("ScreenToGif", false);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to set the app to not run on startup.");
        }
        finally
        {
            _ignoreStartup = false;
            Cursor = Cursors.Arrow;
        }
    }

    private void StartCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        UserSettings.All.ShowNotificationIcon = true;
    }

    private void NotificationIconCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        //Can't have a minimized startup, if the icon is not present on the notification area.
        if (!UserSettings.All.ShowNotificationIcon)
            UserSettings.All.StartMinimized = false;

        if (App.NotifyIcon != null)
            App.NotifyIcon.Visibility = UserSettings.All.ShowNotificationIcon ? Visibility.Visible : Visibility.Collapsed;
    }

    #endregion

    #region Editor

    private void EditorPanel_Loaded(object sender, RoutedEventArgs e)
    {
        //Editor.
        CheckScheme(false);
        CheckSize(false);
    }

    private void ColorSchemesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CheckScheme();
    }

    private void ColorBox_ColorChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        CheckScheme(false);
    }

    private void CheckScheme(bool schemePicked = true)
    {
        try
        {
            EvenColorBox.IgnoreEvent = true;
            OddColorBox.IgnoreEvent = true;

            if (schemePicked)
            {
                #region If ComboBox Selected

                switch (ColorSchemesComboBox.SelectedIndex)
                {
                    case 0:
                        UserSettings.All.GridColorsFollowSystem = false;
                        UserSettings.All.GridColor1 = Constants.VeryLightEven;
                        UserSettings.All.GridColor2 = Constants.VeryLightOdd;
                        break;
                    case 1:
                        UserSettings.All.GridColorsFollowSystem = false;
                        UserSettings.All.GridColor1 = Constants.LightEven;
                        UserSettings.All.GridColor2 = Constants.LightOdd;
                        break;
                    case 2:
                        UserSettings.All.GridColorsFollowSystem = false;
                        UserSettings.All.GridColor1 = Constants.MediumEven;
                        UserSettings.All.GridColor2 = Constants.MediumOdd;
                        break;
                    case 3:
                        UserSettings.All.GridColorsFollowSystem = false;
                        UserSettings.All.GridColor1 = Constants.DarkEven;
                        UserSettings.All.GridColor2 = Constants.DarkOdd;
                        break;
                    case 4:
                        UserSettings.All.GridColorsFollowSystem = true;
                        var isSystemUsingDark = ThemeHelper.IsSystemUsingDarkTheme();
                        UserSettings.All.GridColor1 = isSystemUsingDark ? Constants.DarkEven : Constants.VeryLightEven;
                        UserSettings.All.GridColor2 = isSystemUsingDark ? Constants.DarkOdd : Constants.VeryLightOdd;
                        break;
                }

                return;

                #endregion
            }

            #region If Color Picked

            if (UserSettings.All.GridColor1.Equals(Constants.VeryLightEven) && UserSettings.All.GridColor2.Equals(Constants.VeryLightOdd) && !UserSettings.All.GridColorsFollowSystem)
                ColorSchemesComboBox.SelectedIndex = 0;
            else if (UserSettings.All.GridColor1.Equals(Constants.LightEven) && UserSettings.All.GridColor2.Equals(Constants.LightOdd))
                ColorSchemesComboBox.SelectedIndex = 1;
            else if (UserSettings.All.GridColor1.Equals(Constants.MediumEven) && UserSettings.All.GridColor2.Equals(Constants.MediumOdd))
                ColorSchemesComboBox.SelectedIndex = 2;
            else if (UserSettings.All.GridColor1.Equals(Constants.DarkEven) && UserSettings.All.GridColor2.Equals(Constants.DarkOdd) && !UserSettings.All.GridColorsFollowSystem)
                ColorSchemesComboBox.SelectedIndex = 3;
            else if (UserSettings.All.GridColorsFollowSystem &&
                     (UserSettings.All.GridColor1.Equals(Constants.VeryLightEven) || UserSettings.All.GridColor1.Equals(Constants.DarkEven)) &&
                     (UserSettings.All.GridColor2.Equals(Constants.VeryLightOdd) || UserSettings.All.GridColor2.Equals(Constants.DarkOdd)))
                ColorSchemesComboBox.SelectedIndex = 4;
            else
            {
                UserSettings.All.GridColorsFollowSystem = false;
                ColorSchemesComboBox.SelectedIndex = 6;
            }

            #endregion
        }
        finally
        {
            EvenColorBox.IgnoreEvent = false;
            OddColorBox.IgnoreEvent = false;
        }
    }

    #region Grid Size

    private void GridSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CheckSize();
    }

    private void GridSizeBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        GridWidthIntegerUpDown.ValueChanged -= GridSizeIntegerUpDown_ValueChanged;
        GridHeightIntegerUpDown.ValueChanged -= GridSizeIntegerUpDown_ValueChanged;

        GridWidthIntegerUpDown.Value = (int)UserSettings.All.GridSize.Width;
        GridHeightIntegerUpDown.Value = (int)UserSettings.All.GridSize.Height;
        GridSizeGrid.Visibility = Visibility.Visible;
        _latestGridSize = UserSettings.All.GridSize;

        Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => GridHeightIntegerUpDown.Focus()));

        GridWidthIntegerUpDown.ValueChanged += GridSizeIntegerUpDown_ValueChanged;
        GridHeightIntegerUpDown.ValueChanged += GridSizeIntegerUpDown_ValueChanged;
    }

    private void GridSizeIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        try
        {
            UserSettings.All.GridSize = new Rect(new Point(0, 0), new Point(GridWidthIntegerUpDown.Value, GridHeightIntegerUpDown.Value));

            CheckSize(false);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Adjusting the Grid Size");
        }
    }

    private void ApplySizeButton_Click(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => GridSizeBorder.Focus()));
        GridSizeGrid.Visibility = Visibility.Collapsed;

        GridSizeIntegerUpDown_ValueChanged(sender, e);
    }

    private void CancelSizeButton_Click(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => GridSizeBorder.Focus()));
        GridSizeGrid.Visibility = Visibility.Collapsed;
        UserSettings.All.GridSize = _latestGridSize;

        CheckSize(false);
    }

    private void CheckSize(bool sizePicked = true)
    {
        try
        {
            GridSizeComboBox.SelectionChanged -= GridSizeComboBox_SelectionChanged;

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
                GridSizeComboBox.SelectedIndex = 0;
            else if (sizeW == 15)
                GridSizeComboBox.SelectedIndex = 1;
            else if (sizeW == 20)
                GridSizeComboBox.SelectedIndex = 2;
            else if (sizeW == 25)
                GridSizeComboBox.SelectedIndex = 3;
            else if (sizeW == 30)
                GridSizeComboBox.SelectedIndex = 4;
            else if (sizeW == 50)
                GridSizeComboBox.SelectedIndex = 5;
            else if (sizeW == 100)
                GridSizeComboBox.SelectedIndex = 6;
            else
                GridSizeComboBox.SelectedIndex = 8;

            #endregion
        }
        finally
        {
            GridSizeComboBox.SelectionChanged += GridSizeComboBox_SelectionChanged;
        }
    }

    #endregion

    #endregion

    #region Automated Tasks

    private void TasksPanel_Loaded(object sender, RoutedEventArgs e)
    {
        var list = UserSettings.All.AutomatedTasksList?.Cast<BaseTaskViewModel>().ToList() ?? new List<BaseTaskViewModel>();

        TasksDataGrid.ItemsSource = _effectList = new ObservableCollection<BaseTaskViewModel>(list);
    }

    private void MoveUp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible && TasksDataGrid.SelectedIndex > 0;
    }

    private void MoveDown_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible && TasksDataGrid.SelectedIndex > -1 && TasksDataGrid.SelectedIndex < TasksDataGrid.Items.Count - 1;
    }

    private void Remove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible && TasksDataGrid.SelectedIndex != -1;
    }

    private void Add_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible;
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

        if (e.Key == Key.Space)
        {
            if (TasksDataGrid.SelectedItem is not BaseTaskViewModel selected)
                return;

            selected.IsEnabled = !selected.IsEnabled;
            e.Handled = true;

            //UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
        }
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
        if (sender is not KeyBox box)
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
        if (!IsLoaded || e.AddedItems.Count == 0)
            return;

        try
        {
            LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);

            ForceUpdateSystemTray();
        }
        catch (Exception ex)
        {
            ErrorDialog.Ok(LocalizationHelper.Get("S.Options.Title"), "Error while stopping", ex.Message, ex);
            LogWriter.Log(ex, "Error while trying to set the language.");
        }
    }

    private void TranslateHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://github.com/NickeManarin/ScreenToGif/wiki/Localization");
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
        {
            try
            {
                LocalizationHelper.SaveDefaultResource(sfd.FileName);
            }
            catch (Exception ex)
            {
                Dialog.Ok("Impossible to Save", "Impossible to save the Xaml file", ex.Message, Icons.Warning);
            }
        }
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
            ProcessHelper.StartWithShell("mailto:nicke@outlook.com.br");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Open MailTo");
        }
    }

    private void ForceUpdateSystemTray()
    {
        if (App.NotifyIcon == null || App.NotifyIcon.ContextMenu == null)
            return;

        var items = App.NotifyIcon.ContextMenu.Items.OfType<ExtendedMenuItem>();

        foreach (var item in items)
            item.Header = LocalizationHelper.Get((string) item.Tag);
    }

    #endregion

    #region Storage

    private void TempPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (TempPanel.Visibility != Visibility.Visible)
            return;

        _previousPath = UserSettings.All.TemporaryFolderResolved;

        CheckSpace();

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

            AppDataPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
        }

        //Local.
        if (!File.Exists(LocalPathTextBlock.Text))
        {
            LocalPathTextBlock.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Strikethrough, new Pen(Brushes.DarkSlateGray, 1),
                0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));

            LocalPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
        }

        #endregion
    }


    private void Cache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && !string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder) && !_isBusy;
    }

    private void BrowseCache_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && !string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder) && Directory.Exists(UserSettings.All.TemporaryFolderResolved) && !_isBusy;
    }

    private void BrowseLogs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && !string.IsNullOrWhiteSpace(UserSettings.All.LogsFolder) && Directory.Exists(UserSettings.All.LogsFolder);
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


    private void CheckCache_Execute(object sender, RoutedEventArgs e)
    {
        CheckSpace();
    }

    private async void ClearCache_Execute(object sender, RoutedEventArgs e)
    {
        _isBusy = true;
        StatusProgressBar.State = ExtendedProgressBar.ProgressState.Primary;
        StatusProgressBar.IsIndeterminate = true;
        FilesTextBlock.Visibility = Visibility.Collapsed;

        try
        {
            var parent = Util.Other.AdjustPath(UserSettings.All.TemporaryFolderResolved);
            var path = Path.Combine(parent, "ScreenToGif", "Recording");

            if (!Directory.Exists(path))
                return;

            //Force to be 1 day or more.
            UserSettings.All.AutomaticCleanUpDays = UserSettings.All.AutomaticCleanUpDays > 0 ? UserSettings.All.AutomaticCleanUpDays : 5;

            //Asks if the user wants to remove all files or just the old ones.
            if (!CacheDialog.Ask(true, out bool ignoreRecent))
                return;

            _folderList = await Task.Factory.StartNew(() => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Select(x => new DirectoryInfo(x)).ToList());

            if (ignoreRecent)
                _folderList = await Task.Factory.StartNew(() => _folderList.Where(w => (DateTime.Now - w.CreationTime).Days > UserSettings.All.AutomaticCleanUpDays).ToList());

            foreach (var folder in _folderList.Where(folder => !MutexList.IsInUse(folder.Name)))
                Directory.Delete(folder.FullName, true);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while cleaning the cache folder.");
        }
        finally
        {
            App.MainViewModel.CheckDiskSpace();
            CheckSpace(true);
        }
    }

    private void ChooseCachePath_Click(object sender, RoutedEventArgs e)
    {
        var path = UserSettings.All.TemporaryFolderResolved;

        if (UserSettings.All.TemporaryFolderResolved.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            path = "";

        //It's only a relative path if not null/empty and there's no root folder declared.
        var isRelative = !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path);
        var notAlt = !string.IsNullOrWhiteSpace(path) && UserSettings.All.TemporaryFolderResolved.Contains(Path.DirectorySeparatorChar);

        path = Util.Other.AdjustPath(path);

        var initial = Directory.Exists(path) ? path : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var folderDialog = new FolderSelector();

        if (!string.IsNullOrWhiteSpace(initial))
            folderDialog.SelectedPath = initial;

        if (!folderDialog.ShowDialog())
            return;

        //Converts to a relative path again.
        if (isRelative && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
        {
            var selected = new Uri(folderDialog.SelectedPath);
            var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
            var relativeFolder = selected.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) == baseFolder.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) ?
                "." : Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

            //This app even returns you the correct slashes/backslashes.
            UserSettings.All.TemporaryFolder = notAlt ? relativeFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) :
                relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        else
        {
            UserSettings.All.TemporaryFolder = folderDialog.SelectedPath;
        }

        _previousPath = UserSettings.All.TemporaryFolderResolved;
        CheckSpace();
    }

    private void CacheTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_previousPath == UserSettings.All.TemporaryFolderResolved)
            return;

        _previousPath = UserSettings.All.TemporaryFolderResolved;
        CheckSpace();
    }

    private void BrowseCache_Execute(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(UserSettings.All.TemporaryFolderResolved);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the cache folder.");
        }
    }

    private void ChooseLogsPath_Click(object sender, RoutedEventArgs e)
    {
        var path = UserSettings.All.LogsFolder;

        if (UserSettings.All.LogsFolder.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            path = "";

        //It's only a relative path if not null/empty and there's no root folder declared.
        var isRelative = !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path);
        var notAlt = !string.IsNullOrWhiteSpace(path) && UserSettings.All.LogsFolder.Contains(Path.DirectorySeparatorChar);

        path = Util.Other.AdjustPath(path);

        var initial = Directory.Exists(path) ? path : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var folderDialog = new FolderSelector();

        if (!string.IsNullOrWhiteSpace(initial))
            folderDialog.SelectedPath = initial;

        if (!folderDialog.ShowDialog())
            return;

        //Converts to a relative path again.
        if (isRelative && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
        {
            var selected = new Uri(folderDialog.SelectedPath);
            var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
            var relativeFolder = selected.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) == baseFolder.AbsolutePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar) ?
                "." : Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

            //This app even returns you the correct slashes/backslashes.
            UserSettings.All.LogsFolder = notAlt ? relativeFolder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) :
                relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        else
        {
            UserSettings.All.LogsFolder = folderDialog.SelectedPath;
        }
    }

    private void BrowseLogs_Execute(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(UserSettings.All.LogsFolder);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the logs folder.");
        }
    }


    private void OpenAppDataSettings_Execute(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
                ProcessHelper.StartWithShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml"));
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

            AppDataPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
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
                ProcessHelper.StartWithShell(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml"));
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

            LocalPathTextBlock.SetResourceReference(ToolTipProperty, "S.Options.Storage.NotExists");
        }
        catch (Exception ex)
        {
            Dialog.Ok("Remove Local Settings", "Impossible to remove local settings", ex.Message);
        }
    }


    private async void CheckSpace(bool force = false)
    {
        if (_isBusy && !force)
            return;

        _isBusy = true;
        StatusProgressBar.State = ExtendedProgressBar.ProgressState.Primary;
        StatusProgressBar.IsIndeterminate = true;
        FilesTextBlock.Visibility = Visibility.Collapsed;

        #region Status

        var path = Util.Other.AdjustPath(UserSettings.All.TemporaryFolderResolved);
        var drive = DriveInfo.GetDrives().FirstOrDefault(w => w.RootDirectory.FullName == Path.GetPathRoot(path));

        if (drive != null)
        {
            VolumeTextBlock.Text = $"{drive.VolumeLabel} ({drive.Name.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar)})".TrimStart();
            FreeSpaceTextBlock.Text = LocalizationHelper.GetWithFormat("S.Options.Storage.Status.FreeSpace", "{0} free of {1}", Humanizer.BytesToString(drive.TotalFreeSpace), Humanizer.BytesToString(drive.TotalSize));
            StatusProgressBar.Value = 100 - ((double)drive.AvailableFreeSpace / drive.TotalSize * 100);
            StatusProgressBar.State = StatusProgressBar.Value < 90 ? ExtendedProgressBar.ProgressState.Info : ExtendedProgressBar.ProgressState.Danger;
            LowSpaceTextBlock.Visibility = drive.AvailableFreeSpace > 2_000_000_000 ? Visibility.Collapsed : Visibility.Visible; //2 GB.
        }
        else
        {
            VolumeTextBlock.Text = Path.GetPathRoot(path);
            FreeSpaceTextBlock.Text = LocalizationHelper.Get("S.Options.Storage.Status.Error");
            StatusProgressBar.Value = 0;
            LowSpaceTextBlock.Visibility = Visibility.Collapsed;
        }

        #endregion

        //Calculates the quantity of files and folders.
        await Task.Run(CheckDrive);

        try
        {
            App.MainViewModel.CheckDiskSpace();

            FilesRun.Text = _fileCount == 0 ? LocalizationHelper.Get("S.Options.Storage.Status.Files.None") :
                LocalizationHelper.GetWithFormat("S.Options.Storage.Status.Files." + (_fileCount > 1 ? "Plural" : "Singular"), "{0} files", _fileCount);
            FoldersRun.Text = _folderList.Count == 0 ? LocalizationHelper.Get("S.Options.Storage.Status.Folders.None") :
                LocalizationHelper.GetWithFormat("S.Options.Storage.Status.Folders." + (_folderList.Count > 1 ? "Plural" : "Singular"), "{0} folders", _folderList.Count);
            UsedSpaceRun.Text = LocalizationHelper.GetWithFormat("S.Options.Storage.Status.InUse", "{0} in use", Humanizer.BytesToString(_cacheSize));
            FilesTextBlock.Visibility = Visibility.Visible;
            StatusProgressBar.IsIndeterminate = false;
        }
        catch (Exception)
        { }
        finally
        {
            _isBusy = false;
        }
    }

    private void CheckDrive()
    {
        _folderList = new List<DirectoryInfo>();

        var path = Util.Other.AdjustPath(UserSettings.All.TemporaryFolderResolved);
        var cache = Path.Combine(path, "ScreenToGif", "Recording");

        if (!Directory.Exists(cache))
        {
            _folderList = new List<DirectoryInfo>();
            _fileCount = 0;
            _cacheSize = 0;
            return;
        }

        _folderList = Directory.GetDirectories(cache).Select(x => new DirectoryInfo(x)).ToList();
        _fileCount = _folderList.Sum(folder => Directory.EnumerateFiles(folder.FullName).Count());
        _cacheSize = _folderList.Sum(s => s.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length));
    }

    #endregion

    #region Cloud Services

    private void CloudGrid_Loaded(object sender, RoutedEventArgs e)
    {
        var list = UserSettings.All.UploadPresets?.Cast<UploadPreset>().ToList() ?? new List<UploadPreset>();

        UploadDataGrid.ItemsSource = _uploadList = new ObservableCollection<UploadPreset>(list);
    }

    private void AddUpload_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CloudGrid.IsVisible;
    }

    private void Upload_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CloudGrid.IsVisible && UploadDataGrid.SelectedIndex != -1;
    }

    private void AddUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var upload = new Upload();
        var result = upload.ShowDialog();

        if (result != true)
            return;

        _uploadList.Add(upload.CurrentPreset);

        UserSettings.All.UploadPresets = new ArrayList(_uploadList.ToArray());
    }

    private void EditUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var index = UploadDataGrid.SelectedIndex;
        var current = _uploadList[UploadDataGrid.SelectedIndex];
        var selected = current.ShallowCopy();

        var preset = new Upload { CurrentPreset = selected, IsEditing = true };
        var result = preset.ShowDialog();

        if (result != true)
            return;

        _uploadList[UploadDataGrid.SelectedIndex] = preset.CurrentPreset;
        UploadDataGrid.Items.Refresh();
        UploadDataGrid.SelectedIndex = index;

        //Update the upload preset in all export presets.
        if (current.Title != preset.CurrentPreset.Title)
        {
            foreach (var exportPreset in UserSettings.All.ExportPresets.OfType<ExportPreset>().Where(w => w.UploadService == current.Title))
                exportPreset.UploadService = preset.CurrentPreset.Title;
        }

        UserSettings.All.UploadPresets = new ArrayList(_uploadList.ToArray());
    }

    private void RemoveUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var index = UploadDataGrid.SelectedIndex;

        //Ask if the user really wants to remove the preset.
        if (index < 0 || !Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Title"), LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Instruction"),
                LocalizationHelper.Get("S.SaveAs.Upload.Ask.Delete.Message")))
            return;

        var selected = _uploadList[UploadDataGrid.SelectedIndex];
        _uploadList.RemoveAt(UploadDataGrid.SelectedIndex);

        //Automatically selects the closest item from the position of the one that was removed.
        UploadDataGrid.SelectedIndex = _uploadList.Count == 0 ? -1 : _uploadList.Count <= index ? _uploadList.Count - 1 : index;

        UserSettings.All.UploadPresets = new ArrayList(_uploadList.ToArray());

        //Remove the upload preset from all export presets.
        foreach (var exportPreset in UserSettings.All.ExportPresets.OfType<ExportPreset>().Where(w => w.UploadService == selected.Title))
            exportPreset.UploadService = null;
    }

    private void HistoryUpload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var history = new UploadHistory
        {
            CurrentPreset = _uploadList[UploadDataGrid.SelectedIndex]
        };
        history.ShowDialog();
    }

    private void UploadDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EditUploadCommandBinding.Command.CanExecute(sender))
            EditUploadCommandBinding.Command.Execute(sender);
    }

    private void UploadDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && EditUploadCommandBinding.Command.CanExecute(sender))
        {
            EditUploadCommandBinding.Command.Execute(sender);
            e.Handled = true;
        }

        if (e.Key == Key.Space)
        {
            if (UploadDataGrid.SelectedItem is not UploadPreset selected)
                return;

            selected.IsEnabled = !selected.IsEnabled;
            e.Handled = true;
        }
    }

    #endregion

    #region Extras

    private void ExtrasGrid_Loaded(object sender, RoutedEventArgs e)
    {
        CheckTools();

        //Gifski is only supported in x64.
        GifskiImageCard.IsEnabled = Environment.Is64BitProcess;
    }

    private async void FfmpegImageCard_Click(object sender, RoutedEventArgs e)
    {
        CheckTools(true, false);

        var adjusted = Util.Other.AdjustPath(UserSettings.All.FfmpegLocation);

        if (!string.IsNullOrWhiteSpace(adjusted) && File.Exists(adjusted))
        {
            Native.Helpers.Other.ShowFileProperties(Path.GetFullPath(adjusted));
            return;
        }

        var url = Environment.Is64BitProcess ? "https://www.screentogif.com/downloads/FFmpeg-4.4.1-x64.zip" :
            "https://www.screentogif.com/downloads/FFmpeg-4.3.1-x86.zip";

#if FULL_MULTI_MSIX_STORE
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Extras.DownloadRestriction"));
            ProcessHelper.StartWithShell(url);            
            return;
#else
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
            Filter = $"{LocalizationHelper.Get("S.Options.Extras.FfmpegLocation.File")} (.exe)|*.exe",
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
        FfmpegImageCard.Description = LocalizationHelper.Get("S.Options.Extras.Downloading");

        try
        {
            //Save to a temp folder.
            var temp = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            using (var client = new WebClient { Proxy = WebHelper.GetProxy() })
                await client.DownloadFileTaskAsync(new Uri(url), temp);

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
            ExceptionDialog.Ok(ex, "Downloading FFmpeg", "It was not possible to download FFmpeg", ex.Message);
        }
        finally
        {
            ExtrasGrid.IsEnabled = true;
            Cursor = Cursors.Arrow;
            CheckTools();
        }

        #endregion
#endif
    }

    private async void GifskiImageCard_Click(object sender, RoutedEventArgs e)
    {
        CheckTools(false, true);

        var adjusted = Util.Other.AdjustPath(UserSettings.All.GifskiLocation);

        if (!string.IsNullOrWhiteSpace(adjusted) && File.Exists(adjusted))
        {
            Native.Helpers.Other.ShowFileProperties(Path.GetFullPath(adjusted));
            return;
        }

#if FULL_MULTI_MSIX_STORE
            StatusBand.Warning(LocalizationHelper.Get("S.Options.Extras.DownloadRestriction"));
            return;
#else
        #region Save as

        var output = UserSettings.All.GifskiLocation ?? "";

        if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            output = "";

        //It's only a relative path if not null/empty and there's no root folder declared.
        var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
        var notAlt = !string.IsNullOrWhiteSpace(output) && output.Contains(Path.DirectorySeparatorChar);

        var name = Path.GetFileNameWithoutExtension(output) ?? "";
        var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";
        var initial = Directory.Exists(directory) ? directory : AppDomain.CurrentDomain.BaseDirectory;

        var sfd = new SaveFileDialog
        {
            FileName = string.IsNullOrWhiteSpace(name) ? "gifski" : name,
            InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
            Filter = $"{LocalizationHelper.Get("S.Options.Extras.GifskiLocation.File")} (.dll)|*.dll",
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
        GifskiImageCard.Description = LocalizationHelper.Get("S.Options.Extras.Downloading");

        try
        {
            //Save to a temp folder.
            var temp = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            using (var client = new WebClient { Proxy = WebHelper.GetProxy() })
                await client.DownloadFileTaskAsync(new Uri("https://www.screentogif.com/downloads/Gifski.zip", UriKind.Absolute), temp);

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
#endif
    }

    private void LocationTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var box = sender as TextBox;

        CheckTools(box?.Tag?.Equals("FFmpeg") ?? false, box?.Tag?.Equals("Gifski") ?? false);
    }


    private void SelectFfmpeg_Click(object sender, RoutedEventArgs e)
    {
        var output = UserSettings.All.FfmpegLocation ?? "";

        if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            output = "";

        //It's only a relative path if not null/empty and there's no root folder declared.
        var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
        var notAlt = !string.IsNullOrWhiteSpace(output) && (UserSettings.All.FfmpegLocation ?? "").Contains(Path.DirectorySeparatorChar);

        //Gets the current directory folder, where the file is located. If empty, it means that the path is relative.
        var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";

        if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(directory))
            directory = AppDomain.CurrentDomain.BaseDirectory;

        var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var ofd = new OpenFileDialog
        {
            FileName = "ffmpeg",
            Filter = $"{LocalizationHelper.Get("S.Options.Extras.FfmpegLocation.File")} (*.exe)|*.exe", //TODO: Localize.
            Title = LocalizationHelper.Get("S.Options.Extras.FfmpegLocation.Select"),
            InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
            DefaultExt = ".exe"
        };

        var result = ofd.ShowDialog();

        if (!result.HasValue || !result.Value)
            return;

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

        CheckTools(true, false);
    }

    private void SelectGifski_Click(object sender, RoutedEventArgs e)
    {
        var output = UserSettings.All.GifskiLocation ?? "";

        if (output.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
            output = "";

        //It's only a relative path if not null/empty and there's no root folder declared.
        var isRelative = !string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output);
        var notAlt = !string.IsNullOrWhiteSpace(output) && (UserSettings.All.GifskiLocation ?? "").Contains(Path.DirectorySeparatorChar);

        //Gets the current directory folder, where the file is located. If empty, it means that the path is relative.
        var directory = !string.IsNullOrWhiteSpace(output) ? Path.GetDirectoryName(output) : "";

        if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(directory))
            directory = AppDomain.CurrentDomain.BaseDirectory;

        var initial = Directory.Exists(directory) ? directory : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var ofd = new OpenFileDialog
        {
            FileName = "gifski",
            Filter = $"{LocalizationHelper.Get("S.Options.Extras.GifskiLocation.File")} (*.dll)|*.dll", //TODO: Localize.
            Title = LocalizationHelper.Get("S.Options.Extras.GifskiLocation.Select"),
            InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
            DefaultExt = ".dll"
        };

        var result = ofd.ShowDialog();

        if (!result.HasValue || !result.Value)
            return;

        UserSettings.All.GifskiLocation = ofd.FileName;

        //Converts to a relative path again.
        if (isRelative && !string.IsNullOrWhiteSpace(UserSettings.All.GifskiLocation))
        {
            var selected = new Uri(UserSettings.All.GifskiLocation);
            var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
            var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

            //This app even returns you the correct slashes/backslashes.
            UserSettings.All.GifskiLocation = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        CheckTools(false, true);
    }


    private void BrowseFfmpeg_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && FfmpegImageCard.Status == ExtrasStatus.Ready;
    }

    private void BrowseGifski_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsLoaded && GifskiImageCard.Status == ExtrasStatus.Ready;
    }

    private void BrowseFfmpeg_Execute(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var path = Util.Other.AdjustPath(UserSettings.All.FfmpegLocation);

            if (string.IsNullOrWhiteSpace(path))
                return;

            var folder = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(folder))
                return;

            ProcessHelper.StartWithShell(folder);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the FFmpeg folder.");
        }
    }

    private void BrowseGifski_Execute(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var path = Util.Other.AdjustPath(UserSettings.All.GifskiLocation);

            if (string.IsNullOrWhiteSpace(path))
                return;

            var folder = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(folder))
                return;

            ProcessHelper.StartWithShell(folder);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to browse the Gifski folder.");
        }
    }


    private void ExtrasHyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(e.Uri.AbsoluteUri);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to navigate to the license website.");
        }
    }

    private void CheckTools(bool ffmpeg = true, bool gifski = true)
    {
        if (!IsLoaded)
            return;

        try
        {
            if (ffmpeg)
            {
                #region FFmpeg

                if (Util.Other.IsFfmpegPresent(true, true))
                {
                    var info = new FileInfo(Util.Other.AdjustPath(UserSettings.All.FfmpegLocation));
                    info.Refresh();

                    FfmpegImageCard.Status = ExtrasStatus.Ready;
                    FfmpegImageCard.Description = string.Format(LocalizationHelper.Get("S.Options.Extras.Ready", "{0}"), Humanizer.BytesToString(info.Length));
                }
                else
                {
                    FfmpegImageCard.Status = ExtrasStatus.Available;
                    FfmpegImageCard.Description = string.Format(LocalizationHelper.Get("S.Options.Extras.Download", "{0}"), Environment.Is64BitProcess ? "~ 43 MB" : "~ 24,6 MB");
                }

                #endregion
            }

            if (gifski)
            {
                #region Gifski

                if (Util.Other.IsGifskiPresent(true, true))
                {
                    var info = new FileInfo(Util.Other.AdjustPath(UserSettings.All.GifskiLocation));
                    info.Refresh();

                    GifskiImageCard.Status = ExtrasStatus.Ready;
                    GifskiImageCard.Description = string.Format(LocalizationHelper.Get("S.Options.Extras.Ready", "{0}"), Humanizer.BytesToString(info.Length));
                }
                else
                {
                    GifskiImageCard.Status = ExtrasStatus.Available;
                    GifskiImageCard.Description = string.Format(LocalizationHelper.Get("S.Options.Extras.Download", "{0}"), "~ 512 KB");
                }

                #endregion
            }
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Checking the existence of external tools.");
            StatusBand.Error("It was not possible to check the existence of the external tools.");
        }
    }

    #endregion

    #region Donate

    private void DonateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Donation website");
            ErrorDialog.Ok(Title, "Error opening the donation website", ex.Message, ex);
        }
    }

    private void DonateEuroButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Donation website");

            ErrorDialog.Ok(Title, "Error opening the donation website", ex.Message, ex);
        }
    }

    private void DonateOtherButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var currency = CurrencyComboBox.Text.Substring(0, 3);

            ProcessHelper.StartWithShell($"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code={currency}&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Donation website");

            ErrorDialog.Ok(LocalizationHelper.Get("S.Options.Title"), "Error opening the donation website", ex.Message, ex);
        }
    }

    private void PatreonButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.patreon.com/nicke");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Patreon website");
            ErrorDialog.Ok(Title, "Error opening the Patreon website", ex.Message, ex);
        }
    }

    private void FlattrButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://flattr.com/@NickeManarin/domain/screentogif.com");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Flattr website");

            ErrorDialog.Ok(LocalizationHelper.Get("S.Options.Title"), "Error opening the Flattr website", ex.Message, ex);
        }
    }

    private void SteamButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://steamcommunity.com/id/nickesm/wishlist");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Steam website");
            ErrorDialog.Ok(Title, "Error opening the Steam website", ex.Message, ex);
        }
    }

    private void GogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.gog.com/u/Nickesm/wishlist");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the GOG website");
            ErrorDialog.Ok(Title, "Error opening the GOG website", ex.Message, ex);
        }
    }

    private void KofiButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://ko-fi.com/nickemanarin");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Ko-fi website");
            ErrorDialog.Ok(Title, "Error opening the Ko-fi website", ex.Message, ex);
        }
    }

    private void BitcoinCashCopy_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText("1HN81cAwDo16tRtiYfkzvzFqikQUimM3S8");
    }

    private void MoneroHyperlink_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText("44yC9CkwHVfKPsKxg5RcA67GZEqiQH6QoBYtRKwkhDaE3tvRpiw1E5i6GShZYNsDq9eCtHnq49SrKjF4DG7NwjqWMoMueD4");
    }

    private void SupportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.screentogif.com/donate");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the donation website");
            ErrorDialog.Ok(Title, "Error opening the donation website", ex.Message, ex);
        }
    }

    #endregion

    #region About

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell(e.Uri.AbsoluteUri);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Open Hyperlink");
        }
    }

    private async void CheckForUpdatesLabel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CheckForUpdatesLabel.IsEnabled = false;

            await App.MainViewModel.CheckForUpdates(true);

            if (Global.UpdateAvailable != null)
            {
                App.MainViewModel.PromptUpdate.Execute(null);
                return;
            }

            StatusBand.Info(LocalizationHelper.Get("S.Options.About.UpdateCheck.Nothing"));
        }
        finally
        {
            CheckForUpdatesLabel.IsEnabled = true;
        }
    }

    #endregion

    #region Other

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SizeToContent = SizeToContent.Manual;

        try
        {
            if (!string.IsNullOrWhiteSpace(UserSettings.All.ProxyPassword))
                ProxyPasswordBox.Password = WebHelper.Unprotect(UserSettings.All.ProxyPassword);
        }
        catch (Exception ex)
        {
            StatusBand.Warning("It was not possible to correctly load your proxy password. This usually happens when sharing the app settings with different computers.");
            LogWriter.Log(ex, "Unprotect data");
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        #region Validation

        if (UserSettings.All.CursorFollowing && UserSettings.All.FollowShortcut == Key.None)
        {
            Dialog.Ok(LocalizationHelper.Get("S.Options.Title"), LocalizationHelper.Get("S.Options.Warning.Follow.Header"),
                LocalizationHelper.Get("S.Options.Warning.Follow.Message"), Icons.Warning);

            ShortcutsRadio.IsChecked = true;
            FollowKeyBox.Focus();

            e.Cancel = true;
            return;
        }

        #endregion

        Global.IgnoreHotKeys = false;

        BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure = UserSettings.All.WorkaroundQuota ? BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Reset : BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Continue;
        RenderOptions.ProcessRenderMode = UserSettings.All.DisableHardwareAcceleration ? RenderMode.SoftwareOnly : RenderMode.Default;

        if (!string.IsNullOrWhiteSpace(ProxyPasswordBox.Password))
            UserSettings.All.ProxyPassword = WebHelper.Protect(ProxyPasswordBox.Password);

        UserSettings.Save();
    }

    #endregion


    public void NotificationUpdated()
    {
        LowSpaceTextBlock.Visibility = Global.AvailableDiskSpace > 2_000_000_000 ? Visibility.Collapsed : Visibility.Visible; //2 GB.
    }

    internal void SelectTab(int index)
    {
        if (index <= -1 || index >= OptionsStackPanel.Children.Count - 1)
            return;

        if (OptionsStackPanel.Children[index] is RadioButton radio)
            radio.IsChecked = true;
    }
}