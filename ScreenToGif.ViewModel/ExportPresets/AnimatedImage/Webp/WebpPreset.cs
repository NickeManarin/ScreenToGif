using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Webp;

public class WebpPreset : AnimatedImagePreset
{
    public WebpPreset()
    {
        Type = ExportFormats.Webp;
        DefaultExtension = ".webp";
        Extension = ".webp";
    }
}