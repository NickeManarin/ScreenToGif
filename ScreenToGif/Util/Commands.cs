using System.Windows.Input;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Custom Commands.
    /// </summary>
    public static class Commands
    {
        #region File Tab

        #region New Recording

        private static RoutedUICommand _newRecording = new RoutedUICommand("NewRecording", "NewRecording", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl + N") });

        /// <summary>
        /// New Recording Command, Ctrl + N
        /// </summary>
        public static RoutedUICommand NewRecording
        {
            get { return _newRecording; }
            set { _newRecording = value; }
        }

        #endregion

        #region New Webcam Recording

        private static RoutedUICommand _newWebcamRecording = new RoutedUICommand("NewWebcamRecording", "NewWebcamRecording", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.W, ModifierKeys.Control, "Ctrl + W") });

        /// <summary>
        /// New Webcam Recording Command, Ctrl + W
        /// </summary>
        public static RoutedUICommand NewWebcamRecording
        {
            get { return _newWebcamRecording; }
            set { _newWebcamRecording = value; }
        }

        #endregion

        #region New Empty Animation

        private static RoutedUICommand _newAnimation = new RoutedUICommand("NewAnimation", "NewAnimation", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.A, ModifierKeys.Control, "Ctrl + A") });

        /// <summary>
        /// New Animation Command, Ctrl + A
        /// </summary>
        public static RoutedUICommand NewAnimation
        {
            get { return _newAnimation; }
            set { _newAnimation = value; }
        }

        #endregion

        #region New From Media/Project

        private static RoutedUICommand _newFromMediaProject = new RoutedUICommand("NewFromMediaProject", "NewFromMediaProject", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl + O") });

        /// <summary>
        /// New From Media/Project Command, Ctrl + O
        /// </summary>
        public static RoutedUICommand NewFromMediaProject
        {
            get { return _newFromMediaProject; }
            set { _newFromMediaProject = value; }
        }

        #endregion

        #region Insert Recording

        private static RoutedUICommand _insertRecording = new RoutedUICommand("InsertRecording", "InsertRecording", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + N") });

        /// <summary>
        /// Insert Recording Command, Ctrl + Shift + N
        /// </summary>
        public static RoutedUICommand InsertRecording
        {
            get { return _insertRecording; }
            set { _insertRecording = value; }
        }

        #endregion

        #region Insert Webcam Recording

        private static RoutedUICommand _insertWebcamRecording = new RoutedUICommand("InsertWebcamRecording", "InsertWebcamRecording", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + W") });

        /// <summary>
        /// Insert Webcam Recording Command, Ctrl + Shift + W
        /// </summary>
        public static RoutedUICommand InsertWebcamRecording
        {
            get { return _insertWebcamRecording; }
            set { _insertWebcamRecording = value; }
        }

        #endregion

        #region Insert From Media

        private static RoutedUICommand _insertFromMedia = new RoutedUICommand("InsertFromMedia", "InsertFromMedia", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + O") });

        /// <summary>
        /// Insert From Media Command, Ctrl + Shift + O
        /// </summary>
        public static RoutedUICommand InsertFromMedia
        {
            get { return _insertFromMedia; }
            set { _insertFromMedia = value; }
        }

        #endregion

        #region Save as Gif

        private static RoutedUICommand _saveAsGif = new RoutedUICommand("SaveAsGif", "SaveAsGif", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl + S") });

        /// <summary>
        /// Save as Gif Command, Ctrl + S
        /// </summary>
        public static RoutedUICommand SaveAsGif
        {
            get { return _saveAsGif; }
            set { _saveAsGif = value; }
        }

        #endregion

        #region Save as Video

        private static RoutedUICommand _saveAsVideo = new RoutedUICommand("SaveAsVideo", "SaveAsVideo", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + S") });

        /// <summary>
        /// Save as Video Command, Ctrl + Shift + S
        /// </summary>
        public static RoutedUICommand SaveAsVideo
        {
            get { return _saveAsVideo; }
            set { _saveAsVideo = value; }
        }

        #endregion

        #region Save as Project

        private static RoutedUICommand _saveAsProject = new RoutedUICommand("SaveAsProject", "SaveAsProject", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + S") });

        /// <summary>
        /// Save as Project Command, Ctrl + Alt + S
        /// </summary>
        public static RoutedUICommand SaveAsProject
        {
            get { return _saveAsProject; }
            set { _saveAsProject = value; }
        }

        #endregion

        #region Discart Project

        private static RoutedUICommand _discardProject = new RoutedUICommand("DiscardProject", "DiscardProject", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.Delete, ModifierKeys.Control, "Ctrl + Delete") });

        /// <summary>
        /// Discart Project Command, Ctrl + Delete
        /// </summary>
        public static RoutedUICommand DiscardProject
        {
            get { return _discardProject; }
            set { _discardProject = value; }
        }

        #endregion

        #endregion

        #region Reset

        private static RoutedUICommand _reset = new RoutedUICommand ("Reset", "Reset", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl + R") });

        /// <summary>
        /// Reset Command, Ctrl + R
        /// </summary>
        public static RoutedUICommand Reset
        {
            get { return _reset; }
            set { _reset = value; }
        }

        #endregion

        #region Reverse

        private static RoutedUICommand _reverse = new RoutedUICommand("Reverse", "Reverse", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.I, ModifierKeys.Control) });

        /// <summary>
        /// Yoyo Command, Ctrl + I
        /// </summary>
        public static RoutedUICommand Reverse
        {
            get { return _reverse; }
            set { _reverse = value; }
        }

        #endregion

        #region Yoyo

        private static RoutedUICommand _yoyo = new RoutedUICommand("Yoyo", "Yoyo", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.U, ModifierKeys.Control) });

        /// <summary>
        /// Yoyo Command, Ctrl + U
        /// </summary>
        public static RoutedUICommand Yoyo
        {
            get { return _yoyo; }
            set { _yoyo = value; }
        }

        #endregion

        #region Move Left

        private static RoutedUICommand _moveLeft = new RoutedUICommand("MoveLeft", "MoveLeft", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.Left, ModifierKeys.Control) });

        /// <summary>
        /// Move Left Command, Ctrl + Left
        /// </summary>
        public static RoutedUICommand MoveLeft
        {
            get { return _moveLeft; }
            set { _moveLeft = value; }
        }

        #endregion

        #region Move Right

        private static RoutedUICommand _moveRight = new RoutedUICommand("MoveRight", "MoveRight", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.Right, ModifierKeys.Control) });

        /// <summary>
        /// Move Right Command, Ctrl + Right
        /// </summary>
        public static RoutedUICommand MoveRight
        {
            get { return _moveRight; }
            set { _moveRight = value; }
        }

        #endregion

        #region Resize

        private static RoutedUICommand _resize = new RoutedUICommand("Resize", "Resize", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.R, ModifierKeys.Alt) });

        /// <summary>
        /// Resize Command, Alt + R
        /// </summary>
        public static RoutedUICommand Resize
        {
            get { return _resize; }
            set { _resize = value; }
        }

        #endregion

        #region Crop

        private static RoutedUICommand _crop = new RoutedUICommand("Crop", "Crop", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.C, ModifierKeys.Alt) });

        /// <summary>
        /// Crop Command, Alt + C
        /// </summary>
        public static RoutedUICommand Crop
        {
            get { return _crop; }
            set { _crop = value; }
        }

        #endregion

        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Alt) });
    }
}
