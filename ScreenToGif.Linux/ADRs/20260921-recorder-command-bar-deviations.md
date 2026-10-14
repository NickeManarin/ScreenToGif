# ADR: Where the Linux Recorder's command bar departs from the Windows one

## Status
Accepted

## Area
Recorder

## Context
The Linux Recorder follows the Windows Recorder's layout (`ScreenToGif/Windows/Recorder.xaml`): a transparent viewport with a 1 px border and a 31 px command bar with the Windows control order. Some Windows elements rely on subsystems the Linux build does not have, some layout values do not survive Avalonia's controls at that height, and the user's standing direction was not to imitate Windows where its choices were questionable.

## Decision
These departures are deliberate:

- The capture mode is a dropdown in the bar rather than a separate frequency button, and the frame count and pre-start countdown show in the bar.
- Discard is visible whenever a recording is running or paused, and closing the window with frames asks first.
- The bar is wider than Windows (684 against 530 minimum) because the dropdown and the frame count share it, and the bar's width is the smallest recordable width.
- The numeric fields have no spinners, which at 31 px left no room for the value, and they are the dropdown's height.
- A typed width or height applies on Enter or when the field loses focus, not on every keystroke, and the field then shows the size actually recorded.
- The frame-rate field, indicator and unit are hidden in Manual and the unit reads fps, fpm or fph by mode, as Windows does.
- Being paused is shown by the Resume button and the re-enabled frame-rate field rather than the status line, which spends its one short line on the count.
- Recorder shortcuts are fixed at F7 (record, pause or snap), F8 (stop) and F9 (discard) and act only while the Recorder has focus, because there is no shortcut editor and no global key grab.
- Every Windows control whose subsystem is missing stays in place, disabled, with a tooltip naming what is missing; under a Wayland session Record and Snap are disabled that way.
- Confirmation dialogs focus Cancel and close on Escape, and return keyboard focus to the Recorder so the shortcuts keep working.

## Alternatives Considered
- **Pixel-faithful copy of the Windows bar**: — The Windows frequency button and spinners do not fit Avalonia's controls at 31 px, and a copy would carry over controls with nothing behind them.
- **Hide unavailable controls**: — Users moving from Windows would not know the capability exists or why it is missing.

## Consequences

### Positive
- Every control either works or says why it does not.
- The bar works from the keyboard alone.

### Negative
- The trailing group runs Discard, Stop, Record where Windows puts Stop last.
- The 84 px status label truncates its two longest messages and has no tooltip.
- A typed size that has not been applied is dropped when F7 starts a recording, while clicking Record applies it first.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: [In-process X11 screen capture](20260921-in-process-x11-screen-capture.md)
