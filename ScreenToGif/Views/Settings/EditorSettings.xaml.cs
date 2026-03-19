using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScreenToGif.Views.Settings;

public partial class EditorSettings : Page
{
    /// <summary>
    /// The latest size of the grid before being altered.
    /// </summary>
    private Rect _latestGridSize = Rect.Empty;

    public EditorSettings()
    {
        InitializeComponent();
    }

    private void EditorSettings_Loaded(object sender, RoutedEventArgs e)
    {
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
}