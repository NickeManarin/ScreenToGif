using ScreenToGif.Domain.Enums;
using ScreenToGif.ViewModel.ExportPresets.Video;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Avif;

public class AvifPreset : AnimatedImagePreset
{
    public AvifPreset()
    {
        Type = ExportFormats.Avif;
        DefaultExtension = ".avif";
        Extension = ".avif";
    }
}