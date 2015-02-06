using System;
using System.Windows.Input;

namespace HookTest.Util
{
    public class CustomMouseEventArgs : EventArgs
    {
        public MouseButton Button { get; private set; }

        public int Clicks { get; private set; }

        public int PosX { get; private set; }

        public int PosY { get; private set; }

        public int Delta { get; private set; }

        public CustomMouseEventArgs(MouseButton button, int clicks, int x, int y, int delta)
        {
            Button = button;
            Clicks = clicks;
            PosX = x;
            PosY = y;
            Delta = delta;
        }
    }
}
