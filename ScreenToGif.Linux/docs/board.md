# Board

The Board is the "record as you draw" recorder: a white canvas you draw on with a pen and two erasers, which captures a frame at a fixed rate while you draw and hands the result to the editor. It opens from the Startup window, from the editor's New and Insert ribbon groups, from a configured startup or tray action, or with `--board`.

## Drawing

Four tools sit at the top left, and exactly one is active at a time: **Pen**, the point **Eraser**, **Selection**, and the **Stroke eraser**. Selection is disabled on Linux and its tooltip says so.

Pen mode shows the brush settings: color, width and height from 1 to 100 pixels, an ellipse or rectangle stylus tip, **Fit to Curve**, and **Highlighter**. Point-eraser mode replaces them with the eraser's own width, height and tip; leaving that mode brings the brush settings back.

- The **pen** draws a stroke in the chosen color, as wide and as tall as the brush fields say, with the chosen tip.
- **Fit to Curve** smooths each stroke as it is committed, so a hand-drawn line stops looking polygonal. The stroke's first and last points stay where they were drawn.
- **Highlighter** draws translucent strokes. A single stroke does not darken where it crosses itself; two overlapping strokes do.
- The **point eraser** removes the part of a stroke its tip sweeps over and leaves the rest, so erasing through the middle of a line leaves two lines in the original color. It detects the painted line between sparse input points and the edge of a thick stroke.
- The **stroke eraser** removes every whole stroke it touches, and nothing else.

## Recording

**Auto Record** is on by default: pressing the pointer on the canvas starts recording, and lifting it pauses. Holding **Ctrl** inverts Auto Record while held and reverts on release or when the window loses focus. With Auto Record off, the **Record** command next to it starts and pauses recording by hand, and a recording started that way keeps going between strokes.

While recording, a frame is captured every `1000 / fps` milliseconds. The first frame after a start or a resume carries that interval as its delay; every later frame carries the gap actually measured since the previous one, so a stalled UI thread shows up as a longer frame rather than as drift. Frames are PNGs the size of the canvas in logical pixels multiplied by the window's render scaling.

The frame rate and the canvas size are fixed while a recording is in progress, and the window cannot be resized, so every frame in one recording has the same dimensions. The bottom-left status shows the state and the frame count at all times.

A recording stops growing at the most frames the editor can still save at the canvas's size, the same bound the screen and webcam recorders keep (see [ADRs/20260923-a-recording-stops-growing-where-the-editor-can-still-save-it.md](../ADRs/20260923-a-recording-stops-growing-where-the-editor-can-still-save-it.md)). It pauses there, cannot be resumed, and an on-canvas banner says to press Stop. A frame that cannot be saved also pauses the recording and keeps every frame already taken. Either message remains visible in the banner and status tooltip until you record again or discard.

**Stop**, the **F8** key, and closing the window all hand the frames to the editor. Stopping with nothing recorded does nothing. **Discard** deletes the recorded frames and their workspace from disk, clears the canvas and returns the fields, after asking first unless that confirmation has been turned off in Options.

**Options** opens the Linux application settings as a dialog. The Board drops Topmost while it is open and takes it back when it closes, and the command is unavailable while recording.

## Into the editor

From the Startup window, a Board that recorded something opens the editor on those frames and closes Startup; a Board that recorded nothing returns to Startup.

From the editor, **New → Board** and **Insert → Board** open the Board over the editor, one capture window at a time: asking again while one is open only brings it forward. **New → Board** replaces the project with the recording when the Board closes, asking first when there are unsaved changes, exactly as **New → Webcam** does, and the recording arrives unsaved. **Insert → Board** asks whether to put the recording before or after the selected frame, at the beginning, or at the end; Cancel leaves the project unchanged. It inserts the frames as one undoable step and copies them into the editor's own workspace first, so deleting the Board's workspace cannot break them. A Board closed with nothing recorded leaves the project and its history as they were.

## Settings

Each Board tool setting — brush color, width, height and tip, Fit to Curve, Highlighter, eraser width, height and tip, and the frame rate — is committed to `settings.json` on its own as it changes, so the Board never writes over a setting Options saved in the meantime. A write that fails is reported in the status. The canvas size is the window's own and is written when the window closes, as the Recorder writes its frame. Whether Discard asks first is **Ask me before discarding the recording** on the Recorder page of Options, a setting the Board shares with the screen recorder. Deleting or corrupting `settings.json` restores the defaults: 15 fps, black, a 10×10 ellipse brush, a 10×10 rectangle eraser, Fit to Curve off, Highlighter off, and the discard confirmation on.

## Departures from the Windows Board

The Windows Board is the reference for what a person can do, not for how it is built. Each line below is a deliberate difference, with the reason.

- **Point erasing splits strokes instead of subtracting geometry.** The WPF eraser subtracts the eraser sweep from the widened stroke. Avalonia can express that, but every geometry operation resolves through the platform render interface, which does not exist in the test host — so that eraser could only ever be checked by eye. Splitting the stroked path where the eraser touches is plain arithmetic and is covered by tests for sparse input, painted thickness, a miss, and repeated splitting. Because the stroke model cannot retain a partly erased cross-section, touching a thick stroke removes that part of its path across the full brush width.
- **Highlighter strokes are drawn at a fixed 50% opacity**, in one draw operation per stroke. WPF blends highlighter ink through the ink renderer; one translucent fill per stroke reproduces what that is for — self-overlap stays even, crossing strokes darken.
- **The canvas fills the window, and the size fields report it.** The Windows Board window is fixed-size and its width *is* the board width. On Linux the window is resizable, the canvas is everything between the two toolbars, and the size fields and the canvas track each other in both directions: a typed size resizes the window to give the canvas exactly that many pixels once you press Enter or leave the field, and dragging the window updates the fields. The window will not go below 800×453, because the lower toolbar stops being readable, so the smallest canvas is 798×387; a smaller typed size reads back as the size the canvas actually got.
- **Resizing is locked while recording**, which is what keeps every frame of one recording the same size now that the window can be resized at all.
- **A recording stops at the editor's frame limit.** The Windows Board has no bound, which on Linux would hand the editor a project it can load and cannot save. The Board pauses at the limit, as the screen recorder does.
- **The frame count and recording state are always visible**, at the bottom left. Windows shows them only in thin mode, and Linux has no thin mode.
- **Discard is always present and disabled until there are frames.** Windows collapses the button and animates it in on the first stroke; a control that appears where none was is harder to find again than one that was always there.
- **A Record/Pause command sits next to Auto Record**, enabled when Auto Record is off. Windows offers only the Ctrl-hold inversion, which leaves drawing with Auto Record off silently unrecorded.
- **The stop shortcut is F8 and is not configurable.** Windows reads it from `UserSettings.All.StopShortcut`; the Linux Options window lists shortcuts as fixed text and has no store for them.
- **New → Board asks about unsaved work when the recording comes back, not before the Board opens.** Every capture window hands its recording to the editor through one intake, so the Board asks when the webcam asks. Asking late costs nothing, because declining discards only the recording and the project is untouched until then.
- **The brush color is picked from a palette-and-hex dialog.** Avalonia's color picker lives in a package this project does not reference, and the Board needs a color, not a color studio.
- **The pointer is a crosshair rather than a preview of the brush.** Windows renders the stylus tip into a cursor image through a Win32 `.cur`; there is no portable equivalent, and the brush size is visible in its own fields.
- **Tool settings are saved as they change**, not once when the window closes, so a failure to write one can be reported while there is still somewhere to report it. The canvas size, which belongs to the window, is saved when it closes.
- **Options has no Board page.** The button opens the existing Linux Options window. The Board's own settings are edited from its toolbar, except whether Discard asks first, which it shares with the Recorder as the Windows Board shares `NotifyRecordingDiscard`. Options can choose Board as the startup window or the window opened by a tray click.
- **Selection is disabled.** Selecting, moving and transforming strokes is a second authoring model on top of the ink one, and nothing else in the Linux application needs it yet.
- **Thin mode is absent**, because the Linux application has no such setting, and with it the thin-mode caption bar, its close button and its window dragging.
- **Board recordings are not Windows `.stg` projects.** The Board hands the editor the same in-memory project every other Linux source produces, and the editor saves `.stg-linux`.
