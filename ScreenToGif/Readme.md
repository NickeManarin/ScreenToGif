#ScreenToGif

This is the current project of ScreenToGif.

VS 2015 and .Net 4.6.1 required.


Version 2.5:

• Keypresses recording.
• Option to remove X frames after Y frames, also known as reduce framerate.
• Save directly to clipboard option for gifs.
• 64 bits executable (will run as 32 bits when 64 bits not available).

Fixed:

♦ The frame viewer now adjusts its scale based on the screen dpi. 
♦ The shortcuts for the recording are no longer raised when using key modifiers (Ctrl, Shift, Alt and Windows keys). 
♦ The focus is set to the selected frame after loading the list of frames. (for example, when applying an overlay).
♦ The font size of the Title Frame is now saved everytime you change the value, as expected.
♦ The legacy (1.0) encoder was failing to find the transparent color of frames with small changes.
♦ Improved the selection of frames while using the Shift key.
♦ When applying the Progress overlay, a heavy load could freeze the window and crash the app.

Known bugs:

♠ OutOfMemory exception when importing videos. 
♠ The crop feature fails when cropping a board recording (with high DPI);

Todo:

♣ 