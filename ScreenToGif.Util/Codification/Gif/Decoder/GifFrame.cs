using System.IO;

namespace ScreenToGif.Util.Codification.Gif.Decoder;

public class GifFrame : GifBlock
{
    public const int ImageSeparator = 0x2C;

    public GifImageDescriptor Descriptor { get; private set; }
    public GifColor[] LocalColorTable { get; private set; }
    public IList<GifExtension> Extensions { get; private set; }
    public GifImageData ImageData { get; private set; }

    private GifFrame()
    {}

    public override GifBlockKind Kind => GifBlockKind.GraphicRendering;

    public static GifFrame ReadFrame(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
    {
        var frame = new GifFrame();

        frame.Read(stream, controlExtensions, metadataOnly);

        return frame;
    }

    private void Read(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
    {
        //Note: at this point, the Image Separator (0x2C) has already been read
        Descriptor = GifImageDescriptor.ReadImageDescriptor(stream);

        if (Descriptor.HasLocalColorTable)
            LocalColorTable = GifHelpers.ReadColorTable(stream, Descriptor.LocalColorTableSize);

        ImageData = GifImageData.ReadImageData(stream, metadataOnly);
        Extensions = controlExtensions.ToList().AsReadOnly();
    }
}