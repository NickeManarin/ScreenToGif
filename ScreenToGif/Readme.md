# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.6.1 or newer required._


## What's new? (Version 2.19.3)

• German translation was updated.

### Fixed:

♦ The KeyStrokes and MouseClicks tasks were not in the correct place when adding a new task.  
♦ The gif encoders had an issue with the timings of the frames.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ With the new recorder UI: The Accept/Retry/Cancel commands are overflowing to the left when the selection is too small and to the left.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  