# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.21)

• You can now import Apng's files.  
• Option to run the app on startup.   
• Option to allow only one instance of the app, switching to the already opened app when trying to open another instance. (Single instance per user and executable)  

### Fixed:

♦ If the SharpDX libraries were not in the same folder as the app, it was not possible to load them.  
♦ The editor window was not receiving focus upon loading.  
♦ Sometime, the button to select a region in the new recorder was not working.  
♦ The Accept/Retry/Cancel controls were not getting positioned correctly when the whole right side of a screen was selected.  
♦ When the folder path of the SharpDx was empty, the options window was displaying an error while checking the external tools.  
♦ The video importer (MediaPlayer) was not respecting the selected scale.  

### Known Bugs:

♠ When importing multiple images with different sizes at the same time, the app does not ask to resize all images to the same size.   
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ It's impossible to capture snapshots with the DirectX method.  