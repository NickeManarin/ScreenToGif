using System.Runtime.Serialization;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.UploadPresets.Yandex;

public class YandexPreset : UploadPreset
{
    private string _oAuthToken = "";

    [DataMember(EmitDefaultValue = false)]
    public string OAuthToken
    {
        get => _oAuthToken;
        set => SetProperty(ref _oAuthToken, value);
    }

    public YandexPreset()
    {
        Type = UploadDestinations.Yandex;
        ImageId = "Vector.YandexDisk";
        AllowedTypes = new List<ExportFormats>();
    }
}