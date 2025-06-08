using ScreenToGif.Domain.Models.Project;
using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util.Settings;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenToGif.ViewModel;

public class EditorViewModel : BaseViewModel
{
    private Project _project = null;
    private TimeSpan _currentTime = TimeSpan.Zero;
    private int _currentIndex = -1;
    private WriteableBitmap _renderedImage = null;
    private double _zoom = 1d;

    //Free Drawing.
    private InkCanvasEditingMode _freeDrawingEditingMode = InkCanvasEditingMode.InkAndGesture;
    private Color _freeDrawingBrushColor = UserSettings.All.FreeDrawingColor;
    private int _freeDrawingBrushWidth = UserSettings.All.FreeDrawingPenWidth;
    private int _freeDrawingBrushHeight = UserSettings.All.FreeDrawingPenHeight;
    private StylusTip _freeDrawingStylusTip = UserSettings.All.FreeDrawingStylusTip;
    private bool _freeDrawingFitToCurve = UserSettings.All.FreeDrawingFitToCurve;
    private bool _freeDrawingIsHighlighter = UserSettings.All.FreeDrawingIsHighlighter;
    private int _freeDrawingEraserBrushWidth = UserSettings.All.FreeDrawingEraserWidth;
    private int _freeDrawingEraserBrushHeight = UserSettings.All.FreeDrawingEraserHeight;
    private StylusTip _freeDrawingEraserStylusTip = UserSettings.All.FreeDrawingEraserStylusTip;

    private ObservableCollection<FrameViewModel> _frames = [];
    
    //Properties.
    public CommandBindingCollection CommandBindings => new()
    {
        new CommandBinding(FindCommand("Command.NewRecording"), (sender, args) => { Console.WriteLine(""); }, (sender, args) => { args.CanExecute = true; }),
        new CommandBinding(FindCommand("Command.NewWebcamRecording"), (sender, args) => { Console.WriteLine(""); }, (sender, args) => { args.CanExecute = true; }),
    };

    public Project Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        set => SetProperty(ref _currentIndex, value);
    }

    internal WriteableBitmap RenderedImage
    {
        get => _renderedImage;
        set => SetProperty(ref _renderedImage, value);
    }

    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }

    /// <summary>
    /// The list of frames.
    /// </summary>
    public ObservableCollection<FrameViewModel> Frames
    {
        get => _frames;
        set => SetProperty(ref _frames, value);
    }

    public DrawingAttributes FreeDrawingDrawingAttributes => new()
    {
        Color = FreeDrawingBrushColor,
        Width = FreeDrawingBrushWidth,
        Height = FreeDrawingBrushHeight,
        FitToCurve = FreeDrawingFitToCurve,
        IsHighlighter = FreeDrawingIsHighlighter,
        StylusTip = FreeDrawingStylusTip
    };

    public Cursor FreeDrawingCursor
    {
        get
        {
            var rtb = new RenderTargetBitmap(FreeDrawingBrushWidth, FreeDrawingBrushHeight, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();

            var backColor = FreeDrawingIsHighlighter ? Color.FromArgb((byte)(FreeDrawingBrushColor.A * 0.8), FreeDrawingBrushColor.R, FreeDrawingBrushColor.G, FreeDrawingBrushColor.B) : FreeDrawingBrushColor;

            using (var dc = dv.RenderOpen())
            {
                if (FreeDrawingStylusTip is StylusTip.Ellipse)
                    dc.DrawEllipse(new SolidColorBrush(backColor), new Pen(Brushes.DimGray, 1), new Point(FreeDrawingBrushWidth / 2D, FreeDrawingBrushHeight / 2D), FreeDrawingBrushWidth / 2D - 1, FreeDrawingBrushHeight / 2D - 1);
                else
                    dc.DrawRectangle(new SolidColorBrush(backColor), new Pen(Brushes.DimGray, 1), new Rect(0, 0, FreeDrawingBrushWidth - 1, FreeDrawingBrushHeight - 1));
            }

            rtb.Render(dv);

            return ConvertToCursor(rtb, new Point(FreeDrawingBrushWidth / 2D, FreeDrawingBrushHeight / 2D));
        }
    }

    public bool FreeDrawingForceCursor => FreeDrawingEditingMode is InkCanvasEditingMode.InkAndGesture or InkCanvasEditingMode.Ink;

    public InkCanvasEditingMode FreeDrawingEditingMode
    {
        get => _freeDrawingEditingMode;
        set
        {
            SetProperty(ref _freeDrawingEditingMode, value);
            
            OnPropertyChanged(nameof(FreeDrawingCursor));
            OnPropertyChanged(nameof(FreeDrawingForceCursor));
            OnPropertyChanged(nameof(FreeDrawingBrushSettingsVisibility));
            OnPropertyChanged(nameof(FreeDrawingEraserBrushSettingsVisibility));
        }
    }

    public bool FreeDrawingIsInkMode
    {
        get => FreeDrawingEditingMode is InkCanvasEditingMode.Ink or InkCanvasEditingMode.InkAndGesture;
        set
        {
            if (value)
                FreeDrawingEditingMode = InkCanvasEditingMode.InkAndGesture;
        }
    }

    public bool FreeDrawingIsEraserMode
    {
        get => FreeDrawingEditingMode is InkCanvasEditingMode.EraseByPoint;
        set
        {
            if (value)
                FreeDrawingEditingMode = InkCanvasEditingMode.EraseByPoint;
        }
    }

    public bool FreeDrawingIsStrokeEraserMode
    {
        get => FreeDrawingEditingMode is InkCanvasEditingMode.EraseByStroke;
        set
        {
            if (value)
                FreeDrawingEditingMode = InkCanvasEditingMode.EraseByStroke;
        }
    }

    public bool FreeDrawingIsSelectionMode
    {
        get => FreeDrawingEditingMode is InkCanvasEditingMode.Select;
        set
        {
            if (value)
                FreeDrawingEditingMode = InkCanvasEditingMode.Select;
        }
    }

    public Color FreeDrawingBrushColor
    {
        get => _freeDrawingBrushColor;
        set
        {
            SetProperty(ref _freeDrawingBrushColor, value);

            UserSettings.All.FreeDrawingColor = value;

            OnPropertyChanged(nameof(FreeDrawingDrawingAttributes));
            OnPropertyChanged(nameof(FreeDrawingCursor));
            OnPropertyChanged(nameof(FreeDrawingForceCursor));
        }
    }

    public int FreeDrawingBrushWidth
    {
        get => _freeDrawingBrushWidth;
        set
        {
            SetProperty(ref _freeDrawingBrushWidth, value);

            UserSettings.All.FreeDrawingPenWidth = value;

            OnPropertyChanged(nameof(FreeDrawingDrawingAttributes));
            OnPropertyChanged(nameof(FreeDrawingCursor));
            OnPropertyChanged(nameof(FreeDrawingForceCursor));
        }
    }

    public int FreeDrawingBrushHeight
    {
        get => _freeDrawingBrushHeight;
        set
        {
            SetProperty(ref _freeDrawingBrushHeight, value);

            UserSettings.All.FreeDrawingPenHeight = value;

            OnPropertyChanged(nameof(FreeDrawingDrawingAttributes));
            OnPropertyChanged(nameof(FreeDrawingCursor));
            OnPropertyChanged(nameof(FreeDrawingForceCursor));
        }
    }

    public StylusTip FreeDrawingStylusTip
    {
        get => _freeDrawingStylusTip;
        set
        {
            SetProperty(ref _freeDrawingStylusTip, value);

            UserSettings.All.FreeDrawingStylusTip = value;

            OnPropertyChanged(nameof(FreeDrawingDrawingAttributes));
            OnPropertyChanged(nameof(FreeDrawingCursor));
            OnPropertyChanged(nameof(FreeDrawingForceCursor));
            OnPropertyChanged(nameof(FreeDrawingStylusTipEllipse));
            OnPropertyChanged(nameof(FreeDrawingStylusTipRectangle));
        }
    }

    public bool FreeDrawingStylusTipEllipse
    {
        get => FreeDrawingStylusTip is StylusTip.Ellipse;
        set => FreeDrawingStylusTip = value ? StylusTip.Ellipse : StylusTip.Rectangle;
    }

    public bool FreeDrawingStylusTipRectangle
    {
        get => FreeDrawingStylusTip is StylusTip.Rectangle;
        set => FreeDrawingStylusTip = value ? StylusTip.Rectangle : StylusTip.Ellipse;
    }

    public Visibility FreeDrawingBrushSettingsVisibility => FreeDrawingEditingMode is InkCanvasEditingMode.Ink or InkCanvasEditingMode.InkAndGesture ? Visibility.Visible : Visibility.Collapsed;

    public bool FreeDrawingFitToCurve
    {
        get => _freeDrawingFitToCurve;
        set
        {
            SetProperty(ref _freeDrawingFitToCurve, value);

            UserSettings.All.FreeDrawingFitToCurve = value;

            OnPropertyChanged(nameof(FreeDrawingDrawingAttributes));
        }
    }

    public bool FreeDrawingIsHighlighter
    {
        get => _freeDrawingIsHighlighter;
        set
        {
            SetProperty(ref _freeDrawingIsHighlighter, value);

            UserSettings.All.FreeDrawingIsHighlighter = value;

            OnPropertyChanged(nameof(FreeDrawingDrawingAttributes));
            OnPropertyChanged(nameof(FreeDrawingCursor));
            OnPropertyChanged(nameof(FreeDrawingForceCursor));
        }
    }

    public StylusShape FreeDrawingEraserShape => FreeDrawingEraserStylusTip == StylusTip.Ellipse ? new EllipseStylusShape(FreeDrawingEraserBrushWidth, FreeDrawingEraserBrushHeight) : new RectangleStylusShape(FreeDrawingEraserBrushWidth, FreeDrawingEraserBrushHeight);

    public int FreeDrawingEraserBrushWidth
    {
        get => _freeDrawingEraserBrushWidth;
        set
        {
            SetProperty(ref _freeDrawingEraserBrushWidth, value);

            UserSettings.All.FreeDrawingEraserWidth = value;

            OnPropertyChanged(nameof(FreeDrawingEraserShape));
        }
    }

    public int FreeDrawingEraserBrushHeight
    {
        get => _freeDrawingEraserBrushHeight;
        set
        {
            SetProperty(ref _freeDrawingEraserBrushHeight, value);

            UserSettings.All.FreeDrawingEraserHeight = value;

            OnPropertyChanged(nameof(FreeDrawingEraserShape));
        }
    }

    public StylusTip FreeDrawingEraserStylusTip
    {
        get => _freeDrawingEraserStylusTip;
        set
        {
            SetProperty(ref _freeDrawingEraserStylusTip, value);

            UserSettings.All.FreeDrawingEraserStylusTip = value;

            OnPropertyChanged(nameof(FreeDrawingEraserShape));
            OnPropertyChanged(nameof(FreeDrawingEraserStylusTipEllipse));
            OnPropertyChanged(nameof(FreeDrawingEraserStylusTipRectangle));
        }
    }

    public bool FreeDrawingEraserStylusTipEllipse
    {
        get => FreeDrawingEraserStylusTip is StylusTip.Ellipse;
        set => FreeDrawingEraserStylusTip = value ? StylusTip.Ellipse : StylusTip.Rectangle;
    }

    public bool FreeDrawingEraserStylusTipRectangle
    {
        get => FreeDrawingEraserStylusTip is StylusTip.Rectangle;
        set => FreeDrawingEraserStylusTip = value ? StylusTip.Rectangle : StylusTip.Ellipse;
    }

    public Visibility FreeDrawingEraserBrushSettingsVisibility => FreeDrawingEditingMode is InkCanvasEditingMode.EraseByPoint ? Visibility.Visible : Visibility.Collapsed;

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

    #region Methods

    internal void Init()
    {
        RenderedImage = new WriteableBitmap(Project.Width, Project.Height, Project.HorizontalDpi, Project.VerticalDpi, PixelFormats.Bgra32, null);
    }

    internal void Render()
    {
        //Display mode: By timestamp or frame index.
        //Display properties in Statistic tab.

        //Get current timestamp/index and render the scene and apply to the RenderedImage property.

        //How are previews going to work?
        //  Text rendering
        //  Rendering that needs access to the all layers.
        //  Rendering that changes the size of the canvas.

        //Preview quality.
        //Render the list preview for the frames.
    }

    //How are the frames/data going to be stored in the disk?
    //Project file for the user + opened project should have a cache
    //  Project file for user: I'll need to create a file spec.
    //  Cache folder for the app:

    //As a single cache for each track? (storing as pixel array, to improve performance)
    //I'll need a companion json with positions and other details.
    //I also need to store in memory for faster usage.

    #endregion
}