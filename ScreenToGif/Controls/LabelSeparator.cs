using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls;

public class LabelSeparator : Control
{
    #region Dependency Properties

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(LabelSeparator), 
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TextRightProperty = DependencyProperty.Register("TextRight", typeof(string), typeof(LabelSeparator),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(LabelSeparator), 
        new PropertyMetadata(TextAlignment.Left));

    #endregion

    #region Properties

    public string Text
    {
        get => (string) GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string TextRight
    {
        get => (string)GetValue(TextRightProperty);
        set => SetValue(TextRightProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    #endregion

    static LabelSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LabelSeparator), new FrameworkPropertyMetadata(typeof(LabelSeparator)));
    }
}