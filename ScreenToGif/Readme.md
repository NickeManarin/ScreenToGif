    ___  ___  ___  ___  ___.---------------.
  .'\__\'\__\'\__\'\__\'\__,`   .  ____ ___ \
  |\/ __\/ __\/ __\/ __\/ _:\   |`.  \  \___ \
   \\'\__\'\__\'\__\'\__\'\_`.__|""`. \  \___ \
    \\/ __\/ __\/ __\/ __\/ __:                \
     \\'\__\'\__\'\__\ \__\'\_;-----------------`
      \\/   \/   \/   \/   \/ :   ScreenToGif   |
       \|______________________;________________|

Nicke Manarin, 19/05/2016

Language Codes:

https://msdn.microsoft.com/pt-br/goglobal/bb896001.aspx
http://www.science.co.il/language/locale-codes.asp

TO-DO:

• Board Recorder.
• Text recorder.

• Pressed Keys overlay.
• Follow the cursor while recording without need to reajust the window position (free recording).
• Option to reduce number of colors.  http://www.codeproject.com/Articles/34792/Quick-how-to-Reduce-number-of-colors-programmatica
• Zoom effect.
• Transitions: Morph, etc.
• Image upload (ImgUr)  http://api.imgur.com/, Example 
• Fast Rewind transition.
• Show path of cursor using lines.
• Diferent palettes.
• Resizable list of frames listbox. 

• I can later calculate the estimated file size: Header + extensions + color count + lzw encoding.
• Frame image types... one color for transitions, other color for not altered frames, etc

• delete range dialog where I can specify start and end frame of deletion
• move range dialog where I can specify start and end frames and then offset frame number
• clone range dialog: start/end/new position

• Board Recorder should use different LastFps Width/Height parameters (it is using the same from the recorder), same applies to the webcam recorder.

• Cinemagraph and Transitions can only be applied if more than 1 frame.
• Remove the WindowSize converter, make into code.
• Visual confirmation after delete, it's too quick.

• Color filters, hue, saturation, etc.
• Macros.

• MORE SAVE OPTIONS, compressed avi, new save panel, etc.

BUGS:

• Bitrate of webcam too low (while recording).
• Trying to record while moving using the Alt + Space option to -Y (above top screen) does not work.
• Video import is buggy.
• Resize with differente dpi makes the image have a black border.
• After stopping the recording, takes too much to bring up the editor.

• STRANGE BUG: Saving the board image when all white saves as grey grid.
• OTHER: If drawing goes beyond the Y axis, for example, it stretches way beyond. FreeDrawing.

• If W7, Maybe extend 1 pixel all sides, to avoid the border.

• Dragging the window shows the cursor even if is not set to be recorded. 
• Closing the app, without finishing an action such as discarding...
• Don't apply free drawing or watermark or crop or resize if there is nothing to do.
• If the recording is too big, it hangs after choosing to save, right while copying the frames.

• After selecting the file location, small hang. Right before opening the Encoder.
• Undo 2x times after inserting from a media.
• Selecting a languages fires two times the selected changed event.
• Croping the image and opening the free text may hide the previous text label.

• Minimum width for the framelistview item.

• Black cursors.
• Cleaning the temp folder removes everything, it should remove only the folders from yesterday and older.

• Select fill color high dpi button.
 
• 4 corners pixel are wrong by one value. [128 64 0] to [127 64 0], it's the shadow.
• Keyboard selection by Shift + Arrow keys does not update the preview panel

• Board's size textboxes not working.

 Done:
 
 • Localization added: Arabic.
 ♦ Fixed: Send feedback bug.
 ♦ Project import bug prevented importing from a zip file.
 ♦ Gif import bug when the gif had only 1 frame.