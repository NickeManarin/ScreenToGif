using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Util.Extensions;
using ScreenToGif.ViewModel.UploadPresets.Gfycat;

namespace ScreenToGif.ViewModel.UploadPresets.History;

public class GfycatHistory : History
{
    private string _gfyId;
    private string _gfyName;
    private string _mp4Url;
    private string _webmUrl;
    private string _gifUrl;
    private string _mobileUrl;
    private long _webmSize;
    private long _gifSize;


    [DataMember(EmitDefaultValue = false)]
    public string GfyId
    {
        get => _gfyId;
        set => SetProperty(ref _gfyId, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string GfyName
    {
        get => _gfyName;
        set => SetProperty(ref _gfyName, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Mp4Url
    {
        get => _mp4Url;
        set
        {
            SetProperty(ref _mp4Url, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string WebmUrl
    {
        get => _webmUrl;
        set
        {
            SetProperty(ref _webmUrl, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string GifUrl
    {
        get => _gifUrl;
        set
        {
            SetProperty(ref _gifUrl, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string MobileUrl
    {
        get => _mobileUrl;
        set
        {
            SetProperty(ref _mobileUrl, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public long WebmSize
    {
        get => _webmSize;
        set => SetProperty(ref _webmSize, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public long GifSize
    {
        get => _gifSize;
        set => SetProperty(ref _gifSize, value);
    }

    [IgnoreDataMember]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override FlowDocument Content
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
                .WithKeyLink("Mp4:", Mp4Url, true)
                .WithLineBreak()
                .WithKeyLink("Webm:", WebmUrl, true)
                .WithLineBreak()
                .WithKeyLink("Gif:", GifUrl, true);

            document.Blocks.Add(paragraph);
            return document;
        }
    }


    public GfycatHistory()
    {
        Type = UploadDestinations.Gfycat;
    }

    public override string GetLink(IPreset preset)
    {
        if (!(preset is GfycatPreset gfycatPreset) || !gfycatPreset.UseDirectLinks)
            return Link;

        switch (gfycatPreset.UrlType)
        {
            case ExportFormats.Webm:
                return WebmUrl;
                
            case ExportFormats.Gif:
                return GifUrl;

            default:
                return Link;
        }
    }
}