using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Mkv;

public class MkvPreset : VideoPreset
{
    public MkvPreset()
    {
        Type = ExportFormats.Mkv;
        DefaultExtension = ".mkv";
        Extension = ".mkv";
    }
}