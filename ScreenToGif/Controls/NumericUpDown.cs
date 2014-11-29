using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls
{
    public class NumericUpDown : Control
    {
        #region Variables

        private RepeatButton _upButton;
        private RepeatButton _downButton;
        private TextBox _textBox;

        public readonly static DependencyProperty MaximumProperty;
        public readonly static DependencyProperty MinimumProperty;
        public readonly static DependencyProperty ValueProperty;
        public readonly static DependencyProperty StepProperty;

        #endregion

        #region Properties

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set
            {
                //Change de actual value to the range.
                if (value < Value)
                {
                    Value = value;
                }

                SetValue(MaximumProperty, value);
            }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set
            {
                //Change de actual value to the range.
                if (value > Value)
                {
                    Value = value;
                }
                
                SetValue(MinimumProperty, value);
            }
        }

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetCurrentValue(ValueProperty, value); }
        }

        public int StepValue
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        #endregion

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));

            MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown), new UIPropertyMetadata(10));
            MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDown), new UIPropertyMetadata(0));
            StepProperty = DependencyProperty.Register("StepValue", typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(5));
            ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(0));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _upButton = Template.FindName("Part_UpButton", this) as RepeatButton;
            _downButton = Template.FindName("Part_DownButton", this) as RepeatButton;
            _textBox = Template.FindName("InternalBox", this) as TextBox;

            Value = Minimum;

            _textBox.Text = Minimum.ToString();
            _upButton.Click += _UpButton_Click;
            _downButton.Click += _DownButton_Click;
        }

        #region Events

        private void _DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value -= StepValue;
                if (Value < Minimum)
                    Value = Minimum;

                _textBox.Text = Value.ToString();
            }
        }

        private void _UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value += StepValue;
                if (Value > Maximum)
                    Value = Maximum;

                _textBox.Text = Value.ToString();
            }
        }

        #endregion
    }
}
