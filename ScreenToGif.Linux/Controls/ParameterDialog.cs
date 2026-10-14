using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;

namespace ScreenToGif.Linux.Controls;

public sealed class ParameterDialog : Window
{
    private readonly TextBox[] _inputs;

    public ParameterDialog(string title, params (string Label, string Value)[] fields)
    {
        Title = title;
        Width = 360;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Avalonia.Media.Brush.Parse("#25272A");

        var panel = new StackPanel { Margin = new Thickness(18), Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        });

        _inputs = new TextBox[fields.Length];
        for (var index = 0; index < fields.Length; index++)
        {
            panel.Children.Add(new TextBlock { Text = fields[index].Label });
            _inputs[index] = new TextBox { Text = fields[index].Value, MinHeight = 30 };
            panel.Children.Add(_inputs[index]);
        }

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var cancel = new Button { Content = "Cancel", MinWidth = 84 };
        cancel.Click += (_, _) => Close(null);
        var apply = new Button { Content = "Apply", MinWidth = 84 };
        apply.Click += (_, _) => Close(_inputs.Select(input => input.Text ?? string.Empty).ToArray());
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);
        panel.Children.Add(buttons);
        Content = panel;
    }

    public Task<string[]?> ShowForAsync(Window owner) => ShowDialog<string[]?>(owner);
}

public sealed class ChoiceDialog : Window
{
    private readonly ComboBox _choice;

    public ChoiceDialog(string title, IReadOnlyList<string> choices)
    {
        Title = title;
        Width = 380;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Avalonia.Media.Brush.Parse("#25272A");
        _choice = new ComboBox { ItemsSource = choices, SelectedIndex = 0, MinHeight = 32 };

        var cancel = new Button { Content = "Cancel", MinWidth = 84 };
        cancel.Click += (_, _) => Close(null);
        var apply = new Button { Content = "Apply", MinWidth = 84 };
        apply.Click += (_, _) => Close(_choice.SelectedIndex);
        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = title, FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = "Operation" },
                _choice,
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

    public Task<int?> ShowForAsync(Window owner) => ShowDialog<int?>(owner);
}

public sealed class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmLabel)
    {
        Title = title;
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Avalonia.Media.Brush.Parse("#25272A");
        // Escape and the initial focus both land on Cancel, so a stray Enter or Space cannot confirm
        // a destructive action; confirming takes a deliberate move to the other button.
        var cancel = new Button { Content = "Cancel", MinWidth = 84, IsCancel = true };
        cancel.Click += (_, _) => Close(false);
        Opened += (_, _) => cancel.Focus(NavigationMethod.Tab);
        // Keyboard focus does not return to the owner by itself when a dialog closes: dismissed from
        // the keyboard, the window manager leaves X input focus on the owner's frame rather than
        // the owner, and a window driven by shortcut keys, such as the Recorder, stops hearing them
        // until clicked. Activating the owner once the dialog is gone asks the window manager to
        // hand input focus back.
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
        var confirm = new Button { Content = confirmLabel, MinWidth = 100 };
        confirm.Click += (_, _) => Close(true);
        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = title, FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, confirm }
                }
            }
        };
    }

    public Task<bool> ShowForAsync(Window owner) => ShowDialog<bool>(owner);
}

/// <summary>Reports something the user cannot act on beyond acknowledging it.</summary>
public sealed class MessageDialog : Window
{
    public MessageDialog(string title, string message)
    {
        Title = title;
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Avalonia.Media.Brush.Parse("#25272A");
        var close = new Button { Content = "Close", MinWidth = 84 };
        close.Click += (_, _) => Close();
        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = title, FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { close }
                }
            }
        };
    }

    public Task ShowForAsync(Window owner) => ShowDialog(owner);
}
