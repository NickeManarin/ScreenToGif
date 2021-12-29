using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Apng;

public class ApngPreset : AnimatedImagePreset
{
    public ApngPreset()
    {
        Type = ExportFormats.Apng;
        DefaultExtension = ".apng";
        Extension = ".apng";
    }
}