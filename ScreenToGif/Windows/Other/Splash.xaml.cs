using System.Windows;

namespace ScreenToGif.Windows.Other;

public partial class Splash : Window
{
    private static Splash _splash;

    #region Properties

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(Splash), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty SecondsLeftProperty = DependencyProperty.Register(nameof(SecondsLeft), typeof(int), typeof(Splash), new PropertyMetadata(default(int)));

    public string Subtitle
    {
        get => (string) GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public int SecondsLeft
    {
        get => (int)GetValue(SecondsLeftProperty);
        set => SetValue(SecondsLeftProperty, value);
    }

    #endregion

    public Splash()
    {
        InitializeComponent();
    }

    public static void Display(string title, string subtitle = null)
    {
        _splash?.Close();

        _splash = new Splash
        {
            Title = title,
            Subtitle = subtitle
        };
        _splash.Show();
    }

    public static void SetTime(int seconds)
    {
        if (_splash != null)
            _splash.SecondsLeft = seconds;
    }

    public static void Dismiss()
    {
        _splash?.Close();
        _splash = null;
    }

    public static bool IsBeingDisplayed()
    {
        return _splash != null;
    }
}