# ADR: Record webcam frames at the camera's capture resolution

## Status
Accepted

## Area
Webcam recorder

## Context
The Windows webcam recorder does not read frames from the camera when recording. It takes a screenshot of the preview region of its own window, so the recorded frame size follows the window's Scale slider (0.4 to 1.5 of the camera size, 0.5 by default). The Linux recorder receives decoded camera frames directly, so it has to choose what size to write. The Linux Editor can resize frames after recording.

## Decision
Recorded frames are written at the resolution the camera stream was opened with. The Scale control only resizes the preview window; it never changes the recorded output. The initial capture size is the largest format the camera offers at or below 1280x720, preferring compressed formats such as MJPEG over raw formats at the same size because USB cameras typically sustain higher frame rates in compressed modes. The Webcam window now lets the user select any listed usable size and remembers a concrete selection for each camera.

## Alternatives Considered
- **Match Windows and record at the preview size**: Keeps output sizes identical across platforms — rejected because it requires a resize stage in the recording path, ties output quality to a window layout decision, and reproduces a side effect of the Windows screenshot implementation rather than a deliberate user contract.
- **Add a resolution picker to the Webcam window**: Deferred in the first implementation because Windows offers no such control. It was subsequently added to support native capture above 720p without changing the recorded-frame decision.

## Consequences

### Positive
- Recording never scales pixels, so the Editor receives the camera's actual output.
- The recording path is a copy plus PNG encode, with no dependency on window size or display scaling.

### Negative
- Recordings from the same camera are larger on Linux than on Windows at the default scale, until the user resizes in the Editor.
- Users who relied on the Windows Scale slider to shrink output must use the Editor instead.

## Source
- Related: 20260921-capture-webcam-frames-through-ffmpeg-v4l2
