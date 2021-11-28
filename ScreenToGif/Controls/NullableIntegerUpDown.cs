using System.Windows;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls;

/// <summary>
/// Integer only control with up and down buttons to change the value.
/// </summary>
public class NullableIntegerUpDown : NullableIntegerBox
{
    #region Variables

    private RepeatButton _upButton;
    private RepeatButton _downButton;

    #endregion

    static NullableIntegerUpDown()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NullableIntegerUpDown), new FrameworkPropertyMetadata(typeof(NullableIntegerUpDown)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _upButton = Template.FindName("UpButton", this) as RepeatButton;
        _downButton = Template.FindName("DownButton", this) as RepeatButton;

        if (_upButton != null)
            _upButton.Click += UpButton_Click;

        if (_downButton != null)
            _downButton.Click += DownButton_Click;
    }

    #region Event Handlers

    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        if (!Value.HasValue)
        {
            Value = Minimum;
            return;
        }

        if (Value > Minimum)
            Value -= StepValue;
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (!Value.HasValue)
        {
            Value = Minimum;
            return;
        }

        if (Value < Maximum)
            Value += StepValue;
    }

    #endregion
}