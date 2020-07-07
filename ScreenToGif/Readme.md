# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.26.1)

• Updated the French, Russian, and Chinese (Simplified) translations.

### Fixed:

♦ The BitBlt capture mode with Memory Cache was resulting in some black frames.  
♦ Clicking on a link of an encoding (when setting to upload the file) was causing a crash.  
♦ The error and exception details windows were not adjusting to the correct theme.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ When capturing with the DirectX, the recording will crash if the recording area is outside of the screen.  