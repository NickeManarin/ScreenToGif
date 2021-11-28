using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenToGif.Controls;

/// <summary>
/// ListBoxItem used by the languages listBox.
/// </summary>
public class ExtendedListBoxItem : ListBoxItem
{
    #region Variables

    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(UIElement), typeof(ExtendedListBoxItem));
        
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(ExtendedListBoxItem));
        
    public static readonly DependencyProperty MainAuthorProperty = DependencyProperty.Register(nameof(MainAuthor), typeof(string), typeof(ExtendedListBoxItem), new FrameworkPropertyMetadata(""));
        
    public static readonly DependencyProperty AuthorProperty = DependencyProperty.Register(nameof(Author), typeof(string), typeof(ExtendedListBoxItem), new FrameworkPropertyMetadata(""));

    public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ExtendedListBoxItem), new FrameworkPropertyMetadata(20d));

    public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ExtendedListBoxItem), new FrameworkPropertyMetadata(20d));
        
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(ExtendedListBoxItem), new FrameworkPropertyMetadata(0));
        
    public static readonly DependencyProperty ShowMarkOnSelectionProperty = DependencyProperty.Register(nameof(ShowMarkOnSelection), typeof(bool), typeof(ExtendedListBoxItem), new FrameworkPropertyMetadata(true));
        
    #endregion

    #region Properties

    /// <summary>
    /// The Image of the ListBoxItem.
    /// </summary>
    [Description("The Image of the ListBoxItem.")]
    public UIElement Image
    {
        get => (UIElement)GetValue(ImageProperty);
        set => SetCurrentValue(ImageProperty, value);
    }

    /// <summary>
    /// The icon of the ListBoxItem as a Brush.
    /// </summary>
    [Description("The icon of the ListBoxItem as a Brush.")]
    public Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set => SetCurrentValue(IconProperty, value);
    }

    /// <summary>
    /// The author of the ListBoxItem.
    /// </summary>
    [Description("The main author of the ListBoxItem.")]
    public string MainAuthor
    {
        get => (string)GetValue(MainAuthorProperty);
        set => SetCurrentValue(MainAuthorProperty, value);
    }

    /// <summary>
    /// The author of the ListBoxItem.
    /// </summary>
    [Description("The author of the ListBoxItem.")]
    public string Author
    {
        get => (string)GetValue(AuthorProperty);
        set => SetCurrentValue(AuthorProperty, value);
    }

    /// <summary>
    /// The height of the icon.
    /// </summary>
    [Description("The height of the icon."), Category("Common")]
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetCurrentValue(ContentHeightProperty, value);
    }

    /// <summary>
    /// The width of the icon.
    /// </summary>
    [Description("The width of the icon."), Category("Common")]
    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        set => SetCurrentValue(ContentWidthProperty, value);
    }

    /// <summary>
    /// The index of the item on the list. Must be manually set.
    /// </summary>
    [Description("The index of the item on the list. Must be manually set.")]
    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetCurrentValue(IndexProperty, value);
    }

    /// <summary>
    /// True if the item must show the checkmark on selection.
    /// </summary>
    [Description("True if the item must show the checkmark on selection.")]
    public bool ShowMarkOnSelection
    {
        get => (bool)GetValue(ShowMarkOnSelectionProperty);
        set => SetCurrentValue(ShowMarkOnSelectionProperty, value);
    }

    #endregion

    static ExtendedListBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedListBoxItem), new FrameworkPropertyMetadata(typeof(ExtendedListBoxItem)));
    }
}