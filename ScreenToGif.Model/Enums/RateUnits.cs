using System.ComponentModel;

namespace ScreenToGif.Domain.Enums;

public enum RateUnits
{
    [Description("B")]
    Bits,

    [Description("K")]
    Kilobits,

    [Description("M")]
    Megabits
}