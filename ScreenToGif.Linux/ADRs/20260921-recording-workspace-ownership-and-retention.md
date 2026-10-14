# ADR: A recording's workspace becomes the Editor's, and an abandoned one is kept for the retention period

## Status
Accepted

## Area
Recorder

## Context
Recorded frames are written into an `EditorWorkspace` batch. Stop has to give them to the Editor, and several things can go wrong around that moment: the Editor can fail to open the recording, the user can press Stop twice while the first Stop drains the encoder, the Recorder can close, or the process can die. Separately, the application's scavenger deletes stale workspaces from the shared temporary root, and before this work a workspace whose owner process was gone was deleted on the next launch, which was safe while workspaces held only work derived from a project file the user still had.

## Decision
The recording's workspace becomes the Editor's workspace on hand-off, with no copy, through the Editor's existing project-replacement path. Ownership moves in one place (`RecordingHandOff`): the Recorder reports `HandedOffToEditor` only after the Editor has taken the project, so a hand-off that fails part way deletes the recording and brings Startup back rather than leaving no window. Once a session has handed its frames over, Stop and Discard on it do nothing, because those frames are no longer its to delete; a second Stop arriving during the first used to delete the batch the first had just handed over.

The scavenger deletes a workspace on sight only when its owner claim names a process known to be gone; an unreadable claim is the ordinary shape of a workspace another instance is still creating, so it gets grace. A workspace that holds recorded frames is kept for the Options retention period whoever owned it, because a Recorder that died before handing over leaves the only copy there is.

## Alternatives Considered
- **Copy the frames into a new Editor workspace**: — Doubles disk use for long recordings and adds a failure point, for no ownership benefit.
- **Guard the second Stop at the button**: — Would need a separate latch per command; guarding in the session also covers Discard and close arriving during a Stop.
- **Keep deleting dead-owner workspaces on next launch**: — Destroys the only copy of a recording whose Recorder crashed.

## Consequences

### Positive
- No path through Stop, Discard, close, or a failed hand-off deletes frames the Editor owns, or strands frames nobody owns.
- A crashed recording can still be recovered from the temporary root until retention expires.

### Negative
- The startup sweep runs only when Options' remove-old-projects setting is on, and the exit sweep that the delete-cache-on-close setting triggers uses no retention, so with that setting on an abandoned recording does not survive a normal exit.
- Nothing yet offers to reopen an abandoned recording; recovering one means finding it in the temporary root.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: [A failing recording pauses, keeps its frames, and says so once](20260922-a-failing-recording-pauses-and-keeps-its-frames.md)
