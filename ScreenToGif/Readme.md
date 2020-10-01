# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.28)

• Updated the German, Dutch and Hungarian localizations.  

### Fixed:

♦ The FFmpeg video importer was ignoring a 270° rotation of videos.
♦ When trying to download SharpDX to a folder in which you have no write permissions, the app was crashing. Now it asks if you want to elevate the process.  
♦ The troubleshooter was not able to reposition correctly the windows when the primary monitor had a scale different than x1.  
♦ The recorders where not able to be moved sideways (via arrow keys) correctly when the primary monitor had a scale different than x1 and while having other monitors with different scales.  
♦ The older recorder was not opening in the correct position after being closed when in a secondary monitor to the right, while the primary monitor had a scale different than x1.
♦ By using the scroll wheel in the width and height number boxes, while having a screen scale different than 1, it was not moving the cursor to the correct position.   
♦ When switching from a smaller screen resolution to a bigger one, the screen selection was not updating and limiting itself to the new available size.  
♦ The selection mode of the new recorder was not displaying correctly in the secondary monitor to the left of the main monitor, if it had different scale.  
♦ It was possible to start updating the app while encodings were running. Now a warning appears explaining that there is an active encoding being processed.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   