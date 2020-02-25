using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Troubleshoot : Window
    {
        public Troubleshoot()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Windows.OfType<Window>().Any(a => a.GetType() != typeof(Troubleshoot)))
                NowRadioButton.IsChecked = true;
            else
                LaterRadioButton.IsChecked = true;
        }

        private void KindRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            DetectMonitors();
        }

        private async void HyperlinkMove_Click(object sender, RoutedEventArgs e)
        {
            var monitor = Monitor.AllMonitorsScaled(this.Scale()).FirstOrDefault(f => f.IsPrimary);

            if (monitor == null)
            {
                //Uh-oh.
                return;
            }

            //Move all windows to the main monitor.
            foreach (var window in Application.Current.Windows.OfType<Window>().Where(w => w.GetType() != typeof(Troubleshoot)).OrderBy(o => o.Width).ThenBy(o => o.Height))
            {
                var top = window.Top;
                var left = window.Left;
                var width = window.ActualWidth;
                var height = window.ActualHeight;

                //Pause any active recording...
                if (window is RecorderNew newRecorder)
                {
                    if (newRecorder.Stage == Stage.Recording)
                        await newRecorder.RecordPause();

                    //I need to adjust to the Left/Top Bounds since the recorder ocupies the all windows.

                    top = Canvas.GetTop(newRecorder.MainBorder) + newRecorder.Top;
                    left = Canvas.GetLeft(newRecorder.MainBorder) + newRecorder.Left;
                    width = newRecorder.MainBorder.ActualWidth;
                    height = newRecorder.MainBorder.ActualHeight;

                    if (monitor.Bounds.Top > top)
                        top = monitor.Bounds.Top;

                    if (monitor.Bounds.Left > left)
                        left = monitor.Bounds.Left;

                    if (monitor.Bounds.Bottom < top + height)
                        top = monitor.Bounds.Bottom - height;

                    if (monitor.Bounds.Right < left + width)
                        left = monitor.Bounds.Right - width;

                    Canvas.SetTop(newRecorder.MainBorder, top - newRecorder.Top);
                    Canvas.SetLeft(newRecorder.MainBorder, left - newRecorder.Left);

                    continue;
                }

                if (window is Recorder recorder)
                {
                    if (recorder.Stage == Stage.Recording)
                        await recorder.RecordPause();
                }

                if (monitor.Bounds.Top > top)
                    top = monitor.Bounds.Top;

                if (monitor.Bounds.Left > left)
                    left = monitor.Bounds.Left;

                if (monitor.Bounds.Bottom < top + height)
                    top = monitor.Bounds.Bottom - height;

                if (monitor.Bounds.Right < left + width)
                    left = monitor.Bounds.Right - width;

                window.Top = top;
                window.Left = left;
                window.WindowState = WindowState.Normal;
                //window.Width = width;
                //window.Height = height;
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
            var monitors = Monitor.AllMonitorsScaled(this.Scale());
            var minLeft = monitors.Min(m => m.Bounds.Left);
            var minTop = monitors.Min(m => m.Bounds.Top);
            var maxRight = monitors.Max(m => m.Bounds.Right);
            var maxBottom = monitors.Max(m => m.Bounds.Bottom);

            MainCanvas.Children.Clear();

            if (NowRadioButton.IsChecked == true)
            {
                foreach (var window in Application.Current.Windows.OfType<Window>().Where(w => w.GetType() != typeof(Troubleshoot) && w.IsVisible).OrderBy(o => o.Width).ThenBy(o => o.Height))
                {
                    var top = window.Top;
                    var left = window.Left;
                    var width = window.ActualWidth;
                    var height = window.ActualHeight;
                    var title = window.Title.Remove("ScreenToGif - ");

                    if (window is RecorderNew newRecorder)
                    {
                        top = Canvas.GetTop(newRecorder.MainBorder) + newRecorder.Top;
                        left = Canvas.GetLeft(newRecorder.MainBorder) + newRecorder.Left;
                        width = newRecorder.MainBorder.ActualWidth;
                        height = newRecorder.MainBorder.ActualHeight;
                        title = LocalizationHelper.Get("S.StartUp.Recorder");
                    }
                    
                    minLeft = Math.Min(minLeft, left);
                    minTop = Math.Min(minTop, top);
                    maxRight = Math.Max(maxRight, left + width);
                    maxBottom = Math.Max(maxBottom, top + height);

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
                    minLeft = Math.Min(minLeft, UserSettings.All.RecorderLeft);
                    minTop = Math.Min(minTop, UserSettings.All.RecorderTop);
                    maxRight = Math.Max(maxRight, UserSettings.All.RecorderLeft + UserSettings.All.RecorderWidth);
                    maxBottom = Math.Max(maxBottom, UserSettings.All.RecorderTop + UserSettings.All.RecorderHeight);

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
                    minLeft = Math.Min(minLeft, UserSettings.All.SelectedRegion.Left + SystemParameters.VirtualScreenLeft);
                    minTop = Math.Min(minTop, UserSettings.All.SelectedRegion.Top + SystemParameters.VirtualScreenTop);
                    maxRight = Math.Max(maxRight, UserSettings.All.SelectedRegion.Right + SystemParameters.VirtualScreenLeft);
                    maxBottom = Math.Max(maxBottom, UserSettings.All.SelectedRegion.Bottom + SystemParameters.VirtualScreenTop);

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
                    minLeft = Math.Min(minLeft, UserSettings.All.EditorLeft);
                    minTop = Math.Min(minTop, UserSettings.All.EditorTop);
                    maxRight = Math.Max(maxRight, UserSettings.All.EditorLeft + UserSettings.All.EditorWidth);
                    maxBottom = Math.Max(maxBottom, UserSettings.All.EditorTop + UserSettings.All.EditorHeight);

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
                    minLeft = Math.Min(minLeft, UserSettings.All.StartupLeft);
                    minTop = Math.Min(minTop, UserSettings.All.StartupTop);
                    maxRight = Math.Max(maxRight, UserSettings.All.StartupLeft + UserSettings.All.StartupWidth);
                    maxBottom = Math.Max(maxBottom, UserSettings.All.StartupTop + UserSettings.All.StartupHeight);

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

                int index = 1;
                foreach (var border in MainCanvas.Children.OfType<Border>().OrderBy(o => o.ActualWidth).ThenBy(t => t.ActualHeight))
                    Panel.SetZIndex(border, index++);
            }

            CurrentTextBlock.IsEnabled = MainCanvas.Children.OfType<Border>().Count(c => (string)c.Tag == "C") > 0;
            NextTextBlock.IsEnabled = MainCanvas.Children.OfType<Border>().Count(c => (string) c.Tag == "N") > 0;
            
            foreach (var monitor in monitors)
            {
                var rect = new Rectangle
                {
                    Stroke = TryFindResource("Element.Foreground") as SolidColorBrush ??  Brushes.Black,
                    Fill = monitor.IsPrimary ? TryFindResource("Element.Background.Checked") as SolidColorBrush ?? Brushes.LightBlue : TryFindResource("Element.Background.Hover") as SolidColorBrush ?? Brushes.LightGray,
                    Width = monitor.Bounds.Width,
                    Height = monitor.Bounds.Height,
                    StrokeThickness = 6,
                };

                MainCanvas.Children.Add(rect);

                Canvas.SetLeft(rect, monitor.Bounds.Left);
                Canvas.SetTop(rect, monitor.Bounds.Top);
                Panel.SetZIndex(rect, 1);
            }

            MainCanvas.SizeChanged += (args, o) => SetViewPort(MainCanvas, minLeft, maxRight, minTop, maxBottom);
            MainCanvas.Width = Math.Abs(minLeft) + Math.Abs(maxRight);
            MainCanvas.Height = Math.Abs(minTop) + Math.Abs(maxBottom);
        }

        public static void SetViewPort(Canvas canvas, double minX, double maxX, double minY, double maxY)
        {
            var width = maxX - minX;
            var height = maxY - minY;

            var group = new TransformGroup();
            group.Children.Add(new TranslateTransform(-minX, -minY));
            group.Children.Add(new ScaleTransform(canvas.ActualWidth / width, canvas.ActualHeight / height));
            canvas.RenderTransform = group;
        }
    }
}