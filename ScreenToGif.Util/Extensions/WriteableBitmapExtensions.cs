#region Usings

using System.Windows.Media.Imaging;

using KGySoft.Drawing.Imaging;

using ScreenToGif.Util.Imaging;

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
    public static IReadWriteBitmapData GetReadWriteBitmapData(this WriteableBitmap bitmap) => new WriteableBitmapData(bitmap);

    #endregion
}
