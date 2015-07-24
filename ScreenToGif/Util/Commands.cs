using System.Windows.Input;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Custom Commands.
    /// </summary>
    public static class Commands
    {
        #region Reset

        private static RoutedUICommand _reset = new RoutedUICommand ("Reset", "Reset", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.R, ModifierKeys.Control) });

        public static RoutedUICommand Reset
        {
            get { return _reset; }
            set { _reset = value; }
        }

        #endregion

        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Alt) });
    }
}
