# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.9)

• New color quantization algorithm for the encoder 2.0.  
• An installer is available.  
• Options to automatically adjust the window based on the frame size and to automatically adjust the image zoom based on the window size (after loading a project).   
• New translation: Dutch.  
• Updated the German, Italian, Portuguese (Brazilian), Simplified Chinese and Russian translations.  
• A message appears when before deleting frames or discarding the project. It's optional, you can disable this behavior.  
• The updater detects which type of update it should download (portable or installer).  

### Fixed:

♦ The Insert window was not taking into account the DPI of the image being inserted.  
♦ A bug related to the crop feature under a high dpi environment.   
♦ The recorder window could stay out of view if it was previously used in a monitor that got disconnected.  
♦ After unplugging a monitor, the recorder window was not adjusting its position.  
♦ The "Go to frame" feature was not displaying the correct frame.  
♦ After deleting frames, the project info was not being saved into the file (project.json), this was causing some false warnings while trying to load the project via "Recent Projects".  
♦ Added the missing "New Board Recording" context menu entry on the editor window.  
♦ Several bugs with the new recorder that happened when using screens to the left or above the primary screen.
♦ The recorder feature called "snap to window" wasn't working with windows on screens to the left or above the primary screen.

### Known Bugs:

♠ ???