using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls;

public class ExtendedCheckBox : CheckBox
{
    #region Dependency Properties

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string),
        typeof(ExtendedCheckBox), new PropertyMetadata());

    public static readonly DependencyProperty InfoProperty = DependencyProperty.Register(nameof(Info), typeof(string),
        typeof(ExtendedCheckBox), new PropertyMetadata());

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping),
        typeof(ExtendedCheckBox), new PropertyMetadata(TextWrapping.Wrap));

    public static readonly DependencyProperty UncheckOnDisableProperty = DependencyProperty.Register(nameof(UncheckOnDisable), typeof(bool),
        typeof(ExtendedCheckBox), new PropertyMetadata(false));

    #endregion

    #region Properties

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Info
    {
        get => (string)GetValue(InfoProperty);
        set => SetValue(InfoProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public bool UncheckOnDisable
    {
        get => (bool)GetValue(UncheckOnDisableProperty);
        set => SetValue(UncheckOnDisableProperty, value);
    }

    #endregion

    #region Custom Events

    public static readonly RoutedEvent CheckedChangedEvent = EventManager.RegisterRoutedEvent(nameof(CheckedChanged), RoutingStrategy.Bubble,
        typeof(RoutedEventHandler), typeof(ExtendedCheckBox));

    public event RoutedEventHandler CheckedChanged
    {
        add => AddHandler(CheckedChangedEvent, value);
        remove => RemoveHandler(CheckedChangedEvent, value);
    }

    public void RaiseCheckedChangedEvent()
    {
        if (CheckedChangedEvent == null) return;

        var newEventArgs = new RoutedEventArgs(CheckedChangedEvent);
        RaiseEvent(newEventArgs);
    }

    #endregion

    static ExtendedCheckBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedCheckBox), new FrameworkPropertyMetadata(typeof(ExtendedCheckBox)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        Checked += (sender, args) => RaiseCheckedChangedEvent();
        Unchecked += (sender, args) => RaiseCheckedChangedEvent();

        if (UncheckOnDisable)
            IsEnabledChanged += (sender, args) =>
            {
                if (!IsEnabled)
                    IsChecked = false;
            };
    }
}