# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.17)

• You can now use different themes! Light, medium, dark and very dark are now available.  
• New automated task: You can now create a task that alters the delay of each frame.  
• Progress indicator: Now you can add the exact timestamp of each frame (based on the start date of the recording).  
• When you start the app without any settings, the default folder where the frames are stored (Location folder) is now set to %temp% instead of a hardcoded path.   
• Now you can use your arrow keys to move and resize the selection rectangles from the new recorder and from the Crop feature. Just use the three combinations of commands with Ctrl, Shift and arrow keys.  
• Updated the Japanese translation.  

### Fixed:

♦ When removing duplicated frames, there was no indication that the process was still going on.  
♦ When applying the KeyStrokes with a margin, it would result in a wrongly sized panel.  
♦ If you cancelled the video import, the progress bar would not be hidden away and the app could crash.  
♦ When creating a new blank project and trying to apply the Obfuscation feature, the app would crash, because of the image depth.  

### Known Bugs:

♠ Using an automated task to add the key strokes will still use the color and font settings from the main settings.  
♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ With the new recorder UI: The Accept/Retry/Cancel commands are overflowing to the left when the selection is too small and to the left.  