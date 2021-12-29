namespace ScreenToGif.Domain.Enums;

public enum ExportFormats
{
    /// <summary>
    /// Graphics Interchange Format.
    /// </summary>
    Gif,

    /// <summary>
    /// Animated Portable Network Graphics.
    /// </summary>
    Apng,

    /// <summary>
    /// Web Picture.
    /// </summary>
    Webp,

    /// <summary>
    /// Better portable graphics.
    /// </summary>
    Bpg,


    /// <summary>
    /// Audio Video Interleaved.
    /// </summary>
    Avi,

    /// <summary>
    /// Matroska.
    /// </summary>
    Mkv,

    /// <summary>
    /// Quicktime movie.
    /// </summary>
    Mov,

    /// <summary>
    /// MPEG-4 Part 14.
    /// </summary>
    Mp4,

    /// <summary>
    /// Web Movie.
    /// </summary>
    Webm,


    /// <summary>
    /// Bitmap.
    /// </summary>
    Bmp,

    /// <summary>
    /// Joint Photographic Experts Group.
    /// </summary>
    Jpeg,

    /// <summary>
    /// Portable Network Graphics.
    /// </summary>
    Png,


    /// <summary>
    /// Project file, .stg or .zip.
    /// </summary>
    Stg,

    /// <summary>
    /// Photoshop file.
    /// </summary>
    Psd,

    /// <summary>
    /// Compressed file.
    /// Not in directly use by the encoder, but as an option for the images and the project.
    /// </summary>
    Zip
}