using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using ScreenToGif.Model.Events;
using ScreenToGif.Util;
using ScreenToGif.Windows;

namespace ScreenToGif.Model.UploadPresets.Gfycat
{
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
        private Export _urlType = Export.Mp4;


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

        public Export UrlType
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
            Type = UploadType.Gfycat;
            ImageId = "Vector.Gfycat";
            AllowedTypes = new List<Export>
            {
                Export.Gif,
                
                Export.Avi,
                Export.Mov,
                Export.Mkv,
                Export.Mp4,
                Export.Webm
            };
        }

        public override async Task<ValidatedEventArgs> IsValid()
        {
            if (!IsAnonymous && !await Cloud.Gfycat.Gfycat.IsAuthorized(this))
                return new ValidatedEventArgs("S.SaveAs.Warning.Upload.NotAuthorized", StatusReasons.UploadServiceUnauthorized, () => App.MainViewModel.OpenOptions.Execute(Options.UploadIndex));

            return await base.IsValid();
        }
    }
}