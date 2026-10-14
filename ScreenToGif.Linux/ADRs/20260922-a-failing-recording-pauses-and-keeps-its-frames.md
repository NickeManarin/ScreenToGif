# ADR: A failing recording pauses, keeps its frames, and says so once

## Status
Accepted

## Area
Recorder, Board

## Context
A recording can fail part way: the X server refuses an image, the disk fills, the workspace becomes unwritable, the project reaches the Editor's 10,000-frame cap, or the PNG encoder falls behind. Encoding runs off the capture thread, so an encoder that cannot keep up does not slow capture down; it queues. Measured on the development machine, a 1080p frame of ordinary desktop content takes longer to encode than the interval 15 fps allows, so the backlog only grows until the process is killed and takes every frame with it. Early versions discarded frames on a write failure, repeated the same error on every tick, and left a Manual recording with no way to take another frame after one transient failure.

## Decision
Every recoverable failure gets the same answer: stop capturing, keep every frame already written, and report once, naming the cause in the writer's or source's own words. A paced recording pauses, so the user can fix the cause and resume; resuming clears the one-report latch, because it is the user asking to try again. A Manual recording stays in its stage, because Paused is a stage it could not leave (Snap is unavailable there), and every Snap clears the latch. Stop always hands over the frames already on disk, even after a failure.

The same answer covers two limits. At `EditorResourceLimits.MaximumFramesAt` for the locked region size, the most frames the Editor can still save (see [A recording stops growing where the Editor can still save it](20260923-a-recording-stops-growing-where-the-editor-can-still-save-it.md)), the recording pauses. When the writer reports more than 512 MB handed to it and not yet written, the recording pauses and says frames are arriving faster than they can be saved. The writer reports its backlog; the session decides.

The Board gives the same answer. A frame it cannot save pauses the recording and keeps every frame already taken, and its status keeps the message until the user records again or discards, so the next refresh cannot replace the one line that says why recording stopped. At the frame limit it pauses and cannot resume.

## Alternatives Considered
- **Bound the encoder queue and pace capture to it (back-pressure)**: — A pacing policy, trading real per-frame delays for a slower cadence, that the capture decision did not choose and the port had no grounds to choose. Pausing reuses an answer the session already gives a full disk.
- **Drop frames when behind**: — Silently changes the recording's timing and content.
- **Stop the recording on failure**: — Throws away the chance to fix the cause and continue, and on a Manual recording ends the session outright.
- **Report every failed tick**: — A capture loop that keeps failing floods the user.

## Consequences

### Positive
- No failure mode destroys frames already captured; the worst outcome is a shorter recording.
- One behavior across every failure is one thing to document and test.

### Negative
- A lost X connection is not in this set: Xlib's I/O error handler exits the process before managed code runs. The recording's workspace survives to the next launch (see [Recording workspace ownership and retention](20260921-recording-workspace-ownership-and-retention.md)), but nothing announces the loss.
- The 512 MB budget is a fixed constant, not a setting; at high frame rates it is about a second of headroom.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: [In-process X11 screen capture](20260921-in-process-x11-screen-capture.md)
