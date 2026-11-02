# Webcam recorder

The Webcam window previews a camera and records it straight into the Editor. Open it from StartUp, from the Editor's *File → New → Webcam*, from the tray menu, by setting Options → Start with to "Webcam recorder", or with `--webcam` on the command line.

## Choosing a camera

Cameras come from `/sys/class/video4linux`, and each node is asked through the `VIDIOC_QUERYCAP` ioctl whether it captures video. Modern UVC cameras expose a second node for metadata that carries the same name as the camera; that node fails the check and is not offered. A camera whose card string names the same word twice, as `BP-6500: BP-6500` does, is listed once.

Refresh looks again, which is how a camera plugged in after the window opened becomes available.

## Capture size

FFmpeg lists the camera's formats before the stream opens. The **Capture size** selector shows each usable frame size once, including 1080p and higher when the camera offers them. On first use, the recorder selects the largest mode at or below 1280x720, or the smallest mode if none fits, and saves that concrete choice for the camera. Later choices replace it. A compressed mode such as MJPEG is preferred over a raw one at the same size because USB cameras usually sustain higher frame rates in it. The chosen camera and size are named in the window title and in the corner of the preview.

Each camera's size is stored separately in `WebcamResolutions` in the settings file. The preference follows a camera when Linux provides a stable hardware path and the `/dev/videoN` number changes; without one, that device path is the key. If a saved size is no longer offered, the recorder selects and saves a currently supported initial size. A size that FFmpeg fails to open remains selected, so choose another capture size or press Refresh to retry.

The Scale control resizes the window and nothing else. Recorded frames keep the camera's own resolution, so a recording made at Scale 0.5 still holds full-size frames; resize in the Editor when you want smaller output. See the decision record [Record webcam frames at the camera's capture resolution](../ADRs/20260921-record-webcam-at-camera-resolution.md).

## Recording

Record (F7) starts, and pressing it again pauses. Stop (F8) ends the recording and opens it in the Editor. Discard (F9) throws away what has been recorded so far and leaves the preview running. Discard appears, and Stop becomes usable, only once at least one frame exists. While a recording is running or paused, the device selector, capture size, Refresh, Scale and the frame rate are locked, so the frames in one recording all come from one camera at one size.

The frame rate accepts 1 to 60 and is remembered in the settings file as `WebcamFps`, defaulting to 15. A value outside that range in a hand-edited file is brought back into it when the settings load.

Each frame's delay measures the time actually recorded between it and the next, with paused time excluded, and the last frame keeps the nominal interval. That differs from the Windows recorder, which writes a fixed `1000/fps` for every frame; the Editor's timing tools can override either.

Closing the window with frames recorded hands them to the Editor, as the Windows recorder does. Closing with none returns to StartUp.

## When something goes wrong

Three unusable states read differently, because they need different answers:

| What happened | What the window says |
|---|---|
| No camera is attached | **No camera found** — plug in a webcam, then press Refresh |
| A camera exists but this user may not open it | **Permission needed** — names the device path and the `video` group that grants access |
| FFmpeg could not read the camera | **Camera unavailable** — names the cause in plain words where it recognises one, with FFmpeg's own text under Details |

Each of those offers a Refresh button beside the message and keeps the toolbar's Refresh usable. When a camera was listed but failed to open, the device selector stays usable too, so another camera can be chosen. The causes the message spells out are a camera held by another program, a camera the account may not open, a camera that has been unplugged, and FFmpeg missing altogether; anything else keeps a general line and the Details text.

A camera unplugged or taken by another program mid-preview reports the same way, and a recording in progress stops rather than continuing against a dead stream. If frames cannot be written, recording stops, the message names the failure, and the frames already written are removed rather than handed over half-complete.

A recording also stops itself once it holds as many frames as the Editor will accept at the capture size, which is about a thousand frames at 1280x720. Everything recorded up to that point is kept, and Stop hands it over as usual.

A recording the Webcam recorder never handed to the Editor, because the application ended mid-recording, stays on disk for as long as the retention period in Options, under **Storage**, the same as one the screen Recorder left behind.

## How it captures

One long-lived FFmpeg process per open camera decodes into BGRA frames on a pipe, which the application reads as fixed-size frames. Closing the window, switching cameras, or Refresh all end that process before anything else starts. See the decision record [Capture webcam frames through an FFmpeg V4L2 subprocess](../ADRs/20260921-capture-webcam-frames-through-ffmpeg-v4l2.md).

## Not covered here

Snapshot (manual capture frequency) mode, the Editor's *Insert → Webcam*, system-wide hotkeys, camera controls beyond choosing the device, and PipeWire or desktop-portal camera access are not implemented. Sandboxed packaging will need the PipeWire path before the recorder works inside it.
