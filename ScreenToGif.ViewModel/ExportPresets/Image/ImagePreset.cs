using System.ComponentModel;
using System.Runtime.Serialization;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Image;

public class ImagePreset : ExportPreset
{
    private bool _zipFiles;


    public bool ZipFiles
    {
        get => _zipFiles;
        set
        {
            SetProperty(ref _zipFiles, value);

            CanExportMultipleFiles = !value;
            Extension = value ? ".zip" : DefaultExtension;
        }
    }

    /// <summary>
    /// Internal accessor for controlling the switch of the ZipFiles property without altering the extension. 
    /// </summary>
    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ZipFilesInternal
    {
        get => _zipFiles;
        set
        {
            SetProperty(ref _zipFiles, value);
            OnPropertyChanged(nameof(ZipFiles));

            CanExportMultipleFiles = !value;
        }
    }


    public ImagePreset()
    {
        OutputFilenameKey = "S.Preset.Filename.Image";
        IsEncoderExpanded = false;
        ExportPartially = true;
        PartialExport = PartialExportModes.Selection;
        CanExportMultipleFiles = true;
    }
}