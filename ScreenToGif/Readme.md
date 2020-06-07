# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.24.2)

• You can disable tasks without having to remove them.  
• Added the option to upload APNGs too.  
• Added a new parameter to the post encoding commands (URL).  
• Updated the Japanese, Chinese (Simplified), Dutch and Russian localizations.  
• The options window will adjust its width to its contents when opening.  

### Fixed:

♦ The screen capture (BitBlt + cursor) over a remote desktop connection was not working properly (thanks to Luis for the help).  
♦ The editor window chrome (title bar) was not getting extended when needed.  
♦ The app could crash before warning of the missing .Net 4.8 when having .Net 4.6.2 or older versions.  
♦ If the project was too big (too many frames or frames too big), the PSD exporter could fail.  
♦ Several texts where not translatable (thanks to László for the help).  
♦ Copy and pasting a frame, then doing it again multiple times, could exceed the filename limit (thanks to Riku for the help).
♦ The 'duplicated filename' warning was not getting hidden when unticking the option to save the file to a selected folder.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ It's impossible to capture snapshots with the DirectX method.  
♠ When capturing with the DirectX, the recording will crash if the recording area is outside of the screen.  