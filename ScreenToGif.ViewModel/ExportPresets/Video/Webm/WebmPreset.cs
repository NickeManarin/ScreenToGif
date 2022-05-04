using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Webm;

public class WebmPreset : VideoPreset
{
    public WebmPreset()
    {
        Type = ExportFormats.Webm;
        DefaultExtension = ".webm";
        Extension = ".webm";
    }
}