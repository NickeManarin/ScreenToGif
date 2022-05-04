using System.ComponentModel;
using System.Runtime.Serialization;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Events;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util;

namespace ScreenToGif.ViewModel.ExportPresets;

public abstract class ExportPreset : BindableBase, IExportPreset
{
    private ExportFormats _type;
    private EncoderTypes _encoder;
    private string _title;
    private string _titleKey;
    private string _description;
    private string _descriptionKey;
    private string _imageId;
    private string _defaultExtension;
    private bool _isSelected;
    private bool _isSelectedForEncoder;
    private bool _isDefault;
    private bool _hasAutoSave;
    private DateTime _creationDate;
    private bool _isEncoderExpanded = true;
    private bool _isEncoderOptionsExpanded = true;
    private bool _isPartialExportExpanded = true;
    private bool _isExportOptionsExpanded = false;
    private bool _isUploadExpanded = true;
    private bool _isOutputExpanded = true;
    private bool _exportPartially;
    private PartialExportModes _partialExport = PartialExportModes.Selection;
    private TimeSpan _partialExportTimeStart = TimeSpan.Zero;
    private TimeSpan _partialExportTimeEnd = TimeSpan.Zero;
    private int _partialExportFrameStart = 0;
    private int _partialExportFrameEnd = 0;
    private string _partialExportFrameExpression;
    private bool _pickLocation = true;
    private OverwriteModes _overwriteMode;
    private bool _exportAsProjectToo;
    private bool _uploadFile;
    private string _uploadService;
    private bool _saveToClipboard;
    private CopyModes _copyType = CopyModes.File;
    private bool _executeCustomCommands;
    private string _customCommands = "{p}";
    private string _outputFolder;
    private string _outputFilename;
    private string _outputFilenameKey;
    private string _extension;


    public ExportFormats Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public EncoderTypes Encoder
    {
        get => _encoder;
        set => SetProperty(ref _encoder, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string TitleKey
    {
        get => _titleKey;
        set => SetProperty(ref _titleKey, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string DescriptionKey
    {
        get => _descriptionKey;
        set => SetProperty(ref _descriptionKey, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ImageId
    {
        get => _imageId;
        set => SetProperty(ref _imageId, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string DefaultExtension
    {
        get => _defaultExtension;
        set => SetProperty(ref _defaultExtension, value);
    }

    /// <summary>
    /// True if this preset was the latest selected preset for the selected file type.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// True if this preset was the latest selected preset for the selected file type and encoder.
    /// </summary>
    public bool IsSelectedForEncoder
    {
        get => _isSelectedForEncoder;
        set => SetProperty(ref _isSelectedForEncoder, value);
    }

    /// <summary>
    /// True if this preset was provided by the app.
    /// </summary>
    public bool IsDefault
    {
        get => _isDefault;
        set => SetProperty(ref _isDefault, value);
    }

    /// <summary>
    /// True if this preset automatically saves it's new property values when the user changes something.
    /// </summary>
    public bool HasAutoSave
    {
        get => _hasAutoSave;
        set => SetProperty(ref _hasAutoSave, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public DateTime CreationDate
    {
        get => _creationDate;
        set => SetProperty(ref _creationDate, value);
    }


    public bool IsEncoderExpanded
    {
        get => _isEncoderExpanded;
        set => SetProperty(ref _isEncoderExpanded, value);
    }

    public bool IsEncoderOptionsExpanded
    {
        get => _isEncoderOptionsExpanded;
        set => SetProperty(ref _isEncoderOptionsExpanded, value);
    }

    public bool IsExportOptionsExpanded
    {
        get => _isExportOptionsExpanded;
        set => SetProperty(ref _isExportOptionsExpanded, value);
    }

    public bool IsPartialExportExpanded
    {
        get => _isPartialExportExpanded;
        set => SetProperty(ref _isPartialExportExpanded, value);
    }
        
    public bool IsOutputExpanded
    {
        get => _isOutputExpanded;
        set => SetProperty(ref _isOutputExpanded, value);
    }

    public bool IsUploadExpanded
    {
        get => _isUploadExpanded;
        set => SetProperty(ref _isUploadExpanded, value);
    }


    public bool ExportPartially
    {
        get => _exportPartially;
        set => SetProperty(ref _exportPartially, value);
    }

    public PartialExportModes PartialExport
    {
        get => _partialExport;
        set => SetProperty(ref _partialExport, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TimeSpan PartialExportTimeStart
    {
        get => _partialExportTimeStart;
        set => SetProperty(ref _partialExportTimeStart, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TimeSpan PartialExportTimeEnd
    {
        get => _partialExportTimeEnd;
        set => SetProperty(ref _partialExportTimeEnd, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PartialExportFrameStart
    {
        get => _partialExportFrameStart;
        set => SetProperty(ref _partialExportFrameStart, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PartialExportFrameEnd
    {
        get => _partialExportFrameEnd;
        set => SetProperty(ref _partialExportFrameEnd, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PartialExportFrameExpression
    {
        get => _partialExportFrameExpression;
        set => SetProperty(ref _partialExportFrameExpression, value);
    }
        

    public bool PickLocation
    {
        get => _pickLocation;
        set => SetProperty(ref _pickLocation, value);
    }

    public OverwriteModes OverwriteMode
    {
        get => _overwriteMode;
        set => SetProperty(ref _overwriteMode, value);
    }

    public bool ExportAsProjectToo
    {
        get => _exportAsProjectToo;
        set => SetProperty(ref _exportAsProjectToo, value);
    }

    public bool UploadFile
    {
        get => _uploadFile;
        set => SetProperty(ref _uploadFile, value);
    }

    public string UploadService
    {
        get => _uploadService;
        set => SetProperty(ref _uploadService, value);
    }

    public bool SaveToClipboard
    {
        get => _saveToClipboard;
        set => SetProperty(ref _saveToClipboard, value);
    }

    public CopyModes CopyType
    {
        get => _copyType;
        set => SetProperty(ref _copyType, value);
    }

    public bool ExecuteCustomCommands
    {
        get => _executeCustomCommands;
        set => SetProperty(ref _executeCustomCommands, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string CustomCommands
    {
        get => _customCommands;
        set => SetProperty(ref _customCommands, value);
    }


    [DataMember(EmitDefaultValue = false)]
    public string OutputFolder
    {
        get => _outputFolder;
        set => SetProperty(ref _outputFolder, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string OutputFilename
    {
        get => _outputFilename;
        set => SetProperty(ref _outputFilename, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string OutputFilenameKey
    {
        get => _outputFilenameKey;
        set => SetProperty(ref _outputFilenameKey, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Extension
    {
        get => _extension;
        set => SetProperty(ref _extension, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CanExportMultipleFiles { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool RequiresFfmpeg { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool RequiresGifski { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Width { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Height { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Scale { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ResolvedFilename { get; set; }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FullPath { get; set; }


    protected ExportPreset()
    {
        CreationDate = DateTime.UtcNow;
    }


    public virtual Task<ValidatedEventArgs> IsValid()
    {
        if (!PickLocation && !UploadFile && !SaveToClipboard)
            return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Type", StatusReasons.InvalidState));

        if (PickLocation)
        {
            if (string.IsNullOrWhiteSpace(Extension))
            {
                if (!string.IsNullOrWhiteSpace(DefaultExtension))
                    Extension = DefaultExtension;
                else
                    return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Extension", StatusReasons.EmptyProperty));
            }

            if (string.IsNullOrWhiteSpace(OutputFolder))
                return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Folder", StatusReasons.EmptyProperty));

            if (OutputFolder.ToCharArray().Any(x => Path.GetInvalidPathChars().Contains(x)))
                return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Folder.Invalid", StatusReasons.InvalidState));

            if (!Directory.Exists(OutputFolder))
                return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Folder.NotExists", StatusReasons.InvalidState));

            if (string.IsNullOrWhiteSpace(OutputFilename))
                return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Filename", StatusReasons.EmptyProperty));

            ResolvedFilename = PathHelper.ReplaceRegexInName(OutputFilename); //TODO: Cyclical reference

            if (ResolvedFilename.ToCharArray().Any(x => Path.GetInvalidFileNameChars().Contains(x)))
                return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Filename.Invalid", StatusReasons.InvalidState));
        }

        //Upload set, but no service selected.
        if (UploadFile && string.IsNullOrWhiteSpace(UploadService))
            return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Upload.None", StatusReasons.InvalidState));

        //Copy link to clipboard set, but no upload set.
        if (SaveToClipboard && CopyType == CopyModes.Link && !UploadFile)
            return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Copy.Link", StatusReasons.InvalidState));

        //Custom command not set.
        if (ExecuteCustomCommands && string.IsNullOrWhiteSpace(CustomCommands))
            return Task.FromResult(new ValidatedEventArgs("S.SaveAs.Warning.Commands.Empty", StatusReasons.EmptyProperty));

        return Task.FromResult((ValidatedEventArgs) null);
    }

    public ExportPreset ShallowCopy()
    {
        return (ExportPreset) MemberwiseClone();
    }
}