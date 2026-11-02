# ADR: A recording keeps the settings it started with

## Status
Accepted

## Area
Recorder

## Context
Several settings shape a recording: capture mode, frame rate, fixed frame rate, pointer, manual delay, and the frame's size. The Recorder lets some of them be edited while a recording is paused, and Options can change them while a recording runs. The Windows recorder freezes most of them but applies a frame-rate edit made while paused when the recording resumes. Separately, a project whose frames are not all one size is not one the Editor can open, and the session originally read the region live on every capture, so a window manager, a restored placement or a drag of the frame's edge could change the size of every later frame.

## Decision
A recording runs on the settings captured when it starts, for its whole life. The frame rate field re-opens while paused, but the value it holds applies to the next recording, not the resumed one; the status line says so. The frame's size is taken once, at the moment Record is pressed (which is also where a countdown begins, so both routes into capturing take it in one place), and held by the session; the window's resize is also locked while recording. The frame's origin stays live, so moving the frame still moves what is recorded. The mode and rate fields stand still while a recording runs and are seeded again from the store when it ends, so they always describe the next recording.

## Alternatives Considered
- **Apply a paused frame-rate edit on resume, as Windows does**: — Mixes two cadences in one recording and makes the delay accounting depend on when an edit landed; the benefit is small because a new recording is one keypress away. This is the one deliberate divergence from Windows here and is cheap to reverse.
- **Lock size only in the window**: — Disabling the fields and the resize handle does not stop other code paths moving the frame, and the session read the size live, so frames could still change size mid-recording.
- **Freeze the origin too**: — Moving the frame over what is being recorded is useful, and a moving origin cannot break the project, since every frame keeps its size.

## Consequences

### Positive
- Every recording is a single consistent project the Editor can open.
- The delay rules have one set of inputs per recording, which is what the tests assert.

### Negative
- A user who pauses, changes the rate and resumes gets the old rate, unlike Windows; the status line has to explain it.
- Dragging the frame partly off-screen while recording faults the capture with a message rather than clamping the region.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: [The recording session runs on its own thread](20260921-recording-session-owns-its-thread.md)
