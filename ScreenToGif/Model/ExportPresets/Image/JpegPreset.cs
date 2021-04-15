using System;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Image
{
    public class JpegPreset : ImagePreset
    {
        public JpegPreset()
        {
            Type = Export.Jpeg;
            ImageId = "Vector.Logo";
            DefaultExtension = ".jpeg";
            Extension = ".jpeg";
        }


        public static JpegPreset Default => new JpegPreset
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