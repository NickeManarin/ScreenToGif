using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.ViewModel.Tasks;

public class BorderViewModel : BaseTaskViewModel
{
    #region Variables

    private Color _color;
    private double _leftThickness;
    private double _topThickness;
    private double _rightThickness;
    private double _bottomThickness;
        
    #endregion

    public BorderViewModel()
    {
        TaskType = TaskTypes.Border;
    }

    public Color Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    public double LeftThickness
    {
        get => _leftThickness;
        set => SetProperty(ref _leftThickness, value);
    }

    public double TopThickness
    {
        get => _topThickness;
        set => SetProperty(ref _topThickness, value);
    }

    public double RightThickness
    {
        get => _rightThickness;
        set => SetProperty(ref _rightThickness, value);
    }

    public double BottomThickness
    {
        get => _bottomThickness;
        set => SetProperty(ref _bottomThickness, value);
    }

    public override string ToString()
    {
        return $"{LocalizationHelper.Get("S.Color")} #{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}, " +
               $"{(LocalizationHelper.Get("S.Caption.Thickness"))} ({LeftThickness}, {TopThickness}, {LeftThickness}, {BottomThickness})";
    }

    public static BorderViewModel Default()
    {
        return new BorderViewModel
        {
            Color = Color.FromArgb(255, 0, 0, 0),
            LeftThickness = 1,
            TopThickness = 1,
            RightThickness = 1,
            BottomThickness = 1,
        };
    }

    public static BorderViewModel FromSettings(bool isManual = false)
    {
        return new BorderViewModel
        {
            Color = UserSettings.All.BorderColor,
            LeftThickness = UserSettings.All.BorderLeftThickness,
            TopThickness = UserSettings.All.BorderTopThickness,
            RightThickness = UserSettings.All.BorderRightThickness,
            BottomThickness = UserSettings.All.BorderBottomThickness,
            IsManual = isManual
        };
    }
}