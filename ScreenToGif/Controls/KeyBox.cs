using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenToGif.Controls
{
    public class KeyBox : ContentControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ModifierKeysProperty = DependencyProperty.Register("ModifierKeys", typeof(ModifierKeys), typeof(KeyBox),
            new PropertyMetadata(ModifierKeys.None));

        public static readonly DependencyProperty MainKeyProperty = DependencyProperty.Register("MainKey", typeof(Key?), typeof(KeyBox), new PropertyMetadata(null));

        public static readonly DependencyProperty MaximumModifierCountProperty = DependencyProperty.Register("MaximumModifierCount", typeof(int), typeof(KeyBox),
            new PropertyMetadata(2));

        public static readonly DependencyProperty IsControlDownProperty = DependencyProperty.Register("IsControlDown", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

        public static readonly DependencyProperty IsShiftDownProperty = DependencyProperty.Register("IsShiftDown", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

        public static readonly DependencyProperty IsAltDownProperty = DependencyProperty.Register("IsAltDown", typeof(bool), typeof(KeyBox), new PropertyMetadata(false));

        #endregion

        #region Properties

        public ModifierKeys ModifierKeys
        {
            get { return (ModifierKeys)GetValue(ModifierKeysProperty); }
            set { SetValue(ModifierKeysProperty, value); }
        }

        public Key? MainKey
        {
            get { return (Key?)GetValue(MainKeyProperty); }
            set { SetValue(MainKeyProperty, value); }
        }

        public int MaximumModifierCount
        {
            get { return (int)GetValue(MaximumModifierCountProperty); }
            set { SetValue(MaximumModifierCountProperty, value); }
        }

        public bool IsControlDown
        {
            get { return (bool)GetValue(IsControlDownProperty); }
            set { SetValue(IsControlDownProperty, value); }
        }

        public bool IsShiftDown
        {
            get { return (bool)GetValue(IsShiftDownProperty); }
            set { SetValue(IsShiftDownProperty, value); }
        }

        public bool IsAltDown
        {
            get { return (bool)GetValue(IsAltDownProperty); }
            set { SetValue(IsAltDownProperty, value); }
        }

        private bool _finished;
        private bool _ignore;
        private Key _previousKey;
        private ModifierKeys _previousModifier;

        #endregion

        #region Events

        public static readonly RoutedEvent KeyChangedEvent = EventManager.RegisterRoutedEvent("KeyChanged", RoutingStrategy.Bubble, typeof(KeyChangedEventHandler), typeof(KeyBox));

        public event KeyChangedEventHandler KeyChanged
        {
            add { AddHandler(KeyChangedEvent, value); }
            remove { RemoveHandler(KeyChangedEvent, value); }
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

            IsControlDown = ModifierKeys.HasFlag(ModifierKeys.Control);
            IsAltDown = ModifierKeys.HasFlag(ModifierKeys.Alt);
            IsShiftDown = ModifierKeys.HasFlag(ModifierKeys.Shift);

            _previousModifier = ModifierKeys;
            _previousKey = MainKey ?? Key.None;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Keyboard.Focus(this);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                return;
            }

            //Clear current values.
            ModifierKeys = ModifierKeys.None;
            MainKey = null;
            _finished = false;
            _ignore = false;

            //Check the modifiers.
            IsControlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            IsAltDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            IsShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            //Accept or ignore new values.
            var key = e.Key != Key.System ? e.Key : e.SystemKey;

            //If any modifier.
            if (IsControlDown || IsAltDown || IsShiftDown)
            {
                if ((int)key > 33 && (int)key < 114)
                {
                    //D0 to F24. Valid combinations.
                    ModifierKeys = Keyboard.Modifiers;
                    MainKey = key;
                    _finished = true;
                }
                else
                {
                    //Anything else. Invalid combinations.
                    _ignore = true;
                }

                e.Handled = true;
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

                e.Handled = true;
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
                return;

            if (_finished)
            {
                //If the values are not accepted.
                if (RaiseKeyChangedEvent())
                {
                    IsControlDown = ModifierKeys.HasFlag(ModifierKeys.Control);
                    IsAltDown = ModifierKeys.HasFlag(ModifierKeys.Alt);
                    IsShiftDown = ModifierKeys.HasFlag(ModifierKeys.Shift);
                    return;
                }

                _previousKey = MainKey ?? Key.None;
                _previousModifier = ModifierKeys;
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
                return;
            }

            IsControlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            IsAltDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            IsShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            MainKey = Key.None;

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
}
