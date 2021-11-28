using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Image;

public class BmpPreset : ImagePreset
{
    public BmpPreset()
    {
        Type = ExportFormats.Bmp;
        ImageId = "Vector.Logo";
        DefaultExtension = ".bmp";
        Extension = ".bmp";
    }


    public static BmpPreset Default => new()
    {
        TitleKey = "S.Preset.Default.Title",
        DescriptionKey = "S.Preset.Default.Description",
        HasAutoSave = true,
        IsSelectedForEncoder = true,
        IsDefault = true,
        CreationDate = new DateTime(2021, 02, 20),

        ExportPartially = true,
        PartialExport = PartialExportModes.Selection
    };
}