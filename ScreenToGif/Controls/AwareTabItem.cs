using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenToGif.Controls;

public class AwareTabItem : TabItem
{
    #region Dependency Property

    public static readonly DependencyProperty IsDarkProperty = DependencyProperty.Register(nameof(IsDark), typeof(bool), typeof(AwareTabItem),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnPropertyChanged));

    public static readonly DependencyProperty ShowBackgroundProperty = DependencyProperty.Register(nameof(ShowBackground), typeof(bool), typeof(AwareTabItem),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, ShowBackground_OnPropertyChanged));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(AwareTabItem));

    #endregion

    #region Property accessors

    /// <summary>
    /// True if the titlebar color is dark.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public bool IsDark
    {
        get => (bool)GetValue(IsDarkProperty);
        set => SetValue(IsDarkProperty, value);
    }

    /// <summary>
    /// True if should display the background of the tab while not selected.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public bool ShowBackground
    {
        get => (bool)GetValue(ShowBackgroundProperty);
        set => SetValue(ShowBackgroundProperty, value);
    }

    /// <summary>
    /// The icon of the tab.
    /// </summary>
    [Description("The icon of the tab.")]
    public Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set => SetCurrentValue(IconProperty, value);
    }

    #endregion

    static AwareTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AwareTabItem), new FrameworkPropertyMetadata(typeof(AwareTabItem)));
    }

    /// <summary>
    /// This method is called when any of our dependency properties change.
    /// </summary>
    /// <param name="d">Dependency Object</param>
    /// <param name="e">EventArgs</param>
    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((AwareTabItem)d).IsDark = (bool)e.NewValue;
    }

    /// <summary>
    /// This method is called when any of our dependency properties change.
    /// </summary>
    /// <param name="d">Dependency Object</param>
    /// <param name="e">EventArgs</param>
    private static void ShowBackground_OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((AwareTabItem)d).ShowBackground = (bool)e.NewValue;
    }
}