using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.Gfycat;

[DataContract]
public class GfycatAuthRequest
{
    [DataMember(Name = "grant_type", EmitDefaultValue = false)]
    public string GrantType { get; set; }

    [DataMember(Name = "client_id", EmitDefaultValue = false)]
    public string ClientId { get; set; }

    [DataMember(Name = "client_secret", EmitDefaultValue = false)]
    public string ClientSecret { get; set; }

    [DataMember(Name = "username", EmitDefaultValue = false)]
    public string Username { get; set; }

    [DataMember(Name = "password", EmitDefaultValue = false)]
    public string Password { get; set; }

    [DataMember(Name = "refresh_token", EmitDefaultValue = false)]
    public string RefreshToken { get; set; }
}