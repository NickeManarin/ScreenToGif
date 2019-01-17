# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.16)

• Added FFmpeg as an optional encoder for Apng.  
• Updated the German and Russian translations.  

### Fixed:

♦ A message about a missing FFmpeg instance was not clickable. 
♦ When loading images, if those images were not in the correct format, the app would not encode properly as apng.  
♦ There was an issue with one of the numerical input fields not working as expected (with numbers being typed).  

### Known Bugs:

♠ Using an automated task to add the key strokes will still use the color and font settings from the main settings.  
♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  