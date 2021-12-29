using System.IO;

namespace ScreenToGif.Util.Codification.Gif.Decoder;

public abstract class GifExtension : GifBlock
{
    public const int ExtensionIntroducer = 0x21;

    public static GifExtension ReadExtension(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
    {
        //Note: at this point, the Extension Introducer (0x21) has already been read
        var label = stream.ReadByte();

        if (label < 0)
            throw GifHelpers.UnexpectedEndOfStreamException();

        switch (label)
        {
            case GifGraphicControlExtension.ExtensionLabel:
                return GifGraphicControlExtension.ReadGraphicsControl(stream);
            case GifCommentExtension.ExtensionLabel:
                return GifCommentExtension.ReadComment(stream);
            case GifPlainTextExtension.ExtensionLabel:
                return GifPlainTextExtension.ReadPlainText(stream, controlExtensions, metadataOnly);
            case GifApplicationExtension.ExtensionLabel:
                return GifApplicationExtension.ReadApplication(stream);
            default:
                throw GifHelpers.UnknownExtensionTypeException(label);
        }
    }
}