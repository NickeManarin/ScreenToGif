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
• Action with system events (lock, hibernation, turn off, etc)

• delete range dialog where I can specify start and end frame of deletion
• move range dialog where I can specify start and end frames and then offset frame number
• clone range dialog: start/end/new position

• Board Recorder should use different LastFps Width/Height parameters (it is using the same from the recorder), same applies to the webcam recorder.
• Board recorder with different background options, like intersecting lines,

• Xaml vector for Missing file, encoder window
• Display a better warning for the Missing file status.

• Pause the preview if idle for too many time.

BUGS:

• Bitrate of webcam too low (while recording).
• Takes a while to render the inserted frames.
• Trying to record while moving using the Alt + Space option to -Y (above top screen) does not work.
• Video import is buggy.
• DoubleNumericUpDown not showing new binding value.

• Closing the app, without finishing an action such as discarding...
• Don't apply free drawing or watermark or crop or resize if there is nothing to do.

• After selecting the file location, small hang. Right before opening the Encoder.
• While dragging the recorder window, the cursos is doubled.

• Undo 2x times after inserting from a media.

 Done:

• Improvements with the loading time of operations. 
• Multiple Clipboard entries, improved clipboard panel with context menu.
• Drag and Drop to import or load.
• System events (suspend) detection.
• TaskBar buttons added.
• Improved animation of the HideableTabControl.
• Info about the selected frame on the status bar.
♦ Fixed: Scrolling while the overlay was active (for example, while drawing something).
♦ Fixed: Delay control was disabled while not previewing.
♦ Fixed: Frame selection control was disabled for Arrow keys.
♦ Fixed: Fit image action.
♦ Fixed: Cut action wrong frame selection.