using System;
using System.Windows.Input;

namespace ScreenToGif.Util.ActivityHook
{
    /// <summary>
    /// Custom Key Event Args
    /// </summary>
    public class CustomKeyEventArgs : EventArgs
    {
        public Key Key { get; private set; }
     
        public bool Handled { get; private set; }

        public CustomKeyEventArgs(Key key)
        {
            Key = key;
        }
    }
}
