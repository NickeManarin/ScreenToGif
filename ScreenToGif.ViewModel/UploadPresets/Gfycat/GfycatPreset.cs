using System.ComponentModel;
using System.Globalization;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Models.Upload.Gfycat;
using ScreenToGif.Util;

namespace ScreenToGif.ViewModel.UploadPresets.Gfycat;

public class GfycatPreset : UploadPreset
{
    private string _accessToken = "";
    private string _refreshToken = "";
    private DateTime? _accessTokenExpiryDate;
    private DateTime? _refreshTokenExpiryDate;
    private bool _askForDetails;
    private string _defaultTitle = "";
    private string _defaultDescription = "";
    private string _defaultTags = "";
    private bool _defaultIsPrivate;
    private bool _useDirectLinks = false;
    private ExportFormats _urlType = ExportFormats.Mp4;


    public string AccessToken
    {
        get => _accessToken;
        set => SetProperty(ref _accessToken, value);
    }

    public string RefreshToken
    {
        get => _refreshToken;
        set => SetProperty(ref _refreshToken, value);
    }

    public DateTime? AccessTokenExpiryDate
    {
        get => _accessTokenExpiryDate;
        set => SetProperty(ref _accessTokenExpiryDate, value);
    }

    public DateTime? RefreshTokenExpiryDate
    {
        get => _refreshTokenExpiryDate;
        set
        {
            SetProperty(ref _refreshTokenExpiryDate, value);
            OnPropertyChanged(nameof(Status));
        }
    }


    public bool AskForDetails
    {
        get => _askForDetails;
        set => SetProperty(ref _askForDetails, value);
    }

    public string DefaultTitle
    {
        get => _defaultTitle;
        set => SetProperty(ref _defaultTitle, value);
    }

    public string DefaultDescription
    {
        get => _defaultDescription;
        set => SetProperty(ref _defaultDescription, value);
    }

    public string DefaultTags
    {
        get => _defaultTags;
        set => SetProperty(ref _defaultTags, value);
    }

    public bool DefaultIsPrivate
    {
        get => _defaultIsPrivate;
        set => SetProperty(ref _defaultIsPrivate, value);
    }


    public bool UseDirectLinks
    {
        get => _useDirectLinks;
        set => SetProperty(ref _useDirectLinks, value);
    }

    public ExportFormats UrlType
    {
        get => _urlType;
        set => SetProperty(ref _urlType, value);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Status => RefreshToken == null || !RefreshTokenExpiryDate.HasValue ?
        LocalizationHelper.Get("S.Options.Upload.Preset.Info.NotAuthorized") : RefreshTokenExpiryDate < DateTime.UtcNow ?
            string.Format(LocalizationHelper.Get("S.Options.Upload.Preset.Info.Expired"), RefreshTokenExpiryDate.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture)) :
            string.Format(LocalizationHelper.Get("S.Options.Upload.Preset.Info.Valid"), RefreshTokenExpiryDate.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture));


    public GfycatPreset() : base(null, TimeSpan.FromMinutes(1))
    {
        Type = UploadDestinations.Gfycat;
        ImageId = "Vector.Gfycat";
        AllowedTypes = new List<ExportFormats>
        {
            ExportFormats.Gif,
                
            ExportFormats.Avi,
            ExportFormats.Mov,
            ExportFormats.Mkv,
            ExportFormats.Mp4,
            ExportFormats.Webm
        };
    }
    
    public GfycatCreateRequest ToCreateRequest()
    {
        var tags = DefaultTags?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[] { };
        tags = tags.Length > 0 ? tags.Select(s => s.Trim()).ToArray() : null;

        return new GfycatCreateRequest
        {
            Tile = DefaultTitle,
            Description = DefaultDescription,
            Tags = tags,
            IsPrivate = DefaultIsPrivate
        };
    }
}