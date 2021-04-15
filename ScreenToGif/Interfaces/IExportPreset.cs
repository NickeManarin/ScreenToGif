using ScreenToGif.Util;

namespace ScreenToGif.Interfaces
{
    public interface IExportPreset : IPreset
    {
        string TitleKey { get; set; }
        string DescriptionKey { get; set; }
        Export Type { get; set; }
        bool PickLocation { get; set; }
        bool OverwriteOnSave { get; set; }
        bool ExportAsProjectToo { get; set; }
        bool UploadFile { get; set; }
        string UploadService { get; set; }
        
        bool ExportPartially { get; set; }
        PartialExportType PartialExport { get; set; }
        string PartialExportFrameExpression { get; set; }

        string OutputFolder { get; set; }
        string OutputFilename { get; set; }

        bool RequiresFfmpeg { get; set; }
        bool RequiresGifski { get; set; }
    }
}