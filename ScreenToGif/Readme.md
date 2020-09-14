# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.27.1)

• Updated the Korean, Russian, Hungarian, French, Dutch, and Chinese (Simplified) translations.  

### Fixed:

♦ Using non-supported shortcut keys (Shift + letter or just letters) as the recorder shortcuts (record, pause, stop) was crashing the app.  
♦ The 'interaction' capture frequency was not working with clicks on fullscreen mode.  
♦ After opening and closing the options window while on fullscreen mode, the recorder was displaying the guidelines.  
♦ The 'Pause' button on the taskbar preview was using the 'Stop' icon (also, two Stop buttons were being displayed).  
♦ The window icons (minimize and close) were not being updated to match the selected theme (dark vs light).  
♦ The updater window was clipping outside of the screen (when the text inside was too big).  
♦ The screen recorders were working on Windows 7.  
♦ Some texts were not translatable.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   