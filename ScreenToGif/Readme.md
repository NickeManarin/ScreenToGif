# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.11)

• APNG support (for saving).  
• Added the option of copying the output file/filename/folder after the encoding.  
• Added the option of running post encoding commands.  
• Added the Obfuscate feature. It's used to pixelate a region of the image.  
• You can now use FFmpeg or Gifski to save your gif.    
• Added the option of uploading the gif to Imgur or Gfycat. (Experimental feature)
• Added the Spanish (Spain) translations.  
• Updated the Russian and Chinese (Simplified) translations.  
• You can now download FFmpeg and Gifski directly using the app. Head over to Options > Extras to download it.  
• It's now possible to press the enter key to load the selected project from the table of recent projects.

### Fixed:

♦ Images with high dpi (for example, 600dpi) were not displayed correctly.
♦ Clicking on 'Recent Projects' if the temporary folder does not exist would give an error.  
♦ After changing the delay of frames, the Statistics tab was not updating.  
♦ When opening a project (or any other file) via drag and drop on top of the executable (or link) or via the "Open with..." 
    context menu, the app was opening the startup window instead of the editor.  
♦ While using the new recorder, if the record button was pressed without a screen region selected, an error was happening. 
    Now it will enter the selection mode.  
♦ While clicking on the link to open the file that already exists for types other than Gif, it was only opening the gif file.  

### Known Bugs:

♠ When saving a gif using the overwrite option while the output file has a usage lock, no error appears.  
