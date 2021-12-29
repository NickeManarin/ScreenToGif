namespace ScreenToGif.Domain.Enums.Native;

///<summary>
///Specifies a raster-operation code. These codes define how the color data for the
///source rectangle is to be combined with the color data for the destination
///rectangle to achieve the final color.
///</summary>
[Flags]
public enum CopyPixelOperations
{
    NoMirrorBitmap = -2147483648,

    /// <summary>dest = BLACK, 0x00000042</summary>
    Blackness = 66,

    ///<summary>dest = (NOT src) AND (NOT dest), 0x001100A6</summary>
    NotSourceErase = 1114278,

    ///<summary>dest = (NOT source), 0x00330008</summary>
    NotSourceCopy = 3342344,

    ///<summary>dest = source AND (NOT dest), 0x00440328</summary>
    SourceErase = 4457256,

    /// <summary>dest = (NOT dest), 0x00550009</summary>
    DestinationInvert = 5570569,

    /// <summary>dest = pattern XOR dest, 0x005A0049</summary>
    PatInvert = 5898313,

    ///<summary>dest = source XOR dest, 0x00660046</summary>
    SourceInvert = 6684742,

    ///<summary>dest = source AND dest, 0x008800C6</summary>
    SourceAnd = 8913094,

    /// <summary>dest = (NOT source) OR dest, 0x00BB0226</summary>
    MergePaint = 12255782,

    ///<summary>dest = (source AND pattern), 0x00C000CA</summary>
    MergeCopy = 12583114,

    ///<summary>dest = source, 0x00CC0020</summary>
    SourceCopy = 13369376,

    /// <summary>dest = source OR dest, 0x00EE0086</summary>
    SourcePaint = 15597702,

    /// <summary>dest = pattern, 0x00F00021</summary>
    PatCopy = 15728673,

    /// <summary>dest = DPSnoo, 0x00FB0A09</summary>
    PatPaint = 16452105,

    /// <summary>dest = WHITE, 0x00FF0062</summary>
    Whiteness = 16711778,

    /// <summary>
    /// Capture window as seen on screen.  This includes layered windows 
    /// such as WPF windows with AllowsTransparency="true", 0x40000000
    /// </summary>
    CaptureBlt = 1073741824,
}