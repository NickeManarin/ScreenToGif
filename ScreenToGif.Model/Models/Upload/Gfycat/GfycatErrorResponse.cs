using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Gfycat;

[DataContract]
public class GfycatErrorResponse
{
    [DataMember(Name = "code", EmitDefaultValue = false)]
    public string Code { get; set; }

    [DataMember(Name = "description", EmitDefaultValue = false)]
    public string Description { get; set; }
}