# ADR: Capture webcam frames through an FFmpeg V4L2 subprocess

## Status
Accepted

## Area
Webcam recorder

## Context
The Linux Webcam window was a visual scaffold: no camera discovery, no preview, no recording, and no hand-off to the Editor. Implementing it needs a way to read live frames from a camera in an Avalonia application. Avalonia has no camera API. The Linux application already requires FFmpeg at runtime for import and export, and every media boundary in the Linux editor goes through the `IFfmpegTool` seam. The target sessions are unsandboxed X11 or Wayland desktops launched from a per-user desktop entry, so the process can open `/dev/video*` directly when the user has device access. On the reference machine `ffmpeg -f v4l2` produced exact raw BGRA frame streams at 640x480 and 1280x720 and listed the camera's formats and sizes, while the installed FFmpeg has no PipeWire input device.

## Decision
Capture frames by running one long-lived FFmpeg process per open camera with the `v4l2` input and a `rawvideo` BGRA output on standard output. The application reads fixed-size frames from that pipe, renders them for the live preview, and samples the latest frame on the recording clock. Camera nodes are enumerated from `/sys/class/video4linux` and filtered with the `VIDIOC_QUERYCAP` ioctl so that metadata nodes, which share the camera's name, are not offered as cameras. The capture format and size are chosen from FFmpeg's `-list_formats` output before the stream starts, so the pipe's frame size is known up front.

## Alternatives Considered
- **Direct V4L2 streaming through P/Invoke (mmap buffers, format negotiation, MJPEG decoding)**: Avoids a child process and gives the lowest latency — rejected because it reimplements format negotiation and MJPEG/YUYV conversion that FFmpeg already provides, and it would be the largest native surface in the Linux project for one feature.
- **PipeWire camera access through the desktop portal**: The right path for Flatpak or Snap packaging and for sessions that deny raw device access — rejected for now because this branch does not ship a sandboxed package, the installed FFmpeg cannot consume PipeWire streams, and it would add a D-Bus and PipeWire client dependency; it remains the migration path if packaging changes.
- **GStreamer bindings**: Mature camera pipeline with preview sinks — rejected because it adds a second media framework beside the already required FFmpeg.
- **Copy the Windows implementation, which screenshots the preview area of its own window**: Rejected because the Linux application has no desktop capture, and the compatibility rules forbid approximating a missing subsystem inside the caller.

## Consequences

### Positive
- No new runtime dependency; FFmpeg is already mandatory and configurable through the existing settings.
- Format and pixel conversion happen in FFmpeg, so YUYV, MJPEG, and other camera formats all arrive as the same BGRA frames.
- The frame reader is testable without a camera by pointing the same reader at FFmpeg's `lavfi` test source.

### Negative
- Preview start-up carries FFmpeg's process launch cost, about one second on the reference machine.
- Raw BGRA over a pipe is bandwidth-heavy at large sizes, which is why capture is capped at 1280x720 unless the camera offers nothing smaller.
- Sandboxed packaging will need the PipeWire path before the webcam recorder works there.

## Source
- Related: 20260921-record-webcam-at-camera-resolution
