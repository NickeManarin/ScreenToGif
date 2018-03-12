# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.13)

• Option to upload to a Imgur personal account, you can also upload to albums.
• Option to display an icon on the system tray, with the option to launch the recorders and editors from a context menu or from a keyboard shortcut.  
• Added proxy support. 
• Added support for selecting the PrintScreen key for the keyboard shortcuts.  
• The position of the startup window is now remembered.  

### Fixed:

♦ The warning that apears when there's not enough space left on disk will only de displayed when there's less than 2GB left.  
♦ Gifski was not able to saving board recordings.  

### Known Bugs:

♠ When saving a gif using the overwrite option while the output file has a usage lock, no error appears.  
♠ The Cinemagraph feature is broken for high DPI PCs. 
♠ Gifski does not accepts saving something in a path with "回复".  


### To Test:

• When removing a setting from the settings file, try removing from a unformatted file.  
• DoubleBox (decimals stuff).   
• Progress percentage.