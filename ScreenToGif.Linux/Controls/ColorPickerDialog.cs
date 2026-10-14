using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ScreenToGif.Linux.Controls;

/// <summary>
/// Picks a brush color from the Windows Board's palette or from a typed hex value. Avalonia's
/// full color picker lives in a package this project does not reference, and a swatch grid plus
/// a hex box covers what the Board needs.
/// </summary>
public sealed class ColorPickerDialog : Window
{
    private static readonly string[] Swatches =
    [
        "#000000", "#404040", "#808080", "#C0C0C0", "#FFFFFF", "#7F0000",
        "#FF0000", "#FF7F00", "#FFD800", "#00A000", "#00C8C8", "#0060FF",
        "#3F00FF", "#8B00FF", "#FF00FF", "#8B4513"
    ];

    private readonly TextBox _hex;
    private readonly Border _sample;

    public ColorPickerDialog(Color current)
    {
        Title = "Brush color";
        Width = 300;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#25272A");

        _sample = new Border
        {
            Height = 28,
            Background = new SolidColorBrush(current),
            BorderBrush = Brush.Parse("#8A8E93"),
            BorderThickness = new Thickness(1)
        };

        _hex = new TextBox { Text = ToHex(current), MinHeight = 30 };
        _hex.TextChanged += (_, _) =>
        {
            if (TryParse(_hex.Text, out var parsed))
                _sample.Background = new SolidColorBrush(parsed);
        };

        var palette = new WrapPanel { ItemWidth = 32, ItemHeight = 28 };
        foreach (var swatch in Swatches)
        {
            var button = new Button
            {
                Width = 28,
                Height = 24,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Background = Brush.Parse(swatch),
                BorderBrush = Brush.Parse("#8A8E93"),
                BorderThickness = new Thickness(1)
            };
            button.Click += (_, _) => _hex.Text = swatch;
            palette.Children.Add(button);
        }

        var cancel = new Button { Content = "Cancel", MinWidth = 84 };
        cancel.Click += (_, _) => Close(null);
        var apply = new Button { Content = "Apply", MinWidth = 84 };
        apply.Click += (_, _) => Close(TryParse(_hex.Text, out var parsed) ? parsed : (Color?)null);

        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Brush color", FontSize = 18, FontWeight = FontWeight.SemiBold },
                palette,
                _sample,
                new TextBlock { Text = "Hex value" },
                _hex,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, apply }
                }
            }
        };
    }

    public Task<Color?> ShowForAsync(Window owner) => ShowDialog<Color?>(owner);

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool TryParse(string? text, out Color color)
    {
        color = Colors.Black;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            color = Color.Parse(text.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
