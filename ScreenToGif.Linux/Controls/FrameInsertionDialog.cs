using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux.Controls;

/// <summary>Asks where a captured Board recording should enter the current timeline.</summary>
public sealed class FrameInsertionDialog : Window
{
    private readonly List<(RadioButton Button, BoardInsertionPosition Position)> _choices = [];

    public FrameInsertionDialog(int frameCount, int selectedIndex)
    {
        Title = "Insert Board recording";
        Width = 390;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#25272A");

        var validSelection = selectedIndex >= 0 && selectedIndex < frameCount;
        var options = new StackPanel { Spacing = 8 };
        if (validSelection)
        {
            AddChoice(options, BoardInsertionPosition.BeforeSelected, $"Before selected frame {selectedIndex + 1}", selected: true);
            AddChoice(options, BoardInsertionPosition.AfterSelected, $"After selected frame {selectedIndex + 1}");
        }

        AddChoice(options, BoardInsertionPosition.Beginning, "At the beginning", selected: !validSelection);
        AddChoice(options, BoardInsertionPosition.End, $"At the end, after frame {frameCount}");

        var cancel = new Button { Content = "Cancel", MinWidth = 84, IsCancel = true };
        cancel.Click += (_, _) => Close(null);
        var insert = new Button { Content = "Insert frames", MinWidth = 100, IsDefault = true };
        insert.Click += (_, _) => Close(_choices.First(choice => choice.Button.IsChecked == true).Position);

        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Place the recorded frames", FontSize = 18, FontWeight = FontWeight.SemiBold },
                new TextBlock
                {
                    Text = "Choose where the Board recording joins the current timeline.",
                    TextWrapping = TextWrapping.Wrap
                },
                options,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, insert }
                }
            }
        };

        Opened += (_, _) => _choices[0].Button.Focus(NavigationMethod.Tab);
        Closed += (_, _) =>
        {
            if (Owner is not Window owner)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                owner.Activate();
                owner.Focus();
            });
        };
    }

    public Task<BoardInsertionPosition?> ShowForAsync(Window owner) => ShowDialog<BoardInsertionPosition?>(owner);

    private void AddChoice(StackPanel options, BoardInsertionPosition position, string label, bool selected = false)
    {
        var button = new RadioButton
        {
            Content = label,
            GroupName = "BoardInsertionPosition",
            IsChecked = selected,
            MinHeight = 28
        };
        _choices.Add((button, position));
        options.Children.Add(button);
    }
}
