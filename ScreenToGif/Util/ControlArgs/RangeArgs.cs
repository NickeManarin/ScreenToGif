using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Controls;

namespace ScreenToGif.Util.ControlArgs
{
    /// <summary>
    /// Event arguments created for the RangeSlider's SelectionChanged event.
    /// <see cref="RangeSlider"/>
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
        public Double OldValue { get; private set; }
        public Double NewValue { get; private set; }

        internal RangeParameterChangedEventArgs(RangeParameterChangeType type, Double _old, Double _new)
        {
            ParameterType = type;

            OldValue = _old;
            NewValue = _new;
        }
    }

    public enum RangeParameterChangeType
    {
        Lower = 1,
        Upper = 2
    }
}
