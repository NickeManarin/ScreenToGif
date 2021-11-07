# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._

## What's new? (Version 2.34)

• Added Hebrew localization.  

### Fixed:

♦ Added support for the new distributition system for v2.35 and newer releases. It's important to download this version if you want to properly update to newer versions afterwards.
♦ Numerical fields will now only react to the scroll if they have focus.
♦ The window selection mode could crash because of a wrongly sized window.
♦ When in frame selection mode, the frame list was allowing the loop between start and end selection to happen.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   
♠ Capturing with DirectX using a screen not in landscaped mode results in a rotated frame.