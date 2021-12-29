using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Image;

public class JpegPreset : ImagePreset
{
    public JpegPreset()
    {
        Type = ExportFormats.Jpeg;
        ImageId = "Vector.Logo";
        DefaultExtension = ".jpeg";
        Extension = ".jpeg";
    }


    public static JpegPreset Default => new()
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