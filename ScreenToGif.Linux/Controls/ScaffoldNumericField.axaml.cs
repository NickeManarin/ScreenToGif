using Avalonia;
using Avalonia.Controls;

namespace ScreenToGif.Linux.Controls;

public partial class ScaffoldNumericField : UserControl
{
    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<ScaffoldNumericField, string>(nameof(Value), "0");

    public ScaffoldNumericField() => InitializeComponent();

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
