# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.20.3)

• Just bug fixes.   

### Fixed:

♦ Some users could not reverse, undo, reset or save projects imported by video.  
♦ The feedback tool was not working for some users.  
♦ Some users were unable to download any external tool (TLS/SSL bug).  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ With the new recorder UI: The Accept/Retry/Cancel commands are overflowing to the left when the selection is too small and to the left.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  