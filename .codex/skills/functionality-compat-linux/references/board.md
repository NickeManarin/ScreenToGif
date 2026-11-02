# Board

Guidance earned while making the Linux Board draw and record. The user-facing behavior and every departure from the Windows Board are in `ScreenToGif.Linux/docs/board.md`; the Board follows the recorder ADRs for the capture-shell lifecycle, the frame limit, failure handling and settings ownership, all indexed in `ScreenToGif.Linux/ADRs/README.md`. What the Recorder already banked about fields that resize a window, compact command bars, and driving the built application is in [recorder.md](recorder.md) and is not repeated here.

## Keep ink geometry out of Avalonia

Every Avalonia geometry operation — widening, combining, hit-testing a `Geometry` — resolves through the platform render interface, which a test host does not have. An eraser built on `CombinedGeometry` exclusion can only be checked by eye. Model strokes as point lists with their drawing attributes in a plain class (`Services/BoardInk.cs`), do smoothing, stroke hit-testing and point erasing as arithmetic on those points, and let the control only turn the result into geometry to draw. Point erasing splits a stroke where the eraser touches its painted path. Sample between sparse input points and include the stroke tip in the collision test: checking only stored centerline points missed both a visible two-point line and the painted edge of a thick stroke while all original Board tests passed.

## Draw each stroke in one operation

A highlighter stroke must not darken where it crosses itself, only where it crosses another stroke. That holds only if each stroke is filled or stroked once. A tip as wide as it is tall is one stroked polyline with round or square caps; an anisotropic tip cannot be a pen, so that stroke is the union of the tip stamped along its path, filled once. Stamps must overlap deeply — a third of the smaller half-extent apart — or the edge reads as a bead chain rather than a line.

## Capturing the canvas

`RenderTargetBitmap.Render(control)` renders the control from its own origin, so a canvas inside a toolbar layout needs no offset correction. Allocating a render target on every tick cost more than the render and showed up directly in the measured frame delays (114–143 ms against a 100 ms interval under Xvfb); keep one target and replace it only when the pixel size or the scaling changes.

## Decisions a test cannot reach

The first Board kept "does a press record", "what does Ctrl do", "did the user decline Discard" and "what does closing hand over" in the window's click handlers. With no headless Avalonia in the test project, none of it could be tested, and three separate verification findings traced back to that one fact. `BoardRecordingSession` owns those decisions and takes the confirmation dialog as an injected `Func<Task<bool>>`, so the decline path is a unit test. The window schedules ticks and mirrors state; it decides nothing.

Two rules fell out of making the transitions explicit. Releasing the pointer pauses only a recording that pressing it started, so a recording started from the Record command survives the next stroke. A window that loses focus never hears Ctrl come up, so deactivation reverts the Ctrl inversion, and a later Ctrl release elsewhere must not invert it back.

## A message that must survive the next refresh

A window that rebuilds its status line from state on every change will overwrite the one message that says why recording stopped, in the same synchronous call that set it. Hold failures and the frame-limit message in a field the refresh prefers over the derived label, and clear it only when the user records again or discards.

## Closing and the hand-over

A window manager's close request raises `Closing` and then `Closed`; a window torn down by the platform raises only `Closed`. End capture on both. Hand the recording over through `ICaptureShellWindow.TakeRecording()`, which the coordinator calls after the window has closed, rather than from a close handler: the first version built its result in `Closing` alone, and a platform teardown that skipped `Closing` lost the recording. Driving this with `xdotool windowclose` is the platform teardown case, not the window-manager one — see [recorder.md](recorder.md) for sending a real `WM_DELETE_WINDOW`.
