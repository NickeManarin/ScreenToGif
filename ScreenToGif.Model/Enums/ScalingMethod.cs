namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Scaling quality options for resizing
/// This enum is a subset of <seealso cref="System.Windows.Media.BitmapScalingMode"/>.
/// It is used to expose this enum to the Editor and choose which options are available
/// </summary>
public enum ScalingMethod
{
    Fant = System.Windows.Media.BitmapScalingMode.Fant,
    Linear = System.Windows.Media.BitmapScalingMode.Linear,
    NearestNeighbor = System.Windows.Media.BitmapScalingMode.NearestNeighbor
}