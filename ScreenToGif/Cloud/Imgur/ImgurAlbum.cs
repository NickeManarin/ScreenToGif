﻿using System.Runtime.Serialization;

namespace ScreenToGif.Cloud.Imgur
{
    [DataContract]
    public class ImgurImageData
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "datetime")]
        public int Datetime { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "animated")]
        public bool Animated { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name = "views")]
        public int Views { get; set; }

        [DataMember(Name = "bandwidth")]
        public long Bandwidth { get; set; }

        [DataMember(Name = "deletehash")]
        public string DeleteHash { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "section")]
        public string Section { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }

        [DataMember(Name = "gifv")]
        public string Gifv { get; set; }

        [DataMember(Name = "mp4")]
        public string Mp4 { get; set; }

        [DataMember(Name = "webm")]
        public string Webm { get; set; }

        [DataMember(Name = "looping")]
        public bool Looping { get; set; }

        [DataMember(Name = "favorite")]
        public bool Favorite { get; set; }

        [DataMember(Name = "nsfw")]
        public bool? Nsfw { get; set; }

        [DataMember(Name = "vote")]
        public string Vote { get; set; }

        [DataMember(Name = "comment_preview")]
        public string CommentPreview { get; set; }
    }

    [DataContract]
    public class ImgurAlbumData
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "datetime")]
        public int Datetime { get; set; }

        [DataMember(Name = "cover")]
        public string Cover { get; set; }

        [DataMember(Name = "cover_width")]
        public string CoverWidth { get; set; }

        [DataMember(Name = "cover_height")]
        public string CoverHeight { get; set; }

        [DataMember(Name = "account_url")]
        public string AccountUrl { get; set; }

        [DataMember(Name = "account_id")]
        public long? AccountId { get; set; }

        [DataMember(Name = "privacy")]
        public string Privacy { get; set; }

        [DataMember(Name = "layout")]
        public string Layout { get; set; }

        [DataMember(Name = "views")]
        public int Views { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }

        [DataMember(Name = "favorite")]
        public bool Favorite { get; set; }

        [DataMember(Name = "nsfw")]
        public bool? Nsfw { get; set; }

        [DataMember(Name = "section")]
        public string Section { get; set; }

        [DataMember(Name = "order")]
        public int Order { get; set; }

        [DataMember(Name = "deletehash")]
        public string DeleteHash { get; set; }

        [DataMember(Name = "images_count")]
        public int ImagesCount { get; set; }

        [DataMember(Name = "images")]
        public ImgurImageData[] Images { get; set; }
    }
}