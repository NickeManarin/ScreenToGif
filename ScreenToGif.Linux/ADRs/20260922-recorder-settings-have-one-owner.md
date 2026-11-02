# ADR: The settings store owns recorder settings, and each window commits only what it changed

## Status
Accepted

## Area
Recorder, Options

## Context
The Recorder and the Options Recorder page both edit recorder settings, and the capture mode is edited in both. The first version had each window hold its own copy: the Recorder copied the settings when it opened and wrote its fields back when it closed, and Options copied them when it opened and wrote the whole copy back on Ok. Whichever window closed last won. Changing the mode in the Recorder and then pressing its own Options button reverted it; a mode saved in Options during a recording was overwritten when the Recorder closed; and with Options open beside the Recorder, reachable from the tray or from Startup, a frame rate typed into the Recorder was undone by Ok. The remembered frame size is a capture region in physical pixels, while a window's size is in logical units.

## Decision
The settings store (`LinuxSettings.Current` and `settings.json`) is the single owner; each window is an editor that commits to it. Settings are read from the store where they are used, not from a copy taken when a window opened. The Recorder writes the mode and frame rate the moment their controls change, through one serialized write chain so two edits cannot each copy the store before the other lands. Options applies its controls onto a copy of the store taken at save time, so fields it has no control for keep their latest value, and it writes the capture mode only when its own radios changed from what they loaded. Closing the Recorder writes only the frame's size and position, and only when remembering them is on. The remembered size is stored in physical pixels and restored once the window is on a screen and its scaling is known.

Defaults mirror the Windows recorder (15 fps per second, pointer on, no countdown with 3 s when enabled, ask before discarding, remember size and position, 502 by 203), and the tests assert those values rather than whatever the Linux class declares.

## Alternatives Considered
- **Keep per-window copies and flush on close**: — The ordering bug above; the last window to close wins.
- **Make Options modal over the Recorder only**: — Options is also reachable from the tray and from Startup, and making every route modal would not remove the second writer.
- **One process-wide serialized settings service**: — The right shape if more windows start sharing fields; not yet needed for one shared field.

## Consequences

### Positive
- A change made in either window reaches the other and survives the other closing.
- Restart restores what the user last set, at the right size on scaled desktops.

### Negative
- The Recorder's writes and Options' save are still unordered with respect to each other and share one temporary file, so two saves landing within milliseconds could collide; no single user action produces that today.
- Options guards the one shared field by name; the next field both windows edit needs the same treatment.
- A Recorder opened while Options, opened from the tray, saves a new mode keeps showing its old mode until the next change or reopen.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: [A recording keeps the settings it started with](20260922-a-recording-keeps-the-settings-it-started-with.md)
