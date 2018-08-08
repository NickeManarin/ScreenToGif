# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2017 and .Net 4.6.1 or newer required._


## What's new? (Version 2.14)

• You can now set up to avoid receiving a confirmation message before closing the app via the tray icon.  
• Added the ability to get updates for the translations.  
• The color picker hexadecimal field now supports pasting/typing without specifying the alpha/transparency value.  
• You can now review your feedback before sending it to the developer.  
• It's now possible to personalize the size of the mouse clicks highlight. Also, you can apply it even after the recording.    
• You can now let the app start minimized to the system tray.  
• You can now create some tasks to be executed after each recording. (More to come)   
• Troubleshooter: If any window is missing (outside of the screen bounds), you can use the troubleshooter to reset its current or startup position.  
• Added the option to disable hardware acceleration.  
• Now the app warns when trying to open a project that is already opened in another editor instance. Also, the task that clears the temporary folder will not delete any project that is open.    

### Fixed:

♦ The code that positions the recorders was improved.  
♦ Two buttons were being displayed at the KeyStrokes editor, one got removed.    
♦ The progress indicator value (for the percentage, without showing the total) was wrong.   
♦ If you tried to apply a caption with just spaces, the app would crash. Now a warning appears, explaining that you need to type something.  
♦ Improved the "FFMpeg/Gifski is not present" warning.   
♦ When clicking to save the project as multiple images, without zipping and pressing 'No' right after a prompt appeared, the Save/Cancel buttons of the panel would be disabled.    
♦ The framerate of the encoder 2.0 could be wrong by an offset of 9ms.  
♦ When importing a translation, the 'new lines' were not being correctly identified.  
♦ When using relative paths such as '.' for the temporary folder, an error would appear.  
♦ When trying to crop, the app could crash (because of a high DPI issue).  
♦ When selecting a screen region (using the new recorder UI), the app could crash (also a high DPI issue).  
♦ The slide transition was getting the amount of frames from a wrong slider (this caused a crash when undoing the transition).  
♦ When entering snapshot mode with the recorder UI, without having any region selected and pressing "Record", the app would crash. Now it prompts for a region of the screen to be selected.  
♦ Editing the key strokes of the last frame was not possible. The key input was being ignored.  
♦ When a screen gets disconected, all Editor windows will be moved to an available screen.  
♦ When cancelling the selection of the compression method used by the system encoder (video export), the app would crash.  
♦ When using the 'Remove duplicates' feature, the option to conserve the total delay of the removed frames was not working when selecting the option to remove the first duplicate frame.  
♦ When using the 'Remove duplicates' feature, tring to undo the previous action that had no frames to be removed, a crash would happen.  
♦ Pressing 'Stop' when a the pre-starter countdown is still active was not really stoping it.  

### Known Bugs:

♠ Using an automated task to add the key strokes will still use the color and font settings from the main settings.  