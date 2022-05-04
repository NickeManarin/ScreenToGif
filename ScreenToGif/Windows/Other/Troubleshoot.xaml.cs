using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Windows.Other;

public partial class Troubleshoot : Window
{
    private double _minLeft = SystemParameters.VirtualScreenLeft;
    private double _minTop = SystemParameters.VirtualScreenTop;
    private double _maxRight = SystemParameters.VirtualScreenWidth;
    private double _maxBottom = SystemParameters.VirtualScreenHeight;

    public Troubleshoot()
    {
        InitializeComponent();

        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current.Windows.OfType<Window>().Any(a => a.GetType() != typeof(Troubleshoot)))
            NowRadioButton.IsChecked = true;
        else
            LaterRadioButton.IsChecked = true;
    }

    private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e) => DetectMonitors();

    private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SetViewPort(_minLeft, _maxRight, _minTop, _maxBottom);
    }

    private void KindRadioButton_Checked(object sender, RoutedEventArgs e) => DetectMonitors();

    private void HyperlinkMove_Click(object sender, RoutedEventArgs e)
    {
        var monitor = MonitorHelper.AllMonitorsGranular().FirstOrDefault(f => f.IsPrimary);

        if (monitor == null)
            return;

        //Move all windows to the main monitor.
        foreach (var window in Application.Current.Windows.OfType<Window>().Where(w => w.GetType() != typeof(Troubleshoot) && w.GetType() != typeof(RegionSelection)).OrderBy(o => o.Width).ThenBy(o => o.Height))
        {
            //Pause any active recording...
            if (window is NewRecorder newRecorder)
            {
                if (newRecorder.Stage == RecorderStages.Recording && newRecorder.Project?.Any == true)
                    newRecorder.Pause();

                newRecorder.MoveToMainScreen();
                continue;
            }

            if (window is Recorder recorder)
            {
                if (recorder.Stage == RecorderStages.Recording && recorder.Project?.Any == true)
                    recorder.Pause();
            }

            var diff = window.Scale() / monitor.Scale;
            var top = window.Top / diff;
            var left = window.Left / diff;
            var width = window.ActualWidth;
            var height = window.ActualHeight;

            if (monitor.NativeBounds.Top > top)
                top = monitor.NativeBounds.Top;

            if (monitor.Bounds.Left > left)
                left = monitor.NativeBounds.Left;

            if (monitor.NativeBounds.Bottom < top + height)
                top = monitor.NativeBounds.Bottom - height;

            if (monitor.NativeBounds.Right < left + width)
                left = monitor.NativeBounds.Right - width;

            window.MoveToScreen(monitor);

            window.WindowState = WindowState.Normal;
            window.Left = monitor.NativeBounds.Left + 1;
            window.Top = monitor.NativeBounds.Top + 1;

            window.Left = left;
            window.Top = top;
        }

        DetectMonitors();
    }

    private void HyperlinkReset_Click(object sender, RoutedEventArgs e)
    {
        UserSettings.All.RecorderTop = double.NaN;
        UserSettings.All.RecorderLeft = double.NaN;
        UserSettings.All.RecorderWidth = 518;
        UserSettings.All.RecorderHeight = 269;

        UserSettings.All.SelectedRegion = Rect.Empty;

        UserSettings.All.EditorTop = double.NaN;
        UserSettings.All.EditorLeft = double.NaN;
        UserSettings.All.EditorWidth = double.NaN;
        UserSettings.All.EditorHeight = double.NaN;
        UserSettings.All.EditorWindowState = WindowState.Normal;

        UserSettings.All.StartupTop = double.NaN;
        UserSettings.All.StartupLeft = double.NaN;
        UserSettings.All.StartupWidth = double.NaN;
        UserSettings.All.StartupHeight = double.NaN;
        UserSettings.All.StartupWindowState = WindowState.Normal;

        //Move currently open windows to the main monitor?

        DetectMonitors();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }


    private void DetectMonitors()
    {
        var monitors = MonitorHelper.AllMonitorsGranular();
        _minLeft = monitors.Min(m => m.NativeBounds.Left);
        _minTop = monitors.Min(m => m.NativeBounds.Top);
        _maxRight = monitors.Max(m => m.NativeBounds.Right);
        _maxBottom = monitors.Max(m => m.NativeBounds.Bottom);

        MainCanvas.Children.Clear();

        if (NowRadioButton.IsChecked == true)
        {
            foreach (var window in Application.Current.Windows.OfType<Window>().Where(w => w.GetType() != typeof(Troubleshoot) && w.IsVisible).OrderBy(o => o.Width).ThenBy(o => o.Height))
            {
                var scale = window.Scale();

                var top = window.Top * scale;
                var left = window.Left * scale;
                var width = window.ActualWidth * scale;
                var height = window.ActualHeight * scale;
                var title = window.Title.Remove("ScreenToGif - ");

                if (window is Recorder or NewRecorder or RegionSelection)
                    title = LocalizationHelper.Get("S.StartUp.Recorder");
                    
                _minLeft = Math.Min(_minLeft, left);
                _minTop = Math.Min(_minTop, top);
                _maxRight = Math.Max(_maxRight, left + width);
                _maxBottom = Math.Max(_maxBottom, top + height);

                var rect = new Border
                {
                    BorderBrush = TryFindResource("Element.Border.Required") as SolidColorBrush ?? Brushes.DarkBlue,
                    BorderThickness = new Thickness(3),
                    Background = TryFindResource("Panel.Background.Level3") as SolidColorBrush ?? Brushes.WhiteSmoke,
                    Width = width,
                    Height = height,
                    Tag = "C",
                    Child = new Viewbox
                    {
                        Child = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Padding = new Thickness(2),
                            Text = title,
                            Foreground = TryFindResource("Element.Foreground") as SolidColorBrush ?? Brushes.Black,
                        }
                    }
                };

                MainCanvas.Children.Add(rect);

                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                Panel.SetZIndex(rect, MainCanvas.Children.Count + 1);
            }
        }
        else
        {
            #region Recorder position

            if (!double.IsNaN(UserSettings.All.RecorderTop) && !double.IsNaN(UserSettings.All.RecorderLeft))
            {
                _minLeft = Math.Min(_minLeft, UserSettings.All.RecorderLeft);
                _minTop = Math.Min(_minTop, UserSettings.All.RecorderTop);
                _maxRight = Math.Max(_maxRight, UserSettings.All.RecorderLeft + UserSettings.All.RecorderWidth);
                _maxBottom = Math.Max(_maxBottom, UserSettings.All.RecorderTop + UserSettings.All.RecorderHeight);

                var rect = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.DarkBlue),
                    BorderThickness = new Thickness(3),
                    Background = new SolidColorBrush(Colors.WhiteSmoke),
                    Width = UserSettings.All.RecorderWidth,
                    Height = UserSettings.All.RecorderHeight,
                    Tag = "N",
                    Child = new Viewbox
                    {
                        Child = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Padding = new Thickness(2),
                            Text = LocalizationHelper.Get("S.StartUp.Recorder")
                        }
                    }
                };

                MainCanvas.Children.Add(rect);

                Canvas.SetLeft(rect, UserSettings.All.RecorderLeft);
                Canvas.SetTop(rect, UserSettings.All.RecorderTop);
            }

            if (!UserSettings.All.SelectedRegion.IsEmpty)
            {
                _minLeft = Math.Min(_minLeft, UserSettings.All.SelectedRegion.Left + SystemParameters.VirtualScreenLeft);
                _minTop = Math.Min(_minTop, UserSettings.All.SelectedRegion.Top + SystemParameters.VirtualScreenTop);
                _maxRight = Math.Max(_maxRight, UserSettings.All.SelectedRegion.Right + SystemParameters.VirtualScreenLeft);
                _maxBottom = Math.Max(_maxBottom, UserSettings.All.SelectedRegion.Bottom + SystemParameters.VirtualScreenTop);

                var rect = new Border
                {
                    BorderBrush = TryFindResource("Element.Border.Required") as SolidColorBrush ?? Brushes.DarkBlue,
                    BorderThickness = new Thickness(3),
                    Background = TryFindResource("Panel.Background.Level3") as SolidColorBrush ?? Brushes.WhiteSmoke,
                    Width = UserSettings.All.SelectedRegion.Width,
                    Height = UserSettings.All.SelectedRegion.Height,
                    Tag = "N",
                    Child = new Viewbox
                    {
                        Child = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Padding = new Thickness(2),
                            Text = LocalizationHelper.Get("S.StartUp.Recorder") + " 2",
                            Foreground = TryFindResource("Element.Foreground") as SolidColorBrush ?? Brushes.Black,
                        }
                    }
                };

                MainCanvas.Children.Add(rect);

                Canvas.SetLeft(rect, UserSettings.All.SelectedRegion.Left + SystemParameters.VirtualScreenLeft);
                Canvas.SetTop(rect, UserSettings.All.SelectedRegion.Top + SystemParameters.VirtualScreenTop);
            }

            #endregion

            if (!double.IsNaN(UserSettings.All.EditorTop) && !double.IsNaN(UserSettings.All.EditorLeft))
            {
                _minLeft = Math.Min(_minLeft, UserSettings.All.EditorLeft);
                _minTop = Math.Min(_minTop, UserSettings.All.EditorTop);
                _maxRight = Math.Max(_maxRight, UserSettings.All.EditorLeft + UserSettings.All.EditorWidth);
                _maxBottom = Math.Max(_maxBottom, UserSettings.All.EditorTop + UserSettings.All.EditorHeight);

                var rect = new Border
                {
                    BorderBrush = TryFindResource("Element.Border.Required") as SolidColorBrush ?? Brushes.DarkBlue,
                    BorderThickness = new Thickness(3),
                    Background = TryFindResource("Panel.Background.Level3") as SolidColorBrush ?? Brushes.WhiteSmoke,
                    Width = UserSettings.All.EditorWidth,
                    Height = UserSettings.All.EditorHeight,
                    Tag = "N",
                    Child = new Viewbox
                    {
                        Child = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Padding = new Thickness(2),
                            Text = LocalizationHelper.Get("S.StartUp.Editor"),
                            Foreground = TryFindResource("Element.Foreground") as SolidColorBrush ?? Brushes.Black,
                        }
                    }
                };

                MainCanvas.Children.Add(rect);

                Canvas.SetLeft(rect, UserSettings.All.EditorLeft);
                Canvas.SetTop(rect, UserSettings.All.EditorTop);
            }

            if (!double.IsNaN(UserSettings.All.StartupTop) && !double.IsNaN(UserSettings.All.StartupLeft))
            {
                _minLeft = Math.Min(_minLeft, UserSettings.All.StartupLeft);
                _minTop = Math.Min(_minTop, UserSettings.All.StartupTop);
                _maxRight = Math.Max(_maxRight, UserSettings.All.StartupLeft + UserSettings.All.StartupWidth);
                _maxBottom = Math.Max(_maxBottom, UserSettings.All.StartupTop + UserSettings.All.StartupHeight);

                var rect = new Border
                {
                    BorderBrush = TryFindResource("Element.Border.Required") as SolidColorBrush ?? Brushes.DarkBlue,
                    BorderThickness = new Thickness(3),
                    Background = TryFindResource("Panel.Background.Level3") as SolidColorBrush ?? Brushes.WhiteSmoke,
                    Width = UserSettings.All.StartupWidth,
                    Height = UserSettings.All.StartupHeight,
                    Tag = "N",
                    Child = new Viewbox
                    {
                        Child = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Padding = new Thickness(2),
                            Text = (LocalizationHelper.Get("S.StartUp.Title") ?? "").Remove("ScreenToGif - "),
                            Foreground = TryFindResource("Element.Foreground") as SolidColorBrush ?? Brushes.Black,
                        }
                    }
                };

                MainCanvas.Children.Add(rect);

                Canvas.SetLeft(rect, UserSettings.All.StartupLeft);
                Canvas.SetTop(rect, UserSettings.All.StartupTop);
            }

            var index = 1;
            foreach (var border in MainCanvas.Children.OfType<Border>().OrderBy(o => o.ActualWidth).ThenBy(t => t.ActualHeight))
                Panel.SetZIndex(border, index++);
        }

        CurrentTextBlock.IsEnabled = MainCanvas.Children.OfType<Border>().Count(c => (string)c.Tag == "C") > 0;
        NextTextBlock.IsEnabled = MainCanvas.Children.OfType<Border>().Count(c => (string) c.Tag == "N") > 0;
            
        foreach (var monitor in monitors)
        {
            var rect = new Rectangle
            {
                Width = monitor.NativeBounds.Width,
                Height = monitor.NativeBounds.Height,
                StrokeThickness = 6
            };
            rect.SetResourceReference(Shape.StrokeProperty, "Element.Foreground");
            rect.SetResourceReference(Shape.FillProperty, monitor.IsPrimary ? "Element.Background.Checked" : "Element.Background.Hover");

            MainCanvas.Children.Add(rect);

            Canvas.SetLeft(rect, monitor.NativeBounds.Left);
            Canvas.SetTop(rect, monitor.NativeBounds.Top);
            Panel.SetZIndex(rect, 1);
        }

        MainCanvas.Width = Math.Abs(_minLeft) + Math.Abs(_maxRight);
        MainCanvas.Height = Math.Abs(_minTop) + Math.Abs(_maxBottom);

        SetViewPort(_minLeft, _maxRight, _minTop, _maxBottom);
    }

    private void SetViewPort(double minX, double maxX, double minY, double maxY)
    {
        var width = maxX - minX;
        var height = maxY - minY;

        var group = new TransformGroup();
        group.Children.Add(new TranslateTransform(-minX, -minY));
        group.Children.Add(new ScaleTransform(MainCanvas.ActualWidth / width, MainCanvas.ActualHeight / height));
        MainCanvas.RenderTransform = group;
    }
}