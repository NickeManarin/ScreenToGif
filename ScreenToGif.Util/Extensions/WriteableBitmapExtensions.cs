#region Usings

#region Used Namespaces

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using KGySoft.Drawing.Imaging;

#endregion

#region Used Aliases

using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Size = System.Drawing.Size;

#endregion

#endregion

namespace ScreenToGif.Util.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="WriteableBitmap"/> type.
/// </summary>
public static class WriteableBitmapExtensions
{
    #region Methods

    /// <summary>
    /// Gets a managed read-write accessor for a <see cref="WriteableBitmapData"/> instance.
    /// </summary>
    /// <param name="bitmap">The bitmap to get the managed accessor.</param>
    /// <returns>An <see cref="IReadWriteBitmapData"/> instance that provides managed access to the specified <see cref="bitmap"/>.</returns>
    public static IReadWriteBitmapData GetReadWriteBitmapData(this WriteableBitmap bitmap)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));
        if (bitmap.IsFrozen)
            throw new ArgumentException("Bitmap must not be frozen");

        var format = bitmap.Format;

        // Actually you can support any other formats, including non-natively supported ones by custom PixelFormatInfo and getter/setter delegates
        var pixelFormat = format == PixelFormats.Bgra32 ? PixelFormat.Format32bppArgb
            : format == PixelFormats.Pbgra32 ? PixelFormat.Format32bppPArgb
            : format == PixelFormats.Bgr32 ? PixelFormat.Format32bppRgb
            : format == PixelFormats.Bgr24 ? PixelFormat.Format24bppRgb
            : throw new NotSupportedException(bitmap.Format.ToString());
        
        bitmap.Lock();
        return BitmapDataFactory.CreateBitmapData(bitmap.BackBuffer, new Size(bitmap.PixelWidth, bitmap.PixelHeight), bitmap.BackBufferStride, pixelFormat,
            disposeCallback: () =>
            {
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                bitmap.Unlock();
            });
    }

    #endregion
}
