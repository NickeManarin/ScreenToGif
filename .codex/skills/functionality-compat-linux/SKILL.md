---
name: functionality-compat-linux
description: Port ScreenToGif behavior from Windows/WPF to the Avalonia Linux application while preserving complete user workflows and Linux platform contracts. Use for functionality parity and workflow implementation; use layout-compat-linux instead when the work is only visual geometry or styling parity.
---

# Linux functionality compatibility

Preserve the Windows user contract rather than copying Windows/WPF implementation details. For editor work, extend the existing normalized-PNG model and keep commands complete at the user boundary: define selection rules, validate parameters, preserve work on cancellation or failure, refresh timeline/preview/status state, and include the mutation in shared history when it changes frames, order, or timing.

Windows `.stg` compatibility and a general vector-annotation canvas require foundations beyond an isolated editor command. Do not approximate such a dependency inside the caller; when it is outside the current task, keep the control unavailable and identify the missing subsystem instead of leaving it accidentally inert.

## Task router

Read only the references needed for the active surface:

- First-time component parity or an unclear Windows behavior: [references/porting-workflow.md](references/porting-workflow.md)
- Options, settings persistence, autostart, tray behavior, or application startup/lifetime: [references/options.md](references/options.md)
- Screen capture, the Recorder window, or anything driving the built app against an X server: [references/recorder.md](references/recorder.md)
- The Webcam recorder, camera devices, or any capture window that owns a long-lived FFmpeg child: [references/webcam.md](references/webcam.md)
- The Board, ink strokes and erasing, or a capture window whose decisions need tests without a running Avalonia: [references/board.md](references/board.md)
- Any command that mutates frames, order, timing, selection, or project state: [references/editor-session.md](references/editor-session.md)
- Image transforms or their reusable FFmpeg/history foundation: [references/image.md](references/image.md)
- Fade or directional Slide generation: [references/transitions.md](references/transitions.md)
- Home selection, clipboard, zoom, and action history: [references/home.md](references/home.md)
- Edit sequence and timing operations: [references/edit.md](references/edit.md)
- File and project lifecycle operations: [references/file.md](references/file.md)
- Playback navigation and timing: [references/playback.md](references/playback.md)
- Statistics and live-state projection: [references/statistics.md](references/statistics.md)

References are added as tab implementations earn decision-changing knowledge. A missing reference means there is not yet banked tab-specific guidance; inspect the current implementation and tests rather than inventing a rule.
