using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Bpg;

public class BpgPreset : AnimatedImagePreset
{
    public BpgPreset()
    {
        Type = ExportFormats.Bpg;
        DefaultExtension = ".bpg";
        Extension = ".bpg";
    }
}