using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls;

public class ExtendedProgressBar : ProgressBar
{
    public enum ProgressState
    {
        Primary,
        Info,
        Warning,
        Danger
    }

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(ProgressState), typeof(ExtendedProgressBar), new PropertyMetadata(ProgressState.Primary));
    public static readonly DependencyProperty ShowPercentageProperty = DependencyProperty.Register(nameof(ShowPercentage), typeof(bool), typeof(ExtendedProgressBar), new PropertyMetadata(default(bool)));

    public ProgressState State
    {
        get => (ProgressState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool ShowPercentage
    {
        get => (bool) GetValue(ShowPercentageProperty);
        set => SetValue(ShowPercentageProperty, value);
    }

    static ExtendedProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedProgressBar), new FrameworkPropertyMetadata(typeof(ExtendedProgressBar)));
    }
}