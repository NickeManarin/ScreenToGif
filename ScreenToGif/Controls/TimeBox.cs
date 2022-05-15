using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace ScreenToGif.Controls;

public class TimeBox : ExtendedTextBox
{
    private bool _ignore = false;

    #region Dependency Properties

    public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(TimeSpan?), typeof(TimeBox),
        new FrameworkPropertyMetadata(null, Selected_PropertyChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(TimeSpan?), typeof(TimeBox),
        new FrameworkPropertyMetadata(new TimeSpan(0, 23, 59, 59, 999), Maximum_PropertyChanged));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(TimeSpan?), typeof(TimeBox),
        new FrameworkPropertyMetadata(TimeSpan.Zero, Minimum_PropertyChanged));

    public static readonly DependencyProperty AvoidScrollProperty = DependencyProperty.Register(nameof(AvoidScroll), typeof(bool), typeof(TimeBox),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty DisplaySecondsProperty = DependencyProperty.Register(nameof(DisplaySeconds), typeof(bool), typeof(TimeBox),
        new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty DisplayMillisecondsProperty = DependencyProperty.Register(nameof(DisplayMilliseconds), typeof(bool), typeof(TimeBox),
        new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty DisplayEmptyAsMidnightProperty = DependencyProperty.Register(nameof(DisplayEmptyAsMidnight), typeof(bool), typeof(TimeBox),
        new FrameworkPropertyMetadata(false));

    #endregion

    #region Property Accessor

    [Bindable(true), Category("Common")]
    public TimeSpan? Selected
    {
        get => (TimeSpan?)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    [Bindable(true), Category("Common")]
    public TimeSpan? Maximum
    {
        get => (TimeSpan?)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    [Bindable(true), Category("Common")]
    public TimeSpan? Minimum
    {
        get => (TimeSpan?)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    [Bindable(true), Category("Common")]
    public bool AvoidScroll
    {
        get => (bool)GetValue(AvoidScrollProperty);
        set => SetValue(AvoidScrollProperty, value);
    }

    [Bindable(true), Category("Common")]
    public bool DisplaySeconds
    {
        get => (bool)GetValue(DisplaySecondsProperty);
        set => SetValue(DisplaySecondsProperty, value);
    }

    [Bindable(true), Category("Common")]
    public bool DisplayMilliseconds
    {
        get => (bool)GetValue(DisplayMillisecondsProperty);
        set => SetValue(DisplayMillisecondsProperty, value);
    }

    [Bindable(true), Category("Common")]
    public bool DisplayEmptyAsMidnight
    {
        get => (bool)GetValue(DisplayEmptyAsMidnightProperty);
        set => SetValue(DisplayEmptyAsMidnightProperty, value);
    }

    protected string Format => "hh':'mm" + (DisplaySeconds ? "':'ss" + (DisplayMilliseconds ? "'.'fff" : "") : "");

    #endregion

    #region Property Changed

    private static void Selected_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is TimeBox timeBox) || timeBox._ignore)
            return;

        if (timeBox.Selected > timeBox.Maximum)
        {
            timeBox.Tag = timeBox.Maximum;
            timeBox.Selected = timeBox.Maximum;
        }
        else if (timeBox.Selected < timeBox.Minimum)
        {
            timeBox.Tag = timeBox.Minimum;
            timeBox.Selected = timeBox.Minimum;
        }

        timeBox.Text = timeBox.Selected?.ToString(timeBox.Format, CultureInfo.InvariantCulture) ?? "";
    }

    private static void Maximum_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var timeBox = d as TimeBox;

        if (!(timeBox?.Tag is TimeSpan selected))
            return;

        if (selected > timeBox.Maximum)
        {
            timeBox.Tag = timeBox.Maximum;
            timeBox.Selected = timeBox.Maximum;
        }
    }

    private static void Minimum_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var timeBox = d as TimeBox;

        if (!(timeBox?.Tag is TimeSpan selected))
            return;

        if (selected < timeBox.Minimum)
        {
            timeBox.Tag = timeBox.Minimum;
            timeBox.Selected = timeBox.Minimum;
        }
    }

    #endregion

    static TimeBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeBox), new FrameworkPropertyMetadata(typeof(TimeBox)));
    }

    #region Overrides

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        base.OnPreviewTextInput(e);

        if (_ignore)
            return;

        if (SelectionLength > 0)
        {
            e.Handled = false;
            return;
        }

        if (Text.Length + e.Text.Length < 2)
            return;

        #region Hour 01

        if (Text.Length == 1)
        {
            //Text property is old. In order to test, add new characters right now.
            Text += e.TextComposition.Text;

            //Validate if it's a valid hour value (0 - 23).
            if (int.TryParse(Text.Substring(0, 2), out var hour))
            {
                if (hour > 23)
                    Text = "23";

                Select(Text.Length, 0);
                e.Handled = true;
            }

            return;
        }

        #endregion

        #region Minute 01:02

        if (Text.Length == 4)
        {
            Text += e.TextComposition.Text;

            //Validate if it's a valid minute value (0 - 59).
            if (int.TryParse(Text.Substring(3, 2), out var minute))
            {
                if (minute > 59)
                    Text = Text.Substring(0, 3) + "59";

                Select(Text.Length, 0);
                e.Handled = true;
            }

            return;
        }

        #endregion

        if (!DisplaySeconds && Text.Length > 4)
        {
            UpdateSource();

            if (!e.Handled)
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            e.Handled = true;
            return;
        }

        #region Second 01:02:03

        if (Text.Length == 7)
        {
            Text = Text.Insert(SelectionStart, e.TextComposition.Text);

            //Validate if it's a valid seconds value (0 to 59).
            if (int.TryParse(Text.Substring(6, 2), out var second))
            {
                if (second > 59)
                    Text = Text.Substring(0, 6) + "59";

                e.Handled = true;
            }
        }

        #endregion

        #region Millisecond 01:02:03.004

        if (Text.Length == 11)
        {
            Text = Text.Insert(SelectionStart, e.TextComposition.Text);

            //SelectionStart = 7;
            //SelectionLength = 0;
        }

        #endregion

        //Don't let the user add more numbers if the maximum length will be surpassed.
        if (Text.Length > (DisplayMilliseconds ? 11 : 6))
        {
            UpdateSource();

            if (!e.Handled)
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            e.Handled = true;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        #region Navigation or selection

        if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Tab || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Escape || e.Key == Key.Home || e.Key == Key.End)
        {
            e.Handled = false;
            return;
        }

        #endregion

        if (IsReadOnly)
        {
            e.Handled = true;
            return;
        }

        #region Remove

        if (e.Key == Key.Back || e.Key == Key.Delete)
        {
            if (SelectionLength == Text.Length || Text.Length == 1 && (SelectionStart == 0 && e.Key == Key.Delete || SelectionStart == 1 && e.Key == Key.Back))
            {
                Text = "";
                RaiseEvent(new RoutedEventArgs(TextChangedEvent));
                UpdateSource();
            }

            e.Handled = false;
            return;
        }

        #endregion

        #region Colon (:) and period (.)

        if ((e.Key == Key.OemQuestion || e.Key == Key.OemPeriod) && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            var separatorSelected = Text.Substring(SelectionStart, SelectionLength).Contains(":") || Text.Substring(SelectionStart, SelectionLength).Contains(".");

            //Let it add a separator if in the right position.
            if (SelectionStart == 2 || SelectionStart == 5 && DisplaySeconds || SelectionStart == 8 || separatorSelected)
            {
                e.Handled = false;
                return;
            }

            if (Text.Length > 8)
            {
                e.Handled = true;
                return;
            }

            #region Adds the hour, minute, second and millisecond

            //1 --> 01:
            //0 --> 01:
            if (Text.Length == 1)
                Text = "0" + (Text.Equals("0") ? "1" : Text) + ":";

            //01:2 --> 01:02:
            //01:0 --> 01:01:
            else if (Text.Length == 4)
                Text = Text.Substring(0, 3) + "0" + (Text.Substring(3, 1).Equals("0") ? "1" : Text.Substring(3, 1)) + (DisplaySeconds ? ":" : "");

            //01:02:5 --> 01:02:05
            //01:02:0 --> 01:02:00
            else if (Text.Length == 7)
                Text = Text.Substring(0, 6) + "0" + Text.Substring(6, 1);

            //01:02:03.5 --> 01:02:03.005
            //01:02:03.0 --> 01:02:03.000
            else if (Text.Length == 10)
                Text = Text.Substring(0, 9) + Text.Substring(6, 1).PadLeft(3, '0');

            #endregion

            SelectionStart = Text.Length;
            e.Handled = true;
            return;
        }

        #endregion

        #region Numeric

        if (e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
        {
            //01
            if (Text.Length - SelectionLength == 2)
            {
                Text = Text + ":";
                Select(Text.Length, 0);
            }

            //01:02
            if (Text.Length - SelectionLength == 5 && DisplaySeconds)
            {
                Text = Text + ":";
                Select(Text.Length, 0);
            }

            //01:02:03
            if (Text.Length - SelectionLength == 8 && DisplayMilliseconds)
            {
                Text = Text + ".";
                Select(Text.Length, 0);
            }

            e.Handled = false;
            return;
        }

        #endregion

        #region Value Navigation

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            //System's actions. Ignore.
            if (e.Key == Key.A || e.Key == Key.X || e.Key == Key.C || e.Key == Key.V)
            {
                e.Handled = false;
                return;
            }

            //Now or maximum.
            if (e.Key == Key.OemSemicolon || e.Key == Key.Oem2)
            {
                //Text = DateTime.Now.TimeOfDay.ToString(Format);
                Selected = Maximum ?? DateTime.Now.TimeOfDay;
                SelectAll();
                return;
            }

            //Increase or decrease.
            if (e.Key == Key.OemComma || e.Key == Key.Decimal)
            {
                Change(Selected, -1, TimeSpan.FromMinutes(1));

                //Text = string.IsNullOrWhiteSpace(Text) ? DateTime.Now.TimeOfDay.ToString(Format) : Text;

                ////Previous minute.
                //if (TimeSpan.TryParse(Text, out var aux))
                //{
                //    if (aux - TimeSpan.FromMinutes(1) < (Minimum ?? TimeSpan.Zero)) //Deal with milliseconds...
                //    {
                //        aux = Maximum ?? new TimeSpan(0, 23, 59, 59, 999);
                //        Text = aux.ToString(Format);
                //    }
                //    else
                //        Text = aux.Add(TimeSpan.FromMinutes(-1)).ToString(Format);
                //}
            }
            else if (e.Key == Key.OemPeriod)
            {
                Change(Selected, 1, TimeSpan.FromMinutes(1));

                //Text = string.IsNullOrWhiteSpace(Text) ? DateTime.Now.TimeOfDay.ToString(Format) : Text;

                ////Next minute.
                //if (TimeSpan.TryParse(Text, out var aux))
                //{
                //    if (aux + TimeSpan.FromMinutes(1) > (Maximum ?? new TimeSpan(0, 23, 59, 59, 999))) //Deal with milliseconds...
                //    {
                //        aux = Minimum ?? TimeSpan.Zero;
                //        Text = aux.ToString(Format);
                //    }
                //    else
                //        Text = aux.Add(TimeSpan.FromMinutes(1)).ToString(Format);
                //}
            }

            //UpdateSource();
        }

        #endregion
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        SelectAll();
    }

    protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        if (e.NewFocus == e.OldFocus)
            return;

        //Validate on LostFocus.
        if (!TimeSpan.TryParse(Text, out var aux))
        {
            Selected = null;
        }
        else
        {
            //If the TryParse converted a single digit group to days, transform it to hours.
            if (aux.Days > 0 && aux.Days < 24 && aux.Minutes == 0 && aux.Seconds == 0)
                aux = new TimeSpan(aux.Days, 0, 0);

            Selected = aux;
        }

        UpdateSource();

        base.OnPreviewLostKeyboardFocus(e);
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        //Validate on LostFocus.
        if (!TimeSpan.TryParse(Text, out var aux))
        {
            Selected = null;
        }
        else
        {
            //If the TryParse converted a single digit group to days, transform it to hours.
            if (aux.Days > 0 && aux.Days < 24 && aux.Minutes == 0 && aux.Seconds == 0)
                aux = new TimeSpan(aux.Days, 0, 0);

            Selected = aux;
        }

        UpdateSource();

        base.OnLostKeyboardFocus(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        //Validate on LostFocus.
        if (!TimeSpan.TryParse(Text, out var aux))
        {
            Selected = null;
        }
        else
        {
            //If the TryParse converted a single digit group to days, transform it to hours.
            if (aux.Days > 0 && aux.Days < 24 && aux.Minutes == 0 && aux.Seconds == 0)
                aux = new TimeSpan(aux.Days, 0, 0);

            Selected = aux;
        }

        UpdateSource();

        base.OnLostFocus(e);
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!IsKeyboardFocusWithin)
        {
            e.Handled = true;
            Focus();
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (IsReadOnly || AvoidScroll || !IsFocused)
        {
            base.OnMouseWheel(e);
            return;
        }

        switch (Keyboard.Modifiers)
        {
            case ModifierKeys.Control: //Milliseconds.
            {
                if (!DisplayMilliseconds)
                    return;

                Selected = Change(Selected, e.Delta, new TimeSpan(0, 0, 0, 0, 100));
                break;
            }

            case ModifierKeys.None: //Seconds.
            {
                if (!DisplaySeconds)
                    return;

                Selected = Change(Selected, e.Delta, new TimeSpan(0, 0, 1));
                break;
            }

            case ModifierKeys.Shift: //Minutes.
            {
                Selected = Change(Selected, e.Delta, new TimeSpan(0, 1, 0));
                break;
            }

            case ModifierKeys.Shift | ModifierKeys.Control: //Hours.
            {
                Selected = Change(Selected, e.Delta, new TimeSpan(1, 0, 0));
                break;
            }
        }

        e.Handled = true;
        base.OnMouseWheel(e);
    }

    #endregion

    #region Methods

    private void UpdateSource()
    {
        var prop = GetBindingExpression(TextProperty);

        prop?.UpdateSource();
    }

    private TimeSpan Change(TimeSpan? current, int delta, TimeSpan amount)
    {
        return delta > 0 ? current?.Add(amount) ?? Maximum ?? new TimeSpan(0, 23, 59, 59, 999) :
            current?.Subtract(amount) ?? Minimum ?? new TimeSpan(0, 0, 0);
    }

    #endregion
}