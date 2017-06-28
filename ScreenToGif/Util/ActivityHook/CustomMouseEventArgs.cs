using System;
using System.Windows.Input;

namespace ScreenToGif.Util.ActivityHook
{
    /// <summary>
    /// Custom Mouse Event Args.
    /// </summary>
    public class CustomMouseEventArgs : EventArgs
    {
        /// <summary>
        /// X Axis position
        /// </summary>
        public int PosX { get; }

        /// <summary>
        /// Y Axis position.
        /// </summary>
        public int PosY { get; }

        /// <summary>
        /// The type of the mouse event.
        /// </summary>
        public UserActivityHook.MouseEventType EventType { get; }

        /// <summary>
        /// State of the left mouse button.
        /// </summary>
        public MouseButtonState LeftButton { get; }

        /// <summary>
        /// State of the right mouse button.
        /// </summary>
        public MouseButtonState RightButton { get; }

        /// <summary>
        /// State of the middle mouse button.
        /// </summary>
        public MouseButtonState MiddleButton { get; }

        /// <summary>
        /// The state of the scroll wheel. Up or down scroll flow.
        /// </summary>
        public short MouseDelta { get; }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="x">X Axis position.</param>
        /// <param name="y">Y Axis position.</param>
        /// <param name="eventType">The type of mouse event.</param>
        /// <param name="leftButton">The state of the left mouse button.</param>
        /// <param name="rightButton">The state of the right mouse button.</param>
        /// <param name="middleButton">The state of the middle mouse button.</param>
        /// <param name="mouseDelta">The state scroll wheel.</param>
        public CustomMouseEventArgs(int x, int y, UserActivityHook.MouseEventType eventType, MouseButtonState leftButton, MouseButtonState rightButton, MouseButtonState middleButton = MouseButtonState.Released, short mouseDelta = 0)
        {
            PosX = x;
            PosY = y;
            EventType = eventType;
            LeftButton = leftButton;
            RightButton = rightButton;
            MiddleButton = middleButton;
            MouseDelta = mouseDelta;
        }
    }
}