using System.Runtime.Serialization;

namespace ScreenToGif.Model;

[DataContract]
public class FosshubResponse
{
    [DataMember(Name = "release")]
    public FosshubRelease Release { get; set; }
}