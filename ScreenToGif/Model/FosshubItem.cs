using System.Runtime.Serialization;

namespace ScreenToGif.Model
{
    [DataContract]
    public class FosshubItem
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}