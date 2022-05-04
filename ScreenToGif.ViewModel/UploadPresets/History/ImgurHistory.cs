using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Util.Extensions;
using ScreenToGif.ViewModel.UploadPresets.Imgur;

namespace ScreenToGif.ViewModel.UploadPresets.History;

public class ImgurHistory : History
{
    private string _id;
    private string _mp4;
    private string _webm;
    private string _gifv;
    private string _gif;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Mp4
    {
        get => _mp4;
        set
        {
            SetProperty(ref _mp4, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string Webm
    {
        get => _webm;
        set => SetProperty(ref _webm, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Gifv
    {
        get => _gifv;
        set
        {
            SetProperty(ref _gifv, value);
            OnPropertyChanged(nameof(Content));
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string Gif
    {
        get => _gif;
        set
        {
            SetProperty(ref _gif, value);
            OnPropertyChanged(nameof(Content));
        }
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
                .WithKeyLink("Mp4:", Mp4, true)
                .WithLineBreak()
                .WithKeyLink("Gifv:", Gifv, true)
                .WithLineBreak()
                .WithKeyLink("Gif:", Gif, true)
                .WithLineBreak()
                .WithKeyLink("S.Options.Upload.History.Detail.DeleteLink", DeletionLink);

            document.Blocks.Add(paragraph);
            return document;
        }
    }

    public ImgurHistory()
    {
        Type = UploadDestinations.Imgur;
    }

    public override string GetLink(IPreset preset)
    {
        if (preset is not ImgurPreset imgurPreset)
            return Link;

        if (imgurPreset.UseDirectLinks)
        {
            if (imgurPreset.UseGifvLinks && !string.IsNullOrEmpty(Gifv))
                return Gifv ?? Gif;

            return Gif;
        }

        return $"https://imgur.com/{Id}";
    }
}