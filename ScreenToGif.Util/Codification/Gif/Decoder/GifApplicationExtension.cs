using System.IO;
using System.Text;

namespace ScreenToGif.Util.Codification.Gif.Decoder;

public class GifApplicationExtension : GifExtension
{
    public const int ExtensionLabel = 0xFF;

    public int BlockSize { get; private set; }

    public string ApplicationIdentifier { get; private set; }

    public byte[] AuthenticationCode { get; private set; }

    public byte[] Data { get; private set; }

    private GifApplicationExtension(){ }

    public override GifBlockKind Kind => GifBlockKind.SpecialPurpose;


    public static GifApplicationExtension ReadApplication(Stream stream)
    {
        var ext = new GifApplicationExtension();
        ext.Read(stream);
        return ext;
    }

    private void Read(Stream stream)
    {
        // Note: at this point, the label (0xFF) has already been read
        var bytes = new byte[12];
        stream.ReadAll(bytes, 0, bytes.Length);
        BlockSize = bytes[0]; // should always be 11

        if (BlockSize != 11)
            throw GifHelpers.InvalidBlockSizeException("Application Extension", 11, BlockSize);

        ApplicationIdentifier = Encoding.ASCII.GetString(bytes, 1, 8);
        var authCode = new byte[3];
        Array.Copy(bytes, 9, authCode, 0, 3);
        AuthenticationCode = authCode;
        Data = GifHelpers.ReadDataBlocks(stream, false);
    }
}