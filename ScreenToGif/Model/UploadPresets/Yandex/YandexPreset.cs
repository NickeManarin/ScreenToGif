using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ScreenToGif.Model.Events;
using ScreenToGif.Util;
using ScreenToGif.Windows;

namespace ScreenToGif.Model.UploadPresets.Yandex
{
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
            Type = UploadType.Yandex;
            ImageId = "Vector.YandexDisk";
            AllowedTypes = new List<Export>();
        }

        public override async Task<ValidatedEventArgs> IsValid()
        {
            if (!IsAnonymous && !Cloud.YandexDisk.YandexDisk.IsAuthorized(this))
                return new ValidatedEventArgs("S.SaveAs.Warning.Upload.NotAuthorized", StatusReasons.UploadServiceUnauthorized, () => App.MainViewModel.OpenOptions.Execute(Options.UploadIndex));

            return await base.IsValid();
        }
    }
}