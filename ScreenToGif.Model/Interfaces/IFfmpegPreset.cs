using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Interfaces
{
    public interface IFfmpegPreset
    {
        VideoSettingsModes SettingsMode { get; set; }

        string Parameters { get; set; }
    }
}