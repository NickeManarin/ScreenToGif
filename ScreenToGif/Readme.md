# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.22)

• It's more appareant that a new update is available.   
• Added an updater, which automatically downloads and installs new updates (optional), even for portable installations.
• You can now manage the presets of the encoding paramaters of FFmpeg for Gif and Apng too.  

### Fixed:

♦ The DirectX capture method was crashing in some machines when trying to record with the cursor visible.  
♦ The installer was crashing when the user had no .Net Framework 4.7.2 or newer.  
♦ When inserting a new media into an existing project, the app was saving the state to the action stack twice, which caused a crash when trying to undo twice.  
♦ When exporting as images, the app was not resolving the date/time format of the filename.   
♦ The text of 'Help' button was missing.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ It's impossible to capture snapshots with the DirectX method.  
♠ When capturing with the DirectX, the recording will crash of the recording area is outside of the screen.  