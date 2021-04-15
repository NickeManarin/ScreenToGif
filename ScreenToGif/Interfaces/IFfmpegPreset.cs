using ScreenToGif.Util;

namespace ScreenToGif.Interfaces
{
    public interface IFfmpegPreset
    {
        VideoSettingsMode SettingsMode { get; set; }

        string Parameters { get; set; }
    }
}