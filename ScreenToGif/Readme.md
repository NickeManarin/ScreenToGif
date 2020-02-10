# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.20.2)

• German translation was updated.  
• Added the privacy policy link inside Options.  

### Fixed:

♦ Updated the startup check to ask for .Net Framewok 4.8.  
♦ The FFmpeg video importer now detects corretly videos with rotation.  
♦ The app was ocasionally crashing when loading the editor window when trying to apply the glass effect.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ With the new recorder UI: The Accept/Retry/Cancel commands are overflowing to the left when the selection is too small and to the left.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  