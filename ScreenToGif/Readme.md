    ___  ___  ___  ___  ___.---------------.
  .'\__\'\__\'\__\'\__\'\__,`   .  ____ ___ \
  |\/ __\/ __\/ __\/ __\/ _:\   |`.  \  \___ \
   \\'\__\'\__\'\__\'\__\'\_`.__|""`. \  \___ \
    \\/ __\/ __\/ __\/ __\/ __:                \
     \\'\__\'\__\'\__\ \__\'\_;-----------------`
      \\/   \/   \/   \/   \/ :   ScreenToGif   |
       \|______________________;________________|

Nicke Manarin, 21/12/2015

TO-DO:

• Board Recorder.
• Text recorder.
• You shouldnt play the preview while executing something.

• Pressed Keys overlay.
• Follow the cursor while recording without need to reajust the window position (free recording).
• Option to reduce number of colors.  http://www.codeproject.com/Articles/34792/Quick-how-to-Reduce-number-of-colors-programmatica
• Zoom effect.
• Transitions: Fade, slide, Morph, etc.
• (Stalled) Image upload (ImgUr)  http://api.imgur.com/, Example 
• More startup args. Import image via Open with...
• Fast Rewind transition.
• TitleFrame delay should be a param.
• Any action shouldn't remove the image and change everything.

• "Select" tab with lots of selections options, (select first, etc)

• delete range dialog where I can specify start and end frame of deletion
• move range dialog where I can specify start and end frames and then offset frame number
• clone range dialog: start/end/new position

• Board Recorder should use different LastFps Width/Height parameters (it is using the same from the recorder), same applies to the webcam recorder.
• Board recorder with different background options, like intersecting lines,

• Xaml vector for Missing file, encoder window
• Display a better warning for the Missing file status.

• DoubleToSizeConverter to the Recorder window too.
• Pause the preview if idle for too many time.

• Deselect should remove the image from the ZoomViewer and grey out some options. (insert, zoom, select frame delay)
• Clipboard with multiples values.
• Drag and drop o import.

• Context menu, editor window!

BUGS:

• Undo/Redo/Reset is a little bit buggy.
• Bitrate of webcam too low (while recording).
• Takes a while to render the inserted frames.
• Trying to record while moving using the Alt + Space option to -Y (above top screen) does not work.
• Video import is buggy.
• DoubleNumericUpDown not showing new binding value.

• Don't apply free drawing or watermark or crop or resize if there is nothing to do.

• While dragging the recorder window, the cursos is doubled.
• Opening a panel, playback controls take time to disable, rush the Command avaliation.

 Done:

• Added Discard option to the Webcam recorder.
• Added a context menu on the editor. 
• Added tooltips to most buttons on the Editor Window.
• Border option redesigned.
• Fixed: Tab order on the Recorder window.
• Fixed: Cinemagraph's Eraser's shape's binding.
• Fixed: Wrong delay while preview the first 2 frames.
• Fixed: Incorrect size of overlay grid while the image uses a scrollbar.
• Fixed: Deselect wasn't removing the select frame from the ZoomBox.
• Fixed: Frame selection (and exhibition) related bugs.
• Fixed: Misplaced warning about the default save location while trying to save.