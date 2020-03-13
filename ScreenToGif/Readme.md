# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.22.1)

• Updated the French translation.   
• Added support for Gifski 0.10.2 (delete the old DLL and download it again).  

### Fixed:

♦ Fixed bug with Gifski encoding which resulted in skewed frames.  
♦ The text box that accepts only integers could cause a crash when trying to parse the text.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ It's impossible to capture snapshots with the DirectX method.  
♠ When capturing with the DirectX, the recording will crash of the recording area is outside of the screen.  