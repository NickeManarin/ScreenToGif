using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.ViewModel.Tasks;

public class ShadowViewModel : BaseTaskViewModel
{
    #region Variables

    private Color _shadowColor;
    private Color _backgroundColor;
    private double _direction;
    private double _blurRadius;
    private double _opacity;
    private double _depth;

    #endregion

    public ShadowViewModel()
    {
        TaskType = TaskTypes.Shadow;
    }

    public Color Color
    {
        get => _shadowColor;
        set => SetProperty(ref _shadowColor, value);
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public double Direction
    {
        get => _direction;
        set => SetProperty(ref _direction, value);
    }

    public double BlurRadius
    {
        get => _blurRadius;
        set => SetProperty(ref _blurRadius, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

    public double Depth
    {
        get => _depth;
        set => SetProperty(ref _depth, value);
    }

    public override string ToString()
    {
        return $"{LocalizationHelper.Get("S.Shadow.ShadowColor")} #{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}, " +
               $"{LocalizationHelper.Get("S.Shadow.BackgroundColor")} #{BackgroundColor.A:X2}{BackgroundColor.R:X2}{BackgroundColor.G:X2}{BackgroundColor.B:X2}, " +
               $"{LocalizationHelper.Get("S.Shadow.Direction")} {Direction}°, " +
               $"{LocalizationHelper.Get("S.Shadow.BlurRadius")} {BlurRadius}, " +
               $"{LocalizationHelper.Get("S.Shadow.Depth")} {Depth}";
    }

    public static ShadowViewModel Default()
    {
        return new ShadowViewModel
        {
            Color = Color.FromArgb(255, 0, 0, 0),
            BackgroundColor = Color.FromArgb(255, 255, 255, 255),
            Direction = 270,
            BlurRadius = 20,
            Opacity = 60,
            Depth = 5
        };
    }

    public static ShadowViewModel FromSettings()
    {
        return new ShadowViewModel
        {
            Color = UserSettings.All.ShadowColor,
            BackgroundColor = UserSettings.All.ShadowBackgroundColor,
            Direction = UserSettings.All.ShadowDirection,
            BlurRadius = UserSettings.All.ShadowBlurRadius,
            Opacity = UserSettings.All.ShadowOpacity,
            Depth = UserSettings.All.ShadowDepth,
        };
    }
}