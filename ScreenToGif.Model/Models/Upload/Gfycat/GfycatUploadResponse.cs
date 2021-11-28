using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Gfycat;

/// <summary>
/// Gfycat upload response.
/// Not used properties:
/// “gfyNumber”, “mobilePosterUrl”, “posterUrl”, “thumb100PosterUrl”, “max5mbGif”, “max2mbGif”, “width”, “height”,
/// “avgColor”, “frameRate”, “numFrames”, “source”, “createDate”, “nsfw”, “likes”, “published”, “dislikes”, “extraLemmas”, “md5”,
/// “views”, “tags”, “userName”, “title”, “description”, “languageCategories”, “task”, “gfyname”, “md5Found”
/// </summary>
[DataContract]
public class GfycatUploadResponse
{
    [DataMember(Name = "task", EmitDefaultValue = false)]
    public string Task { get; set; }

    [DataMember(Name = "time", EmitDefaultValue = false)]
    public int Time { get; set; }

    [DataMember(Name = "gfyname", EmitDefaultValue = false)]
    public string GfyName { get; set; }


    [DataMember(Name = "gfyId", EmitDefaultValue = false)]
    public string GfyId { get; set; }

    [DataMember(Name = "gfyName", EmitDefaultValue = false)]
    public string GfyNameAux { get; set; }

    [DataMember(Name = "mp4Url", EmitDefaultValue = false)]
    public string Mp4Url { get; set; }

    [DataMember(Name = "webmUrl", EmitDefaultValue = false)]
    public string WebmUrl { get; set; }

    [DataMember(Name = "gifUrl", EmitDefaultValue = false)]
    public string GifUrl { get; set; }

    [DataMember(Name = "mobileUrl", EmitDefaultValue = false)]
    public string MobileUrl { get; set; }


    [DataMember(Name = "mp4Size", EmitDefaultValue = false)]
    public int Mp4Size { get; set; }

    [DataMember(Name = "webmSize", EmitDefaultValue = false)]
    public int WebmSize { get; set; }

    [DataMember(Name = "gifSize", EmitDefaultValue = false)]
    public int GifSize { get; set; }

    [DataMember(Name = "errorMessage", EmitDefaultValue = false)]
    public GfycatErrorResponse Error { get; set; }
}