using System;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Image
{
    public class PngPreset : ImagePreset
    {
        public PngPreset()
        {
            Type = Export.Png;
            ImageId = "Vector.Logo";
            DefaultExtension = ".png";
            Extension = ".png";
        }


        public static PngPreset Default => new PngPreset
        {
            TitleKey = "S.Preset.Default.Title",
            DescriptionKey = "S.Preset.Default.Description",
            HasAutoSave = true,
            IsSelectedForEncoder = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),

            ExportPartially = true,
            PartialExport = PartialExportType.Selection
        };
    }
}