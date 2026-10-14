using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ScreenToGif.Linux.Controls;

public sealed class StartupFailureWindow : Window
{
    public StartupFailureWindow()
    {
        Title = "ScreenToGif — editor unavailable";
        Width = 480;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Avalonia.Media.Brush.Parse("#25272A");

        var close = new Button
        {
            Content = "Close",
            MinWidth = 84,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        close.Click += (_, _) => Close();
        Content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "The editor could not create its temporary workspace.",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = "Check that the system temporary folder exists, has free space, and is writable, then restart ScreenToGif.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                close
            }
        };
    }
}
