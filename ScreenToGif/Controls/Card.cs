using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Controls;

public class Card : Button
{
    #region Dependency Properties

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(Card));

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(Card));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(Card));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(ExtrasStatus), typeof(Card),
        new PropertyMetadata(ExtrasStatus.Available));

    #endregion

    #region Property Accessors

    public Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public ExtrasStatus Status
    {
        get => (ExtrasStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    #endregion

    static Card()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Card), new FrameworkPropertyMetadata(typeof(Card)));
    }
}