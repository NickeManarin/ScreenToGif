using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace ScreenToGif.ViewModel;

public class BoardRecorderViewModel : BindableBase
{
    private InkCanvasEditingMode _editingMode = InkCanvasEditingMode.InkAndGesture;
    private Color _brushColor = UserSettings.All.BoardColor;
    private int _brushWidth = UserSettings.All.BoardStylusWidth;
    private int _brushHeight = UserSettings.All.BoardStylusHeight;
    private StylusTip _stylusTip = UserSettings.All.BoardStylusTip;
    private bool _fitToCurve = UserSettings.All.BoardFitToCurve;
    private bool _isHighlighter = UserSettings.All.BoardIsHighlighter;
    private int _eraserBrushWidth = UserSettings.All.BoardEraserWidth;
    private int _eraserBrushHeight = UserSettings.All.BoardEraserHeight;
    private StylusTip _eraserStylusTip = UserSettings.All.BoardEraserStylusTip;
    
    public DrawingAttributes DrawingAttributes => new()
    {
        Color = BrushColor,
        Width = BrushWidth,
        Height = BrushHeight,
        FitToCurve = FitToCurve,
        IsHighlighter = IsHighlighter,
        StylusTip = StylusTip
    }; 

    public Cursor Cursor
    {
        get
        {
            var rtb = new RenderTargetBitmap(BrushWidth, BrushHeight, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();

            var backColor = IsHighlighter ? Color.FromArgb((byte)(BrushColor.A * 0.8), BrushColor.R, BrushColor.G, BrushColor.B) : BrushColor;

            using (var dc = dv.RenderOpen())
            {
                if (StylusTip is StylusTip.Ellipse)
                    dc.DrawEllipse(new SolidColorBrush(backColor), new Pen(Brushes.DimGray, 1), new Point(BrushWidth / 2D, BrushHeight / 2D), BrushWidth / 2D - 1, BrushHeight / 2D - 1);
                else
                    dc.DrawRectangle(new SolidColorBrush(backColor), new Pen(Brushes.DimGray, 1), new Rect(0, 0, BrushWidth - 1, BrushHeight - 1));
            }

            rtb.Render(dv);

            return ConvertToCursor(rtb, new Point(BrushWidth / 2D, BrushHeight / 2D));
        }
    }

    public bool ForceCursor => EditingMode is InkCanvasEditingMode.InkAndGesture or InkCanvasEditingMode.Ink;

    public InkCanvasEditingMode EditingMode
    {
        get => _editingMode;
        set
        {
            SetProperty(ref _editingMode, value);
            
            OnPropertyChanged(nameof(Cursor));
            OnPropertyChanged(nameof(ForceCursor));
            OnPropertyChanged(nameof(BrushSettingsVisibility));
            OnPropertyChanged(nameof(EraserBrushSettingsVisibility));
        }
    }

    public bool IsInkMode
    {
        get => EditingMode is InkCanvasEditingMode.Ink or InkCanvasEditingMode.InkAndGesture;
        set
        {
            if (value)
                EditingMode = InkCanvasEditingMode.InkAndGesture;
        }
    }

    public bool IsEraserMode
    {
        get => EditingMode is InkCanvasEditingMode.EraseByPoint;
        set
        {
            if (value)
                EditingMode = InkCanvasEditingMode.EraseByPoint;
        }
    }

    public bool IsStrokeEraserMode
    {
        get => EditingMode is InkCanvasEditingMode.EraseByStroke;
        set
        {
            if (value)
                EditingMode = InkCanvasEditingMode.EraseByStroke;
        }
    }

    public bool IsSelectionMode
    {
        get => EditingMode is InkCanvasEditingMode.Select;
        set
        {
            if (value)
                EditingMode = InkCanvasEditingMode.Select;
        }
    }

    public Color BrushColor
    {
        get => _brushColor;
        set
        {
            SetProperty(ref _brushColor, value);

            UserSettings.All.BoardColor = value;

            OnPropertyChanged(nameof(DrawingAttributes));
            OnPropertyChanged(nameof(Cursor));
            OnPropertyChanged(nameof(ForceCursor));
        }
    }

    public int BrushWidth
    {
        get => _brushWidth;
        set
        {
            SetProperty(ref _brushWidth, value);

            UserSettings.All.BoardStylusWidth = value;

            OnPropertyChanged(nameof(DrawingAttributes));
            OnPropertyChanged(nameof(Cursor));
            OnPropertyChanged(nameof(ForceCursor));
        }
    }

    public int BrushHeight
    {
        get => _brushHeight;
        set
        {
            SetProperty(ref _brushHeight, value);

            UserSettings.All.BoardStylusHeight = value;

            OnPropertyChanged(nameof(DrawingAttributes));
            OnPropertyChanged(nameof(Cursor));
            OnPropertyChanged(nameof(ForceCursor));
        }
    }

    public StylusTip StylusTip
    {
        get => _stylusTip;
        set
        {
            SetProperty(ref _stylusTip, value);

            UserSettings.All.BoardStylusTip = value;

            OnPropertyChanged(nameof(DrawingAttributes));
            OnPropertyChanged(nameof(Cursor));
            OnPropertyChanged(nameof(ForceCursor));
            OnPropertyChanged(nameof(StylusTipEllipse));
            OnPropertyChanged(nameof(StylusTipRectangle));
        }
    }

    public bool StylusTipEllipse
    {
        get => StylusTip is StylusTip.Ellipse;
        set => StylusTip = value ? StylusTip.Ellipse : StylusTip.Rectangle;
    }

    public bool StylusTipRectangle
    {
        get => StylusTip is StylusTip.Rectangle;
        set => StylusTip = value ? StylusTip.Rectangle : StylusTip.Ellipse;
    }

    public Visibility BrushSettingsVisibility => EditingMode is InkCanvasEditingMode.Ink or InkCanvasEditingMode.InkAndGesture ? Visibility.Visible : Visibility.Collapsed;

    public bool FitToCurve
    {
        get => _fitToCurve;
        set
        {
            SetProperty(ref _fitToCurve, value);

            UserSettings.All.BoardFitToCurve = value;

            OnPropertyChanged(nameof(DrawingAttributes));
        }
    }

    public bool IsHighlighter
    {
        get => _isHighlighter;
        set
        {
            SetProperty(ref _isHighlighter, value);

            UserSettings.All.BoardIsHighlighter = value;

            OnPropertyChanged(nameof(DrawingAttributes));
            OnPropertyChanged(nameof(Cursor));
            OnPropertyChanged(nameof(ForceCursor));
        }
    }

    public StylusShape EraserShape => EraserStylusTip == StylusTip.Ellipse ? new EllipseStylusShape(EraserBrushWidth, EraserBrushHeight) : new RectangleStylusShape(EraserBrushWidth, EraserBrushHeight);

    public int EraserBrushWidth
    {
        get => _eraserBrushWidth;
        set
        {
            SetProperty(ref _eraserBrushWidth, value);

            UserSettings.All.BoardEraserWidth = value;

            OnPropertyChanged(nameof(EraserShape));
        }
    }

    public int EraserBrushHeight
    {
        get => _eraserBrushHeight;
        set
        {
            SetProperty(ref _eraserBrushHeight, value);

            UserSettings.All.BoardEraserHeight = value;

            OnPropertyChanged(nameof(EraserShape));
        }
    }

    public StylusTip EraserStylusTip
    {
        get => _eraserStylusTip;
        set
        {
            SetProperty(ref _eraserStylusTip, value);

            UserSettings.All.BoardEraserStylusTip = value;

            OnPropertyChanged(nameof(EraserShape));
            OnPropertyChanged(nameof(EraserStylusTipEllipse));
            OnPropertyChanged(nameof(EraserStylusTipRectangle));
        }
    }

    public bool EraserStylusTipEllipse
    {
        get => EraserStylusTip is StylusTip.Ellipse;
        set => EraserStylusTip = value ? StylusTip.Ellipse : StylusTip.Rectangle;
    }

    public bool EraserStylusTipRectangle
    {
        get => EraserStylusTip is StylusTip.Rectangle;
        set => EraserStylusTip = value ? StylusTip.Rectangle : StylusTip.Ellipse;
    }

    public Visibility EraserBrushSettingsVisibility => EditingMode is InkCanvasEditingMode.EraseByPoint ? Visibility.Visible : Visibility.Collapsed;

    public Cursor ConvertToCursor(RenderTargetBitmap rtb, Point hotSpot)
    {
        using var pngStream = new MemoryStream();
        
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(rtb));
        png.Save(pngStream);

        //Write cursor header info.
        using var cursorStream = new MemoryStream();
        cursorStream.Write([0x00, 0x00], 0, 2); //IconDir: Reserved. Must always be 0.
        cursorStream.Write([0x02, 0x00], 0, 2); //IconDir: Specifies image type: 1 for icon (.ICO) image, 2 for cursor (.CUR) image. Other values are invalid
        cursorStream.Write([0x01, 0x00], 0, 2); //IconDir: Specifies number of images in the file.
        cursorStream.Write([(byte)rtb.PixelWidth], 0, 1); //IconDirEntry: Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
        cursorStream.Write([(byte)rtb.PixelHeight], 0, 1); //IconDirEntry: Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels.
        cursorStream.Write([0x00], 0, 1); //IconDirEntry: Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette.
        cursorStream.Write([0x00], 0, 1); //IconDirEntry: Reserved. Should be 0.
        cursorStream.Write([(byte)hotSpot.X, 0x00], 0, 2); //IconDirEntry: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
        cursorStream.Write([(byte)hotSpot.Y, 0x00], 0, 2); //IconDirEntry: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
        cursorStream.Write([ //IconDirEntry: Specifies the size of the image's data in bytes
          (byte)(pngStream.Length & 0x000000FF),
          (byte)((pngStream.Length & 0x0000FF00) >> 0x08),
          (byte)((pngStream.Length & 0x00FF0000) >> 0x10),
          (byte)((pngStream.Length & 0xFF000000) >> 0x18)
        ], 0, 4);
        cursorStream.Write([0x16, 0x00, 0x00, 0x00], 0, 4); //IconDirEntry: Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file

        pngStream.Seek(0, SeekOrigin.Begin);
        pngStream.CopyTo(cursorStream);

        cursorStream.Seek(0, SeekOrigin.Begin);
        return new Cursor(cursorStream);
    }

    public Cursor ConvertToCursor(UIElement control, Point hotSpot)
    {
        control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var rect = new Rect(0, 0, control.DesiredSize.Width, control.DesiredSize.Height);
        var rtb = new RenderTargetBitmap((int)control.DesiredSize.Width, (int)control.DesiredSize.Height, 96, 96, PixelFormats.Pbgra32);

        control.Arrange(rect);
        rtb.Render(control);

        using var pngStream = new MemoryStream();
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(rtb));
        png.Save(pngStream);

        //Write cursor header info.
        using var cursorStream = new MemoryStream();
        cursorStream.Write([0x00, 0x00], 0, 2); //IconDir: Reserved. Must always be 0.
        cursorStream.Write([0x02, 0x00], 0, 2); //IconDir: Specifies image type: 1 for icon (.ICO) image, 2 for cursor (.CUR) image. Other values are invalid
        cursorStream.Write([0x01, 0x00], 0, 2); //IconDir: Specifies number of images in the file.
        cursorStream.Write([(byte)control.DesiredSize.Width], 0, 1); //IconDirEntry: Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
        cursorStream.Write([(byte)control.DesiredSize.Height], 0, 1); //IconDirEntry: Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels.
        cursorStream.Write([0x00], 0, 1); //IconDirEntry: Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette.
        cursorStream.Write([0x00], 0, 1); //IconDirEntry: Reserved. Should be 0.
        cursorStream.Write([(byte)hotSpot.X, 0x00], 0, 2); //IconDirEntry: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
        cursorStream.Write([(byte)hotSpot.Y, 0x00], 0, 2); //IconDirEntry: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
        cursorStream.Write([ //IconDirEntry: Specifies the size of the image's data in bytes
            (byte)(pngStream.Length & 0x000000FF),
            (byte)((pngStream.Length & 0x0000FF00) >> 0x08),
            (byte)((pngStream.Length & 0x00FF0000) >> 0x10),
            (byte)((pngStream.Length & 0xFF000000) >> 0x18)
        ], 0, 4);
        cursorStream.Write([0x16, 0x00, 0x00, 0x00], 0, 4); //IconDirEntry: Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file

        pngStream.Seek(0, SeekOrigin.Begin);
        pngStream.CopyTo(cursorStream);

        cursorStream.Seek(0, SeekOrigin.Begin);
        return new Cursor(cursorStream);
    }
}
