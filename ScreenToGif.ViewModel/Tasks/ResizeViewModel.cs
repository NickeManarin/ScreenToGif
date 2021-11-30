using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.ViewModel.Tasks;

public class ResizeViewModel : BaseTaskViewModel
{
    #region Variables

    private decimal _widthRatio = -1;
    private decimal _heightRatio = -1;

    private int _originalWidth;
    private int _originalHeight;
    private double _originalDpi;
    private int _width;
    private int _height;
    private decimal _widthInPercent;
    private decimal _heightInPercent;
    private double _dpi;
    private bool _keepAspectRatio;
    private SizeUnits _sizeUnit;
    private BitmapScalingMode _scalingMode;

    #endregion

    public ResizeViewModel()
    {
        TaskType = TaskTypes.Resize;
    }


    public int OriginalWidth
    {
        get => _originalWidth;
        set => SetProperty(ref _originalWidth, value);
    }

    public int OriginalHeight
    {
        get => _originalHeight;
        set => SetProperty(ref _originalHeight, value);
    }

    public double OriginalDpi
    {
        get => _originalDpi;
        set => SetProperty(ref _originalDpi, value);
    }


    public int Width
    {
        get => _width;
        set
        {
            if (!SetProperty(ref _width, value))
                return;

            WidthInPercent = MathExtensions.CrossMultiplication(OriginalWidth, (decimal) Width, null);

            OnPropertyChanged(nameof(WidthDiff));
            OnPropertyChanged(nameof(SizeDiff));

            if (KeepAspectRatio != true)
                return;

            Height = (int)Math.Round(_heightRatio * value / _widthRatio);
        }
    }

    public int Height
    {
        get => _height;
        set
        {
            if (!SetProperty(ref _height, value))
                return;

            HeightInPercent = MathExtensions.CrossMultiplication(OriginalHeight, (decimal) Height, null);

            OnPropertyChanged(nameof(HeightDiff));
            OnPropertyChanged(nameof(SizeDiff));

            if (KeepAspectRatio != true)
                return;

            Width = (int)Math.Round(_widthRatio * value / _heightRatio);
        }
    }

    public decimal WidthInPercent
    {
        get => _widthInPercent;
        set
        {
            if (!SetProperty(ref _widthInPercent, value))
                return;

            Width = (int)Math.Round(MathExtensions.CrossMultiplication(OriginalWidth, null, WidthInPercent));

            if (KeepAspectRatio != true)
                return;

            Height = (int)Math.Round(_heightRatio * Width / _widthRatio);
        }
    }

    public decimal HeightInPercent
    {
        get => _heightInPercent;
        set
        {
            if (!SetProperty(ref _heightInPercent, value))
                return;

            Height = (int)Math.Round(MathExtensions.CrossMultiplication(OriginalHeight, null, HeightInPercent));

            if (KeepAspectRatio != true)
                return;

            Width = (int)Math.Round(_widthRatio * Height / _heightRatio);
        }
    }

    public double Dpi
    {
        get => _dpi;
        set
        {
            SetProperty(ref _dpi, value);

            OnPropertyChanged(nameof(DpiDiff));
        }
    }


    public bool KeepAspectRatio
    {
        get => _keepAspectRatio;
        set
        {
            SetProperty(ref _keepAspectRatio, value);

            if (!value)
                return;

            var gcd = MathExtensions.Gcd((decimal) Height, Width);

            _widthRatio = Width / gcd;
            _heightRatio = Height / gcd;
        }
    }

    public SizeUnits SizeUnit
    {
        get => _sizeUnit;
        set
        {
            if (!SetProperty(ref _sizeUnit, value))
                return;

            OnPropertyChanged(nameof(DisplayInPixels));
            OnPropertyChanged(nameof(DisplayInPercents));
        }
    }

    public BitmapScalingMode ScalingMode
    {
        get => _scalingMode;
        set => SetProperty(ref _scalingMode, value);
    }


    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Visibility DisplayInPixels => SizeUnit == SizeUnits.Pixels ? Visibility.Visible : Visibility.Collapsed;

    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Visibility DisplayInPercents => SizeUnit == SizeUnits.Percent ? Visibility.Visible : Visibility.Collapsed;

    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal SizeDiff => MathExtensions.CrossMultiplication((decimal)OriginalWidth * OriginalHeight, Width * Height, null) - 100m;

    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal WidthDiff => MathExtensions.CrossMultiplication((decimal)OriginalWidth, Width, null) - 100m;

    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal HeightDiff => MathExtensions.CrossMultiplication((decimal)OriginalHeight, Height, null) - 100m;

    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double DpiDiff => MathExtensions.CrossMultiplication(OriginalDpi, Dpi, null) - 100d;


    public static ResizeViewModel Default()
    {
        return new ResizeViewModel
        {
            KeepAspectRatio = true,
            SizeUnit = SizeUnits.Percent,
            ScalingMode = BitmapScalingMode.Linear
        };
    }

    public static ResizeViewModel FromSettings(int width = 0, int height = 0, double dpi = 96d)
    {
        return new ResizeViewModel
        {
            OriginalWidth = width,
            OriginalHeight = height,
            OriginalDpi = dpi,
            Width = width,
            Height = height,
            Dpi = dpi,
            KeepAspectRatio = UserSettings.All.KeepAspectRatio,
            SizeUnit = UserSettings.All.SizeUnit,
            ScalingMode = UserSettings.All.ScalingMode
        };
    }

    public override void Persist()
    {
        UserSettings.All.SizeUnit = SizeUnit;
        UserSettings.All.KeepAspectRatio = KeepAspectRatio;
        UserSettings.All.ScalingMode = ScalingMode;
    }
}