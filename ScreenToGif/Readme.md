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
• Now it remembers the clipboard paste behavior (Clipboard > Paste behavior).
• Updated Danish, Chinese (simplified) and Russian translations.
•

Fixed:

♦ If an action (from a panel such as "Save as") failed because of some validation or when closing with the top-right X button, the playback buttons would stay disabled.
♦ Undoing a transition left a frame that should be deleted.
♦ PageDown and PageUp jumps among various frames instead of one by one. 
♦ Clipboard list was not being cleared after resetting.
♦ Color picker: While returning to the initial color, the numbers were not replaced with the selected color.
♦ The validation of negative numbers for numeric inputs.
♦ Error while pasting and undoing the paste of frames (quickly).

Known bugs:

♠ OutOfMemory exception when importing videos. 
♠ The crop feature fails when cropping a board recording (with high DPI);

Todo:

♣ 