using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Mp4;

public class Mp4Preset : VideoPreset
{
    public Mp4Preset()
    {
        Type = ExportFormats.Mp4;
        DefaultExtension = ".mp4";
        Extension = ".mp4";
    }
}