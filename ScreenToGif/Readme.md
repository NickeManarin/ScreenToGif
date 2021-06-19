# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._

## What's new? (Version 2.32)

• Memory usage improvements with the frame list inside the editor.  
• You can now open the app and start recording by using command line arguments (read the wiki for more details).
• Added option to set the background of the editor to follow the OS color theme (thanks to @pawlos).
• Added option to resize the frames by setting a percentage.  

### Fixed:

♦ The selection adorner could appear in the recording if the region was previously left close to the right corner of the screen.
♦ The new recorder UI command panel was getting in the way of the capture when positioned to the left of the capture region.
♦ The insert window was reporting wrong sizing information about the images and canvas.  
♦ The new recorder UI was width and height text boxes were not displaying the correct scaled size based on the screen DPI.
♦ When exporting and not selecting a file path, the filename of the temporary file was not using the extension (thanks to @pawlos).  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   
♠ Holding the side arrows to seek the frames for a long period makes the scrubing act jump frames.  
♠ Capturing with DirectX using a screen not in landscaped mode results in a rotated frame.