#ScreenToGif  

This is the current project of ScreenToGif.  

VS 2015 and .Net 4.6.1 required.  


Version 2.7:

• You can now load recent projects, which have not been discarded.
• Automatic clean up of old projects, which have not been discarded yet (optional, check out your options).
• More options for keyboard shortcuts, for actions like Record/Stop/Discard.
• Keyboard shortcut for the discard action (when recording).
• Custom timing for the pre-start countdown.
• Custom delay for the transitions (now the app also remembers the latest transition length).

Fixed:

♦ If you open the app for the first time and try to load something, a crash occurs, caused by a property not properly initialized (TemporaryFolder).
♦ Several bugs related to high DPI scaling (including a known bug related to the crop feature).

Known bugs:

♠ OutOfMemory exception when importing videos. 