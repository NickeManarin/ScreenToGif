using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls;

public class SplitButton : ItemsControl
{
    #region Variables

    private ExtendedButton _internalButton;
    private Popup _mainPopup;

    private ExtendedMenuItem _current;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(SplitButton), new PropertyMetadata(""));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(SplitButton));

    public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(SplitButton), new FrameworkPropertyMetadata(16d));

    public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(SplitButton), new FrameworkPropertyMetadata(16d));

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(SplitButton), new FrameworkPropertyMetadata(0,
        FrameworkPropertyMetadataOptions.AffectsRender, SelectedIndex_ChangedCallback));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SplitButton), new FrameworkPropertyMetadata(null));
        
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(SplitButton), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(SplitButton), new FrameworkPropertyMetadata(TextWrapping.NoWrap,
        FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    #endregion

    #region Properties

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// The icon of the button as a Brush
    /// </summary>
    [Description("The icon of the button as a Brush.")]
    public Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set => SetCurrentValue(IconProperty, value);
    }

    /// <summary>
    /// The height of the button content.
    /// </summary>
    [Description("The height of the button content."), Category("Common")]
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetCurrentValue(ContentHeightProperty, value);
    }

    /// <summary>
    /// The width of the button content.
    /// </summary>
    [Description("The width of the button content."), Category("Common")]
    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        set => SetCurrentValue(ContentWidthProperty, value);
    }

    /// <summary>
    /// The index of selected item.
    /// </summary>
    [Description("The index of selected item."), Category("Common")]
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetCurrentValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets the command associated with the menu item.
    /// </summary>
    [Category("Action")]
    public ICommand Command
    {
        get => (ICommand) GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the parameter to pass to the <see cref="Command"/> property.
    /// </summary>
    [Category("Action")]
    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    #endregion


    static SplitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _internalButton = Template.FindName("ActionButton", this) as ExtendedButton;
        _mainPopup = Template.FindName("Popup", this) as Popup;

        PrepareMainAction(this);

        //Raises the click event.
        _internalButton.Click += (sender, args) => _current?.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        //Close on click.
        foreach (var item in Items.OfType<ExtendedMenuItem>().ToList())
            item.Click += (sender, args) =>
            {
                _mainPopup.IsOpen = false;

                if (!(sender is ExtendedMenuItem menu))
                    return;

                var index = Items.OfType<ExtendedMenuItem>().Where(w => (w.Tag as string) != "I").ToList().IndexOf(menu);

                if (index != -1)
                    SelectedIndex = index;
            };
    }


    private static void SelectedIndex_ChangedCallback(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (!(o is SplitButton split) || !split.IsLoaded)
            return;

        split.PrepareMainAction(split);
    }

    private void PrepareMainAction(SplitButton split)
    {
        if (split.SelectedIndex < 0)
            return;

        //Ignore children with the Tag == "I".
        var list = split.Items.OfType<ExtendedMenuItem>().Where(w => (w.Tag as string) != "I").ToList();

        if (split.SelectedIndex > list.Count - 1)
        {
            split.SelectedIndex = list.Count - 1;
            return;
        }

        //I'm using the Tag property to store the resource ID.
        if (list[split.SelectedIndex].Tag is string reference)
            split.SetResourceReference(TextProperty, reference);
        else
            split.Text = list[split.SelectedIndex].Header as string;

        split.Icon = list[split.SelectedIndex].Icon;
        split.Command = list[split.SelectedIndex].Command;
            
        _current = list[split.SelectedIndex];
    }
}