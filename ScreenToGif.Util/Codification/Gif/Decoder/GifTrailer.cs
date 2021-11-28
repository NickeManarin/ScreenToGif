namespace ScreenToGif.Util.Codification.Gif.Decoder;

public class GifTrailer : GifBlock
{
    internal const int TrailerByte = 0x3B;

    private GifTrailer()
    {}

    public override GifBlockKind Kind => GifBlockKind.Other;

    internal static GifTrailer ReadTrailer()
    {
        return new GifTrailer();
    }
}