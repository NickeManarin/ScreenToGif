# ADR: The application owns the capture-shell lifecycle, and a shell reports how it ended

## Status
Accepted

## Area
Application lifetime, Recorder, Webcam recorder, Board

## Context
The Recorder, the Webcam recorder and the Board are capture shells: windows that replace StartUp while they are open and either produce a recording for the Editor or give StartUp back. Only one may be open at a time, because two recorders on one camera or two screen recorders on one display contend for the same device and the same StartUp slot.

The coordinator that enforced "one shell at a time" was first a field of the StartUp window, so the rule held only while exactly one StartUp window existed. Several entry points reach a shell without going through StartUp: `--webcam` on the command line, the remembered startup window, the tray menu, and the Editor's File > New > Webcam. Each worked around the field differently. `--webcam` built a StartUp window it never showed just to borrow its coordinator. The tray looked for an existing StartUp window first, because creating one gave the application a second coordinator that knew nothing of the first. The Editor's button created the window directly, so two clicks opened two recorders on one camera.

## Decision
`App` owns the single `CaptureShellCoordinator` and is its `ICaptureShellHost`. Every surface opens a shell through `App.OpenCaptureShell(kind, adoption)`; nothing constructs a shell window directly. The adoption says what the Editor does with the shell's recording: replace its project, which every surface asks for, or append to it, which the Editor's Insert group asks for. A second request while a shell is open activates the open one instead of creating another. `OpenCaptureShell` also reports a shell that fails to open, so every entry point gets the same "could not open" message.

When the shell closes, the coordinator asks it how it ended and takes exactly one route:

1. The shell handed its result to the Editor itself (`HandedOffToEditor`): StartUp is closed for good, because the Editor now owns the session.
2. The shell returns a project from `TakeRecording()`: the host adopts it into the Editor, replacing the project or appending to it as the shell was opened for. If adoption fails or throws, the host reports the failure, the recording is disposed and StartUp comes back.
3. The shell captured nothing: StartUp comes back.

Both reporting members have default implementations, so a shell that never produces anything implements neither. The screen recorder sets `HandedOffToEditor`; the Webcam recorder and the Board return their recording from `TakeRecording()`.

## Alternatives Considered
- **Keep the coordinator on StartUp and have other entry points find or create a StartUp window**: This is what the workarounds did. The rule stayed per-window rather than per-application, and every new entry point had to rediscover it.
- **A static coordinator**: Gives one instance without giving it a host. The coordinator needs to hide, restore and close StartUp and to open the Editor, and those belong to the application object that already tracks its windows.
- **Let each shell open the Editor itself (one hand-off style only)**: This is what the screen recorder does. It means every shell repeats the Editor intake and its unsaved-work confirmation. The webcam's style of returning a project leaves that to one host method. Both styles exist today; see Consequences.

## Consequences

### Positive
- "One shell at a time" holds no matter how many StartUp windows exist or which surface asked.
- A new entry point is one call, and it gets the single-shell rule, the open-failure message and the close routing for free.
- A failure while adopting a recording is reported from a window-closed notification instead of escaping it, and the recording is released rather than leaked.

### Negative
- Two hand-off styles coexist. The screen recorder opens the Editor itself through `MainWindow.OpenRecordingAsync` and sets `HandedOffToEditor`. The webcam and the Board return a project that `App.AdoptRecording` passes to `MainWindow.AdoptRecordedProjectAsync`, which asks before replacing unsaved work, or, for a shell opened to append, to `MainWindow.InsertRecordedFramesAsync`, which adds the frames as one undoable step. Moving the screen recorder onto the `TakeRecording` route, so that route is the only one, is an open follow-up.
- The coordinator's contract is called from a window-closed notification, so both host callbacks it relies on (`AdoptRecording`, `ReportShellFailure`) must report their own failures and never throw.

## Source
- Session: Linux Webcam recorder implementation and its rebase onto the screen recorder, 2026-09-21 to 2026-09-23 (handoff `~/.manifest-dev/handoffs/handoff-20260923-103704.md`)
- Session: Linux Board recorder and its rebase onto the screen recorder and webcam, 2026-09-21 to 2026-09-23, which added the adoption (execution log `~/.manifest-dev/logs/do-log-20260921-173500.md`)
- Related: [A recording's workspace becomes the Editor's](20260921-recording-workspace-ownership-and-retention.md)
