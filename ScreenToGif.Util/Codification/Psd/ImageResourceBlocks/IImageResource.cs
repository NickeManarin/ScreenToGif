namespace ScreenToGif.Util.Codification.Psd.ImageResourceBlocks;

internal interface IImageResource
{
    ushort Identifier { get; set; }

    string Name { get; set; }

    byte[] Content { get; }
}