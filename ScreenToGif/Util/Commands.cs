using System.Windows.Input;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Custom Commands.
    /// </summary>
    public static class Commands
    {
        //TODO: Localize everything.

        #region File Tab

        /// <summary>
        /// New Recording Command, Ctrl + N
        /// </summary>
        public static RoutedUICommand NewRecording { get; set; } = new RoutedUICommand("New Recording", "NewRecording", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl + N") });

        /// <summary>
        /// New Webcam Recording Command, Ctrl + W
        /// </summary>
        public static RoutedUICommand NewWebcamRecording { get; set; } = new RoutedUICommand("New Webcam Recording", "NewWebcamRecording", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control, "Ctrl + W") });
        
        /// <summary>
        /// New Board Recording Command, Ctrl + B
        /// </summary>
        public static RoutedUICommand NewBoardRecording { get; set; } = new RoutedUICommand("New Board Recording", "Board", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.B, ModifierKeys.Control, "Ctrl + B") });

        #region New Empty Animation

        /// <summary>
        /// New Animation Command, Ctrl + Alt + A
        /// </summary>
        public static RoutedUICommand NewAnimation { get; set; } = new RoutedUICommand("New Animation", "NewAnimation", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + A") });

        #endregion

        #region Insert Recording

        /// <summary>
        /// Insert Recording Command, Ctrl + Shift + N
        /// </summary>
        public static RoutedUICommand InsertRecording { get; set; } = new RoutedUICommand("Insert Recording", "InsertRecording", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + N") });

        #endregion

        #region Insert Webcam Recording

        /// <summary>
        /// Insert Webcam Recording Command, Ctrl + Shift + W
        /// </summary>
        public static RoutedUICommand InsertWebcamRecording { get; set; } = new RoutedUICommand("Insert Webcam Recording", "InsertWebcamRecording", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + W") });

        #endregion

        #region Insert Board Recording

        /// <summary>
        /// Insert Board Recording Command, Ctrl + Shift + B
        /// </summary>
        public static RoutedUICommand InsertBoardRecording { get; set; } = new RoutedUICommand("Insert Board Recording", "InsertBoardRecording", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.B, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + B") });

        #endregion

        #region Insert From Media

        /// <summary>
        /// Insert From Media Command, Ctrl + Shift + O
        /// </summary>
        public static RoutedUICommand InsertFromMedia { get; set; } = new RoutedUICommand("Insert From Media", "InsertFromMedia", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + O") });

        #endregion

        #region Save as

        /// <summary>
        /// Save as Command, Ctrl + S
        /// </summary>
        public static RoutedUICommand SaveAs { get; set; } = new RoutedUICommand("Save As", "SaveAs", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl + S") });

        #endregion

        /// <summary>
        /// Load Command, Ctrl + O
        /// </summary>
        public static RoutedUICommand Load { get; set; } = new RoutedUICommand("Load", "Load", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl + O") });

        /// <summary>
        /// Load Recent Command, Ctrl + L
        /// </summary>
        public static RoutedUICommand LoadRecent { get; set; } = new RoutedUICommand("Load Recent", "LoadRecent", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.L, ModifierKeys.Control, "Ctrl + L") });

        #region Discart Project

        /// <summary>
        /// Discart Project Command, Ctrl + Delete
        /// </summary>
        public static RoutedUICommand DiscardProject { get; set; } = new RoutedUICommand("Discard Project", "DiscardProject", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Delete, ModifierKeys.Control, "Ctrl + Delete") });

        #endregion

        #endregion

        #region Home Tab

        #region Override Delay

        /// <summary>
        /// OverrideDelay Command, Alt + O
        /// </summary>
        public static RoutedUICommand OverrideDelay { get; set; } = new RoutedUICommand("Override Delay", "OverrideDelay", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Alt, "Alt + O") });

        #endregion

        #region Change Speed

        /// <summary>
        /// IncreaseDecreaseDelay Command, Alt + Y
        /// </summary>
        public static RoutedUICommand ChangeDelay { get; set; } = new RoutedUICommand("Change Delay", "IncreaseDecreaseDelay", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Y, ModifierKeys.Alt, "Alt + Y") });

        #endregion

        #region Scale Delay

         /// <summary>
         /// ScaleDelay Command, Alt + 5
         /// </summary>
         public static RoutedUICommand ScaleDelay { get; set; } = new RoutedUICommand("Scale Delay", "ScaleDelay", typeof(Commands),
               new InputGestureCollection { new KeyGesture(Key.D5, ModifierKeys.Alt, "Alt + 5") });

        #endregion

      #region Zoom100

      /// <summary>
      /// Zoom100 Command, Alt + 0
      /// </summary>
      public static RoutedUICommand Zoom100 { get; set; } = new RoutedUICommand("Set Zoom to 100%", "Zoom100", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.D0, ModifierKeys.Alt, "Alt + 0") });

        #endregion

        #region SizeToContent

        /// <summary>
        /// SizeToContent Command, Alt + 1
        /// </summary>
        public static RoutedUICommand SizeToContent { get; set; } = new RoutedUICommand("Size to Content", "SizeToContent", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.D1, ModifierKeys.Alt, "Alt + 1") });

        #endregion

        #region FitImage

        /// <summary>
        /// FitImage Command, Alt + -
        /// </summary>
        public static RoutedUICommand FitImage { get; set; } = new RoutedUICommand("Fit Image", "FitImage", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.OemMinus, ModifierKeys.Alt, "Alt + -") });

        #endregion

        #endregion

        #region Playback Tab

        #region FirstFrame

        /// <summary>
        /// FirstFrame Command, Home
        /// </summary>
        public static RoutedUICommand FirstFrame { get; set; } = new RoutedUICommand("Select First Frame", "FirstFrame", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Home, ModifierKeys.None, "Home") });

        #endregion

        #region PreviousFrame

        /// <summary>
        /// PreviousFrame Command, PageUp
        /// </summary>
        public static RoutedUICommand PreviousFrame { get; set; } = new RoutedUICommand("Select Previous Frame", "PreviousFrame", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.PageUp, ModifierKeys.None, "PageUp") });

        #endregion

        #region Play

        /// <summary>
        /// Play Command, Alt + P
        /// </summary>
        public static RoutedUICommand Play { get; set; } = new RoutedUICommand("Play", "Play", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Alt, "Alt + P"), new KeyGesture(Key.Space) });

        #endregion

        #region NextFrame

        /// <summary>
        /// NextFrame Command, PageDown
        /// </summary>
        public static RoutedUICommand NextFrame { get; set; } = new RoutedUICommand("Select Next Frame", "NextFrame", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.PageDown, ModifierKeys.None, "PageDown") });

        #endregion

        #region LastFrame

        /// <summary>
        /// LastFrame Command, End
        /// </summary>
        public static RoutedUICommand LastFrame { get; set; } = new RoutedUICommand("Select Last Frame", "LastFrame", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.End, ModifierKeys.None, "End") });

        #endregion

        #endregion

        #region Edit Tab

        #region Reset

        /// <summary>
        /// Reset Command, Ctrl + R
        /// </summary>
        public static RoutedUICommand Reset { get; set; } = new RoutedUICommand ("Reset", "Reset", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl + R") });

        #endregion

        #region Clipboard

        /// <summary>
        /// Explore Clipboard Command, No Input
        /// </summary>
        public static RoutedUICommand ExploreClipboard { get; set; } = new RoutedUICommand("Explore Clipboard Content", "ExploreClipboard", typeof(Commands));

        /// <summary>
        /// Remove Clipboard Entry Command, No Input
        /// </summary>
        public static RoutedUICommand RemoveClipboardEntry { get; set; } = new RoutedUICommand("Remove Clipboard Entry", "RemoveClipboardEntry", typeof(Commands));

        #endregion

        #region DeletePrevious

        /// <summary>
        /// DeletePrevious Command, Alt + Left
        /// </summary>
        public static RoutedUICommand DeletePrevious { get; set; } = new RoutedUICommand("Delete All Previous Frames", "DeletePrevious", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Left, ModifierKeys.Alt, "Alt + Left") });

        #endregion

        /// <summary>
        /// DeleteNext Command, Alt + Right
        /// </summary>
        public static RoutedUICommand DeleteNext { get; set; } = new RoutedUICommand("Delete All Next Frames", "DeleteNext", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Right, ModifierKeys.Alt, "Alt + Right") });

        /// <summary>
        /// Remove Duplicates Command, Alt + Delete
        /// </summary>
        public static RoutedUICommand RemoveDuplicates { get; set; } = new RoutedUICommand("Remove Duplicates", "RemoveDuplicates", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Delete, ModifierKeys.Alt, "Alt + Delete") });

        /// <summary>
        /// Reduce Command, Ctrl + Shift + Delete
        /// </summary>
        public static RoutedUICommand Reduce { get; set; } = new RoutedUICommand("Reduce Frame Count", "Reduce", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Delete, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + Delete") });

        #region Reverse

        /// <summary>
        /// Reverse Command, Ctrl + Alt + R
        /// </summary>
        public static RoutedUICommand Reverse { get; set; } = new RoutedUICommand("Reverse", "Reverse", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + R") });

        #endregion

        #region Yoyo

        /// <summary>
        /// Yoyo Command, Ctrl + Y
        /// </summary>
        public static RoutedUICommand Yoyo { get; set; } = new RoutedUICommand("Yoyo", "Yoyo", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Y, ModifierKeys.Control) });

        #endregion

        #region Move Left

        /// <summary>
        /// Move Left Command, Ctrl + Shift + Left
        /// </summary>
        public static RoutedUICommand MoveLeft { get; set; } = new RoutedUICommand("Move Left", "MoveLeft", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift) });

        #endregion

        #region Move Right

        /// <summary>
        /// Move Right Command, Ctrl + Shift + Right
        /// </summary>
        public static RoutedUICommand MoveRight { get; set; } = new RoutedUICommand("Move Right", "MoveRight", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift) });

        #endregion

        #endregion

        #region Image Tab

        #region Resize

        /// <summary>
        /// Resize Command, Alt + R
        /// </summary>
        public static RoutedUICommand Resize { get; set; } = new RoutedUICommand("Resize", "Resize", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Alt, "Alt + R") });

        #endregion

        #region Crop

        /// <summary>
        /// Crop Command, Alt + C
        /// </summary>
        public static RoutedUICommand Crop { get; set; } = new RoutedUICommand("Crop", "Crop", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Alt, "Alt + C") });

        #endregion

        #region Flip/Rotate

        /// <summary>
        /// Flip/Rotate Command, Alt + L
        /// </summary>
        public static RoutedUICommand FlipRotate { get; set; } = new RoutedUICommand("Flip or Rotate", "FlipRotate", typeof(Commands), 
            new InputGestureCollection {new KeyGesture(Key.L, ModifierKeys.Alt, "Alt + L")});

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

        /// <summary>
        /// Filter Command, No Input
        /// </summary>
        public static RoutedUICommand Filter { get; set; } = new RoutedUICommand("Filter", "Filter", typeof(Commands));

        #endregion

        #region Caption

        /// <summary>
        /// Caption Command, Alt + S
        /// </summary>
        public static RoutedUICommand Caption { get; set; } = new RoutedUICommand("Caption", "Caption", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Alt, "Alt + S") });

        #endregion

        #region Free Text

        /// <summary>
        /// Free Text Command, Alt + F
        /// </summary>
        public static RoutedUICommand FreeText { get; set; } = new RoutedUICommand("Free Text", "FreeText", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Alt, "Alt + F") });

        #endregion

        #region Title Frame

        /// <summary>
        /// Title Frame Command, Alt + T
        /// </summary>
        public static RoutedUICommand TitleFrame { get; set; } = new RoutedUICommand("Title Frame", "TitleFrame", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.T, ModifierKeys.Alt, "Alt + T") });

        #endregion

        #region Key Strokes

        /// <summary>
        /// Key Strokes Command, Alt + H
        /// </summary>
        public static RoutedUICommand KeyStrokes { get; set; } = new RoutedUICommand("Key Strokes", "KeyStrokes", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.H, ModifierKeys.Alt, "Alt + H") });

        #endregion

        #region Free Drawing

        /// <summary>
        /// Free Drawing Command, Alt + D
        /// </summary>
        public static RoutedUICommand FreeDrawing { get; set; } = new RoutedUICommand("Free Drawing", "FreeDrawing", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Alt, "Alt + D") });

        #endregion

        #region Shapes

        /// <summary>
        /// Shapes Command, Alt + J
        /// </summary>
        public static RoutedUICommand Shapes { get; set; } = new RoutedUICommand("Shapes", "Shapes", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.J, ModifierKeys.Alt, "Alt + J") });

        #endregion

        #region Mouse Clicks

        /// <summary>
        /// Mouse Clicks Command, Alt + I
        /// </summary>
        public static RoutedUICommand MouseClicks { get; set; } = new RoutedUICommand("Mouse Clicks", "MouseClicks", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.I, ModifierKeys.Alt, "Alt + I") });

        #endregion

        #region Watermark

        /// <summary>
        /// Watermark Command, Alt + W
        /// </summary>
        public static RoutedUICommand Watermark { get; set; } = new RoutedUICommand("Watermark", "Watermark", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Alt, "Alt + W") });

        #endregion

        #region Border

        /// <summary>
        /// Border Command, Alt + B
        /// </summary>
        public static RoutedUICommand Border { get; set; } = new RoutedUICommand("Border", "Border", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.B, ModifierKeys.Alt, "Alt + B") });

        #endregion

        #region Obfuscate

        /// <summary>
        /// Obfuscate Command, Ctrl + H
        /// </summary>
        public static RoutedUICommand Obfuscate { get; set; } = new RoutedUICommand("Obfuscate", "Obfuscate", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.H, ModifierKeys.Control, "Ctrl + H") });

        #endregion

        #region Cinemagraph

        /// <summary>
        /// Cinemagraph Command, Alt + G
        /// </summary>
        public static RoutedUICommand Cinemagraph { get; set; } = new RoutedUICommand("Cinemagraph", "Cinemagraph", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Alt, "Alt + G") });

        #endregion

        #region Progress

        /// <summary>
        /// Progress Command, Alt + K
        /// </summary>
        public static RoutedUICommand Progress { get; set; } = new RoutedUICommand("Progress", "Progress", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.K, ModifierKeys.Alt, "Alt + K") });

        #endregion

        #endregion

        #region Selection Tab

        #region Select All

        /// <summary>
        /// Select All Command, Ctrl + A
        /// </summary>
        public static RoutedUICommand SelectAll { get; set; } = new RoutedUICommand("Select All Frames", "SelectAll", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control, "Ctrl + A") });

        #endregion

        #region Go To

        /// <summary>
        /// Go To Command, Ctrl + G
        /// </summary>
        public static RoutedUICommand GoTo { get; set; } = new RoutedUICommand("Go To the Selected Frame", "GoTo", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control, "Ctrl + G") });

        #endregion

        #region Inverse Selection

        /// <summary>
        /// Inverse Selection Command, Ctrl + I
        /// </summary>
        public static RoutedUICommand InverseSelection { get; set; } = new RoutedUICommand("Inverse Selection of Frames", "InverseSelection", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.I, ModifierKeys.Control, "Ctrl + I") });

        #endregion

        #region Unselect

        /// <summary>
        /// Unselect Command, Ctrl + U
        /// </summary>
        public static RoutedUICommand Unselect { get; set; } = new RoutedUICommand("Unselect Frames", "Unselect", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.U, ModifierKeys.Control, "Ctrl + U") });

        #endregion

        #endregion

        #region Transitions

        /// <summary>
        /// Fade Command, Ctrl + Alt + T
        /// </summary>
        public static RoutedUICommand Fade { get; set; } = new RoutedUICommand("Fade Transition", "Fade ", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + T") });

        /// <summary>
        /// Slide Command, Ctrl + Alt + Y
        /// </summary>
        public static RoutedUICommand Slide { get; set; } = new RoutedUICommand("Slide Transition", "Slide ", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Y, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + Y") });

        #endregion

        #region Other

        /// <summary>
        /// Options Command, Ctrl + Alt + O
        /// </summary>
        public static RoutedUICommand Options { get; set; } = new RoutedUICommand("Options", "Options", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + O") });

        /// <summary>
        /// Update Command, Ctrl + Alt + U
        /// </summary>
        public static RoutedUICommand Update { get; set; } = new RoutedUICommand("Update", "Update", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + U") });

        /// <summary>
        /// Create Local Settings Command, Ctrl + Alt + C
        /// </summary>
        public static RoutedUICommand CreateLocalSettings { get; set; } = new RoutedUICommand("Create Local Settings", "CreateLocalSettings", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + C") });

        /// <summary>
        /// Remove Local Settings Command, Ctrl + Alt + D
        /// </summary>
        public static RoutedUICommand RemoveLocalSettings { get; set; } = new RoutedUICommand("Remove Local Settings", "RemoveLocalSettings", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + D") });

        /// <summary>
        /// Open Local Settings Command, Ctrl + Shift + A
        /// </summary>
        public static RoutedUICommand OpenLocalSettings { get; set; } = new RoutedUICommand("Open Local Settings", "OpenLocalSettings", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + A") });

        /// <summary>
        /// Remove AppData Settings Command, Ctrl + Alt + F
        /// </summary>
        public static RoutedUICommand RemoveAppDataSettings { get; set; } = new RoutedUICommand("Remove AppData Settings", "RemoveAppDataSettings", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + F") });

        /// <summary>
        /// Open AppData Settings Command, Ctrl + Alt + A
        /// </summary>
        public static RoutedUICommand OpenAppDataSettings { get; set; } = new RoutedUICommand("Open AppData Settings", "OpenAppDataSettings", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + A") });

        /// <summary>
        /// Clear all completed encodings, Ctrl + D
        /// </summary>
        public static RoutedUICommand ClearAll { get; set; } = new RoutedUICommand("Clear All", "ClearAll", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control, "Ctrl + D") });

        #region FramesListView

        /// <summary>
        /// Open Image Command, No Input.
        /// </summary>
        public static RoutedUICommand OpenImage { get; set; } = new RoutedUICommand("Open Image", "OpenImage", typeof(Commands));

        /// <summary>
        /// Explore Folder Command, No Input.
        /// </summary>
        public static RoutedUICommand ExploreFolder { get; set; } = new RoutedUICommand("Explore Folder", "ExploreFolder", typeof(Commands));

        /// <summary>
        /// Export Images Command, No Input.
        /// </summary>
        public static RoutedUICommand ExportImages { get; set; } = new RoutedUICommand("Export Selected Images", "ExportImages", typeof(Commands));

        #endregion

        #region Check for Videos Devices

        /// <summary>
        /// Check Video Devices Command, Ctrl + Alt + F5
        /// </summary>
        public static RoutedUICommand CheckVideoDevices { get; set; } = new RoutedUICommand("Check for Video Devices", "CheckVideoDevices", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.F5, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + F5") });

        #endregion

        #region Open Editor

        /// <summary>
        /// Editor Command, Ctrl + E
        /// </summary>
        public static RoutedUICommand Editor { get; set; } = new RoutedUICommand("Editor", "Editor", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Control, "Ctrl + E") });

        #endregion

        #region Enable Snapshot

        /// <summary>
        /// Enable/Disable Snapshot Command, No Input
        /// </summary>
        public static RoutedUICommand EnableSnapshot { get; set; } = new RoutedUICommand("Enable Snapshot", "EnableSnapshot", typeof(Commands));

        #endregion

        #region Enable Thin Mode

        /// <summary>
        /// Enable/Disable Thin mode Command, No Input
        /// </summary>
        public static RoutedUICommand EnableThinMode { get; set; } = new RoutedUICommand("Enable Thin Mode", "EnableThinMode", typeof(Commands));

        #endregion


        /// <summary>
        /// Enable/Disable Snap to Window Command, "Ctrl + Alt + Z"
        /// </summary>
        public static RoutedUICommand EnableSnapToWindow { get; set; } = new RoutedUICommand("Enable Snap To Window", "EnableSnapToWindow", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + Z") });

        /// <summary>
        /// Ok Action Command, Alt + E
        /// </summary>
        public static RoutedUICommand OkAction { get; set; } = new RoutedUICommand("Ok", "OkAction", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Alt, "Alt + E"), new KeyGesture(Key.Enter) });

        /// <summary>
        /// Cancel Action Command, Esc
        /// </summary>
        public static RoutedUICommand CancelAction { get; set; } = new RoutedUICommand("Cancel", "CancelAction", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Escape, ModifierKeys.None, "Esc") });

        #endregion

        #region Localization

        /// <summary>
        /// Move Up Entry Command, Alt + Up
        /// </summary>
        public static RoutedUICommand MoveUpLocalization { get; set; } = new RoutedUICommand("Move Upwards", "MoveUpLocalization", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Up, ModifierKeys.Alt, "Alt + Up") });

        /// <summary>
        /// Move Down Entry Command, Alt + Down
        /// </summary>
        public static RoutedUICommand MoveDownLocalization { get; set; } = new RoutedUICommand("Move Downwards", "MoveDownLocalization", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Down, ModifierKeys.Alt, "Alt + Down") });

        /// <summary>
        /// Add Entry Command, Ctrl + O
        /// </summary>
        public static RoutedUICommand AddLocalization { get; set; } = new RoutedUICommand("Add Localization", "AddLocalization", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl + O") });

        /// <summary>
        /// Save Entry Command, Ctrl + S
        /// </summary>
        public static RoutedUICommand SaveLocalization { get; set; } = new RoutedUICommand("Save Localization", "SaveLocalization", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl + S") });

        /// <summary>
        /// Remove Entry Command, Delete
        /// </summary>
        public static RoutedUICommand RemoveLocalization { get; set; } = new RoutedUICommand("Remove Localization", "RemoveLocalization", typeof(Commands), 
            new InputGestureCollection {new KeyGesture(Key.Delete, ModifierKeys.None, "Delete") });

        #endregion

        #region List of entries

        /// <summary>
        /// Move Up Entry Command, Alt + Up
        /// </summary>
        public static RoutedUICommand MoveUp { get; set; } = new RoutedUICommand("Move Upwards", "MoveUp", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Up, ModifierKeys.Alt, "Alt + Up") });

        /// <summary>
        /// Move Down Entry Command, Alt + Down
        /// </summary>
        public static RoutedUICommand MoveDown { get; set; } = new RoutedUICommand("Move Downwards", "MoveDown", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Down, ModifierKeys.Alt, "Alt + Down") });

        /// <summary>
        /// Add Entry Command, Ctrl + N
        /// </summary>
        public static RoutedUICommand Add { get; set; } = new RoutedUICommand("Add", "AddNew", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl + N") });

        /// <summary>
        /// Add Entry Command, Ctrl + E
        /// </summary>
        public static RoutedUICommand Edit { get; set; } = new RoutedUICommand("Edit", "Edit", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Control, "Ctrl + E") });

        /// <summary>
        /// Remove Entry Command, Delete
        /// </summary>
        public static RoutedUICommand Remove { get; set; } = new RoutedUICommand("Remove", "Remove", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.Delete, ModifierKeys.None, "Delete") });

        #endregion

        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(Commands),
            new InputGestureCollection { new KeyGesture(Key.F4, ModifierKeys.Alt) });
    }
}
