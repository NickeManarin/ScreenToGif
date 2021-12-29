using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using ScreenToGif.Util.Codification;

//Nicke Manarin - ScreenToGif - 26/02/2014, Updated 16/10/2016, Updated 31/05/2018, Again in 26/09/2019, 28/06/2020.

namespace ScreenToGif.Windows.Other;

public partial class ColorSelector : Window
{
    #region Properties and variables

    /// <summary>
    /// The selected color.
    /// </summary>
    public Color SelectedColor { get; set; }

    private readonly TranslateTransform _markerTransform = new();
    private Point? _colorPosition;
    private Size _captureSize;
    private bool _isUpdating = false;

    #endregion

    public ColorSelector(Color selectedColor, bool showAlpha = true)
    {
        InitializeComponent();

        SelectedColor = selectedColor;

        UpdateMarkerPosition(SelectedColor);
        LastColor.Background = CurrentColor.Background;

        ColorMarker.RenderTransform = _markerTransform;
        ColorMarker.RenderTransformOrigin = new Point(0.5, 0.5);

        if (!showAlpha)
        {
            AlphaIntegerUpDown.Visibility = Visibility.Collapsed;
            AlphaLabel.Visibility = Visibility.Collapsed;
            ColorHexadecimalBox.DisplayAlpha = false;
            AlphaSlider.Visibility = Visibility.Collapsed;
            MinHeight = 350;
        }

        InitialColor.Background = CurrentColor.Background = LastColor.Background = new SolidColorBrush(selectedColor);
    }

    #region Events

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isUpdating = true;

        AlphaIntegerUpDown.Value = SelectedColor.A;
        RedIntegerUpDown.Value = SelectedColor.R;
        GreenIntegerUpDown.Value = SelectedColor.G;
        BlueIntegerUpDown.Value = SelectedColor.B;

        _isUpdating = false;
    }

    private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_colorPosition != null)
            DetermineColor((Point) _colorPosition);
    }
        
    private void ColorDetailBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(ColorDetail);
        var p = e.GetPosition(ColorDetail);

        UpdateMarkerPosition(p);
        LastColor.Background = CurrentColor.Background;
    }

    private void ColorDetailBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var p = e.GetPosition(ColorDetail);
        var withinBoundaries = new Point(Math.Max(0, Math.Min(p.X, ColorDetail.ActualWidth)), Math.Max(0, Math.Min(p.Y, ColorDetail.ActualHeight)));

        UpdateMarkerPosition(withinBoundaries);
        Mouse.Synchronize();
    }

    private void ColorDetailBorder_SizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (args.PreviousSize != Size.Empty && args.PreviousSize.Width != 0 && args.PreviousSize.Height != 0)
        {
            var widthDifference = args.NewSize.Width / args.PreviousSize.Width;
            var heightDifference = args.NewSize.Height / args.PreviousSize.Height;

            _markerTransform.X *= widthDifference;
            _markerTransform.Y *= heightDifference;
        }
        else if (_colorPosition != null)
        {
            _markerTransform.X = ((Point)_colorPosition).X * args.NewSize.Width;
            _markerTransform.Y = ((Point)_colorPosition).Y * args.NewSize.Height;
        }
    }

    private void ColorDetailBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null); //Release it.
        LastColor.Background = CurrentColor.Background;
    }

    private void InitialColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SelectedColor = ((SolidColorBrush)InitialColor.Background).Color;

        UpdateMarkerPosition(SelectedColor);
        LastColor.Background = CurrentColor.Background;

        #region Update the values

        _isUpdating = true;

        AlphaIntegerUpDown.Value = SelectedColor.A;
        RedIntegerUpDown.Value = SelectedColor.R;
        GreenIntegerUpDown.Value = SelectedColor.G;
        BlueIntegerUpDown.Value = SelectedColor.B;

        _isUpdating = false;

        #endregion
    }

    private void ColorSlider_OnAfterSelecting()
    {
        LastColor.Background = CurrentColor.Background;
    }

    private void ArgbText_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (AlphaIntegerUpDown == null || _isUpdating)
            return;

        SelectedColor = Color.FromArgb((byte)AlphaIntegerUpDown.Value, (byte)RedIntegerUpDown.Value, (byte)GreenIntegerUpDown.Value, (byte)BlueIntegerUpDown.Value);
            
        UpdateMarkerPosition(SelectedColor);
        LastColor.Background = CurrentColor.Background;
    }

    private void ValueBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!(sender is IntegerUpDown textBox))
            return;

        textBox.Value = e.Delta > 0 ? textBox.Value + 1 : textBox.Value - 1;
    }

    private void EyeDropperButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(EyeDropperButton);

        _captureSize = new Size(Math.Round(EyeDropperButton.ActualWidth / 6d, 0), Math.Round(EyeDropperButton.ActualHeight / 6d, 0));

        EyeDropperButton.PreviewMouseUp += EyeDropperButton_PreviewMouseUp;
        EyeDropperButton.PreviewMouseMove += EyeDropperButton_PreviewMouseMove;

        Cursor = Cursors.Cross;
        EyeDropperImage.Opacity = 1;
        EyeDropperButton.Opacity = 0;
    }

    private void EyeDropperButton_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        var str = new PointW();
        User32.GetCursorPos(ref str);

        var image = Native.Helpers.Capture.CaptureScreenAsBitmapSource((int)_captureSize.Width, (int)_captureSize.Height, str.X - (int)(_captureSize.Width / 2d), str.Y - (int)(_captureSize.Height / 2d));

        if (image.Format != PixelFormats.Bgra32)
            image = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);

        EyeDropperImage.Source = image;

        var pix = new PixelUtil(image);
        pix.LockBits();
        UpdateMarkerPosition(pix.GetPixel((int)(_captureSize.Width / 2d), (int)(_captureSize.Height / 2d)));

        #region Update the values

        _isUpdating = true;

        AlphaIntegerUpDown.Value = SelectedColor.A;
        RedIntegerUpDown.Value = SelectedColor.R;
        GreenIntegerUpDown.Value = SelectedColor.G;
        BlueIntegerUpDown.Value = SelectedColor.B;

        _isUpdating = false;

        #endregion

        pix.UnlockBits();
    }

    private void EyeDropperButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        EyeDropperButton.ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        EyeDropperImage.Opacity = 0;
        EyeDropperButton.Opacity = 1;
        EyeDropperImage.Source = null;

        EyeDropperButton.PreviewMouseUp -= EyeDropperButton_PreviewMouseUp;
        EyeDropperButton.PreviewMouseMove -= EyeDropperButton_PreviewMouseMove;
    }
        
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    #endregion

    #region Methods

    private void UpdateMarkerPosition(Point p)
    {
        _markerTransform.X = p.X;
        _markerTransform.Y = p.Y;
        p.X /= ColorDetail.ActualWidth;
        p.Y /= ColorDetail.ActualHeight;
        _colorPosition = p;

        DetermineColor(p);
    }

    private void UpdateMarkerPosition(Color theColor)
    {
        _colorPosition = null;

        var hsv = ColorExtensions.ConvertRgbToHsv(theColor.R, theColor.G, theColor.B);

        CurrentColor.Background = new SolidColorBrush(theColor);
        ColorSlider.Value = hsv.H;
        AlphaSlider.SpectrumColor = theColor;
        AlphaSlider.Value = theColor.A;

        var p = new Point(hsv.S, 1 - hsv.V);

        _colorPosition = p;
        p.X *= ColorDetail.ActualWidth;
        p.Y *= ColorDetail.ActualHeight;
        _markerTransform.X = p.X;
        _markerTransform.Y = p.Y;

        SelectedColor = theColor;
    }

    private void DetermineColor(Point p)
    {
        var hsv = new HsvColor(360 - ColorSlider.Value, 1, 1)
        {
            S = p.X,
            V = 1 - p.Y
        };

        SelectedColor = ColorExtensions.ConvertHsvToRgb(hsv.H, hsv.S, hsv.V, AlphaSlider.Value);

        CurrentColor.Background = new SolidColorBrush(SelectedColor);
        AlphaSlider.SpectrumColor = SelectedColor;

        #region Update TextBoxes

        _isUpdating = true;

        AlphaIntegerUpDown.Value = SelectedColor.A;
        RedIntegerUpDown.Value = SelectedColor.R;
        GreenIntegerUpDown.Value = SelectedColor.G;
        BlueIntegerUpDown.Value = SelectedColor.B;

        _isUpdating = false;

        #endregion
    }

    #endregion
}