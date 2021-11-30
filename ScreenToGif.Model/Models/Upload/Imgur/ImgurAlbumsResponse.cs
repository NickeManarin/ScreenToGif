using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Imgur;

[DataContract]
public class ImgurAlbumsResponse
{
    [DataMember(Name = "data")]
    public List<ImgurAlbumData> Data { get; set; }

    [DataMember(Name = "success")]
    public bool Success { get; set; }

    [DataMember(Name = "status")]
    public int Status { get; set; }
}