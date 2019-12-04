# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.20)

• The app now uses/requires .Net Framework 4.8.  
• Experimental support for multi-DPI set of screens.  
• Screen capture via DirectX 11.1 (Desktop Duplication API, requires Windows 8 or newer).   
• Screen capture can use a configurable memory cache with support for compression.  
• You can now set actions to be executed when clicking on the app icon on the system tray.  
• The default FFmpeg Webm encoder is now set to VP9.  
• Added support for version 0.9.3 of Gifski (delete the older DLL and download again via Options > Extras).  
• You can now cancel the gif encoding that uses Gifski.  
• When exporting as project, the app will use the encoder window instead of locking down the editor.  
• You can use FFmpeg to import frames from video (I also improved performance while importing video with the default method).  
• The executable can interpret arguments to disable/enable hardware acceleration.  
• The maximum permitted outwards thickness of the border was increased.   
• Turkish and Russian translation were updated.  

### Fixed:

♦ The new recorder had issues with the positioning of the record controls while using multiple screens.  
♦ The troubleshooter had issues displaying the correct position of the screens.  
♦ The screen recorder was not capturing animated cursors correctly.  
♦ The space bar was not set as the Play/Pause button anymore.  
♦ The Gifski encoder now accepts saving gifs to a path (also loading frames from a temporary path) that contains non-latin characters.  
♦ The text was overflowing and not wrapping properly in the FFmpeg command text boxes.  
♦ It was not possible to set shortcuts when deploying with Intune.  
♦ When trying to type special characters with the help of the Right Alt key, some commands were being fired instead. Because the underlying system translates Right Alt to Ctrl + Alt.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ With the new recorder UI: The Accept/Retry/Cancel commands are overflowing to the left when the selection is too small and to the left.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  