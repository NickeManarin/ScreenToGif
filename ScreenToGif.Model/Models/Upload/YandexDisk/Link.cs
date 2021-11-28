using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.YandexDisk
{
    [DataContract]
    public class Link
    {
        [DataMember]
        public string Href { get; set; }
    }
}