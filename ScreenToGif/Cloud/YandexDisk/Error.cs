using System.Runtime.Serialization;

namespace ScreenToGif.Cloud.YandexDisk
{
    [DataContract]
    public class ErrorDescriptor
    {
        [DataMember]
        public string message { get; set; }

        [DataMember]
        public string description { get; set; }

        [DataMember]
        public string error { get; set; }
    }
}