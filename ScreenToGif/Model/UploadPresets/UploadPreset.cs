using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management.Instrumentation;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Interfaces;
using ScreenToGif.Model.Events;
using ScreenToGif.Util;

namespace ScreenToGif.Model.UploadPresets
{
    public class UploadPreset : BindableBase, IPreset
    {
        private UploadType _type = UploadType.NotDefined;
        private bool _isEnabled = true;
        private string _title = "";
        private string _description = "";
        private string _imageId;
        private bool _isAnonymous;
        private ArrayList _history = new ArrayList();
        private List<Export> _allowedTypes;
        
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

        public UploadType Type
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

        [IgnoreMember]
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

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<Export> AllowedTypes
        {
            get => _allowedTypes;
            set => SetProperty(ref _allowedTypes, value);
        }

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case UploadType.Imgur:
                        return "Imgur";
                    case UploadType.Gfycat:
                        return "Gfycat";
                    case UploadType.Yandex:
                        return "Yandex";
                    case UploadType.Custom:
                        return LocalizationHelper.Get("S.Options.Upload.Preset.Custom");
                    default:
                        return LocalizationHelper.Get("S.Options.Upload.Preset.Select");
                }
            }
        }

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasLimit => HasSizeLimit || HasDurationLimit || HasResolutionLimit;

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasSizeLimit => _sizeLimit != null;

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasDurationLimit => _durationLimit != null;

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasResolutionLimit => _resolutionLimit != null;

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long? SizeLimit => _sizeLimit;

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan? DurationLimit => _durationLimit;

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Size? ResolutionLimit => _resolutionLimit;

        [IgnoreMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Limit => (HasLimit ? "▼ " : "") + (HasSizeLimit ? Humanizer.BytesToString(SizeLimit ?? 0L) : "") + (HasSizeLimit && (HasDurationLimit || HasResolutionLimit) ? " • " : "") +
            (HasDurationLimit ? $"{DurationLimit:mm\':\'ss} m" : "") + (HasDurationLimit && HasResolutionLimit ? " • " : "") + (HasResolutionLimit ? $"{ResolutionLimit?.Width}x{ResolutionLimit?.Height}" : "");

        [IgnoreMember]
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
}