# ADR: The recording session runs on its own thread and the window holds no recording rules

## Status
Accepted

## Area
Recorder

## Context
[In-process X11 screen capture](20260921-in-process-x11-screen-capture.md) makes capture a platform-neutral, clock-driven state machine (`RecordingSession`). Something has to drive it. Grabbing a 1080p region costs about 15 ms per frame, and the Recorder window is drawn over the region being captured, so work on the UI thread visibly stalls the frame the user is watching. The Windows recorder's window also carries recording logic in its code-behind, which makes stage and availability rules hard to test without a display.

## Decision
`RecordingLoop` owns one dedicated thread. Every command (record, pause, snap, stop, discard) reaches the session on that thread through a queue, so the session itself stays single-threaded and needs no locks. `RecordingSession.Tick` performs whatever the injected clock says is due and returns when it should next be called; it never waits, so a test drives it directly in irregular clock steps with no real time passing.

State crosses back to the UI as an immutable `RecordingStatus` reading, so the window never reads a session mid-transition. `RecorderWindow` holds no recording rules: it posts commands and renders the reading, including which commands are available in each stage and mode.

## Alternatives Considered
- **Drive the session from a UI-thread timer**: simplest wiring. — A 15 ms grab on the UI thread stalls rendering of the frame being recorded, and grows with region size.
- **A thread-safe session called from both threads**: — Every transition needs locking, and the stage contract becomes a concurrency problem rather than a sequence of steps a test can drive.
- **Keep the rules in the window, as Windows does**: — Availability and timing rules would be reachable only through a launched window, so they could not be unit-tested.

## Consequences

### Positive
- The stage, availability and delay contract lives in one class that tests drive with a fake clock and fake source.
- Orderings that can only happen across threads, such as a second Stop arriving while the first drains, are testable against the real loop and queue.

### Negative
- A command is applied when the capture thread next runs, not synchronously; the window must render from the reading, not assume its command took effect.
- Stop and Discard block on draining the encoder, which on a long recording takes seconds while the controls stay live, so the session must tolerate repeated commands (see [Recording workspace ownership and retention](20260921-recording-workspace-ownership-and-retention.md)).

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: [In-process X11 screen capture](20260921-in-process-x11-screen-capture.md)
