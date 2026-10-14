# Architecture Decision Records

| Date | ADR | Status | Area |
|------|-----|--------|------|
| 2026-09-21 | [Capture webcam frames through an FFmpeg V4L2 subprocess](20260921-capture-webcam-frames-through-ffmpeg-v4l2.md) | Accepted | Webcam recorder |
| 2026-09-21 | [In-process X11 screen capture for the Linux Recorder](20260921-in-process-x11-screen-capture.md) | Accepted | Recorder |
| 2026-09-21 | [The Linux projects use LF line endings and a final newline](20260921-linux-projects-use-lf-line-endings.md) | Accepted | Build |
| 2026-09-21 | [Record webcam frames at the camera's capture resolution](20260921-record-webcam-at-camera-resolution.md) | Accepted | Webcam recorder |
| 2026-09-21 | [Where the Linux Recorder's command bar departs from the Windows one](20260921-recorder-command-bar-deviations.md) | Accepted | Recorder |
| 2026-09-21 | [The recording session runs on its own thread and the window holds no recording rules](20260921-recording-session-owns-its-thread.md) | Accepted | Recorder |
| 2026-09-21 | [A recording's workspace becomes the Editor's, and an abandoned one is kept for the retention period](20260921-recording-workspace-ownership-and-retention.md) | Accepted | Recorder |
| 2026-09-22 | [A failing recording pauses, keeps its frames, and says so once](20260922-a-failing-recording-pauses-and-keeps-its-frames.md) | Accepted | Recorder, Board |
| 2026-09-22 | [A recording keeps the settings it started with](20260922-a-recording-keeps-the-settings-it-started-with.md) | Accepted | Recorder |
| 2026-09-22 | [The settings store owns recorder settings, and each window commits only what it changed](20260922-recorder-settings-have-one-owner.md) | Accepted | Recorder, Options |
| 2026-09-23 | [A recording stops growing where the Editor can still save it](20260923-a-recording-stops-growing-where-the-editor-can-still-save-it.md) | Accepted | Recorder, Webcam recorder, Board |
| 2026-09-23 | [Export writes exactly the project's frames, with the last frame's delay set explicitly](20260923-export-keeps-every-frame-and-its-delay.md) | Accepted | Editor export |
| 2026-09-23 | [Test collections run one at a time](20260923-test-collections-run-one-at-a-time.md) | Accepted | Build, tests |
| 2026-09-23 | [The application owns the capture-shell lifecycle, and a shell reports how it ended](20260923-the-application-owns-the-capture-shell-lifecycle.md) | Accepted | Application lifetime, capture shells |
| 2026-09-23 | [The tray icon is created once and hidden, never disposed while the application runs](20260923-tray-icon-is-hidden-not-disposed.md) | Accepted | Application lifetime |
