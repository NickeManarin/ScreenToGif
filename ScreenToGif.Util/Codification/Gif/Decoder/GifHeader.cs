using System.IO;

namespace ScreenToGif.Util.Codification.Gif.Decoder;

public class GifHeader : GifBlock
{
    public string Signature { get; private set; }
    public string Version { get; private set; }
    public GifLogicalScreenDescriptor LogicalScreenDescriptor { get; private set; }

    private GifHeader()
    {}

    public override GifBlockKind Kind => GifBlockKind.Other;

    public static GifHeader ReadHeader(Stream stream)
    {
        var header = new GifHeader();
        header.Read(stream);
        return header;
    }

    private void Read(Stream stream)
    {
        Signature = GifHelpers.ReadString(stream, 3);

        if (Signature != "GIF")
            throw GifHelpers.InvalidSignatureException(Signature);

        Version = GifHelpers.ReadString(stream, 3);

        if (Version != "87a" && Version != "89a")
            throw GifHelpers.UnsupportedVersionException(Version);

        LogicalScreenDescriptor = GifLogicalScreenDescriptor.ReadLogicalScreenDescriptor(stream);
    }
}