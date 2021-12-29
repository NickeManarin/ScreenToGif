using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenToGif.Controls;

public class KeyBox : ContentControl
{
    #region Variable

    private bool _finished;
    private bool _ignore;
    private Key _previousKey;
    private ModifierKeys _previousModifier;
    private ExtendedButton _removeButton;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty ModifierKeysProperty = DependencyProperty.Register("ModifierKeys", typeof(ModifierKeys), typeof(KeyBox),
        new PropertyMetadata(ModifierKeys.None, Keys_PropertyChanged));

    public static readonly DependencyProperty MainKeyProperty = DependencyProperty.Register("MainKey", typeof(Key?), typeof(KeyBox), new PropertyMetadata(null, Keys_PropertyChanged));

    public static readonly DependencyProperty AllowAllKeysProperty = DependencyProperty.Register("AllowAllKeys", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty IsControlDownProperty = DependencyProperty.Register("IsControlDown", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty IsShiftDownProperty = DependencyProperty.Register("IsShiftDown", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty IsAltDownProperty = DependencyProperty.Register("IsAltDown", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty IsWindowsDownProperty = DependencyProperty.Register("IsWindowsDown", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(KeyBox), new PropertyMetadata(""));

    public static readonly DependencyProperty IsSelectionFinishedProperty = DependencyProperty.Register("IsSelectionFinished", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty CanRemoveProperty = DependencyProperty.Register("CanRemove", typeof(bool), typeof(KeyBox), new PropertyMetadata(true));

    public static readonly DependencyProperty DisplayNoneProperty = DependencyProperty.Register("DisplayNone", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty OnlyModifiersProperty = DependencyProperty.Register(nameof(OnlyModifiers), typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

    public static readonly DependencyProperty IsSingleLetterLowerCaseProperty = DependencyProperty.Register(nameof(IsSingleLetterLowerCase), typeof(bool), typeof(KeyBox), new PropertyMetadata(false, Keys_PropertyChanged));

    public static readonly RoutedEvent KeyChangedEvent = EventManager.RegisterRoutedEvent("KeyChanged", RoutingStrategy.Bubble, typeof(KeyChangedEventHandler), typeof(KeyBox));

    #endregion

    #region Properties

    public ModifierKeys ModifierKeys
    {
        get => (ModifierKeys)GetValue(ModifierKeysProperty);
        set => SetValue(ModifierKeysProperty, value);
    }

    public Key? MainKey
    {
        get => (Key?)GetValue(MainKeyProperty);
        set => SetValue(MainKeyProperty, value);
    }

    public bool AllowAllKeys
    {
        get => (bool)GetValue(AllowAllKeysProperty);
        set => SetValue(AllowAllKeysProperty, value);
    }

    public bool IsControlDown
    {
        get => (bool)GetValue(IsControlDownProperty);
        set => SetValue(IsControlDownProperty, value);
    }

    public bool IsShiftDown
    {
        get => (bool)GetValue(IsShiftDownProperty);
        set => SetValue(IsShiftDownProperty, value);
    }

    public bool IsAltDown
    {
        get => (bool)GetValue(IsAltDownProperty);
        set => SetValue(IsAltDownProperty, value);
    }

    public bool IsWindowsDown
    {
        get => (bool)GetValue(IsWindowsDownProperty);
        set => SetValue(IsWindowsDownProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsSelectionFinished
    {
        get => (bool)GetValue(IsSelectionFinishedProperty);
        set => SetValue(IsSelectionFinishedProperty, value);
    }

    public bool CanRemove
    {
        get => (bool)GetValue(CanRemoveProperty);
        set => SetValue(CanRemoveProperty, value);
    }

    public bool DisplayNone
    {
        get => (bool)GetValue(DisplayNoneProperty);
        set => SetValue(DisplayNoneProperty, value);
    }

    public bool OnlyModifiers
    {
        get => (bool)GetValue(OnlyModifiersProperty);
        set => SetValue(OnlyModifiersProperty, value);
    }

    public bool IsSingleLetterLowerCase
    {
        get => (bool)GetValue(IsSingleLetterLowerCaseProperty);
        set => SetValue(IsSingleLetterLowerCaseProperty, value);
    }

    public event KeyChangedEventHandler KeyChanged
    {
        add => AddHandler(KeyChangedEvent, value);
        remove => RemoveHandler(KeyChangedEvent, value);
    }
       
    #endregion

    #region Events

    private static void Keys_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not KeyBox box)
            return;

        if (box.OnlyModifiers && box.ModifierKeys != ModifierKeys.None)
        {
            box.Text = Native.Helpers.Other.GetSelectKeyText(box.ModifierKeys);
            box.IsSelectionFinished = true;
            return;
        }

        if (box.MainKey == null)
            return;

        box.Text = Native.Helpers.Other.GetSelectKeyText(box.MainKey ?? Key.None, box.ModifierKeys, !(box.IsSingleLetterLowerCase && box.ModifierKeys == ModifierKeys.None), !box.DisplayNone);
        box.IsSelectionFinished = true;
    }

    public bool RaiseKeyChangedEvent()
    {
        var changedArgs = new KeyChangedEventArgs(KeyChangedEvent, _previousModifier, _previousKey);
        RaiseEvent(changedArgs);

        return changedArgs.Cancel;
    }

    #endregion

    static KeyBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(KeyBox), new FrameworkPropertyMetadata(typeof(KeyBox)));
    }

    #region Overrides

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _removeButton = Template.FindName("RemoveButton", this) as ExtendedButton;

        if (_removeButton != null)
            _removeButton.Click += (sender, args) =>
            {
                MainKey = Key.None;
                ModifierKeys = ModifierKeys.None;

                RaiseKeyChangedEvent();

                _previousKey = Key.None;
                _previousModifier = ModifierKeys.None;
                IsSelectionFinished = true;
            };

        _previousModifier = ModifierKeys;
        _previousKey = MainKey ?? Key.None;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        Focus();
        Keyboard.Focus(this);

        e.Handled = true;
        base.OnMouseDown(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        //If not all keys are allowed, enter or tab keys presses moves the focus.
        if (!AllowAllKeys && (e.Key == Key.Enter || e.Key == Key.Tab))
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            return;
        }

        //Clear current values.
        IsSelectionFinished = false;
        ModifierKeys = ModifierKeys.None;
        MainKey = null; // Key.None;
        Text = ""; 
        _finished = false;
        _ignore = false;

        //Check the modifiers.
        IsControlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        IsAltDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        IsShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        if (AllowAllKeys)
            IsWindowsDown = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);

        if (OnlyModifiers)
        {
            ModifierKeys = Keyboard.Modifiers;
            MainKey = null;
            _finished = true;
            return;
        }

        var key = e.Key != Key.System ? e.Key : e.SystemKey;

        //Accept or ignore new values.
        if (AllowAllKeys)
        {
            //More than one modifier key without any other key. Invalid combination.
            if (new[] { IsControlDown, IsAltDown, IsShiftDown, IsWindowsDown }.Count(x => x) > 1 && (((int)key >= 116 && (int)key <= 121) || (int)key == 70 || (int)key == 71))
                _ignore = true;
            else if (key > 0 && (int)key < 172)
            {
                //Cancel to OemClear.
                ModifierKeys = Keyboard.Modifiers;
                MainKey = key;

                //TODO:
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (key == Key.LeftCtrl || key == Key.RightCtrl))
                {
                    IsControlDown = false;
                    ModifierKeys = ModifierKeys.None;
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && (key == Key.LeftAlt || key == Key.RightAlt))
                {
                    IsAltDown = false;
                    ModifierKeys = ModifierKeys.None;
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && (key == Key.LeftShift || key == Key.RightShift))
                {
                    IsShiftDown = false;
                    ModifierKeys = ModifierKeys.None;
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows && (key == Key.LWin || key == Key.RWin))
                {
                    IsWindowsDown = false;
                    ModifierKeys = ModifierKeys.None;
                }

                _finished = true;
            }
        }
        else
        {
            //If any modifier.
            if (IsControlDown || IsAltDown || IsShiftDown || IsWindowsDown)
            {
                if (((int)key > 33 && (int)key < 114) || ((int)key >139 && (int)key < 155))
                {
                    //D0 to F24 and Oem1 to OemBackslash. Valid combinations.
                    ModifierKeys = Keyboard.Modifiers;
                    MainKey = key;
                    _finished = true;
                }
                else
                {
                    //Anything else. Invalid combinations.
                    _ignore = true;
                }
            }
            else
            {
                if ((int)key > 89 && (int)key < 114)
                {
                    //F1 to F24. Valid single keys.
                    ModifierKeys = Keyboard.Modifiers;
                    MainKey = key;
                    _finished = true;
                }
                else
                {
                    //Anything else. Invalid single keys.
                    _ignore = true;
                }
            }
        }

        e.Handled = true;
        base.OnPreviewKeyDown(e);
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        if ((e.Key == Key.Enter || e.Key == Key.Tab) && !AllowAllKeys)
            return;

        if (e.Key == Key.PrintScreen && !OnlyModifiers)
        {
            ModifierKeys = Keyboard.Modifiers;
            MainKey = e.Key;
            _finished = true;
        }

        if (_finished)
        {
            //If the values are not accepted.
            if (RaiseKeyChangedEvent())
            {
                IsControlDown = ModifierKeys.HasFlag(ModifierKeys.Control);
                IsAltDown = ModifierKeys.HasFlag(ModifierKeys.Alt);
                IsShiftDown = ModifierKeys.HasFlag(ModifierKeys.Shift);

                if (AllowAllKeys)
                    IsWindowsDown = ModifierKeys.HasFlag(ModifierKeys.Windows);

                return;
            }

            _previousKey = MainKey ?? Key.None;
            _previousModifier = ModifierKeys;
            IsSelectionFinished = true;
            return;
        }

        //If a invalid key combination is set, return to previous value.
        if (_ignore)
        {
            MainKey = _previousKey;
            ModifierKeys = _previousModifier;
            IsControlDown = ModifierKeys.HasFlag(ModifierKeys.Control);
            IsAltDown = ModifierKeys.HasFlag(ModifierKeys.Alt);
            IsShiftDown = ModifierKeys.HasFlag(ModifierKeys.Shift);
            IsWindowsDown = ModifierKeys.HasFlag(ModifierKeys.Windows);
            return;
        }

        IsControlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        IsAltDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        IsShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        if (AllowAllKeys)
            IsWindowsDown = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);

        MainKey = null;

        base.OnPreviewKeyUp(e);
    }

    #endregion
}

public delegate void KeyChangedEventHandler(object sender, KeyChangedEventArgs e);

public class KeyChangedEventArgs : RoutedEventArgs
{
    public bool Cancel { get; set; }

    public ModifierKeys PreviousModifiers { get; set; }
    public Key PreviousKey { get; set; }
    public ModifierKeys CurrentModifiers { get; set; }
    public Key CurrentKey { get; set; }

    public KeyChangedEventArgs(RoutedEvent routedEvent, ModifierKeys previousMod, Key previousKey, ModifierKeys currentMod, Key currentKey)
    {
        RoutedEvent = routedEvent;
        PreviousModifiers = previousMod;
        PreviousKey = previousKey;
        CurrentModifiers = currentMod;
        CurrentKey = currentKey;
    }

    public KeyChangedEventArgs(RoutedEvent routedEvent, ModifierKeys previousMod, Key previousKey)
    {
        RoutedEvent = routedEvent;
        PreviousModifiers = previousMod;
        PreviousKey = previousKey;
    }
}