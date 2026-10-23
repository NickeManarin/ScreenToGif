# Recorder

The Recorder captures screen content into a project the Editor opens. On X11 it is a frame positioned over what you want to record; everything inside the frame is recorded, excluding its command bar.

On Wayland, Record or Snap opens the desktop's sharing dialog to choose one monitor or window. Screen sharing needs PipeWire, a working ScreenCast portal and GStreamer 1.24 or later with its PipeWire and OpenGL plugins, video library, and a graphics driver supporting DMA-BUF export/import. The PipeWire plugin must support `DMA_DRM` negotiation (verified here with PipeWire/GStreamer plugin 1.6.2); installing a recent GStreamer library alone does not upgrade that plugin. The GPU import/download path preserves source geometry at fractional scales. The software-rendering setting controls the app UI; capture still needs this graphics support. The application may use XWayland for its own windows; capture still goes through the portal.

The entire selected source is recorded at its actual pixel size. Moving Recorder does not move the selection, and the width/height fields show stream dimensions rather than accepting edits. Stop or discard before choosing a new source. The desktop normally asks for consent for each recording. Administrator-protected source reuse can remember an approved source as described below. Canceling or declining leaves Recorder available to try again. Closing Recorder while the dialog is open cancels the request.

Show cursor is negotiated when sharing starts. A desktop that cannot provide the requested hidden or embedded cursor mode reports that limitation and asks you to change the setting. Global hotkeys and arbitrary rectangular cropping are unavailable.

## The command bar

Left to right: window snapping, Options, the capture mode, the frame rate, the region's width and height in screen pixels, the frame count, and the recording commands. The frame rate is labeled fps, fpm, or fph, for frames per second, per minute, or per hour, to match the mode. It is hidden in Manual, which captures only on **Snap**. A typed width or height applies when you press Enter or leave the field.

On X11, the frame's tooltip reports the exact rectangle being recorded, origin included. On Wayland, the source explanation describes portal selection and the fields show negotiated pixel dimensions.

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

**Record** starts, and locks the mode and the frame's size so every frame is the same size. On X11, the frame can still be moved while recording, and what it records moves with it. On Wayland the chosen source remains fixed, independent of Recorder placement. **Pause** stops capturing and re-opens the frame rate, for the next recording rather than this one: a recording keeps the rate it started at, as it keeps the size it started at. **Resume** continues the same recording rather than starting another.

**Stop** ends the recording and opens it in the Editor. The Startup window closes behind it, because the Editor now holds the recording. Stopping with no frames returns the Recorder to idle.

**Discard** throws the recording away and deletes its files. It asks first unless you turn that off in Options. Closing the Recorder while it holds frames asks as well, since closing discards them.

With a pre-start countdown enabled, Record counts down in the command bar before the first frame, giving you time to move the pointer or switch windows.

If sharing ends, the source disappears or its pixel size changes, capture pauses with an explanation and keeps the captured frames. Stop opens them in Editor. Start a fresh recording to select a source again; Resume cannot restore a closed sharing session.

## Shortcuts

`F7` records, pauses, resumes, or snaps. `F8` stops. `F9` discards. They act only while the Recorder window has focus. There are no desktop-wide hotkeys.

## Limits

A recording stops growing at the most frames the Editor can still save at the recorded size. The Editor holds at most 10,000 frames and one billion pixels in all, so the pixel limit usually comes first: 482 frames at 1920x1080, about 32 seconds at 15 fps. The Recorder pauses at that point and keeps everything it captured, so Stop still opens it in the Editor. Resuming pauses again straight away.

Frames are saved on a thread of their own, so a large frame at a high frame rate can arrive faster than it can be written. The Recorder pauses and says so when enough unsaved frames have built up, rather than filling memory until the application is killed. Stop still opens what was captured; a smaller frame or a lower frame rate lets the next recording run on.

On X11, the smallest recordable width is the width of the command bar, which is wider than the Windows recorder's because the mode dropdown and the frame counter share that bar.

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

The Recorder saves the mode and the frame rate the moment you change them, and a mode you pick in Options is saved when Options closes. The frame's size and position are saved when the Recorder closes, if remembering them is on. The Recorder reopens as you left it on X11. Wayland stream dimensions do not overwrite the remembered X11 frame size.

### Reuse an approved Wayland source

Under **Recorder → Capture mode**, **Reuse the approved Wayland recording source** requires administrator authentication to change. It is off by default. The packaged application and `pkexec` supply this setting; a development build without the installed helper shows it disabled with installation guidance. The administrator grant is stored separately from user-editable settings.

After enabling it, start a recording and approve one monitor or window in GNOME's sharing dialog. Allow the desktop to remember the selection if it offers that choice. Future recordings request the same approved source automatically when the desktop supports restoration. A missing source, revoked permission or unavailable restoration opens the ordinary picker. Only the policy helper runs with administrator rights; the GUI and recorder run as the desktop user.

**Forget recording source** clears the app's remembered selection without administrator authentication, so the next recording needs source selection again. Administrator changes apply to the next recording; an existing recording keeps its source. Purging the Debian package removes administrator grants while leaving each user's settings and saved projects alone.

This uses the portal's [persistent-session contract](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.ScreenCast.html). There is no documented ScreenCast administrator policy that supplies a first source without either a previous grant or source selection. The initial source approval remains required; no first-use automation support is claimed.

## Unavailable here

These controls are visible but disabled, each with a tooltip naming what is missing. [`recorder-followups.md`](recorder-followups.md) describes what each would take.

| Control | Missing |
|---------|---------|
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
