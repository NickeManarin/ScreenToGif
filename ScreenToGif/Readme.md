#ScreenToGif

This is the current project of ScreenToGif.

VS 2015 and .Net 4.6.1 required.


Version 2.4:

• Changed the output of the error logs to the Documents folder.
• Added a Scale options to resize your webcam video feed evenly.

Fixed:

♦ Bugs with the management of localization resources.
♦ Check for updates was reporting a new release (even when it shouldn't).
♦ Cursor capture was causing a memory leak.
♦ Error when openning the save dialog when the last selected folder did not exist.
♦ It was impossible to import a project using drag and drop.

Known bugs:

♠ OutOfMemory exception when importing videos. 
♠ The crop feature fails when cropping a board recording (with high DPI);

Todo:

♣ 