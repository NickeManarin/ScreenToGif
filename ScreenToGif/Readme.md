# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.12)

• Remove frame duplicates: It's a feature that allows you to remove frames that are similar to its neighbors.   
• Key Strokes: It's now possible to show the key presses before it actually happened.  (Failed)
• Key Strokes: You can avoid displaying modifier keys while they are not part of a keyboard command.  
• Key Strokes: You can add/remove/edit your keys strokes.  
• If there's not enough space left on the drive, a warning message will appear on the editor window.  
• You can now also control the zoom of the image by using a small up/down field at the bottom of the window.  
• It's now possible to set the maximum age that a project can reach before being deleted automatically. It defaults to 5 days old.  
• Updated Simplified Chinese and Danish translations.  
• Upload of files via Yandex is now supported.  

### Fixed:

♦ While discarding the recording, the app would let you start a new recording before finishing erasing the files of the previous one.  
♦ Encoding with Gifski (Note: It's now using a DLL instead of the executable).  
♦ While adding overlays (Free Text, Free Drawing, Border...) with an image with a DPI not equal to the current DPI of the screen, the overlay content would not appear on the right position of the frame.  
♦ The recorder window position was not being restored properly if the window was closed while it was on a secondary monitor.  
♦  

### Known Bugs:

♠ When saving a gif using the overwrite option while the output file has a usage lock, no error appears.  
♠ The Cinemagraph feature is broken for high DPI PCs.