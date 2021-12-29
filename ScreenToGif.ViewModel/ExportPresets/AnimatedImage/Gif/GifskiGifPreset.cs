using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;

public class GifskiGifPreset : GifPreset
{
    private bool _fast;
    private int _quality = 10;


    public bool Fast
    {
        get => _fast;
        set => SetProperty(ref _fast, value);
    }

    public int Quality
    {
        get => _quality;
        set => SetProperty(ref _quality, value);
    }


    public GifskiGifPreset()
    {
        Encoder = EncoderTypes.Gifski;
        ImageId = "Vector.Gifski";
        RequiresGifski = true;
    }
        
        
    public static List<GifskiGifPreset> Defaults => new()
    {
        new GifskiGifPreset
        {
            TitleKey = "S.Preset.Gif.Gifski.High.Title",
            DescriptionKey = "S.Preset.Gif.Gifski.High.Description",
            HasAutoSave = true,
            IsSelectedForEncoder = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),

            Quality = 1,
            Fast = false
        },

        new GifskiGifPreset
        {
            TitleKey = "S.Preset.Gif.Gifski.Low.Title",
            DescriptionKey = "S.Preset.Gif.Gifski.Low.Description",
            HasAutoSave = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),

            Quality = 20,
            Fast = false
        },
            
        new GifskiGifPreset
        {
            TitleKey = "S.Preset.Gif.Gifski.Fast.Title",
            DescriptionKey = "S.Preset.Gif.Gifski.Fast.Description",
            HasAutoSave = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),

            Quality = 20,
            Fast = true
        }
    };
}