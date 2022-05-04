using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Controls;

public class IntegerBox : ExtendedTextBox
{
    private static bool _ignore;

    /// <summary>
    /// To avoid losing decimals.
    /// </summary>
    public bool UseTemporary;
    public double Temporary;

    /// <summary>
    /// True if it's necessary to prevent the value changed event from firing.
    /// </summary>
    public bool IgnoreValueChanged { get; set; }

    #region Dependency Property

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(IntegerBox),
        new FrameworkPropertyMetadata(int.MaxValue, OnMaximumPropertyChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(IntegerBox),
        new FrameworkPropertyMetadata(0, OnValuePropertyChanged));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(IntegerBox),
        new FrameworkPropertyMetadata(0, OnMinimumPropertyChanged));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(nameof(StepValue), typeof(int), typeof(IntegerBox),
        new FrameworkPropertyMetadata(1));

    public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(nameof(Offset), typeof(int), typeof(IntegerBox),
        new FrameworkPropertyMetadata(0, OnOffsetPropertyChanged));

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(IntegerBox),
        new PropertyMetadata(1d, OnScalePropertyChanged));

    public static readonly DependencyProperty UpdateOnInputProperty = DependencyProperty.Register(nameof(UpdateOnInput), typeof(bool), typeof(IntegerBox),
        new FrameworkPropertyMetadata(false, OnUpdateOnInputPropertyChanged));

    public static readonly DependencyProperty DefaultValueIfEmptyProperty = DependencyProperty.Register(nameof(DefaultValueIfEmpty), typeof(int), typeof(IntegerBox),
        new FrameworkPropertyMetadata(0));

    public static readonly DependencyProperty EmptyIfValueEmptyProperty = DependencyProperty.Register(nameof(EmptyIfValue), typeof(int), typeof(IntegerBox),
        new FrameworkPropertyMetadata(int.MinValue));

    public static readonly DependencyProperty PropagateWheelEventProperty = DependencyProperty.Register(nameof(PropagateWheelEvent), typeof(bool), typeof(IntegerBox), new PropertyMetadata(default(bool)));
        
    #endregion

    #region Property Accessor

    [Bindable(true), Category("Common")]
    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    [Bindable(true), Category("Common")]
    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    [Bindable(true), Category("Common")]
    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// The Increment/Decrement value.
    /// </summary>
    [Description("The Increment/Decrement value.")]
    public int StepValue
    {
        get => (int)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    [Bindable(true), Category("Common")]
    public int Offset
    {
        get => (int)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    [Bindable(true), Category("Common")]
    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    [Bindable(true), Category("Common")]
    public bool UpdateOnInput
    {
        get => (bool)GetValue(UpdateOnInputProperty);
        set => SetValue(UpdateOnInputProperty, value);
    }

    [Bindable(true), Category("Common")]
    public int DefaultValueIfEmpty
    {
        get => (int)GetValue(DefaultValueIfEmptyProperty);
        set => SetValue(DefaultValueIfEmptyProperty, value);
    }

    [Bindable(true), Category("Common")]
    public int EmptyIfValue
    {
        get => (int)GetValue(EmptyIfValueEmptyProperty);
        set => SetValue(EmptyIfValueEmptyProperty, value);
    }

    /// <summary>
    /// True if the wheel events should not be set as handled.
    /// </summary>
    [Bindable(true), Category("Behavior")]
    public bool PropagateWheelEvent
    {
        get => (bool)GetValue(PropagateWheelEventProperty);
        set => SetValue(PropagateWheelEventProperty, value);
    }

    #endregion

    #region Properties Changed

    private static void OnMaximumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var intBox = d as IntegerBox;

        if (intBox?.Value + intBox?.Offset > intBox?.Maximum)
            intBox.Value = intBox.Maximum + intBox.Offset;
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is IntegerBox box) || _ignore) 
            return;

        _ignore = true;

        if (box.Value + box.Offset > box.Maximum)
        {
            box.UseTemporary = false;
            box.Temporary = (box.Maximum / box.Scale) + box.Offset;
            box.Value = box.Maximum + box.Offset;
        }

        if (box.Value + box.Offset < box.Minimum)
        {
            box.UseTemporary = false;
            box.Temporary = (box.Minimum / box.Scale) + box.Offset;
            box.Value = box.Minimum + box.Offset;
        }

        _ignore = false;

        var value = ((int)Math.Round(((box.UseTemporary ? box.Temporary : box.Value) - box.Offset) * box.Scale, MidpointRounding.ToEven));
        var stringValue = value == box.EmptyIfValue ? "" : value.ToString();

        if (!string.Equals(box.Text, stringValue))
            box.Text = stringValue;

        if (!box.IgnoreValueChanged)
            box.RaiseValueChangedEvent();
    }

    private static void OnMinimumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var intBox = d as IntegerBox;

        if (intBox?.Value + intBox?.Offset < intBox?.Minimum)
            intBox.Value = intBox.Minimum + intBox.Offset;
    }

    private static void OnUpdateOnInputPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((IntegerBox)d).UpdateOnInput = (bool)e.NewValue;
    }

    private static void OnOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is IntegerBox box)) 
            return;

        //The offset value dictates the value being displayed.
        //For example, The value 600 and the Offset 20 should display the text 580.
        //Text = Value - Offset.

        var value = ((int)Math.Round((box.Value - box.Offset) * box.Scale));
        box.Text = value == box.EmptyIfValue ? "" : value.ToString();
    }

    private static void OnScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is IntegerBox box)) 
            return;

        //The scale value dictates the value being displayed.
        //For example, The value 600 and the scale 1.25 should display the text 750.
        //Text = Value * Scale.

        var value = ((int)Math.Round((box.Value - box.Offset) * box.Scale));
        box.Text = value == box.EmptyIfValue ? "" : value.ToString();
    }

    #endregion

    static IntegerBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(IntegerBox), new FrameworkPropertyMetadata(typeof(IntegerBox)));
    }

    #region Custom Events

    /// <summary>
    /// Create a custom routed event by first registering a RoutedEventID, this event uses the bubbling routing strategy.
    /// </summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(IntegerBox));

    /// <summary>
    /// Event raised when the numeric value is changed.
    /// </summary>
    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public void RaiseValueChangedEvent()
    {
        if (ValueChangedEvent == null || !IsLoaded)
            return;

        var newEventArgs = new RoutedEventArgs(ValueChangedEvent);
        RaiseEvent(newEventArgs);
    }

    #endregion

    #region Overrides

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(OnPasting));
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        var value = ((int)((Value - Offset) * Scale));
        Text = value == EmptyIfValue ? "" : value.ToString();
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        if (e.Source is IntegerBox)
            SelectAll();
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        //Only sets the focus if not clicking on the Up/Down buttons of a IntegerUpDown.
        if (e.OriginalSource is TextBlock || e.OriginalSource is Border)
            return;

        if (!IsKeyboardFocusWithin)
        {
            e.Handled = true;
            Focus();
        }
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            e.Handled = true;
            return;
        }

        if (!IsEntryAllowed(e.Text))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewTextInput(e);
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        if (!UpdateOnInput || string.IsNullOrEmpty(Text) || !IsTextAllowed(Text))
            return;

        //The offset value dictates the value being displayed.
        //For example, The value 600 and the Offset 20 should display the text 580.
        //Value = (Text + Offset) * Scale.

        Temporary = Convert.ToInt32(Text, CultureInfo.CurrentUICulture) / Scale + Offset;
        Value = (int)Temporary;

        base.OnTextChanged(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        if (!UpdateOnInput)
        {
            if (string.IsNullOrEmpty(Text) || !IsTextAllowed(Text))
            {
                Value = DefaultValueIfEmpty;
                return;
            }

            //The offset value dictates the value being displayed.
            //For example, The value 600 and the Offset 20 should display the text 580.
            //Value = Text + Offset.
            UseTemporary = true;
            Temporary = Convert.ToInt32(Text, CultureInfo.CurrentUICulture) / Scale + Offset;
            Value = (int)Math.Round(Temporary);
            UseTemporary = false;
            return;
        }

        //The offset value dictates the value being displayed.
        //For example, The value 600 and the Offset 20 should display the text 580.
        //Text = Value - Offset.

        var value =((int)((Value - Offset) * Scale));
        Text = value == EmptyIfValue ? "" : value.ToString();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true;
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        base.OnKeyDown(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!IsKeyboardFocusWithin)
            return;
            
        var step = Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Control)
            ? 50 : Keyboard.Modifiers == ModifierKeys.Shift
                ? 10 : Keyboard.Modifiers == ModifierKeys.Control
                    ? 5 : StepValue;

        Value = e.Delta > 0 ? 
            Math.Min(Maximum + Offset, Value + step) : 
            Math.Max(Minimum + Offset, Value - step);

        e.Handled = !PropagateWheelEvent;
    }

    #endregion

    #region Base Properties Changed

    private void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = e.DataObject.GetData(typeof(string)) as string;

            if (!IsTextAllowed(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    #endregion

    #region Methods

    private bool IsEntryAllowed(string text)
    {
        //Only numbers.
        var regex = new Regex(@"^-|[0-9]$");

        //Checks if it's a valid char based on the context.
        return regex.IsMatch(text);
    }

    private bool IsTextAllowed(string text)
    {
        return Minimum < 0 ? Regex.IsMatch(text, @"^[-]?(?:[0-9]{1,9})?$") : Regex.IsMatch(text, @"^(?:[0-9]{1,9})?$");
    }

    #endregion
}