using System;

namespace ScreenToGif.Util
{
    public static class MathHelper
    {
        /// <summary>
        /// Gets the third value based on the other 2 parameters.
        /// Total       =   100 %
        /// Variable    =   percentage
        /// </summary>
        /// <returns>The value that was not filled.</returns>
        public static double CrossMultiplication(double? total, double? variable, double? percentage)
        {
            #region Validation

            //Only one of the parameters can bee null.
            var amount = (total.HasValue ? 0 : 1) + (variable.HasValue ? 0 : 1) + (percentage.HasValue ? 0 : 1);

            if (amount != 1)
                throw new ArgumentException("Only one of the parameters can bee null");

            #endregion

            if (!total.HasValue && percentage.HasValue && variable.HasValue)
                return (percentage.Value * 100d) / variable.Value;

            if (!percentage.HasValue && total.HasValue && variable.HasValue)
                return total > 0 || total < 0 ? (variable.Value * 100d) / total.Value : 0;

            if (!variable.HasValue && total.HasValue && percentage.HasValue)
                return (percentage.Value * total.Value) / 100d;

            return 0;
        }

        public static bool NearlyEquals(this float a, float b, float epsilon = 0.0001F)
        {
            var absA = Math.Abs(a);
            var absB = Math.Abs(b);
            var diff = Math.Abs(a - b);

            if (a == b)
                return true;

            if (a == 0 || b == 0 || diff < float.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < epsilon;
            }

            // use relative error
            return diff / (absA + absB) < epsilon;
        }

        public static bool NearlyEquals(this double a, double b, double epsilon = 0.0001D)
        {
            var absA = Math.Abs(a);
            var absB = Math.Abs(b);
            var diff = Math.Abs(a - b);

            if (a == b)
            { // shortcut, handles infinities
                return true;
            }

            if (a == 0 || b == 0 || diff < double.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < epsilon;
            }

            // use relative error
            return diff / (absA + absB) < epsilon;
        }

        public static bool NearlyEquals(this double a, int absB, double epsilon = 0.0001D)
        {
            var absA = Math.Abs(a);
            var diff = Math.Abs(a - absB);

            if (a == absB)
            { // shortcut, handles infinities
                return true;
            }

            if (a == 0 || absB == 0 || diff < double.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < epsilon;
            }

            // use relative error
            return diff / (absA + absB) < epsilon;
        }

        public static bool NearlyEquals(this double? value1, double? value2, double unimportantDifference = 0.0001)
        {
            if (value1 != value2)
            {
                if (value1 == null || value2 == null)
                    return false;

                return Math.Abs(value1.Value - value2.Value) < unimportantDifference;
            }

            return true;
        }

        /// <summary>
        /// Forces an integer to be between two values.
        /// </summary>
        public static int Clamp(this int value, int min, int max)
        {
            return (value <= min) ? min : (value >= max) ? max : value;
        }

        /// <summary>
        /// Forces a double to be between two values.
        /// </summary>
        public static double Clamp(this double value, double min, double max)
        {
            return (value <= min) ? min : (value >= max) ? max : value;
        }
    }
}