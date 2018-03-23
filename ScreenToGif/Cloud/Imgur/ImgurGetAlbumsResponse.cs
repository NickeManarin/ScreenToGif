using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ScreenToGif.Cloud.Imgur
{
    [DataContract]
    public class ImgurGetAlbumsResponse
    {
        [DataMember(Name = "data")]
        public List<ImgurAlbumData> Data { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "status")]
        public int Status { get; set; }
    }
}