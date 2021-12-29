namespace ScreenToGif.Domain.Events
{
    /// <summary>
    /// Custom KeyPress Event Args
    /// </summary>
    public class CustomKeyPressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the character corresponding to the key pressed.
        /// </summary>
        /// <returns>
        /// The ASCII character that is composed. For example, if the user presses SHIFT + K, 
        /// this property returns an uppercase K.
        /// </returns>
        public char KeyChar { get; private set; }

        public bool Handled { get; private set; }

        public CustomKeyPressEventArgs(char keyChar)
        {
            KeyChar = keyChar;
        }
    }
}