using System.Collections.Generic;
using System.Management.Instrumentation;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class VideoCodec : BindableBase
    {
        [IgnoreMember]
        public VideoCodecs Type { get; internal set; }

        [IgnoreMember]
        public string Name { get; internal set; }

        [IgnoreMember]
        public string Command { get; internal set; }

        [IgnoreMember]
        public string Parameters { get; internal set; }

        [IgnoreMember]
        public bool IsHardwareAccelerated { get; internal set; }
        
        [IgnoreMember]
        public bool CanSetCrf { get; internal set; }

        [IgnoreMember]
        public int MinimumCrf { get; internal set; }

        [IgnoreMember]
        public int MaximumCrf { get; internal set; }

        [IgnoreMember]
        public List<EnumItem<VideoCodecPresets>> CodecPresets { get; internal set; }

        [IgnoreMember]
        public List<EnumItem<VideoPixelFormats>> PixelFormats { get; internal set; }
    }

    public class EnumItem<T> where T : System.Enum
    {
        public T Type { get; set; }

        public string NameKey { get; set; }

        public string Name { get; set; }

        public string Parameter { get; set; }

        public EnumItem()
        { }

        public EnumItem(T type, string nameKey, string name, string parameter)
        {
            Type = type;
            NameKey = nameKey;
            Name = name;
            Parameter = parameter;
        }

        public EnumItem(T type, string nameKey, string parameter)
        {
            Type = type;
            NameKey = nameKey;
            Parameter = parameter;
        }
    }
}