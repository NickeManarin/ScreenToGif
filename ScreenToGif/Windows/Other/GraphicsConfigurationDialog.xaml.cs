using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScreenToGif.Domain.Models.Native;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other;

public partial class GraphicsConfigurationDialog : Window
{
    #region Properties

    private double _minLeft = SystemParameters.VirtualScreenLeft;
    private double _minTop = SystemParameters.VirtualScreenTop;
    private double _maxRight = SystemParameters.VirtualScreenWidth;
    private double _maxBottom = SystemParameters.VirtualScreenHeight;

    public Exception Exception { get; set; }

    public Monitor Monitor { get; set; }

    #endregion

    public GraphicsConfigurationDialog()
    {
        InitializeComponent();
            
        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    #region Eventos

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (Exception == null)
            DetailsButton.IsEnabled = false;
    }

    private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
    {
        DetectScreens();
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        var errorViewer = new ExceptionViewer(Exception);
        errorViewer.ShowDialog();
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        try
        {
            Process.Start("ms-settings:display-advancedgraphics");
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to open Windows Settings");
        }
    }

    private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SetViewPort(_minLeft, _maxRight, _minTop, _maxBottom);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var feedback = new Feedback { Topmost = true };

        if (feedback.ShowDialog() != true)
            return;

        if (App.MainViewModel != null)
            await Task.Factory.StartNew(App.MainViewModel.SendFeedback, TaskCreationOptions.LongRunning);
    }

    #endregion

    #region Methods

    private void PrepareOk()
    {
        //No Graphics Settings page prior to Windows 10, build 17093.
        if (Environment.OSVersion.Version.Major < 10 || Environment.OSVersion.Version.Build < 17093)
        {
            ActionTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Warning.Graphics.Action.Legacy");
            HyperlinkTextBlock.Visibility = Visibility.Collapsed;
        }

        DetectScreens();
        OkButton.Focus();
    }

    public static bool Ok(Exception exception, Monitor monitor)
    {
        var dialog = new GraphicsConfigurationDialog
        {
            Exception = exception,
            Monitor = monitor
        };
        dialog.PrepareOk();
        var result = dialog.ShowDialog();

        return result.HasValue && result.Value;
    }

    private void DetectScreens()
    {
        var monitors = MonitorHelper.AllMonitorsGranular();
        _minLeft = monitors.Min(m => m.NativeBounds.Left);
        _minTop = monitors.Min(m => m.NativeBounds.Top);
        _maxRight = monitors.Max(m => m.NativeBounds.Right);
        _maxBottom = monitors.Max(m => m.NativeBounds.Bottom);

        MainCanvas.Children.Clear();

        foreach (var monitor in monitors)
        {
            var rect = new Rectangle
            {
                Width = monitor.NativeBounds.Width,
                Height = monitor.NativeBounds.Height,
                StrokeThickness = 6
            };
            rect.SetResourceReference(Shape.StrokeProperty, "Element.Foreground");
            rect.SetResourceReference(Shape.FillProperty, monitor.AdapterName == Monitor.AdapterName ? "Element.Background.Checked" : "Element.Background.Hover");

            var textBlock = new TextBlock
            {
                Text = monitor.AdapterName,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 26,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(15)
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "Element.Foreground");

            var viewbox = new Viewbox
            {
                Child = textBlock,
                Width = monitor.NativeBounds.Width,
                Height = monitor.NativeBounds.Height,
            };

            MainCanvas.Children.Add(rect);
            MainCanvas.Children.Add(viewbox);

            Canvas.SetLeft(rect, monitor.NativeBounds.Left);
            Canvas.SetTop(rect, monitor.NativeBounds.Top);
            Canvas.SetLeft(viewbox, monitor.NativeBounds.Left);
            Canvas.SetTop(viewbox, monitor.NativeBounds.Top);
            Panel.SetZIndex(rect, 1);
            Panel.SetZIndex(viewbox, 2);
        }

        MainCanvas.Width = Math.Abs(_minLeft) + Math.Abs(_maxRight);
        MainCanvas.Height = Math.Abs(_minTop) + Math.Abs(_maxBottom);
        MainCanvas.Measure(new Size(MainCanvas.Width, MainCanvas.Height));
        MainCanvas.Arrange(new Rect(MainCanvas.DesiredSize));

        SetViewPort(_minLeft, _maxRight, _minTop, _maxBottom);
    }

    public void SetViewPort(double minX, double maxX, double minY, double maxY)
    {
        var width = maxX - minX;
        var height = maxY - minY;

        var group = new TransformGroup();
        group.Children.Add(new TranslateTransform(-minX, -minY));
        group.Children.Add(new ScaleTransform(MainCanvas.ActualWidth / width, MainCanvas.ActualHeight / height));
        MainCanvas.RenderTransform = group;
    }

    #endregion
}