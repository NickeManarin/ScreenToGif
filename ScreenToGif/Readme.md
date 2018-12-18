# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.15)

• The algorithm that analizes each frame, looking for pixels changes, is now a bit faster.  
• Added the ability to add shapes. This feature is still experimental and there's more shapes to be implemented.  
• You can now export your project as a PSD file.  

### Fixed:

♦ When opening the app via the "Open with" context menu, Gifski and FFmpeg were not being found (if they had a relative path).  
♦ Also, it was fixed some other issues related to selecting the FFmpeg/Gifski files when the previous path was relative.  
♦ The app could crash when editing key strokes of a project.  
♦ It was impossible to undo the "Remove duplicates" action when using the option "Don't adjust".  
♦ Also, because of that bug, when applying the "Remove duplicates" action with other option than "Don't adjust" and later applying with that option, the app would crash too.  
♦ Board recorder: When trying to discard a recording, the app could crash.  
♦ With the new recorder UI, when changing from selecting a Region/Window to selecting a Screen, the Accept/Retry/Cancel UI was appearing when it should not.  
♦ Small adjustments with the new recorder UI for high DPI screens.  

### Known Bugs:

♠ Using an automated task to add the key strokes will still use the color and font settings from the main settings.  