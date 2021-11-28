namespace ScreenToGif.Domain.Enums;

public enum HardwareAccelerationModes
{
    /// <summary>
    /// Only lets you select non-hardware backed encoders. 
    /// </summary>
    Off,

    /// <summary>
    /// Lets you select hardware backed encoders too. -hwaccel auto
    /// </summary>
    On,

    /// <summary>
    /// Only lets you select non-hardware backed encoders, but switches to one if possible. -hwaccel auto
    /// </summary>
    Auto
}