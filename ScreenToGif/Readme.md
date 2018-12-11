# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.15)

• The algorithm that analizes each frame, looking for pixels changes, is now a bit faster.  
• Add the ability to add shapes. This feature is still experimental and there's more shapes to come.  
• You can now export your project as a PSD file.  

### Fixed:

♦ When opening the app via the "Open with" context menu, Gifski and FFmpeg were not being found (if they had a relative path). [Test]
♦ The app could crash when adding key strokes to a project.
♦ It was impossible to undo the "Remove duplicates" action when using the option "Don't adjust".  
♦ Also, because of that bug, when applying the "Remove duplicates" action with other option than "Don't adjust" and later applying with that option, the app would crash too.  
♦ Board recorder: When trying to discard a recording, the app could crash.  

### Known Bugs:

♠ Using an automated task to add the key strokes will still use the color and font settings from the main settings.  