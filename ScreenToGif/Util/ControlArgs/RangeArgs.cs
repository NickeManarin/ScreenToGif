using System.Windows;

namespace ScreenToGif.Util.ControlArgs
{
    /// <summary>
    /// Event arguments created for the RangeSlider's SelectionChanged event.
    /// </summary>
    public class RangeSelectionChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The value of the new range's beginning.
        /// </summary>
        public double NewLowerValue { get; set; }

        /// <summary>
        /// The value of the new range's ending.
        /// </summary>
        public double NewUpperValue { get; set; }

        public double OldLowerValue { get; set; }

        public double OldUpperValue { get; set; }

        internal RangeSelectionChangedEventArgs(double newLowerValue, double newUpperValue, double oldLowerValue, double oldUpperValue)
        {
            NewLowerValue = newLowerValue;
            NewUpperValue = newUpperValue;
            OldLowerValue = oldLowerValue;
            OldUpperValue = oldUpperValue;
        }
    }

    public class RangeParameterChangedEventArgs : RoutedEventArgs
    {
        public RangeParameterChangeType ParameterType { get; private set; }
        public double OldValue { get; private set; }
        public double NewValue { get; private set; }

        internal RangeParameterChangedEventArgs(RangeParameterChangeType type, double old, double _new)
        {
            ParameterType = type;

            OldValue = old;
            NewValue = _new;
        }
    }

    public enum RangeParameterChangeType
    {
        Lower = 1,
        Upper = 2
    }
}
