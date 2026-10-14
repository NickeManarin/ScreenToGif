# Recorder and capture

Guidance earned while making the Linux Recorder record, and only that. The decision behind the capture path is in `ScreenToGif.Linux/ADRs/20260921-in-process-x11-screen-capture.md`, and the later Recorder decisions (session threading, settings fixed per recording, failure handling, workspace ownership, settings ownership, export timing, the tray icon, and the command bar's departures from Windows) are indexed in `ScreenToGif.Linux/ADRs/README.md`; the user-facing behavior in `ScreenToGif.Linux/docs/recorder.md`, and the deferred capabilities in `ScreenToGif.Linux/docs/recorder-followups.md`. What the code already explains at its own site is listed under [Where the rest is written down](#where-the-rest-is-written-down) rather than repeated here.

## Avalonia imaging outside a running application

Encoding a bitmap needs a registered `IPlatformRenderInterface`. The application installs one at startup and a test host does not, so constructing a `WriteableBitmap` in a test throws `InvalidOperationException: Unable to locate 'Avalonia.Platform.IPlatformRenderInterface'` for code that works in the app.

`Avalonia.Skia.SkiaPlatform.Initialize()` registers one. It needs no new package reference, arriving transitively through `Avalonia.Desktop`, which matters when a manifest forbids touching `Directory.Packages.props`. It is idempotent across repeat calls. Detect whether one is already registered by trying to construct a bitmap, not by asking the locator: `AvaloniaLocator.Current` is internal in Avalonia 12.

`Bitmap.Save(string)` is obsolete in Avalonia 12 and fails a zero-warning build. Use the `Save(Stream, BitmapEncoderOptions)` overload.

## Driving the built application

Never use `pkill -f ScreenToGif.Linux`. More than one agent or session may be driving the application at once, and a pattern broad enough to match the app also matches the shell running `pkill`, which then kills itself; kill by PID. Launching the built binary (`ScreenToGif.Linux/bin/Debug/net9.0/ScreenToGif.Linux`) directly rather than through `dotnet run` gives an exact PID to scope every `xdotool` call to. Under `setsid … &` the PID the shell reports belongs to `setsid`, which exits at once; find your own process by matching the scratch config in its environment: `for p in $(pgrep -f net9.0/ScreenToGif.Linux); do grep -qz "XDG_CONFIG_HOME=$SCRATCH/cfg" /proc/$p/environ && echo $p; done`.

On the user's real session, verify the window under the pointer belongs to your own process before every synthetic click. `xdotool windowactivate` does not guarantee the window is on top, and a click at the right coordinates can land in someone else's window. During this port two clicks reached the user's unrelated editor that way.

A locked session cannot be driven at all: root-window captures come back black and the locker holds a pointer grab, so a click that is verified to be over the right window still does nothing. On GNOME, ask the screensaver rather than logind: `gdbus call --session --dest org.gnome.ScreenSaver --object-path /org/gnome/ScreenSaver --method org.gnome.ScreenSaver.GetActive` returns `(true,)` or `(false,)`, while `loginctl show-session <id> -p LockedHint` was seen reading `yes` on a live desktop. The session also locks again on its own, so check before every drive, not once per session.

Guard key presses as well as clicks. `xdotool key` goes to whichever window has focus, so confirm `_NET_ACTIVE_WINDOW` belongs to your process before each one; during this port an unguarded Tab and Return reached the user's terminal. The mode dropdown's popup is override-redirect and resolves to window 0, so a click guard cannot admit it: open the combobox with a guarded click and pick the item by keyboard instead of relaxing the guard. The export file picker belongs to `xdg-desktop-portal-gnome`, not the application; relax the guard for that one dialog, type the path rather than click, and type slowly, since its path completion garbles fast input.

Close windows with a real `WM_DELETE_WINDOW` client message (a few lines of `ctypes` against libX11). `xdotool windowclose` and `windowkill` destroy the window without running its closing handler, which once made a verifier conclude settings were never written. Run `xwininfo -id` before `import -window <id>` and wrap it in `timeout`: on a vanished id ImageMagick falls back to its interactive picker, which grabs the X server and freezes the desktop. Do not take screenshots while a timing-sensitive recording runs; the capture stalls what is being recorded.

Without a compositor there is no transparency, so under a bare Xvfb the Recorder's see-through viewport renders opaque black. That is the server, not the window. Judging anything about the frame's transparency needs the real session.

## Keyboard focus after a dialog

When a modal dialog is dismissed from the keyboard under Mutter, X input focus stays on the owner's window-manager frame (`WM_CLASS` `mutter-x11-frames`) rather than the owner, so a window driven by shortcut keys stops hearing them until it is clicked. Mouse dismissal does not show it. Avalonia's `Focus()` moves only Avalonia's focus; posting `owner.Activate()` after the dialog's `Closed` is what hands X focus back. Verify with `xdotool getwindowfocus -f` after each keyboard dismissal without re-activating the window between steps, because re-activating is exactly what hides the defect.

## Fields that resize the frame

`NumericUpDown.ValueChanged` fires on every keystroke. Resizing the window from it fights the edit: typing 300 resized to 3 first, the clamp wrote 87 back into the field mid-edit, and the frame swung between its minimum and the full screen. Apply a typed size on Enter or focus loss, and do not write the region back into a field that is being typed in. Disabling a focused field does not reliably raise its focus-loss event, so drop a pending typed size explicitly when a recording locks the fields. A clamp that leaves the window size unchanged produces no resize event to correct the field, so read the region back after applying.

## Sizing a 31-pixel command bar

Avalonia's default `NumericUpDown` spinner takes about 70 pixels, which at that height leaves no room for the value: the field renders as spinners with no number. Set `ShowButtonSpinner="False"` in a compact bar. Its Fluent minimum height also outranks an explicit `Height`, which leaves the field taller than the dropdown beside it; the layout skill's porting reference covers that.

Give a status label its own fixed column with a starred spacer beside it, rather than a starred column of its own, or it takes space from its neighbours until it overlaps them. Both were found by screenshotting the built window rather than by reading the markup, which is the only way either shows up: the markup is valid and the control is present, and the value is simply not on screen.

## Where the rest is written down

Four things cost real time and are explained where they are used, so look there rather than re-deriving them:

| What | Where it is explained |
|------|----------------------|
| Why the capture rectangle comes from the control and not the window position | `ScreenToGif.Linux/Services/Capture/RecorderRegion.cs`, on `Calculate` |
| Releasing an `XImage`, cursor pixel layout, and undefined alpha at depth 24 | `ScreenToGif.Linux/Services/Capture/X11ScreenSource.cs`, at each site |
| Why a root-painting test owns its X server and how it picks a display | `ScreenToGif.Linux.Tests/XvfbSession.cs` |
| Why the X connection opens before `xsetroot` runs | `ScreenToGif.Linux.Tests/X11RecordingIntegrationTests.cs` |
