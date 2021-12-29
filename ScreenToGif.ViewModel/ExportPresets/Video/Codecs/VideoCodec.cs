using System.Runtime.Serialization;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.ViewModels;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

public class VideoCodec : BindableBase
{
    [IgnoreDataMember]
    public VideoCodecs Type { get; internal set; }

    [IgnoreDataMember]
    public string Name { get; internal set; }

    [IgnoreDataMember]
    public string Command { get; internal set; }

    [IgnoreDataMember]
    public string Parameters { get; internal set; }

    [IgnoreDataMember]
    public bool IsHardwareAccelerated { get; internal set; }
        
    [IgnoreDataMember]
    public bool CanSetCrf { get; internal set; }

    [IgnoreDataMember]
    public int MinimumCrf { get; internal set; }

    [IgnoreDataMember]
    public int MaximumCrf { get; internal set; }

    [IgnoreDataMember]
    public List<EnumItem<VideoCodecPresets>> CodecPresets { get; internal set; }

    [IgnoreDataMember]
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