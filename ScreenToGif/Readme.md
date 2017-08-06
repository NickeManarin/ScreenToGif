# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## Version 2.9:

• New color quantization algorithm for the encoder 2.0.  
• An installer is available.  
• Options to automatically adjust the window based on the frame size and to automatically adjust the image zoom based on the window size (after loading a project).   
• New translation: Dutch. Updated the Simplified Chinese and Portuguese (Brazilian) translations.  
• A message appears when before deleting frames or discarding the project. It's optional, you can disable this behavior.  
• The updater detects which type of update it should download (portable or installer).  

### Fixed:

♦ A bug related to the crop feature under a high dpi environment.   
♦ The recorder window could stay out of view if it was previously used in a monitor that got disconected.  
♦ After unpluging a monitor, the recorder window was not adjusting it's position.  
♦ The "Go to frame" feature was not displaying the correct frame.  
♦ After deleting frames, the project info was not being saved into the file (project.json), this was causing some false warnings while trying to load the project via "Recent Projects".  
♦ Added the missing "New Board Recording" context menu entry on the editor window.  

### Known Bugs:

♠ The Insert window does not respect the image DPI. 