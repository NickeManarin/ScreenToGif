namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Png prediction methods used by FFmpeg.
/// </summary>
public enum PredictionMethods
{
    None,
    Sub,
    Up,
    Avg,
    Paeth,
    Mixed
}