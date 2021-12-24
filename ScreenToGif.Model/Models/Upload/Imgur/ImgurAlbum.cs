using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Imgur;

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