namespace ScreenToGif.Util.Codification.Psd;

internal class Image : IPsdContent
{
    public long Length { get; }

    public byte[] Content { get; }
}