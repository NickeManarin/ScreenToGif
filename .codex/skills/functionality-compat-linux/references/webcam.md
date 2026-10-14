# Webcam recorder

Guidance earned while making the Linux Webcam recorder record, and only that. The capture decisions are in `ScreenToGif.Linux/ADRs/20260921-capture-webcam-frames-through-ffmpeg-v4l2.md` and `…/20260921-record-webcam-at-camera-resolution.md`; how every capture shell is opened and closed is in `…/20260923-the-application-owns-the-capture-shell-lifecycle.md`; the recording bound is in `…/20260923-a-recording-stops-growing-where-the-editor-can-still-save-it.md`. User-facing behavior is in `ScreenToGif.Linux/docs/webcam.md`. Driving the built application against an X server is covered in [recorder.md](recorder.md) and applies here unchanged.

## When the camera seems to be missing

Read the kernel log before concluding the device is unplugged or that a sandbox hides it: `journalctl -k | grep -i -E 'uvc|USB disconnect'`. The reference camera (BP-6500, `1b3f:1167`) sits behind a dock's USB 2.0 hub that dropped and re-enumerated about nine times in one day, taking `/dev/video*` with it for minutes at a time. A check that fell into one of those gaps was once reported as "no camera attached" while the user had just used it in a browser.

Access comes from logind's per-seat ACL on the device node (`getfacl /dev/video0` shows `user:<name>:rw-`), not from membership in the `video` group. A user who is not in `video` can still open the camera from a local session; a remote or non-seat session may not get the ACL.

## Testing without and with a camera

FFmpeg's `lavfi` test source (`testsrc=size=WxH:rate=N`) runs through the same `CameraFrameStream` reader as a real device, so frame framing, distinctness, startup failure and child exit on disposal are all testable with no camera. Tests that need real hardware use `CameraFactAttribute` and are skipped at discovery unless `SCREENTOGIF_CAMERA_TESTS=1`.

To reproduce a "camera in use" failure, hold the device with a second process: `ffmpeg -nostdin -loglevel error -f v4l2 -i /dev/video0 -f null - &`. UVC cameras accept one streaming client at a time. Kill that process and press Refresh to check recovery.

To reproduce a missing FFmpeg, set `FfmpegPath` in the scratch `settings.json`; see [options.md](options.md) for why the environment variable does not work.

## Failure modes a capture window will hit again

These were each found late, by review or by a live drive, and none of them shows up in a happy-path test.

- **Opening a camera awaits more than once before the stream is installed**: it closes the previous stream, discovers devices, then lists the chosen camera's formats through FFmpeg. A window closed between two awaits still installed a stream when the open resumed, leaving an FFmpeg child holding the camera for the life of the application, with no window left to release it. Check whether the window has closed after every await in an open path, not only at its start.
- **Two opens can overlap.** Refresh and the device selector both start an open. Without one gate held across the whole open, each installed a stream over the other, orphaning one FFmpeg child and filling a preview bitmap sized for one format with a frame of another. Take one gate and disable every control that can start an open while it is held.
- **A stream that dies must be released before the failure is shown.** A dead stream left installed made Record available again, and the recorder then recorded the last frame over and over from a camera that was gone, and handed that to the Editor as a real recording.
- **Control enablement must come from one place.** Three methods each set some buttons and read each other's flags, which let Record go live during an open with no stream behind it. `WebcamControls.For(...)` in `Services/WebcamPresentation.cs` derives every control from the status, the recording stage, whether an open is in flight, whether a stream exists and whether a recording is waiting, and the window applies that one answer. It is a plain record, so the combinations that used to disagree are table tests.
- **A long-lived FFmpeg child needs `-nostdin`.** Without it, an application started from a terminal shares that terminal with FFmpeg, which reads keys from it. Short-lived FFmpeg calls end before this matters.
- **Every entry point opens the window through `App.OpenCaptureShell`.** The Editor's File > New > Webcam once created the window directly, and two clicks opened two recorders on one camera.

## Where the rest is written down

| What | Where it is explained |
|------|----------------------|
| Why a camera whose card string reads `BP-6500: BP-6500` is listed once | `ScreenToGif.Linux/Services/CameraDevices.cs` |
| Why a dropped or late capture advances the schedule instead of bursting | `ScreenToGif.Linux/Services/WebcamRecordingSession.cs` and its tests |
| How a failure is classified into in use, no permission, unplugged or FFmpeg missing | `WebcamStatus` in `ScreenToGif.Linux/Services/WebcamPresentation.cs`, and `ScreenToGif.Linux/docs/webcam.md` |
