# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2022 and .NET 6 or newer required._

## What's new? (Version 2.36)

• New installer and portable versions with the full package (no .NET 6 download required) are available alongside with the lighter versions, which still require the installation of .NET 6 desktop runtime.  
• New installer package (MSIX) available.  
• Added an option to prompt to overwrite when saving (enabled by default).  
• Updated the Danish, French, German, Hungarian, Polish, Norwegian, Russian, and Simplified Chinese localizations.  

### Fixed:

♦ The smooth loop feature was not working properly.
♦ A new message will be displayed if you already have a smooth loop based on current settings instead of a warning.
♦ It was not possible to set the app to start at Windows startup.
♦ A settings migration issue from 2.31 to newer versions was fixed.
♦ It was not possible to export as PSD.
♦ When not having permission to save the settings to the installation location, the app would not try to save to AppData.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   
♠ Capturing with DirectX using a screen not in landscaped mode results in a rotated frame.