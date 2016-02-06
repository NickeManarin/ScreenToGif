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

• The Editor window.
• Board Recorder.
• Text recorder.
• You shouldnt play the preview while executing something.

• Pressed Keys overlay.
• Follow the cursor while recording without need to reajust the window position (free recording).
• Option to reduce number of colors.  http://www.codeproject.com/Articles/34792/Quick-how-to-Reduce-number-of-colors-programmatica
• Zoom effect.
• Morph algorithm.  http://www.codeforge.com/article/216937
• (Stalled) Image upload (ImgUr)  http://api.imgur.com/, Example 
• More startup args.
• Fast Rewind transition.
• TitleFrame delay should be a param.
• Any action shouldn't remove the image and alter everything.

• Tooltips with the shortcuts...
• "Select" tab with lots of selections options, (select all, select inverse, select first, etc)

• Select multiple from frame list with mouse drag
• delete range dialog where I can specify start and end frame of deletion
• move range dialog where I can specify start and end frames and then offset frame number
• clone range dialog: start/end/new position

• Discard the recording from the recorder.
• Board Recorder should use different LastFps Width/Height parameters (it is using the same from the recorder), same applies to the webcam recorder.


BUGS:

• Undo/Redo/Reset is a little bit buggy.
• Takes a while to render the inserted frames.
• Trying to record while moving using the Alt + Space option to -Y (above top screen) does not work.
• Video import is buggy.
• Opening the Recorder with the Snap options active and unchecking 
  Snapshot mode still displays the "Snap" button. Actually, changing the Snapshot mode doesn't change the button.
• DoubleNumericUpDown not showing new binding value.

• If selecting multiples frame from the end to the start, it will scroll to the end one. (Fix frame selection)

• Don't apply border or free drawing or watermark or crop or resize if there is nothing to do.

 Done:

• Board recording working.
• Save to default folder now works.
• By selecting the option "Save to a default output", the user is prompted to select the folder. (If none previously selected).
• Importing project from zip enabled.
• Importing media working again. Faster Gif import.
• Fit image, vertical bug
• Dragging the windows to the side bug, w10, doubleToSize converter