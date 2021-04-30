# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._

## What's new? (Version 2.29)

• Performance improvements in capturing the screen.  
• Added option to improve the performance of the region selection in the new recorder UI (enabled by default).  
• Improved performance of the duplicate frame remover (thanks to @DarkOoze).  

### Fixed:

♦ Delete all previous/next frames: Now multiple frame selection is taken into account (thanks to @pawlos).  
♦ DirectX capture: The cursor was not being correctly captured in non-primary monitors.  
♦ DirectX capture: A crash message was not displaying its details when capturing in async mode.  
♦ DirectX capture: Adjusted message when trying to capture in a screen rendered by another graphics adapter.  
♦ DirectX capture: The legacy recorder was not able to capture using the option to just capture when something changes on screen.  
♦ Screen/window selector: Reduced lag when displaying the screen/window selector for the new recorder UI.  
♦ Remove duplicates: The last frame was not being compared, so it was not being removed when needed (thanks to @DarkOoze).  
♦ Save as project too: Filename was getting ignored (saving as ".stg") and the export could fail depending on the configuration of the default preset.  
♦ Export as images: Files were being overwritten without confirmation.  
♦ Export as images: The notification of the encoder was not displaying correctly the encoding of multiple files.  
♦ New recorder: The new sizing values input in the text boxes were not being saved when closing the window.  
♦ Transparency: The transparency options and unchanged pixel detection were not working well together.  
♦ Cache purge: Fixed the message not appearing correctly when closing the app.  
♦ Yandex: Link to get token was not working.  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ The newer recorder doesn't let you move the selected region to other windows.  
♠ When using the capture option "Capture a frame only when something changes on screen" and moving the recording window, the recording will glitch.  
♠ The Previous/Next repeat buttons are only triggering the events once (because of the command).   
♠ Holding the side arrows to seek the frames for a long period makes the scrubing act jump frames.  
♠ Capturing with DirectX using a screen not in landscaped mode results in a rotated frame.  