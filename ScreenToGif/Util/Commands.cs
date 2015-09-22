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

        #region Edit Tab

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
            new InputGestureCollection() { new KeyGesture(Key.I, ModifierKeys.Control, "Ctrl + I") });

        /// <summary>
        /// Reverse Command, Ctrl + I
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

        #endregion

        #region View Tab

        #region FirstFrame

        private static RoutedUICommand _firstFrame = new RoutedUICommand("FirstFrame", "FirstFrame", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.Home, ModifierKeys.None,"Home") });

        /// <summary>
        /// FirstFrame Command, Home
        /// </summary>
        public static RoutedUICommand FirstFrame
        {
            get { return _firstFrame; }
            set { _firstFrame = value; }
        }

        #endregion

        #region PreviousFrame

        private static RoutedUICommand _previousFrame = new RoutedUICommand("PreviousFrame", "PreviousFrame", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.PageUp, ModifierKeys.None, "PageUp") });

        /// <summary>
        /// PreviousFrame Command, PageUp
        /// </summary>
        public static RoutedUICommand PreviousFrame
        {
            get { return _previousFrame; }
            set { _previousFrame = value; }
        }

        #endregion

        #region Play

        private static RoutedUICommand _play = new RoutedUICommand("Play", "Play", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.P, ModifierKeys.Alt, "Alt + P") });

        /// <summary>
        /// Play Command, Alt + P
        /// </summary>
        public static RoutedUICommand Play
        {
            get { return _play; }
            set { _play = value; }
        }

        #endregion

        #region NextFrame

        private static RoutedUICommand _nextFrame = new RoutedUICommand("NextFrame", "NextFrame", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.PageDown, ModifierKeys.None, "PageDown") });

        /// <summary>
        /// NextFrame Command, PageDown
        /// </summary>
        public static RoutedUICommand NextFrame
        {
            get { return _nextFrame; }
            set { _nextFrame = value; }
        }

        #endregion

        #region LastFrame

        private static RoutedUICommand _lastFrame = new RoutedUICommand("LastFrame", "LastFrame", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.End, ModifierKeys.None, "End") });

        /// <summary>
        /// LastFrame Command, End
        /// </summary>
        public static RoutedUICommand LastFrame
        {
            get { return _lastFrame; }
            set { _lastFrame = value; }
        }

        #endregion

        #region Zoom100

        private static RoutedUICommand _zoom100 = new RoutedUICommand("Zoom100", "Zoom100", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.D0, ModifierKeys.Alt, "Alt + 0") });

        /// <summary>
        /// Zoom100 Command, Alt + 0
        /// </summary>
        public static RoutedUICommand Zoom100
        {
            get { return _zoom100; }
            set { _zoom100 = value; }
        }

        #endregion

        #region FitImage

        private static RoutedUICommand _fitImage = new RoutedUICommand("FitImage", "FitImage", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.OemMinus, ModifierKeys.Alt, "Alt + -") });

        /// <summary>
        /// FitImage Command, Alt + -
        /// </summary>
        public static RoutedUICommand FitImage
        {
            get { return _fitImage; }
            set { _fitImage = value; }
        }

        #endregion

        #endregion

        #region Image Tab

        #region Resize

        private static RoutedUICommand _resize = new RoutedUICommand("Resize", "Resize", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.R, ModifierKeys.Alt, "Alt + R") });

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
            new InputGestureCollection() { new KeyGesture(Key.C, ModifierKeys.Alt, "Alt + C") });

        /// <summary>
        /// Crop Command, Alt + C
        /// </summary>
        public static RoutedUICommand Crop
        {
            get { return _crop; }
            set { _crop = value; }
        }

        #endregion

        #region Flip/Rotate

        private static RoutedUICommand _flipRotate = new RoutedUICommand("FlipRotate", "FlipRotate", typeof(Commands));

        /// <summary>
        /// Flip/Rotate Command, No Input
        /// </summary>
        public static RoutedUICommand FlipRotate
        {
            get { return _flipRotate; }
            set { _flipRotate = value; }
        }

        #region FlipVertical

        //private static RoutedUICommand _flipVertical = new RoutedUICommand("FlipVertical", "FlipVertical", typeof(Commands));

        ///// <summary>
        ///// FlipVertical Command, No Input
        ///// </summary>
        //public static RoutedUICommand FlipVertical
        //{
        //    get { return _flipVertical; }
        //    set { _flipVertical = value; }
        //}

        #endregion

        #region FlipHorizontal

        //private static RoutedUICommand _flipHorizontal = new RoutedUICommand("FlipHorizontal", "FlipHorizontal", typeof(Commands));

        ///// <summary>
        ///// FlipHorizontal Command, No Input
        ///// </summary>
        //public static RoutedUICommand FlipHorizontal
        //{
        //    get { return _flipHorizontal; }
        //    set { _flipHorizontal = value; }
        //}

        #endregion

        #region RotateLeft90

        //private static RoutedUICommand _rotateLeft90 = new RoutedUICommand("RotateLeft90", "RotateLeft90", typeof(Commands));

        ///// <summary>
        ///// RotateLeft90 Command, No Input
        ///// </summary>
        //public static RoutedUICommand RotateLeft90
        //{
        //    get { return _rotateLeft90; }
        //    set { _rotateLeft90 = value; }
        //}

        #endregion

        #region RotateRight90

        //private static RoutedUICommand _rotateRight90 = new RoutedUICommand("RotateRight90", "RotateRight90", typeof(Commands));

        ///// <summary>
        ///// RotateRight90 Command, No Input
        ///// </summary>
        //public static RoutedUICommand RotateRight90
        //{
        //    get { return _rotateRight90; }
        //    set { _rotateRight90 = value; }
        //}

        #endregion

        #endregion

        #region Filter

        private static RoutedUICommand _filter = new RoutedUICommand("Filter", "Filter", typeof(Commands));

        /// <summary>
        /// Filter Command, No Input
        /// </summary>
        public static RoutedUICommand Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        #endregion

        #region Caption

        private static RoutedUICommand _caption = new RoutedUICommand("Caption", "Caption", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Alt, "Alt + S") });

        /// <summary>
        /// Caption Command, Alt + S
        /// </summary>
        public static RoutedUICommand Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        #endregion

        #region Free Text

        private static RoutedUICommand _freeText = new RoutedUICommand("FreeText", "FreeText", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.F, ModifierKeys.Alt, "Alt + F") });

        /// <summary>
        /// Free Text Command, Alt + F
        /// </summary>
        public static RoutedUICommand FreeText
        {
            get { return _freeText; }
            set { _freeText = value; }
        }

        #endregion

        #region Title Frame

        private static RoutedUICommand _titleFrame = new RoutedUICommand("TitleFrame", "TitleFrame", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.T, ModifierKeys.Alt, "Alt + T") });

        /// <summary>
        /// Title Frame Command, Alt + T
        /// </summary>
        public static RoutedUICommand TitleFrame
        {
            get { return _titleFrame; }
            set { _titleFrame = value; }
        }

        #endregion

        #region Free Drawing

        private static RoutedUICommand _freeDrawing = new RoutedUICommand("FreeDrawing", "FreeDrawing", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.D, ModifierKeys.Alt, "Alt + D") });

        /// <summary>
        /// Free Drawing Command, Alt + D
        /// </summary>
        public static RoutedUICommand FreeDrawing
        {
            get { return _freeDrawing; }
            set { _freeDrawing = value; }
        }

        #endregion

        #region Watermark

        private static RoutedUICommand _watermark = new RoutedUICommand("Watermark", "Watermark", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.W, ModifierKeys.Alt, "Alt + W") });

        /// <summary>
        /// Watermark Command, Alt + W
        /// </summary>
        public static RoutedUICommand Watermark
        {
            get { return _watermark; }
            set { _watermark = value; }
        }

        #endregion

        #region Border

        private static RoutedUICommand _border = new RoutedUICommand("Border", "Border", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.B, ModifierKeys.Alt, "Alt + B") });

        /// <summary>
        /// Border Command, Alt + B
        /// </summary>
        public static RoutedUICommand Border
        {
            get { return _border; }
            set { _border = value; }
        }

        #endregion

        #endregion

        #region Options Tab

        #region Options

        private static RoutedUICommand _options = new RoutedUICommand("Options", "Options", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + O") });

        /// <summary>
        /// Options Command, Ctrl + Alt + O
        /// </summary>
        public static RoutedUICommand Options
        {
            get { return _options; }
            set { _options = value; }
        }

        #endregion

        #endregion

        #region Other

        #region Check for Videos Devices

        private static RoutedUICommand _checkVideoDevices = new RoutedUICommand("CheckVideoDevices", "CheckVideoDevices", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.F5, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + F5") });

        /// <summary>
        /// Check Video Devices Command, Ctrl + Alt + F5
        /// </summary>
        public static RoutedUICommand CheckVideoDevices
        {
            get { return _checkVideoDevices; }
            set { _checkVideoDevices = value; }
        }

        #endregion

        #region Open the Board

        private static RoutedUICommand _board = new RoutedUICommand("Board", "Board", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.B, ModifierKeys.Control, "Ctrl + B") });

        /// <summary>
        /// Board Command, Ctrl + B
        /// </summary>
        public static RoutedUICommand Board
        {
            get { return _board; }
            set { _board = value; }
        }

        #endregion

        #region Open Editor

        private static RoutedUICommand _openEditor = new RoutedUICommand("OpenEditor", "OpenEditor", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.E, ModifierKeys.Control, "Ctrl + E") });

        /// <summary>
        /// Open the Editor Command, Ctrl + E
        /// </summary>
        public static RoutedUICommand OpenEditor
        {
            get { return _openEditor; }
            set { _openEditor = value; }
        }

        #endregion

        #region Enable Snapshot

        private static RoutedUICommand _enableSnapshot = new RoutedUICommand("EnableSnapshot", "EnableSnapshot", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + S") });

        /// <summary>
        /// Enable/Disable Snapshot Command, "Ctrl + Alt + S"
        /// </summary>
        public static RoutedUICommand EnableSnapshot
        {
            get { return _enableSnapshot; }
            set { _enableSnapshot = value; }
        }

        #endregion

        #region Enable Snap to Window

        private static RoutedUICommand _enableSnapToWindow = new RoutedUICommand("EnableSnapToWindow", "EnableSnapToWindow", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + Z") });

        /// <summary>
        /// Enable/Disable Snap to Window Command, "Ctrl + Alt + Z"
        /// </summary>
        public static RoutedUICommand EnableSnapToWindow
        {
            get { return _enableSnapToWindow; }
            set { _enableSnapToWindow = value; }
        }

        #endregion

        #endregion

        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(Commands),
            new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Alt) });
    }
}
