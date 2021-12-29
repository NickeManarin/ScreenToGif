using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util.Extensions;

namespace ScreenToGif.ViewModel.UploadPresets.History;

public class History : BindableBase, IHistory
{
    private UploadDestinations _type;
    private string _presetName;
    private DateTime? _dateInUtc;
    private int _result;
    private long _size;
    private TimeSpan? _duration;
    private string _link;
    private string _deletionLink;
    private string _message;


    public UploadDestinations Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string PresetName
    {
        get => _presetName;
        set => SetProperty(ref _presetName, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public DateTime? DateInUtc
    {
        get => _dateInUtc;
        set
        {
            SetProperty(ref _dateInUtc, value);
            OnPropertyChanged(nameof(DateInLocalTime));
        }
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime? DateInLocalTime => _dateInUtc?.ToLocalTime();

    public int Result
    {
        get => _result;
        set
        {
            SetProperty(ref _result, value);
            OnPropertyChanged(nameof(WasSuccessful));
        }
    }

    public bool WasSuccessful => _result == 200;

    [DataMember(EmitDefaultValue = false)]
    public long Size
    {
        get => _size;
        set => SetProperty(ref _size, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public TimeSpan? Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Link
    {
        get => _link;
        set
        {
            SetProperty(ref _link, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string DeletionLink
    {
        get => _deletionLink;
        set
        {
            SetProperty(ref _deletionLink, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string Message
    {
        get => _message;
        set
        {
            SetProperty(ref _message, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual FlowDocument Content
    {
        get
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(3),
                FontFamily = new FontFamily("Segoe UI")
            };

            if (!WasSuccessful)
            {
                document.Blocks.Add(new Paragraph(new Run(Message)));
                return document;
            }

            var paragraph = new Paragraph()
                .WithKeyLink("S.Options.Upload.History.Detail.Link", Link)
                .WithLineBreak()
                .WithKeyLink("S.Options.Upload.History.Detail.DeleteLink", DeletionLink);

            document.Blocks.Add(paragraph);
            return document;
        }
    }

    public virtual string GetLink(IPreset preset)
    {
        return Link;
    }
}