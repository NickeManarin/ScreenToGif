using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenToGif.Util.Extensions;

public static class ImageExtensions
{
    /// <summary>
    /// Gets the BitmapSource from the source and closes the file usage.
    /// </summary>
    /// <param name="fileSource">The file to open.</param>
    /// <param name="size">The maximum height of the image.</param>
    /// <returns>The open BitmapSource.</returns>
    public static BitmapSource SourceFrom(this string fileSource, int? size = null)
    {
        using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            if (size.HasValue)
                bitmapImage.DecodePixelHeight = size.Value;

            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); //Just in case you want to load the image in another thread
            return bitmapImage;
        }
    }

    /// <summary>
    /// Gets the BitmapSource from the source and closes the file usage.
    /// </summary>
    /// <param name="array">The array to open.</param>
    /// <param name="size">The maximum height of the image.</param>
    /// <returns>The open BitmapSource.</returns>
    public static BitmapSource SourceFrom(this byte[] array, int? size = null)
    {
        using (var stream = new MemoryStream(array))
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            if (size.HasValue)
                bitmapImage.DecodePixelHeight = size.Value;

            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); //Just in case you want to load the image in another thread
            return bitmapImage;
        }
    }

    /// <summary>
    /// Gets the BitmapSource from the source and closes the file usage.
    /// </summary>
    /// <param name="stream">The stream to open.</param>
    /// <param name="size">The maximum height of the image.</param>
    /// <returns>The open BitmapSource.</returns>
    public static BitmapSource SourceFrom(this Stream stream, int? size = null)
    {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

        if (size.HasValue)
            bitmapImage.DecodePixelHeight = size.Value;

        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        bitmapImage.Freeze(); //Just in case you want to load the image in another thread
        return bitmapImage;
    }

    /// <summary>
    /// Gets the BitmapSource from the source and closes the file usage.
    /// </summary>
    /// <param name="fileSource">The file to open.</param>
    /// <param name="rect">The desired crop area.</param>
    /// <returns>The open BitmapSource.</returns>
    public static BitmapSource CropFrom(this string fileSource, Int32Rect rect)
    {
        using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read))
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); //Just in case you want to load the image in another thread.

            var scale = Math.Round(bitmapImage.DpiX / 96d, 2);

            var x = Math.Min(bitmapImage.PixelWidth - 1, Math.Max(0, (int)(rect.X * scale)));
            var y = Math.Min(bitmapImage.PixelHeight - 1, Math.Max(0, (int)(rect.Y * scale)));
            var width = (int)(rect.Width * scale);
            var height = (int)(rect.Height * scale);

            width = Math.Min(width, bitmapImage.PixelWidth - x);
            height = Math.Min(height, bitmapImage.PixelHeight - y);

            rect = new Int32Rect(x, y, width, height);

            if (!new Int32Rect(0, 0, bitmapImage.PixelWidth, bitmapImage.PixelHeight).Contains(rect))
                return null;

            return new CroppedBitmap(bitmapImage, rect);
        }
    }
}