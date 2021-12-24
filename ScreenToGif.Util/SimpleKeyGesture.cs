using ScreenToGif.Domain.Interfaces;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Windows.Markup;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenToGif.Util;

///<summary>
///Defines a keyboard combination that can be used to invoke a command.
///</summary>
[DataContract]
[ValueSerializer(typeof(KeyGestureValueSerializer))]
[TypeConverter(typeof(KeyGestureConverter))]
public class SimpleKeyGesture : IKeyGesture
{
    [IgnoreDataMember]
    private static readonly TypeConverter KeyGestureConverter = new KeyGestureConverter();

    /// <summary>Gets the modifier keys associated with this <see cref="T:System.Windows.Input.KeyGesture" />.</summary>
    /// <returns>The modifier keys associated with the gesture. The default value is <see cref="F:System.Windows.Input.ModifierKeys.None" />.</returns>
    [DataMember(EmitDefaultValue = false, Name = "Mod")]
    public ModifierKeys Modifiers { get; set; }

    /// <summary>Gets the key associated with this <see cref="T:System.Windows.Input.KeyGesture" />.</summary>
    /// <returns>The key associated with the gesture.  The default value is <see cref="F:System.Windows.Input.Key.None" />.</returns>
    [DataMember]
    public Key Key { get; set; }

    [DataMember]
    public bool IsUppercase { get; set; }

    [DataMember]
    public bool IsInjected { get; set; }

    /// <summary>Gets a string representation of this <see cref="T:System.Windows.Input.KeyGesture" />.</summary>
    /// <returns>The display string for this <see cref="T:System.Windows.Input.KeyGesture" />. The default value is <see cref="F:System.String.Empty" />.</returns>
    [IgnoreDataMember]
    public string DisplayString { get; }


    /// <summary>
    /// The parameterless constructor.
    /// </summary>
    public SimpleKeyGesture()
    {}

    /// <summary>Initializes a new instance of the <see cref="T:System.Windows.Input.KeyGesture" /> class with the specified <see cref="T:System.Windows.Input.Key" />. </summary>
    /// <param name="key">The key associated with this gesture.</param>
    /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
    /// <paramref name="key" /> is not a valid <see cref="T:System.Windows.Input.Key" />.</exception>
    /// <exception cref="T:System.NotSupportedException">
    /// <paramref name="key" /> is not a valid <see cref="T:System.Windows.Input.KeyGesture" />.</exception>
    public SimpleKeyGesture(Key key) : this(key, ModifierKeys.None)
    { }

    /// <summary>Initializes a new instance of the <see cref="T:System.Windows.Input.KeyGesture" /> class with the specified <see cref="T:System.Windows.Input.Key" /> and <see cref="T:System.Windows.Input.ModifierKeys" />.</summary>
    /// <param name="key">The key associated with the gesture.</param>
    /// <param name="modifiers">The modifier keys associated with the gesture.</param>
    /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
    /// <paramref name="modifiers" /> is not a valid <see cref="T:System.Windows.Input.ModifierKeys" />-or-<paramref name="key" /> is not a valid <see cref="T:System.Windows.Input.Key" />.</exception>
    /// <exception cref="T:System.NotSupportedException">
    /// <paramref name="key" /> and <paramref name="modifiers" /> do not form a valid <see cref="T:System.Windows.Input.KeyGesture" />.</exception>
    public SimpleKeyGesture(Key key, ModifierKeys modifiers) : this(key, modifiers, string.Empty)
    { }

    /// <summary>Initializes a new instance of the <see cref="T:System.Windows.Input.KeyGesture" /> class with the specified <see cref="T:System.Windows.Input.Key" /> and <see cref="T:System.Windows.Input.ModifierKeys" />.</summary>
    /// <param name="key">The key associated with the gesture.</param>
    /// <param name="modifiers">The modifier keys associated with the gesture.</param>
    /// <param name="isUppercase">True if the letter is uppercase.</param>
    /// <param name="isInjected">True if keystroke was simulated by other software.</param>
    /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
    /// <paramref name="modifiers" /> is not a valid <see cref="T:System.Windows.Input.ModifierKeys" />-or-<paramref name="key" /> is not a valid <see cref="T:System.Windows.Input.Key" />.</exception>
    /// <exception cref="T:System.NotSupportedException">
    /// <paramref name="key" /> and <paramref name="modifiers" /> do not form a valid <see cref="T:System.Windows.Input.KeyGesture" />.</exception>
    public SimpleKeyGesture(Key key, ModifierKeys modifiers, bool isUppercase = false, bool isInjected = false) : this(key, modifiers, string.Empty, isUppercase, isInjected)
    {
        //Remove the modifier key, if it's the same as the detected pressend key.
        if (key == Key.LeftCtrl || key == Key.LeftShift || key == Key.LeftAlt || key == Key.LWin || key == Key.RightCtrl || key == Key.RightShift || key == Key.RightAlt || key == Key.RWin)
            Modifiers = ModifierKeys.None;
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Windows.Input.KeyGesture" /> class with the specified <see cref="T:System.Windows.Input.Key" />, <see cref="T:System.Windows.Input.ModifierKeys" />, and display string.</summary>
    /// <param name="key">The key associated with the gesture.</param>
    /// <param name="modifiers">The modifier keys associated with the gesture.</param>
    /// <param name="displayString">A string representation of the <see cref="T:System.Windows.Input.KeyGesture" />.</param>
    /// <param name="isUppercase">True if the letter is uppercase.</param>
    /// <param name="isInjected">True if keystroke was simulated by other software.</param>
    /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
    /// <paramref name="modifiers" /> is not a valid <see cref="T:System.Windows.Input.ModifierKeys" />-or-<paramref name="key" /> is not a valid <see cref="T:System.Windows.Input.Key" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="displayString" /> is null.</exception>
    /// <exception cref="T:System.NotSupportedException">
    /// <paramref name="key" /> and <paramref name="modifiers" /> do not form a valid <see cref="T:System.Windows.Input.KeyGesture" />.</exception>
    public SimpleKeyGesture(Key key, ModifierKeys modifiers, string displayString, bool isUppercase = false, bool isInjected = false)
    {
        if (!IsDefinedKey(key))
            throw new InvalidEnumArgumentException(nameof(key), (int)key, typeof(Key));

        Modifiers = modifiers;
        Key = key;
        IsUppercase = isUppercase;
        IsInjected = isInjected;
        DisplayString = displayString ?? throw new ArgumentNullException(nameof(displayString));
    }


    /// <summary>Returns a string that can be used to display the <see cref="T:System.Windows.Input.KeyGesture" />.</summary>
    /// <returns>The string to display </returns>
    /// <param name="culture">The culture specific information.</param>
    public string GetDisplayStringForCulture(CultureInfo culture)
    {
        if (!string.IsNullOrEmpty(DisplayString))
            return DisplayString;

        return (string)KeyGestureConverter.ConvertTo(null, culture, this, typeof(string));
    }

    /// <summary>Determines whether this <see cref="T:System.Windows.Input.KeyGesture" /> matches the input associated with the specified <see cref="T:System.Windows.Input.InputEventArgs" /> object.</summary>
    /// <returns>true if the event data matches this <see cref="T:System.Windows.Input.KeyGesture" />; otherwise, false.</returns>
    /// <param name="targetElement">The target.</param>
    /// <param name="inputEventArgs">The input event data to compare this gesture to.</param>
    public bool Matches(object targetElement, InputEventArgs inputEventArgs)
    {
        if (inputEventArgs is KeyEventArgs keyEventArgs && IsDefinedKey(keyEventArgs.Key) && (Key == keyEventArgs.Key || Key == keyEventArgs.SystemKey || Key == keyEventArgs.DeadCharProcessedKey || Key == keyEventArgs.ImeProcessedKey))
            return Modifiers == Keyboard.Modifiers;

        return false;
    }

    internal static bool IsDefinedKey(Key key)
    {
        if (key >= Key.None)
            return key <= Key.OemClear;

        return false;
    }

    internal static bool IsValid(Key key, ModifierKeys modifiers)
    {
        if ((key < Key.F1 || key > Key.F24) && (key < Key.NumPad0 || key > Key.Divide))
        {
            if ((modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Windows)) != ModifierKeys.None)
            {
                switch (key)
                {
                    case Key.LWin:
                    case Key.RWin:
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                    case Key.LeftAlt:
                    case Key.RightAlt:
                        return false;
                    default:
                        return true;
                }
            }
            else if (key >= Key.D0 && key <= Key.D9 || key >= Key.A && key <= Key.Z)
                return false;
        }
        return true;
    }

    internal static void AddGesturesFromResourceStrings(string keyGestures, string displayStrings, InputGestureCollection gestures)
    {
        while (!string.IsNullOrEmpty(keyGestures))
        {
            var length1 = keyGestures.IndexOf(";", StringComparison.Ordinal);
            string keyGestureToken;

            if (length1 >= 0)
            {
                keyGestureToken = keyGestures.Substring(0, length1);
                keyGestures = keyGestures.Substring(length1 + 1);
            }
            else
            {
                keyGestureToken = keyGestures;
                keyGestures = string.Empty;
            }

            var length2 = displayStrings.IndexOf(";", StringComparison.Ordinal);
            string keyDisplayString;

            if (length2 >= 0)
            {
                keyDisplayString = displayStrings.Substring(0, length2);
                displayStrings = displayStrings.Substring(length2 + 1);
            }
            else
            {
                keyDisplayString = displayStrings;
                displayStrings = string.Empty;
            }

            var fromResourceStrings = CreateFromResourceStrings(keyGestureToken, keyDisplayString);
            if (fromResourceStrings != null)
                gestures.Add(fromResourceStrings);
        }
    }

    internal static KeyGesture CreateFromResourceStrings(string keyGestureToken, string keyDisplayString)
    {
        if (!string.IsNullOrEmpty(keyDisplayString))
            keyGestureToken = keyGestureToken + "," + keyDisplayString;

        return KeyGestureConverter.ConvertFromInvariantString(keyGestureToken) as KeyGesture;
    }
}