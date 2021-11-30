using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;

namespace ScreenToGif.ViewModel.UploadPresets.Imgur;

public class ImgurPreset : UploadPreset
{
    private string _oAuthToken = "";
    private string _accessToken = "";
    private string _refreshToken = "";
    private DateTime? _expiryDate;
    private bool _useDirectLinks;
    private bool _useGifvLinks;
    private bool _uploadToAlbum;
    private string _selectedAlbum;
    private ArrayList _albums;

    [DataMember(EmitDefaultValue = false)]
    public string OAuthToken
    {
        get => _oAuthToken;
        set => SetProperty(ref _oAuthToken, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string AccessToken
    {
        get => _accessToken;
        set => SetProperty(ref _accessToken, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string RefreshToken
    {
        get => _refreshToken;
        set => SetProperty(ref _refreshToken, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public DateTime? ExpiryDate
    {
        get => _expiryDate;
        set
        {
            SetProperty(ref _expiryDate, value);
            OnPropertyChanged(nameof(Status));
        }
    }
        
    public bool UseDirectLinks
    {
        get => _useDirectLinks;
        set => SetProperty(ref _useDirectLinks, value);
    }

    public bool UseGifvLinks
    {
        get => _useGifvLinks;
        set => SetProperty(ref _useGifvLinks, value);
    }

    public bool UploadToAlbum
    {
        get => _uploadToAlbum;
        set => SetProperty(ref _uploadToAlbum, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string SelectedAlbum
    {
        get => _selectedAlbum;
        set => SetProperty(ref _selectedAlbum, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public ArrayList Albums
    {
        get => _albums;
        set => SetProperty(ref _albums, value);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Status => AccessToken == null || !ExpiryDate.HasValue ?
        LocalizationHelper.Get("S.Options.Upload.Preset.Info.NotAuthorized") : ExpiryDate < DateTime.UtcNow ?
            string.Format(LocalizationHelper.Get("S.Options.Upload.Preset.Info.Expired"), ExpiryDate.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture)) :
            string.Format(LocalizationHelper.Get("S.Options.Upload.Preset.Info.Valid"), ExpiryDate.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture));


    public ImgurPreset() : this(10000000L, TimeSpan.FromMinutes(1))
    { }

    public ImgurPreset(long? sizeLimit = null, TimeSpan? durationLimit = null, Size? resolutionLimit = null) : base(sizeLimit, durationLimit, resolutionLimit)
    {
        Type = UploadDestinations.Imgur;
        ImageId = "Vector.Imgur";
        AllowedTypes = new List<ExportFormats>
        {
            ExportFormats.Apng,
            ExportFormats.Gif,
                
            //Only enable video upload, when the API gets fixed.
            //I also need to pass the correct resource type in the multi-part data (video instead of image).
            //ExportType.Avi,
            //ExportType.Mov,
            //ExportType.Mkv,
            //ExportType.Mp4,
            //ExportType.Webm,

            ExportFormats.Jpeg,
            ExportFormats.Png
        };
    }
}