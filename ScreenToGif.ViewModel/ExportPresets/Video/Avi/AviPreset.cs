using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Avi;

public class AviPreset : VideoPreset
{
    public AviPreset()
    {
        Type = ExportFormats.Avi;
        DefaultExtension = ".avi";
        Extension = ".avi";
        IsAncientContainer = true;
    }
}