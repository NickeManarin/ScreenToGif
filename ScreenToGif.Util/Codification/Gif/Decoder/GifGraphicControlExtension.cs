using System.IO;

namespace ScreenToGif.Util.Codification.Gif.Decoder;

//Label 0xF9
public class GifGraphicControlExtension : GifExtension
{
    public const int ExtensionLabel = 0xF9;

    public int BlockSize { get; private set; }
    public int DisposalMethod { get; private set; }
    public bool UserInput { get; private set; }
    public bool HasTransparency { get; private set; }
    public int Delay { get; private set; }
    public int TransparencyIndex { get; private set; }

    private GifGraphicControlExtension()
    {}

    public override GifBlockKind Kind => GifBlockKind.Control;

    public static GifGraphicControlExtension ReadGraphicsControl(Stream stream)
    {
        var ext = new GifGraphicControlExtension();
        ext.Read(stream);
        return ext;
    }

    private void Read(Stream stream)
    {
        // Note: at this point, the label (0xF9) has already been read
        var bytes = new byte[6];
        stream.ReadAll(bytes, 0, bytes.Length);
        BlockSize = bytes[0]; // should always be 4

        if (BlockSize != 4)
            throw GifHelpers.InvalidBlockSizeException("Graphic Control Extension", 4, BlockSize);

        var packedFields = bytes[1];
        DisposalMethod = (packedFields & 0x1C) >> 2;
        UserInput = (packedFields & 0x02) != 0;
        HasTransparency = (packedFields & 0x01) != 0;
        Delay = BitConverter.ToUInt16(bytes, 2) * 10; // milliseconds
        TransparencyIndex = bytes[4];
    }
}