using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Model.Events;
using ScreenToGif.Util;
using ScreenToGif.Windows;

namespace ScreenToGif.Model.UploadPresets.Imgur
{
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
            Type = UploadType.Imgur;
            ImageId = "Vector.Imgur";
            AllowedTypes = new List<Export>
            {
                Export.Apng,
                Export.Gif,
                
                //Only enable video upload, when the API gets fixed.
                //I also need to pass the correct resource type in the multi-part data (video instead of image).
                //Export.Avi,
                //Export.Mov,
                //Export.Mkv,
                //Export.Mp4,
                //Export.Webm,

                Export.Jpeg,
                Export.Png
            };
        }

        public override async Task<ValidatedEventArgs> IsValid()
        {
            if (!IsAnonymous && !await Cloud.Imgur.Imgur.IsAuthorized(this))
                return new ValidatedEventArgs("S.SaveAs.Warning.Upload.NotAuthorized", StatusReasons.UploadServiceUnauthorized, () => App.MainViewModel.OpenOptions.Execute(Options.UploadIndex));

            return await base.IsValid();
        }
    }
}