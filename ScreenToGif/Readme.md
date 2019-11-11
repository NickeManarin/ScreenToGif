# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.20)

• The app now uses/requires .Net Framework 4.8.  
• Screen capture via DirectX 11.1 (Desktop Duplication API, requires Windows 8 or newer).   
• Screen capture can use a configurable memory cache with support for compression.  
• The default FFmpeg Webm encoder is now set to VP9.  
• Added support for version 0.9.3 of Gifski (delete the older DLL and download again via Options > Extras).  
• You can now cancel the gif encoding that uses Gifski.  
• When exporting as project, the app will use the encoder window instead of locking down the editor.  
• Turkish and Russian translation were updated.  

### Fixed:

♦ The new recorder had issues with the positioning of the record controls while using multiple screens.  
♦ The troubleshooter had issues displaying the correct position of the screens.  
♦ The screen recorder was not capturing animated cursors correctly.  
♦ The space bar was not set as the Play/Pause button anymore.  
♦ The Gifski encoder now accepts saving gifs to a path (also loading frames from a temporary path) that contains non-latin characters.  
♦ The text was overflowing and not wrapping properly in the FFmpeg command text boxes.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ With the new recorder UI: The Accept/Retry/Cancel commands are overflowing to the left when the selection is too small and to the left.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  