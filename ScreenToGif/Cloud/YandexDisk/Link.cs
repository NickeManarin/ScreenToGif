using System.Runtime.Serialization;

namespace ScreenToGif.Cloud.YandexDisk
{
    [DataContract]
    public class Link
    {
        [DataMember]
        public string href { get; set; }
    }
}