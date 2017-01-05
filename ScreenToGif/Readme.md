#ScreenToGif

This is the current project of ScreenToGif.

VS 2015 and .Net 4.6.1 required.


Version 2.4:

• Drag and drop files from the encoder.
• Context menu for each completed encoding, providing some clipboard operations.
• License informations available on the "About page".
• Error messages are now more user-friendly.
• You can now configure where the location of the logs folder.
• The playback buttons are now disabled during the opening of a side panel.
• Now it remembers the paste behavior (Clipboard > Paste behavior).
• Updated Danish translation.
•

Fixed:

♦ If an action (from a panel such as "Save as") failed because of some validation, the buttons would stay disabled.
♦ Undoing a transition left a frame that should be deleted.
♦ PageDown and PageUp jumps among various frames instead of one by one. 

Known bugs:

♠ OutOfMemory exception when importing videos. 
♠ The crop feature fails when cropping a board recording (with high DPI);

Todo:

♣ 