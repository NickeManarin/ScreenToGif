namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Type of color quantization methods of the gif encoder.
/// </summary>
public enum ColorQuantizationTypes
{
    Neural = 0,
    Octree = 1,
    MedianCut = 2,
    Grayscale = 3,
    MostUsed = 4,
    Palette = 5,
}