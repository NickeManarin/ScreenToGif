# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.13)

• Added the option to upload to an Imgur personal account. You can also upload to albums.  
• Added the option to display an icon on the system tray, with the possibility of launching the recorders and editors from a context menu or from a keyboard shortcut.  
• Added proxy support.  
• Added support for selecting the PrintScreen key for the keyboard shortcuts.  
• The hint text that appears when a panel gets opened will stay visible until you close that panel.  
• The zoom is restored to the previous value after closing the side panel.   
• The position of the startup window is now remembered.  

### Fixed:

♦ The warning that apears when there's not enough space left on disk will only be displayed when there's less than 2GB left.  
♦ It was not possible to save board recordings with Gifski as the encoder.  

### Known Bugs:

♠ When saving a gif using the overwrite option while the output file has a usage lock, no error appears.  
♠ The Cinemagraph feature is broken for high DPI PCs.  
♠ The Gifski integration does not accept saving something in a path with Chinese characters, for example: "回复".  



### To Test:

• When removing a setting from the settings file, try removing from a unformatted file.  
• DoubleBox (decimals stuff).   
• Progress percentage (localization).