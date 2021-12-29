using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Imgur;

[DataContract]
public class ImgurUploadResponse
{
    [DataMember(Name = "data")]
    public ImgurImageData Data { get; set; }

    [DataMember(Name = "success")]
    public bool Success { get; set; }

    [DataMember(Name = "status")]
    public int Status { get; set; }
}