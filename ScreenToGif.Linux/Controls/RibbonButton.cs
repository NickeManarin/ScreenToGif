using Avalonia;
using Avalonia.Controls;

namespace ScreenToGif.Linux.Controls;

/// <summary>
/// A compact command button whose icon and caption follow the Windows editor ribbon metrics.
/// </summary>
public sealed class RibbonButton : Button
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<RibbonButton, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconKindProperty =
        AvaloniaProperty.Register<RibbonButton, string>(nameof(IconKind), "File");

    public static readonly StyledProperty<double> IconWidthProperty =
        AvaloniaProperty.Register<RibbonButton, double>(nameof(IconWidth), 28d);

    public static readonly StyledProperty<double> IconHeightProperty =
        AvaloniaProperty.Register<RibbonButton, double>(nameof(IconHeight), 28d);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string IconKind
    {
        get => GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public double IconWidth
    {
        get => GetValue(IconWidthProperty);
        set => SetValue(IconWidthProperty, value);
    }

    public double IconHeight
    {
        get => GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }
}
