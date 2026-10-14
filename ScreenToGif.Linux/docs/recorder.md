# Recorder

The Recorder captures a rectangle of the screen into a project the Editor opens. It is a frame you position over what you want to record, with a command bar beneath it. Everything inside the frame is recorded; the command bar is not.

Recording needs an X11 session. Under a native Wayland session the Record control is disabled and says so; see [Unavailable here](#unavailable-here).

## The command bar

Left to right: window snapping, Options, the capture mode, the frame rate, the region's width and height in screen pixels, the frame count, and the recording commands. The frame rate is labeled fps, fpm, or fph, for frames per second, per minute, or per hour, to match the mode. It is hidden in Manual, which captures only on **Snap**. A typed width or height applies when you press Enter or leave the field.

The frame's tooltip reports the exact rectangle being recorded, origin included, which is what you need when lining the frame up against another window.

## Capture modes

The capture mode sets when the Recorder takes a frame and what playback delay it stamps on that frame:

| Mode | Captures | Delay stamped on each frame |
|------|----------|-----------------------------|
| Per second | At the chosen frames per second | The measured gap to the next frame, or a fixed `1000 / fps` when **Fixed frame rate** is on |
| Per minute | Every `60000 / fps` milliseconds | 66 ms |
| Per hour | Every `3600000 / fps` milliseconds | 66 ms |
| Manual | One frame per press of **Snap** | The manual playback delay set in Options |

A measured delay is how long that frame was actually on screen, so a recording plays back at the speed it happened rather than at the speed it was asked for. A capture that takes longer than its own interval is kept and carries the real gap; the schedule does not fire twice to catch up.

The last frame of a Per second recording has no successor to measure against, so it keeps the nominal interval.

## Recording

**Record** starts, and locks the mode and the frame's size so every frame is the same size. The frame can still be moved while recording, and what it records moves with it. **Pause** stops capturing and re-opens the frame rate, for the next recording rather than this one: a recording keeps the rate it started at, as it keeps the size it started at. **Resume** continues the same recording rather than starting another.

**Stop** ends the recording and opens it in the Editor. The Startup window closes behind it, because the Editor now holds the recording. Stopping with no frames returns the Recorder to idle.

**Discard** throws the recording away and deletes its files. It asks first unless you turn that off in Options. Closing the Recorder while it holds frames asks as well, since closing discards them.

With a pre-start countdown enabled, Record counts down in the command bar before the first frame, giving you time to move the pointer or switch windows.

## Shortcuts

`F7` records, pauses, resumes, or snaps. `F8` stops. `F9` discards. They act only while the Recorder window has focus. There are no desktop-wide hotkeys.

## Limits

A recording stops growing at the most frames the Editor can still save at the recorded size. The Editor holds at most 10,000 frames and one billion pixels in all, so the pixel limit usually comes first: 482 frames at 1920x1080, about 32 seconds at 15 fps. The Recorder pauses at that point and keeps everything it captured, so Stop still opens it in the Editor. Resuming pauses again straight away.

Frames are saved on a thread of their own, so a large frame at a high frame rate can arrive faster than it can be written. The Recorder pauses and says so when enough unsaved frames have built up, rather than filling memory until the application is killed. Stop still opens what was captured; a smaller frame or a lower frame rate lets the next recording run on.

The smallest recordable width is the width of the command bar, which is wider than the Windows recorder's because the mode dropdown and the frame counter share that bar.

A recording the Recorder never handed to the Editor stays on disk for as long as the retention period in Options, under **Storage**. Nothing rescues it into the Editor for you, but it is not swept away the next time the application starts either.

## Settings

Options, under **Recorder**, holds:

* the capture mode and the frame rate's fixed-delay option;
* the manual playback delay;
* whether the pointer is recorded;
* the pre-start countdown and its length;
* whether Discard asks first;
* whether the frame's size and position are remembered between sessions.

The Recorder's own Options button opens that page directly, and a change made there reaches the Recorder as soon as Options closes.

The Recorder saves the mode and the frame rate the moment you change them, and a mode you pick in Options is saved when Options closes. The frame's size and position are saved when the Recorder closes, if remembering them is on. The Recorder reopens as you left it.

## Unavailable here

These controls are visible but disabled, each with a tooltip naming what is missing. [`recorder-followups.md`](recorder-followups.md) describes what each would take.

| Control | Missing |
|---------|---------|
| Record and Snap under Wayland | A capture backend built on xdg-desktop-portal ScreenCast and PipeWire |
| Snap to window | A window picker built on global input hooks |
| User interaction mode | Global input hooks, to capture on each click or keystroke |
| Cursor following | Global pointer tracking |
| Rebinding the recorder shortcuts | A shortcut editor; the recorder's keys are fixed and act only while it has focus |
| Desktop-wide hotkeys | Global input hooks, to receive a key the recorder does not have focus for |
| Guidelines | An overlay drawn inside the recorder frame |
| Thin mode | A custom window frame with its own drag and resize handling |
| BitBlt, DirectX, save to file, memory cache | Windows capture backends and the frame cache this recorder does not keep |
| Force memory cleanup, remote-desktop performance | The same Windows frame cache and its capture backend switch |

Two Windows rows are settled rather than missing, and are disabled for that reason instead. Discard is always shown while recording or paused, so the row that would turn that off is fixed on. The Old and New interface choice picks between two window implementations the Windows recorder carries and this one does not, so it has nothing to choose between.
