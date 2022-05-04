using System.IO;

namespace ScreenToGif.Util.Codification.Gif.Decoder;

public abstract class GifBlock
{
    public static GifBlock ReadBlock(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
    {
        var blockId = stream.ReadByte();

        if (blockId < 0)
            throw GifHelpers.UnexpectedEndOfStreamException();

        switch (blockId)
        {
            case GifExtension.ExtensionIntroducer:
                return GifExtension.ReadExtension(stream, controlExtensions, metadataOnly);
            case GifFrame.ImageSeparator:
                return GifFrame.ReadFrame(stream, controlExtensions, metadataOnly);
            case GifTrailer.TrailerByte:
                return GifTrailer.ReadTrailer();
            default:
                throw GifHelpers.UnknownBlockTypeException(blockId);
        }
    }

    public abstract GifBlockKind Kind { get; }
}