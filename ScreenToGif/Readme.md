# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.27.2)

• Just bug fixes.  

### Fixed:

♦ If you tried to open the app while not having .Net 4.8 installed, a crash was happening the before the message could appear explaining that .Net 4.8 was required.  
♦ Gifski was shrinking the size of gifs bigger than 800x600.  
♦ The download of FFmpeg was not working.   

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   