using System.Runtime.Serialization;

namespace ScreenToGif.Domain.Models.Upload.YandexDisk
{
    [DataContract]
    public class ErrorDescriptor
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Error { get; set; }
    }
}