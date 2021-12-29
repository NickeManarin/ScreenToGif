using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.ViewModel.Tasks;

public class DelayViewModel : BaseTaskViewModel
{
    private DelayUpdateModes _type;
    private int _delay;
    private int _increaseDecreaseDelay;
    private decimal _percent;

    public DelayViewModel()
    {
        TaskType = TaskTypes.Delay;
    }

    public DelayUpdateModes Type
    {
        get => _type;
        set
        {
            SetProperty(ref _type, value);
            OnPropertyChanged(nameof(DelayTypeUnit));
        }
    }

    public int NewDelay
    {
        get => _delay;
        set => SetProperty(ref _delay, value);
    }

    public int IncreaseDecreaseDelay
    {
        get => _increaseDecreaseDelay;
        set => SetProperty(ref _increaseDecreaseDelay, value);
    }

    public decimal Percent
    {
        get => _percent;
        set => SetProperty(ref _percent, value);
    }

    public string DelayType =>
        Type == DelayUpdateModes.Override ? LocalizationHelper.Get("S.Editor.Edit.Delay.Override") :
        Type == DelayUpdateModes.IncreaseDecrease ? LocalizationHelper.Get("S.Editor.Edit.Delay.IncreaseDecrease") :
        Type == DelayUpdateModes.Scale ? LocalizationHelper.Get("S.Editor.Edit.Delay.Scale") : "None";

    public string DelayTypeKind =>
        Type == DelayUpdateModes.Override ? LocalizationHelper.Get("S.DelayMs") :
        Type == DelayUpdateModes.IncreaseDecrease ? LocalizationHelper.Get("S.ValueMs") :
        Type == DelayUpdateModes.Scale ? LocalizationHelper.Get("S.ScaleValue") : "None";

    public string DelayTypeUnitValue =>
        Type == DelayUpdateModes.Scale ? Percent + DelayTypeUnit :
        Type == DelayUpdateModes.Override ? NewDelay + DelayTypeUnit :
        Type == DelayUpdateModes.IncreaseDecrease ? IncreaseDecreaseDelay + DelayTypeUnit : "";

    public string DelayTypeUnit =>
        Type == DelayUpdateModes.Scale ? " %" :
        Type == DelayUpdateModes.Override ? " ms" :
        Type == DelayUpdateModes.IncreaseDecrease ? " ms" : "";

    public override string ToString()
    {
        return $"{LocalizationHelper.Get("S.Delay")}: {DelayType}, {DelayTypeKind} {DelayTypeUnitValue}";
    }

    public static DelayViewModel Default()
    {
        return new DelayViewModel
        {
            Type = DelayUpdateModes.Override,
            NewDelay = 33,
            IncreaseDecreaseDelay = 10,
            Percent = 100,
        };
    }

    public static DelayViewModel FromSettings(DelayUpdateModes type = DelayUpdateModes.Override)
    {
        return new DelayViewModel
        {
            Type = type,
            NewDelay = UserSettings.All.OverrideDelay,
            IncreaseDecreaseDelay = UserSettings.All.IncrementDecrementDelay,
            Percent = UserSettings.All.ScaleDelay,
        };
    }
}