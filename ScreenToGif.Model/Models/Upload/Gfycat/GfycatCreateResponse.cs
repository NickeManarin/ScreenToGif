using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Gfycat;

[DataContract]
public class GfycatCreateResponse
{
    [DataMember(Name = "isOk")]
    public string IsOk { get; set; }

    [DataMember(Name = "gfyname")]
    public string Name { get; set; }

    [DataMember(Name = "secret")]
    public string Secret { get; set; }

    [DataMember(Name = "uploadType")]
    public string UploadType { get; set; }

    [DataMember(Name = "errorType")]
    public string ErrorType { get; set; }

    [DataMember(Name = "errorMessage")]
    public GfycatErrorResponse Error { get; set; }
}