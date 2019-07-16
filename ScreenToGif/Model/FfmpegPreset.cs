using ScreenToGif.Util;

namespace ScreenToGif.Model
{
    public class FfmpegPreset : BindableBase
    {
        private string _extension;
        private string _name;
        private string _parameters;
        private bool _isDefault;
        private bool _lastSelected;

        public string Extension
        {
            get => _extension;
            set => SetProperty(ref _extension, value);
        }

        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        public string ActualName
        {
            get => IsDefault ? LocalizationHelper.Get("S.SaveAs.Presets.Default") + $" ({Extension})" : Name;
            set
            {
                if (!IsDefault)
                    Name = value;
            }
        }

        public string Parameters
        {
            get => _parameters;
            set => SetProperty(ref _parameters, value);
        }

        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }

        public bool LastSelected
        {
            get => _lastSelected;
            set => SetProperty(ref _lastSelected, value);
        }
    }
}