using Avalonia;
using Avalonia.Controls;

namespace ScreenToGif.Linux.Controls;

/// <summary>
/// A tab header with the icon bounds and selected-state geometry of the Windows editor ribbon.
/// </summary>
public sealed class RibbonTabItem : TabItem
{
    public static readonly StyledProperty<string> IconKindProperty =
        AvaloniaProperty.Register<RibbonTabItem, string>(nameof(IconKind), "File");

    public string IconKind
    {
        get => GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }
}
