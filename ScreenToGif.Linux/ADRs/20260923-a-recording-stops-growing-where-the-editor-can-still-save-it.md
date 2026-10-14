# ADR: A recording stops growing where the Editor can still save it

## Status
Accepted

## Area
Recorder, Webcam recorder, Board

## Context
The Editor refuses a project over 10,000 frames (`EditorResourceLimits.MaximumProjectFrames`) or over one billion decoded pixels (`MaximumImportedPixels`). Saving checks both, so a project that breaks either limit can be edited but never saved. No Windows recorder has a bound, the Board included.

The screen recorder originally stopped growing at the frame count alone. At 1920x1080 the pixel limit arrives at 482 frames, about 32 seconds at 15 fps, so a full-screen recording of a minute reached the Editor as a project it could not save. The first webcam recorder had no bound at all. At its default 1280x720 the same thing happened after about 72 seconds at 15 fps.

Every frame of one recording has the same size: the screen recorder locks its region size when recording starts, the webcam records at the size the stream was opened with, and the Board locks its canvas size for as long as a recording is in progress. So the number of frames the Editor will accept is known before the first frame is taken.

## Decision
A recording stops growing at `EditorResourceLimits.MaximumFramesAt(width, height)`: the most frames of that size that stay within both the pixel limit and the frame limit. Every recorder keeps the frames already taken, and Stop hands them to the Editor.

The recorders then differ in what they do at the bound, and each follows its own failure contract:

- **The screen recorder pauses**, the same answer every recoverable failure gets (see [A failing recording pauses](20260922-a-failing-recording-pauses-and-keeps-its-frames.md)). Resuming pauses again straight away, and the message says to press Stop.
- **The Board pauses**, as the screen recorder does, since a frame it cannot save pauses it too. Starting again at the bound does nothing, and the message stays until the recording is stopped or discarded.
- **The webcam recorder stops.** It seals the recording, keeps the preview running, and leaves Stop (open in the Editor) and Discard available. The webcam recorder has no pause-on-failure contract, and pausing at a bound that resuming cannot lift offers a control that does nothing.

Every message names the frame count, so the user can see the bound came from the capture size.

## Alternatives Considered
- **Keep the frame limit alone**: Hands the Editor a project that loads and cannot be saved, and the user finds out only after editing it.
- **Split the recording into several projects at the bound**: The Editor opens one project at a time and has no way to hand over several; the second part would sit in a workspace nothing points at.
- **Check the budget when the recording is handed over, not while recording**: The user would learn about the limit after recording past it, and every frame beyond it would be wasted work.
- **Bound by storage size as well**: The 4 GB archive limit depends on how well each PNG compresses, which is not known in advance. Save still checks it; a recording that breaks it is rare at sizes the pixel limit allows.

## Consequences

### Positive
- A recording that reaches the Editor can be saved, as far as the pixel and frame limits go.
- One function computes the bound for every recorder, so the limits change in one place.

### Negative
- Large captures are short. At 1920x1080 a recording holds 482 frames; at 3840x2160 it holds 120. Raising the pixel limit is an Editor decision, since the limit protects the Editor's memory, not the recorders'.
- The storage limit is still found only at save time.

## Source
- Session: Linux Webcam recorder implementation, 2026-09-21 to 2026-09-23. The screen recorder's gap was found while preparing the branch squash, 2026-09-23.
- Related: [Record webcam frames at the camera's capture resolution](20260921-record-webcam-at-camera-resolution.md)
