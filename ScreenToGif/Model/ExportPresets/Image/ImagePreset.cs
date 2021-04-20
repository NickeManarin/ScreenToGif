using System.ComponentModel;
using System.Management.Instrumentation;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Image
{
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
        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool ZipFilesInternal
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
            PartialExport = PartialExportType.Selection;
            CanExportMultipleFiles = true;
        }
    }
}