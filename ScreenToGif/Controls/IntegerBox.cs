using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls
{
    public class IntegerBox : TextBox
    {
        #region Variables

        private bool _ignore;

        #endregion

        #region Dependency Property

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(IntegerBox),
            new FrameworkPropertyMetadata(int.MaxValue, OnMaximumPropertyChanged));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(IntegerBox),
            new FrameworkPropertyMetadata(0, OnValuePropertyChanged));

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(IntegerBox),
            new FrameworkPropertyMetadata(0, OnMinimumPropertyChanged));

        public static readonly DependencyProperty StepProperty = DependencyProperty.Register("StepValue", typeof(int), typeof(IntegerBox), 
            new FrameworkPropertyMetadata(1));

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register("Offset", typeof(int), typeof(IntegerBox),
            new FrameworkPropertyMetadata(0, OnOffsetPropertyChanged));

        public static readonly DependencyProperty UpdateOnInputProperty = DependencyProperty.Register("UpdateOnInput", typeof(bool), typeof(IntegerBox),
            new FrameworkPropertyMetadata(false, OnUpdateOnInputPropertyChanged));

        public static readonly DependencyProperty IsObligatoryProperty = DependencyProperty.Register("IsObligatory", typeof(bool), typeof(IntegerBox));

        public static readonly DependencyProperty DefaultValueIfEmptyProperty = DependencyProperty.Register("DefaultValueIfEmpty", typeof(int), typeof(IntegerBox), 
            new FrameworkPropertyMetadata(0));

        #endregion

        #region Property Accessor

        [Bindable(true), Category("Common")]
        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// The Increment/Decrement value.
        /// </summary>
        [Description("The Increment/Decrement value.")]
        public int StepValue
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public int Offset
        {
            get { return (int)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public bool UpdateOnInput
        {
            get { return (bool)GetValue(UpdateOnInputProperty); }
            set { SetValue(UpdateOnInputProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public bool IsObligatory
        {
            get { return (bool)GetValue(IsObligatoryProperty); }
            set { SetValue(IsObligatoryProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public int DefaultValueIfEmpty
        {
            get { return (int)GetValue(DefaultValueIfEmptyProperty); }
            set { SetValue(DefaultValueIfEmptyProperty, value); }
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
            var intBox = d as IntegerBox;

            if (intBox == null) return;

            if (intBox.Value + intBox.Offset > intBox.Maximum)
                intBox.Value = intBox.Maximum + intBox.Offset;

            if (intBox.Value + intBox.Offset < intBox.Minimum)
                intBox.Value = intBox.Minimum + intBox.Offset;

            if (!string.Equals(intBox.Text, (intBox.Value - intBox.Offset).ToString()))
                intBox.Text = (intBox.Value - intBox.Offset).ToString();

            intBox.RaiseValueChangedEvent();
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
            var intBox = d as IntegerBox;

            if (intBox == null) return;

            //The offset value dictates the value being displayed.
            //For example, The value 600 and the Offset 20 should display the text 580.
            //Text = Value - Offset.

            intBox.Text = (intBox.Value - intBox.Offset).ToString();
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
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
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

            Text = (Value - Offset).ToString();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            SelectAll();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //Only sets the focus if not clicking on the Up/Down buttons of a IntegerUpDown.
            if (!IsKeyboardFocusWithin && !(e.OriginalSource is Border))
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
            //Value = Text + Offset.

            Value = Convert.ToInt32(Text, CultureInfo.CurrentCulture) + Offset;

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

                Value = Convert.ToInt32(Text, CultureInfo.CurrentCulture) + Offset;
                return;
            }

            //The offset value dictates the value being displayed.
            //For example, The value 600 and the Offset 20 should display the text 580.
            //Text = Value - Offset.

            Text = (Value - Offset).ToString();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            var step = Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Control)
                ? 50 : Keyboard.Modifiers == ModifierKeys.Shift
                    ? 10 : Keyboard.Modifiers == ModifierKeys.Control
                        ? 5 : StepValue;

            if (e.Delta > 0)
                Value += step;
            else
                Value -= step;

            e.Handled = true;
        }

        #endregion

        #region Base Properties Changed

        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = e.DataObject.GetData(typeof(string)) as string;

                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
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
            //Digits, points or commas.
            var regex = new Regex(@"^[0-9]$");

            //Checks if it's a valid char based on the context.
            return regex.IsMatch(text);
        }

        private bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, @"^(?:\d{1,9})?$");
        }

        #endregion
    }
}
