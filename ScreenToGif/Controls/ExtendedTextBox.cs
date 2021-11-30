using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Controls;

public class ExtendedTextBox : TextBox
{
    #region Dependency Properties

    public static readonly DependencyProperty AllowSpacingyProperty = DependencyProperty.Register(nameof(AllowSpacing), typeof(bool), typeof(ExtendedTextBox), new PropertyMetadata(true));

    public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(ExtendedTextBox), new PropertyMetadata(""));

    public static readonly DependencyProperty IsObligatoryProperty = DependencyProperty.Register(nameof(IsObligatory), typeof(bool), typeof(ExtendedTextBox));
        
    public static readonly DependencyProperty AllowedCharactersProperty = DependencyProperty.Register(nameof(AllowedCharacters), typeof(string), typeof(ExtendedTextBox));

    #endregion

    #region Properties

    [Bindable(true), Category("Common")]
    public bool AllowSpacing
    {
        get => (bool)GetValue(AllowSpacingyProperty);
        set => SetValue(AllowSpacingyProperty, value);
    }

    [Bindable(true), Category("Common")]
    public string Watermark
    {
        get => (string)GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    [Bindable(true), Category("Common")]
    public bool IsObligatory
    {
        get => (bool)GetValue(IsObligatoryProperty);
        set => SetValue(IsObligatoryProperty, value);
    }

    /// <summary>
    /// When this property has any character, the input text will be only accepted if the character is present in the list of allowed chars.
    /// </summary>
    [Bindable(true), Category("Common")]
    public string AllowedCharacters
    {
        get => (string)GetValue(AllowedCharactersProperty);
        set => SetValue(AllowedCharactersProperty, value);
    }

    #endregion

    static ExtendedTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedTextBox), new FrameworkPropertyMetadata(typeof(ExtendedTextBox)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(OnPasting));
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!AllowSpacing && e.Key == Key.Space)
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(AllowedCharacters) && !IsEntryAllowed(e.Text))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewTextInput(e);
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!IsKeyboardFocusWithin)
        {
            e.Handled = true;
            Focus();
        }

        if (UserSettings.All.TripleClickSelection && e.ClickCount == 3)
            SelectAll();
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        if (!UserSettings.All.TripleClickSelection)
            SelectAll();
    }

    private void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = e.DataObject.GetData(typeof(string)) as string;

            if (!string.IsNullOrWhiteSpace(AllowedCharacters) && !IsTextAllowed(text))
                e.CancelCommand();

            return;
        }

        e.CancelCommand();
    }

    private bool IsEntryAllowed(string text)
    {
        //Only the allowed chars.
        var regex = new Regex($"^[{AllowedCharacters.Replace("-", @"\-")}]+$");

        //Checks if it's a valid char based on the context.
        return regex.IsMatch(text);
    }

    private bool IsTextAllowed(string text)
    {
        return Regex.IsMatch(text, $"^[{AllowedCharacters.Replace("-", @"\-") + (AllowSpacing ? " " : "")})]+$");
    }

    public bool IsNullOrWhiteSpace()
    {
        return string.IsNullOrWhiteSpace(Text);
    }

    public bool IsNullOrEmpty()
    {
        return string.IsNullOrEmpty(Text);
    }

    public string Trim()
    {
        return Text.Trim();
    }
}