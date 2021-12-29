using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Events;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util;

namespace ScreenToGif.ViewModel.UploadPresets;

public class UploadPreset : BindableBase, IUploadPreset
{
    private UploadDestinations _type = UploadDestinations.NotDefined;
    private bool _isEnabled = true;
    private string _title = "";
    private string _description = "";
    private string _imageId;
    private bool _isAnonymous;
    private ArrayList _history = new();
    private List<ExportFormats> _allowedTypes;
        
    private readonly long? _sizeLimit;
    private readonly TimeSpan? _durationLimit;
    private readonly Size? _resolutionLimit;

    public UploadPreset()
    { }

    public UploadPreset(long? sizeLimit, TimeSpan? durationLimit = null, Size? resolutionLimit = null)
    {
        _sizeLimit = sizeLimit;
        _durationLimit = durationLimit;
        _resolutionLimit = resolutionLimit;
    }

    public UploadDestinations Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ImageId
    {
        get => _imageId;
        set => SetProperty(ref _imageId, value);
    }

    public bool IsAnonymous
    {
        get => _isAnonymous;
        set
        {
            SetProperty(ref _isAnonymous, value);
            OnPropertyChanged(nameof(Mode));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public ArrayList History
    {
        get => _history;
        set => SetProperty(ref _history, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<ExportFormats> AllowedTypes
    {
        get => _allowedTypes;
        set => SetProperty(ref _allowedTypes, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TypeName
    {
        get
        {
            switch (Type)
            {
                case UploadDestinations.Imgur:
                    return "Imgur";
                case UploadDestinations.Gfycat:
                    return "Gfycat";
                case UploadDestinations.Yandex:
                    return "Yandex";
                case UploadDestinations.Custom:
                    return LocalizationHelper.Get("S.Options.Upload.Preset.Custom");
                default:
                    return LocalizationHelper.Get("S.Options.Upload.Preset.Select");
            }
        }
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasLimit => HasSizeLimit || HasDurationLimit || HasResolutionLimit;

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasSizeLimit => _sizeLimit != null;

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasDurationLimit => _durationLimit != null;

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasResolutionLimit => _resolutionLimit != null;

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public long? SizeLimit => _sizeLimit;

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TimeSpan? DurationLimit => _durationLimit;

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Size? ResolutionLimit => _resolutionLimit;

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Limit => (HasLimit ? "▼ " : "") + (HasSizeLimit ? Humanizer.BytesToString(SizeLimit ?? 0L) : "") + (HasSizeLimit && (HasDurationLimit || HasResolutionLimit) ? " • " : "") +
                           (HasDurationLimit ? $"{DurationLimit:mm\':\'ss} m" : "") + (HasDurationLimit && HasResolutionLimit ? " • " : "") + (HasResolutionLimit ? $"{ResolutionLimit?.Width}x{ResolutionLimit?.Height}" : "");

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Mode => IsAnonymous ? LocalizationHelper.Get("S.Options.Upload.Preset.Mode.Anonymous") : LocalizationHelper.Get("S.Options.Upload.Preset.Mode.Authenticated");


    public virtual Task<ValidatedEventArgs> IsValid()
    {
        return Task.FromResult((ValidatedEventArgs) null);
    }
        
    public UploadPreset ShallowCopy()
    {
        return (UploadPreset) MemberwiseClone();
    }
}