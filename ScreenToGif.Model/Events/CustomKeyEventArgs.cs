using System.Windows.Input;

namespace ScreenToGif.Domain.Events
{
    /// <summary>
    /// Custom Key Event Args.
    /// </summary>
    public class CustomKeyEventArgs : EventArgs
    {
        public Key Key { get; }

        public bool IsUppercase { get; }

        public bool IsInjected { get; }

        public bool Handled { get; private set; }

        public CustomKeyEventArgs(Key key, bool isUppercase = false, bool isInjected = false)
        {
            Key = key;
            IsUppercase = isUppercase;
            IsInjected = isInjected;
        }
    }
}