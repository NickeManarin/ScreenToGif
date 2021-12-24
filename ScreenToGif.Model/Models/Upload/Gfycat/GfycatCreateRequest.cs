using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Gfycat;

[DataContract]
public class GfycatCreateRequest
{
    [DataMember(Name = "title", EmitDefaultValue = false)]
    public string Tile { get; set; }

    [DataMember(Name = "description", EmitDefaultValue = false)]
    public string Description { get; set; }

    [DataMember(Name = "tags", EmitDefaultValue = false)]
    public string[] Tags { get; set; }

    [DataMember(Name = "private", EmitDefaultValue = false)]
    public bool IsPrivate { get; set; }
}