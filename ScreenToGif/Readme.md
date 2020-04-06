# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.23)

• Added the option to reduce the frame count to only the selected frames.  
• Also, you can select to adjust the delay when reducing the frame count.  
• Added the option to limit the undo/redo history size.  
• Updated the Japanese translation.   
• Added support for Gifski 0.10.2 (delete the old DLL and download it again).
• The Options > Storage UI was redesigned.   

### Fixed:

♦ Switching back from the snapshot mode from the recorder could cause a small crash.  
♦ The auto-updater was failing to run when the cache folder was set to a relative path.  
♦ The tooltips of the buttons of the startup window were incorrect.  
♦ When trying to undo a 'Reduce Framerate' action that resulted in no frames being deleted, a crash would happen.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ It's impossible to capture snapshots with the DirectX method.  
♠ When capturing with the DirectX, the recording will crash if the recording area is outside of the screen.  