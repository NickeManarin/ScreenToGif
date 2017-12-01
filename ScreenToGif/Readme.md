# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.11)

• APNG support (only for saving).TEST: HIGH DPI  
• Added the option to copy the output file/filename/folder after the encoding.  
• Added the option to run post encoding commands.  
• Added the Obfuscate feature, it's used to pixelate a region of the image. TEST: HIGH DPI, Fix Border issue.  
• Added the Spanish (Spain) localization.  
• Updated the Russian Translation.  
• It's now possible to press the enter key to load the selected project on the table of recent projects.

### Fixed:

♦ Clicking on 'Recent Projects' if the temporary folder does not exists would give an error.  
♦ After changing the delay of frames, the statistics tab was not updating.  
♦ When opening a project (or any other file) via drag and drop on the executable (or link) or via the "Open with..." context menu, the app was opening the startup window instead of the editor.  
♦ 

### Known Bugs:

♠ When saving a gif using the overwrite option while the output file has a usage lock, no error appears.  




TODO:
Finish the encoderlstviewitem, test the project copy to clipboard.