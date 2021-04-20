using System;
using System.IO.Compression;

namespace ScreenToGif.Model.ExportPresets.Other
{
    public class StgPreset : ExportPreset
    {
        private CompressionLevel _compressionLevel = CompressionLevel.Optimal;


        public CompressionLevel CompressionLevel
        {
            get => _compressionLevel;
            set => SetProperty(ref _compressionLevel, value);
        }


        public StgPreset()
        {
            Type = Util.Export.Stg;
            ImageId = "Vector.Logo";
            OutputFilenameKey = "S.Preset.Filename.Project";
            DefaultExtension = ".stg";
            Extension = ".stg";
            IsEncoderExpanded = false;
        }


        public static StgPreset Default => new StgPreset
        {
            TitleKey = "S.Preset.Default.Title",
            DescriptionKey = "S.Preset.Default.Description",
            HasAutoSave = true,
            IsSelectedForEncoder = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),
        };
    }
}