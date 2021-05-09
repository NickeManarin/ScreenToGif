# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._

## What's new? (Version 2.30)

• Added presets for exporting Mp4 and Mov for Twitter.    

### Fixed:

♦ #873 - The multi-frame selection was getting lost after removing all previous/next frames (thanks to @pawlos).  
♦ #883 - Cancelling the media insertion by pressing the cancel button was causing a crash (thanks to @pawlos).  
♦ #885 - The button to open the file after encoding was not appearing.    
♦ #887 - The Caption was not being rendered correctly.   

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   
♠ Holding the side arrows to seek the frames for a long period makes the scrubing act jump frames.  
♠ Capturing with DirectX using a screen not in landscaped mode results in a rotated frame.  