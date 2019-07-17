# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.18)

• Added support for presets/profiles for FFmpeg video encoding feature, separated by file type.  
• Keystrokes: The app will display "Ctrl" instead of diplaying "Control".
• Updated the Korean translation.  

### Fixed:

♦ The process that checks if there's already a file with the same name was changed. Now it should be smoother.  
♦ In Options > About, the Gitter link was openning other site by mistake.  
♦ The task that alters the delay of frames was not working.  
♦ The transition "Fade to color" was ending with a frame not 100% of the selected color.  

### Known Bugs:

♠ Using an automated task to add the key strokes will still use the color and font settings from the main settings.  
♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ With the new recorder UI: The Accept/Retry/Cancel commands are overflowing to the left when the selection is too small and to the left.  