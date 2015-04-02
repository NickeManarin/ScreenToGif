using System;
using System.Collections.Generic;
using System.Drawing;

namespace ScreenToGif.ImageUtil.Quantitizer
{
    /// <summary>
    /// This interface provides a color quantization capabilities.
    /// </summary>
    public interface IColorQuantizer
    {
        /// <summary>
        /// Prepares the quantizer for image processing.
        /// </summary>
        void Prepare(Bitmap image);

        /// <summary>
        /// Adds the color to quantizer.
        /// </summary>
        void AddColor(Color color, Int32 x, Int32 y);

        /// <summary>
        /// Gets the palette with specified count of the colors.
        /// </summary>
        List<Color> GetPalette(Int32 colorCount);

        /// <summary>
        /// Gets the index of the palette for specific color.
        /// </summary>
        Int32 GetPaletteIndex(Color color, Int32 x, Int32 y);

        /// <summary>
        /// Gets the color count.
        /// </summary>
        Int32 GetColorCount();

        /// <summary>
        /// Clears this instance.
        /// </summary>
        void Finish();
    }
}