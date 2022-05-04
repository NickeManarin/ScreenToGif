using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Interfaces
{
    public interface IExportPreset : IPreset
    {
        string TitleKey { get; set; }
        string DescriptionKey { get; set; }
        ExportFormats Type { get; set; }
        bool PickLocation { get; set; }
        OverwriteModes OverwriteMode { get; set; }
        bool ExportAsProjectToo { get; set; }
        bool UploadFile { get; set; }
        string UploadService { get; set; }

        bool ExportPartially { get; set; }
        PartialExportModes PartialExport { get; set; }
        string PartialExportFrameExpression { get; set; }

        string OutputFolder { get; set; }
        string OutputFilename { get; set; }

        bool RequiresFfmpeg { get; set; }
        bool RequiresGifski { get; set; }
    }
}