using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Controls;

public class HexadecimalBox : ExtendedTextBox
{
    #region Dependency Properties

    public static readonly DependencyProperty RedProperty = DependencyProperty.Register("Red", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(0, Value_PropertyChanged));

    public static readonly DependencyProperty GreenProperty = DependencyProperty.Register("Green", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(0, Value_PropertyChanged));

    public static readonly DependencyProperty BlueProperty = DependencyProperty.Register("Blue", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(0, Value_PropertyChanged));

    public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register("Alpha", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(255, Value_PropertyChanged));

    public static readonly DependencyProperty DisplayGlyphProperty = DependencyProperty.Register("DisplayGlyph", typeof(bool), typeof(HexadecimalBox), new PropertyMetadata(true));

    public static readonly DependencyProperty DisplayAlphaProperty = DependencyProperty.Register("DisplayAlpha", typeof(bool), typeof(HexadecimalBox), new PropertyMetadata(true));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HexadecimalBox));

    #endregion

    #region Properties

    public int Red
    {
        get => (int)GetValue(RedProperty);
        set => SetValue(RedProperty, value);
    }

    public int Blue
    {
        get => (int)GetValue(BlueProperty);
        set => SetValue(BlueProperty, value);
    }

    public int Green
    {
        get => (int)GetValue(GreenProperty);
        set => SetValue(GreenProperty, value);
    }

    public int Alpha
    {
        get => (int)GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    public bool DisplayGlyph
    {
        get => (bool)GetValue(DisplayGlyphProperty);
        set => SetValue(DisplayGlyphProperty, value);
    }

    public bool DisplayAlpha
    {
        get => (bool)GetValue(DisplayAlphaProperty);
        set => SetValue(DisplayAlphaProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    #endregion

    private static void Value_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var hexaBox = o as HexadecimalBox;

        if (hexaBox == null)
            return;

        hexaBox.RaiseValueChangedEvent();

        hexaBox.Text = $"{(hexaBox.DisplayGlyph ? "#" : "")}{(hexaBox.DisplayAlpha ? hexaBox.Alpha.ToString("X2") : "")}{hexaBox.Red:X2}{hexaBox.Green:X2}{hexaBox.Blue:X2}";
    }

    static HexadecimalBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HexadecimalBox), new FrameworkPropertyMetadata(typeof(HexadecimalBox)));
    }

    #region Overrides

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(OnPasting));

        Text = $"{(DisplayGlyph ? "#" : "")}{(DisplayAlpha ? Alpha.ToString("X2") : "")}{Red:X2}{Green:X2}{Blue:X2}";
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!IsKeyboardFocusWithin)
        {
            e.Handled = true;
            Focus();
        }
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        SelectAll();
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            e.Handled = true;
            return;
        }

        if (!IsEntryAllowed(this, e.Text))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewTextInput(e);
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(Text)) return;

        if (!IsTextAllowed(Text)) return;

        base.OnTextChanged(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        if (string.IsNullOrEmpty(Text) || !IsTextAllowed(Text))
        {
            Alpha = 255;
            Red = 0;
            Green = 0;
            Blue = 0;

            Text = $"{(DisplayGlyph ? "#" : "")}{(DisplayAlpha ? Alpha.ToString("X2") : "")}{Red:X2}{Green:X2}{Blue:X2}";
            return;
        }

        #region Try parse

        try
        {
            var source = Text.Replace("#", "");

            switch (source.Length)
            {
                case 2:
                    Alpha = 255;
                    Blue = Green = Red = Convert.ToInt32(source.Substring(0, 2), 16);
                    break;
                case 4:
                    Alpha = Convert.ToInt32(source.Substring(0, 2), 16);
                    Blue = Green = Red = Convert.ToInt32(source.Substring(2, 2), 16);
                    break;
                case 6:
                    Alpha = 255;
                    Red = Convert.ToInt32(source.Substring(0, 2), 16);
                    Green = Convert.ToInt32(source.Substring(2, 2), 16);
                    Blue = Convert.ToInt32(source.Substring(4, 2), 16);
                    break;
                case 8:
                    Alpha = Convert.ToInt32(source.Substring(0, 2), 16);
                    Red = Convert.ToInt32(source.Substring(2, 2), 16);
                    Green = Convert.ToInt32(source.Substring(4, 2), 16);
                    Blue = Convert.ToInt32(source.Substring(6, 2), 16);
                    break;
            }
        }
        catch
        {}

        #endregion

        Text = $"{(DisplayGlyph ? "#" : "")}{(DisplayAlpha ? Alpha.ToString("X2") : "")}{Red:X2}{Green:X2}{Blue:X2}";
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

    void RaiseValueChangedEvent()
    {
        var newEventArgs = new RoutedEventArgs(ValueChangedEvent);
        RaiseEvent(newEventArgs);
    }

    private bool IsEntryAllowed(TextBox textBox, string text)
    {
        //Digits, points or commas.
        var regex = new Regex(@"^#|[0-9]|[A-F]|$");

        //Checks if it's a valid char based on the context.
        return regex.IsMatch(text) && IsEntryAllowedInContext(textBox, text);
    }

    private bool IsEntryAllowedInContext(TextBox textBox, string next)
    {
        if (textBox.Text.Replace("#", "").Length > 7 && textBox.SelectionLength == 0)
            return false;

        var nChar = next.ToCharArray().FirstOrDefault();

        if (char.IsNumber(nChar) || (nChar >= 97 && nChar <= 102)) //0 to 9, A to F
        {
            if (textBox.Text.Contains("#") && textBox.SelectionStart == 0)
                return false;

            return true;
        }

        if (nChar == '#')
        {
            if (textBox.Text.Any(x => x.Equals('#')))
                return false;

            if (textBox.SelectionStart != 0)
                return false;

            return true;
        }

        return true;
    }

    private bool IsTextAllowed(string text)
    {
        //Allows: #FF, #FF11, #FF1122, #FF112233
        return Regex.IsMatch(text, @"^#{0,1}(([0-9a-fA-F]{2}){1,4})$");
    }

    #endregion
}