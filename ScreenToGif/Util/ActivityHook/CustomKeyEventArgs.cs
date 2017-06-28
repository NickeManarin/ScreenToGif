using System;
using System.Windows.Input;

namespace ScreenToGif.Util.ActivityHook
{
    /// <summary>
    /// Custom Key Event Args.
    /// </summary>
    public class CustomKeyEventArgs : EventArgs
    {
        public Key Key { get; }

        public bool IsUppercase { get; }

        public bool Handled { get; private set; }

        public CustomKeyEventArgs(Key key, bool isUppercase = false)
        {
            Key = key;
            IsUppercase = isUppercase;
        }
    }
}
