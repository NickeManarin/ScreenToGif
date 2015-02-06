using System;

namespace ScreenToGif.Util.ActivityHook
{
    /// <summary>
    /// Custom Key Event Args
    /// </summary>
    public class CustomKeyEventArgs : EventArgs
    {
        public Keys Key { get; private set; }

        public bool Handled { get; private set; }

        public CustomKeyEventArgs(Keys key)
        {
            Key = key;
        }
    }
}
