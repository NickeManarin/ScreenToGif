using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace ScreenToGif.Util.Codification.Gif.LegacyEncoder;

/// <summary>
/// Helper Class that gets and sets image pixels using Marshal calls. 
/// Uses old System.Drawing classes.
/// </summary>
public class PixelUtilOld
{
    #region Variables and Properties

    private readonly Bitmap _source = null;
    private BitmapData _bitmapData = null;

    /// <summary>
    /// Gets or sets the address of the first pixel data in the bitmap. This can also be thought of as the first scan line in the bitmap.
    /// </summary>
    /// <returns>
    /// The address of the first pixel data in the bitmap.
    /// </returns>
    public IntPtr Scan0 { get; set; } = IntPtr.Zero;

    /// <summary>
    /// Byte Array containing all pixel information.
    /// </summary>
    public byte[] Pixels { get; set; }

    /// <summary>
    /// Color depth.
    /// </summary>
    public int Depth { get; private set; }

    /// <summary>
    /// Width of the image.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Height of the image.
    /// </summary>
    public int Height { get; private set; }

    #endregion

    /// <summary>
    /// Pixel marshalling class, use this to access pixels rapidly.
    /// </summary>
    /// <param name="source">The Bitmap to work with.</param>
    public PixelUtilOld(Bitmap source)
    {
        _source = source;
    }

    /// <summary>
    /// Lock bitmap data.
    /// </summary>
    public void LockBits()
    {
        // Get width and height of bitmap
        Width = _source.Width;
        Height = _source.Height;

        // Get total locked pixels count
        var pixelCount = Width * Height;

        // Create rectangle to lock
        var rect = new Rectangle(0, 0, Width, Height);

        // get source bitmap pixel format size
        Depth = Image.GetPixelFormatSize(_source.PixelFormat);

        // Check if bpp (Bits Per Pixel) is 8, 24, or 32
        if (Depth != 8 && Depth != 24 && Depth != 32)
            throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");

        // Lock bitmap and return bitmap data
        _bitmapData = _source.LockBits(rect, ImageLockMode.ReadWrite, _source.PixelFormat);

        // Create byte array to copy pixel values
        var step = Depth / 8;
        Pixels = new byte[pixelCount * step];
        Scan0 = _bitmapData.Scan0;

        // Copy data from pointer to array
        Marshal.Copy(Scan0, Pixels, 0, Pixels.Length);
    }

    /// <summary>
    /// Unlock bitmap data
    /// </summary>
    public void UnlockBits()
    {
        // Copy data from byte array to pointer
        Marshal.Copy(Pixels, 0, Scan0, Pixels.Length);

        // Unlock bitmap data
        _source.UnlockBits(_bitmapData);
    }

    /// <summary>
    /// Get the color of the specified pixel
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public Color GetPixel(int x, int y)
    {
        var clr = Color.Empty;

        // Get color components count
        var cCount = Depth / 8;

        // Get start index of the specified pixel
        var i = (y * Width + x) * cCount;

        if (i > Pixels.Length - cCount)
            return Color.Transparent; //throw new IndexOutOfRangeException();

        if (Depth == 32) //For 32 bpp get Red, Green, Blue and Alpha
        {
            var b = Pixels[i];
            var g = Pixels[i + 1];
            var r = Pixels[i + 2];
            var a = Pixels[i + 3]; // a
            clr = Color.FromArgb(a, r, g, b);
        }

        if (Depth == 24) //For 24 bpp get Red, Green and Blue
        {
            var b = Pixels[i];
            var g = Pixels[i + 1];
            var r = Pixels[i + 2];
            clr = Color.FromArgb(r, g, b);
        }

        if (Depth == 8) //For 8 bpp get color value (Red, Green and Blue values are the same)
        {
            var c = Pixels[i];
            clr = Color.FromArgb(c, c, c);
        }

        return clr;
    }

    /// <summary>
    /// Set the color of the specified pixel
    /// </summary>
    /// <param name="x">X axis.</param>
    /// <param name="y">Y axis.</param>
    /// <param name="color">The color to be painted.</param>
    public void SetPixel(int x, int y, Color color)
    {
        //Get color components count
        var cCount = Depth / 8;

        //Get start index of the specified pixel
        var i = (y * Width + x) * cCount;

        //Ignore if out of bounds.
        if (i > Pixels.Length - cCount)
            return;

        if (Depth == 32) //For 32 bpp set Red, Green, Blue and Alpha
        {
            Pixels[i] = color.B;
            Pixels[i + 1] = color.G;
            Pixels[i + 2] = color.R;
            Pixels[i + 3] = color.A;
        }

        if (Depth == 24) //For 24 bpp set Red, Green and Blue
        {
            Pixels[i] = color.B;
            Pixels[i + 1] = color.G;
            Pixels[i + 2] = color.R;
        }

        if (Depth == 8) //For 8 bpp set color value (Red, Green and Blue values are the same)
        {
            Pixels[i] = color.B;
        }
    }

    /// <summary>
    /// Load a line from the image into the provided buffer.
    /// </summary>
    /// <param name="source">The image source.</param>
    /// <param name="pixelFormat">The format of the pixel that will be written to the buffer.</param>
    /// <param name="line">The line index to read.</param>
    /// <param name="buffer">Buffer to write to.</param>
    public static void ReadLine(Bitmap source, PixelFormat pixelFormat, int line, int[] buffer)
    {
        var lockRectangle = new Rectangle(0, line, source.Width, 1);

        var lockBits = source.LockBits(lockRectangle, ImageLockMode.ReadOnly, pixelFormat);

        Marshal.Copy(lockBits.Scan0, buffer, 0, lockBits.Stride / sizeof(int));

        source.UnlockBits(lockBits);
    }
}