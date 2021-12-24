# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2022 and .NET 6 or newer required._

## What's new? (Version 2.35.1)

• Just a quick bug fix for the installer.  

### Fixed:

♦ The install process was failling if you had SharpDx set up (error 2711).  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   
♠ Capturing with DirectX using a screen not in landscaped mode results in a rotated frame.