using System;
using System.Windows.Input;

namespace ScreenToGif.Util.ActivityHook
{
    /// <summary>
    /// Custom Mouse Event Args
    /// </summary>
    public class CustomMouseEventArgs : EventArgs
    {
        /// <summary>
        /// The MouseButton being pressed.
        /// </summary>
        public MouseButton Button { get; private set; }

        /// <summary>
        /// Click counter.
        /// </summary>
        public int Clicks { get; private set; }

        /// <summary>
        /// X Axis position
        /// </summary>
        public int PosX { get; private set; }

        /// <summary>
        /// Y Axis position.
        /// </summary>
        public int PosY { get; private set; }

        /// <summary>
        /// Up or down scroll flow.
        /// </summary>
        public int Delta { get; private set; }

        /// <summary>
        /// State of the mouse button.
        /// </summary>
        public MouseButtonState State{ get; private set; }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="button">The MouseButton being pressed.</param>
        /// <param name="clicks">Click counter.</param>
        /// <param name="x">X Axis position.</param>
        /// <param name="y">Y Axis position</param>
        /// <param name="delta">Up or down scroll flow.</param>
        /// <param name="isUp">True if it's a mouse up event</param>
        public CustomMouseEventArgs(MouseButton button, int clicks, int x, int y, int delta, MouseButtonState state = MouseButtonState.Pressed)
        {
            Button = button;
            Clicks = clicks;
            PosX = x;
            PosY = y;
            Delta = delta;
            State = state;
        }
    }
}
