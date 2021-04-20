# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._

## What's new? (Version 2.28)

• Reworked and redesigned exporter and uploader.  
• You can now create presets for each file type and encoder.  
• You can also create presets for the upload services.  
• Added option to partially export a project (by selection, frame range, time range and expression).  
• You can now export as webp, bmp, jpg, mov.  
• Redesigned export options for FFmpeg (you can still manually type the parameters if you want to).  
• Added option to ignore simulated keystrokes (thanks to @cuiliang).  
• Added option to purge the cache when leaving the app (option to ask for it).  
• New settings system, should give you less issues when persisting to disk.
• The free text can now receive text decorations, shadow and it can be aligned (thanks to @mabakay).  
• Added Greek localization.  
• Updated the Italian, Korean, German, Spanish, Portuguese, Dutch, Russian, Chinese (Traditional), Chinese (Simplified), Turkish and Japanese localizations.  

### Fixed:

♦ Pressing the record hotkey while the old recorder was minimized was causing a crash.  
♦ The border auto-task was not working if the border was not set to grow outwards.  
♦ Slow playback in editor previewer (thanks to @mabakay).  
♦ Recorder: A crash was happening if the monitor scheme was changed while the recorder was minimized.  
♦ DirectX recorder: Recording on a non-primary screen was resulting in a project with transparent frames.  
♦ New recorder: Dragging to select a screen region near the borders close to another display was very slow.  
♦ Cinemagraph: This feature was not working as expected in high DPI environments.   
♦ The option to save gifs with transparency was not working if you didn't also select the option to detect unchanged pixels.  
♦ Some gifs from Gfycat where not being correctly loaded.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   
♠ Holding the side arrows to seek the frames for a long period makes the scrubing act jump frames.  
♠ Capturing with DirectX using a screen not in landscaped mode results in a rotated frame.