using System.Runtime.Serialization;

namespace ScreenToGif.Cloud.Imgur
{
    [DataContract]
    internal class ImgurUploadImageResponse
    {
        [DataMember(Name = "data")]
        public ImgurImageData Data { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "status")]
        public int Status { get; set; }
    }
}