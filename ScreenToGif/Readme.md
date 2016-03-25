    ___  ___  ___  ___  ___.---------------.
  .'\__\'\__\'\__\'\__\'\__,`   .  ____ ___ \
  |\/ __\/ __\/ __\/ __\/ _:\   |`.  \  \___ \
   \\'\__\'\__\'\__\'\__\'\_`.__|""`. \  \___ \
    \\/ __\/ __\/ __\/ __\/ __:                \
     \\'\__\'\__\'\__\ \__\'\_;-----------------`
      \\/   \/   \/   \/   \/ :   ScreenToGif   |
       \|______________________;________________|

Nicke Manarin, 26/03/2016

TO-DO:

• Board Recorder.
• Text recorder.
• Mail send mechanism to send logs.

• Pressed Keys overlay.
• Follow the cursor while recording without need to reajust the window position (free recording).
• Option to reduce number of colors.  http://www.codeproject.com/Articles/34792/Quick-how-to-Reduce-number-of-colors-programmatica
• Zoom effect.
• Transitions: Morph, etc.
• Image upload (ImgUr)  http://api.imgur.com/, Example 
• Fast Rewind transition.
• Show path of cursor using lines.

• Frame image types... one color for transitions, other color for not altered frames, etc

• delete range dialog where I can specify start and end frame of deletion
• move range dialog where I can specify start and end frames and then offset frame number
• clone range dialog: start/end/new position

• Board Recorder should use different LastFps Width/Height parameters (it is using the same from the recorder), same applies to the webcam recorder.

• Cinemagraph and Transitions can only be applied if more than 1 frame.
• Remove the WindowSize converter, make into code.
• Visual confirmation after delete, it's too quick.

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

• Cleaning the temp folder removes everything, it should remove only the folders from yesterday and older.

• Check pt-br translation and other localization issues.
• Select fill color high dpi button.
 
 Done:

• Improved Crop action.
• Added the ability to disable the transparent corners on the recorder window.
• Asynchronous recording discard.
• Board recording.
• Portuguese, Simplified Chinese.
♦ Fixed: Wrong parameter as soon as you enter the Editor.
♦ Fixed: Pre start title not clearing up after the pre start countdown.
♦ Fixed: The english localization fallback mechanism.
♦ Fixed: You were able to delete the english localization.
♦ Fixed: File importing via drag and drop validation.
♦ Fixed: Exporting twice (or more) throws an error.
♦ Fixed: Insert frames wrong label position.
♦ Fixed: Font loading bug.
♦ Fixed: Tooltips for the font selector.
♦ Fixed: Issues with the tab's label for < Windows 7 and Windows 10.0.10240.
♦ Fixed: Snap to window ghost rectangle.
♦ Fixed: Crop and also Insert actions had an issue with high dpi resolutions.
♦ Fixed: Webcam capture with high dpi resolutions.