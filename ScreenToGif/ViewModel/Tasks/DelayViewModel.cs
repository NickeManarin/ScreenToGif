using ScreenToGif.Settings;
using ScreenToGif.Util;

namespace ScreenToGif.ViewModel.Tasks
{
    public class DelayViewModel : BaseTaskViewModel
    {
        private DelayUpdateType _type;
        private int _delay;
        private int _increaseDecreaseDelay;
        private decimal _percent;

        public DelayViewModel()
        {
            TaskType = TaskTypeEnum.Delay;
        }

        public DelayUpdateType Type
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
            Type == DelayUpdateType.Override ? LocalizationHelper.Get("S.Editor.Edit.Delay.Override") :
            Type == DelayUpdateType.IncreaseDecrease ? LocalizationHelper.Get("S.Editor.Edit.Delay.IncreaseDecrease") :
            Type == DelayUpdateType.Scale ? LocalizationHelper.Get("S.Editor.Edit.Delay.Scale") : "None";

        public string DelayTypeKind =>
            Type == DelayUpdateType.Override ? LocalizationHelper.Get("S.DelayMs") :
            Type == DelayUpdateType.IncreaseDecrease ? LocalizationHelper.Get("S.ValueMs") :
            Type == DelayUpdateType.Scale ? LocalizationHelper.Get("S.ScaleValue") : "None";

        public string DelayTypeUnitValue =>
            Type == DelayUpdateType.Scale ? Percent + DelayTypeUnit :
            Type == DelayUpdateType.Override ? NewDelay + DelayTypeUnit :
            Type == DelayUpdateType.IncreaseDecrease ? IncreaseDecreaseDelay + DelayTypeUnit : "";

        public string DelayTypeUnit =>
            Type == DelayUpdateType.Scale ? " %" :
            Type == DelayUpdateType.Override ? " ms" :
            Type == DelayUpdateType.IncreaseDecrease ? " ms" : "";

        public override string ToString()
        {
            return $"{LocalizationHelper.Get("S.Delay")}: {DelayType}, {DelayTypeKind} {DelayTypeUnitValue}";
        }

        public static DelayViewModel Default()
        {
            return new DelayViewModel
            {
                Type = DelayUpdateType.Override,
                NewDelay = 33,
                IncreaseDecreaseDelay = 10,
                Percent = 100,
            };
        }

        public static DelayViewModel FromSettings(DelayUpdateType type = DelayUpdateType.Override)
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
}