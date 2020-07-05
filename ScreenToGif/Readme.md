# ScreenToGif  

This is the current project of ScreenToGif.  

_VS 2019 and .Net 4.8 or newer required._


## What's new? (Version 2.26)

• Timelapse recording!  
• The built-in encoders got re-designed from scratch! 
• Now you can select the color quantization method (Neural, Octree, MedianCut, Grayscale, and MostUsed) and understand the differences.  
• You can select the the ammount of colors used for the Neural, Octree, MedianCut, Grayscale, and MostUsed quantization methods.  
• Added support for transparency in the built-in encoder (works with all quantization methods).  
• You can now switch between capture frequency modes from the recorder window (when paused or stopped, and even change the framerate).  
• The encodings are now displayed in a popup attached to the editor (but you can still use the older alternative, a separated window).  
• You can now see the total elapsed time of the encoding process.  
♦ The 'Quality' slider of the Neural quantization method is now called 'Sampling', and it's properly explained what it means.  

### Fixed:

♦ The built-in encoders were giving green artifacts when the color used as chroma key was present in the frame.  
♦ When opening the recorders while on snapshot mode and pressing the stop button, the app would crash (thanks to Naoki for finding that).  
♦ The eye dropper of the color selector window was changing size when the drag started.  
♦ It was impossible to capture the screen in manual mode with the DirectX capture method.  
♦ The context menu items (undo, redo, and delete) were not being enabled (thanks to László for the help).  

### Known Bugs:
  
♠ When exporting with FFmpeg, the last frame may be out of sync with the timmings of the project.  
♠ Cancelling a encoding of FFmpeg will result in a crash (file in use).  
♠ Keystrokes has a 1 pixel transparent border at the bottom-right sides when using a high DPI screen.  
♠ When capturing with the DirectX, the recording will crash if the recording area is outside of the screen.  