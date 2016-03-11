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
• Mail/reddit send mechanism to send logs.

• Pressed Keys overlay.
• Follow the cursor while recording without need to reajust the window position (free recording).
• Option to reduce number of colors.  http://www.codeproject.com/Articles/34792/Quick-how-to-Reduce-number-of-colors-programmatica
• Zoom effect.
• Transitions: Morph, etc.
• (Stalled) Image upload (ImgUr)  http://api.imgur.com/, Example 
• Fast Rewind transition.
• Show path of cursor using lines.

• "Select" tab with lots of selections options, (select first, etc)
• Frame image types... one color for transitions, other color for not altered frames, etc

• delete range dialog where I can specify start and end frame of deletion
• move range dialog where I can specify start and end frames and then offset frame number
• clone range dialog: start/end/new position

• Board Recorder should use different LastFps Width/Height parameters (it is using the same from the recorder), same applies to the webcam recorder.
• Board recorder with different background options, like intersecting lines,

• Cinemagraph and Transitions can only be applied if more than 1 frame.
• Remove the WindowSize converter, make into code.
• Check contents of comboboxes with high dpi.

BUGS:

• Bitrate of webcam too low (while recording).
• Takes a while to render the inserted frames.
• Trying to record while moving using the Alt + Space option to -Y (above top screen) does not work.
• Video import is buggy.
• Resize with differente dpi makes the image have a black border.
• DoubleNumericUpDown not showing new binding value.

• Dragging the window shows the cursor even if is not set to be recorded. 
• Closing the app, without finishing an action such as discarding...
• Don't apply free drawing or watermark or crop or resize if there is nothing to do.

• After selecting the file location, small hang. Right before opening the Encoder.

• Undo 2x times after inserting from a media.

• Selecting a languages fires two times the selected changed event.

 Done:

• Transitions! You can add transitions between frames.
• Tab titles now update its colors based on Window color scheme.
• Font selector performance increased.
• Cursor recording tweaked.
• Improved Title Frame, with one more option and with a proper loading.
• You can export the string resource file to translate the app and import to test.
• Brazillian Portuguese translation added.
• Go to frame number... Ctrl + G
• Board recorder: Start drawing to record.
♦ Fixed: Preview delay of the first frame was being ignored.
♦ Fixed: Font loading.
♦ Fixed: Encoder not properly opening after starting a new recording.
♦ Fixed: Project import.