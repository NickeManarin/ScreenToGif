using System;
using System.Linq;
using System.Runtime.Serialization;
using ScreenToGif.Model.UploadPresets.Gfycat;

namespace ScreenToGif.Cloud.Gfycat
{
    [DataContract]
    public class GfycatCreateRequest
    {
        public GfycatCreateRequest()
        { }

        public GfycatCreateRequest(GfycatPreset preset)
        {
            var tags = preset.DefaultTags?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[]{};
            tags = tags.Length > 0 ? tags.Select(s => s.Trim()).ToArray() : null;

            Tile = preset.DefaultTitle;
            Description = preset.DefaultDescription;
            Tags = tags;
            IsPrivate = preset.DefaultIsPrivate;
        }

        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Tile { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        public string[] Tags { get; set; }

        [DataMember(Name = "private", EmitDefaultValue = false)]
        public bool IsPrivate { get; set; }
    }
}