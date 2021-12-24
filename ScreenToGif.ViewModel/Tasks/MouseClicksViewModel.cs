using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.ViewModel.Tasks;

public class MouseClicksViewModel : BaseTaskViewModel
{
    private Color _leftButtonForegroundColor;
    private Color _rightButtonForegroundColor;
    private Color _middleButtonForegroundColor;
    private double _width;
    private double _height;

    public MouseClicksViewModel()
    {
        TaskType = TaskTypes.MouseClicks;
    }

    public Color LeftButtonForegroundColor
    {
        get => _leftButtonForegroundColor;
        set => SetProperty(ref _leftButtonForegroundColor, value);
    }

    public Color RightButtonForegroundColor
    {
        get => _rightButtonForegroundColor;
        set => SetProperty(ref _rightButtonForegroundColor, value);
    }

    public Color MiddleButtonForegroundColor
    {
        get => _middleButtonForegroundColor;
        set => SetProperty(ref _middleButtonForegroundColor, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public override string ToString()
    {
        return $"{LocalizationHelper.Get("S.MouseClicks.Color.Left")} #{LeftButtonForegroundColor.A:X2}{LeftButtonForegroundColor.R:X2}{LeftButtonForegroundColor.G:X2}{LeftButtonForegroundColor.B:X2}, "+
               $"{LocalizationHelper.Get("S.MouseClicks.Color.Middle")} #{MiddleButtonForegroundColor.A:X2}{MiddleButtonForegroundColor.R:X2}{MiddleButtonForegroundColor.G:X2}{MiddleButtonForegroundColor.B:X2}, "+
               $"{LocalizationHelper.Get("S.MouseClicks.Color.Right")} #{RightButtonForegroundColor.A:X2}{RightButtonForegroundColor.R:X2}{RightButtonForegroundColor.G:X2}{RightButtonForegroundColor.B:X2}, "+
               $"{LocalizationHelper.Get("S.FreeDrawing.Width")} {Width}, {LocalizationHelper.Get("S.FreeDrawing.Height")} {Height}";
    }

    public static MouseClicksViewModel Default()
    {
        return new MouseClicksViewModel
        {
            LeftButtonForegroundColor = Color.FromArgb(120, 255, 255, 0),
            RightButtonForegroundColor = Color.FromArgb(120, 255, 0, 0),
            MiddleButtonForegroundColor = Color.FromArgb(120, 0, 255,255),
            Height = 12,
            Width = 12
        };
    }

    public static MouseClicksViewModel FromSettings()
    {
        return new MouseClicksViewModel
        {
            LeftButtonForegroundColor = UserSettings.All.LeftMouseButtonClicksColor,
            MiddleButtonForegroundColor = UserSettings.All.MiddleMouseButtonClicksColor,
            RightButtonForegroundColor = UserSettings.All.RightMouseButtonClicksColor,
            Height = UserSettings.All.MouseClicksHeight,
            Width = UserSettings.All.MouseClicksWidth
        };
    }
}