# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.27)

• Redesigned the newer recorder UI (you can still use the compact mode if you wish).  
• Added better support for multi-scale set of screens.  
• The new recorder UI performs better, it's less laggy when selecting a screen region.  
• You can now drag the screen region selection after selecting it or during recording with the new recorder UI.  
• Displays the recording time (hover with the cursor on top to see the captured frame count), and also shows a counter for manually captured frames.  
• Added a capture mode to capture frames only when the user clicks and types (it ignores input while focused within the recorder itself).  
• Added a capture mode option, to capture only when something changes within the capture region (it can be activated with PerSecond, PerMinute and PerHour modes).  
• You can now display guidelines (customizable rule of thirds and/or crosshair) in the screen recorder.  
• New obfuscation methods: Darken and ligthen parts of your frames.  
• You can set the obfuscation to be applied to the inverse of the selection, with or without smoothened edges.  
• Replaced the old folder selector dialogs with a modern variant.  
• When saving a project, the button near the folder path now lets you select a folder by default. To select a folder and filename, press Shift while clicking on that button.  
• Improved mouse input capture system.  
• Added an optional (but default) confirmation dialog when discarding a recording.  
• Added the option to display the discard button during the recording (not just when the capture is paused).  
• Added scaling quality options for the resize feature.  
• Added the option to download updates as a Zip to replace the executable manually (for those users who can't execute installers).  
• The screen recorder now displays the recording status over the taskbar icon.  
• Added the option to opt-out from the recorder remembering the previous recording region (size and/or position).  
• Updated Gifski to version 1.2.0 (if using portable version, erase the old DLL and download it again to update).
• Updated the XX, XX translations.  

### Fixed:

♦ The update could fail if the update window was opened before the download of the update finished.  
♦ The recorder (newer UI) was not detecting some windows (those without the system titlebar).  
♦ When capturing with the DirectX, the capture was crashing if the recording area was outside of the screen.  
♦ When selecting a small screen region with the newer recorder, near the side edges, the capture controls would be displayed outside of the screen.  
♦ The newer recorder was crashing in selection mode when the selection was ocuppying the more than 90% of vertical space, while tucked to the right side of the screen.   
♦ When exporting a file with a name with emojis and selecting to copy the filename after encoding, the emojis were not being copied correctly.  
♦ The cinemagraph feature was not working correctly when the frame image had a different DPI than the screen.  
♦ When capturing using the older recorder and moving the window could result in a long pause (while the window was still being moved) in the recording.   
♦ Added missing f, z, g, t, and K date and time formats to the automatic file naming feature.  
♦ When the "Single instance only" mode was enabled, dragging and dropping a media file into the executable or into the shortcut of ScreenToGif would show the app window but it would not load the file.  
♦ When applying a feature multiple times, the image quality would start to degrade (the image would become blurry).  -TODO: Test with High DPI.
♦ The troubleshooter is now displaying correctly the position of the windows in multi-DPI systems (also it can now detect screen settings changes while opened).  
♦ Removed the indefinite taskbar button animation when importing media.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   