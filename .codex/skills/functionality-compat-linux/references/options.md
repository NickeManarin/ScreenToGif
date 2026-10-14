# Options and application settings

Use this reference for settings persistence, Linux startup integration, tray behavior, and settings that alter editor workflows.

## Port only real contracts

Render Windows settings whose backing Linux subsystem is absent in-place but disabled with the established tooltip convention. Task automation, global hotkeys, cloud providers, update and translation services, and Gifski remain unavailable until their underlying subsystem exists. Do not persist inert choices.

Settings with an existing Linux seam should drive it rather than stop at UI state. Current seams include theme selection, single-instance startup, software rendering, tray visibility and left-click behavior, close/delete/discard confirmations, playback frame dropping, history capacity, stale-workspace cleanup, FFmpeg discovery, and the Recorder page (capture mode, fixed frame rate, manual delay, pointer, pre-start, discard confirmation, and remembered size and position).

Avalonia's Linux `TrayIcon` exposes a generic `Clicked` event, not distinct double-left and middle-click events. Keep those interaction rows disabled unless the platform layer gains an observable contract for them. Tray-enabled keep-open behavior requires `ShutdownMode.OnExplicitShutdown` plus an explicit decision when the last visible window closes.

Create the tray icon once and toggle `IsVisible` afterwards. Disposing Avalonia's D-Bus `TrayIcon` while the application runs, for example to rebuild it when Options closes, makes `DBusTrayIconImpl.WatchAsync` throw a `TaskCanceledException` nothing observes, and the process aborts with every window and any recording in progress. It only reproduces with the icon shown, which is off by default, so a default-settings drive never finds it. The exit path still disposes the icon; keep that off any path the process must survive.

## Persistence and startup ordering

Store settings at `$XDG_CONFIG_HOME/ScreenToGif/settings.json`, falling back to the platform ApplicationData directory when `XDG_CONFIG_HOME` is unset. Write a sibling `.tmp` file and atomically replace the destination; remove a leftover temporary file on failure. Malformed or unreadable JSON falls back to safe defaults.

When more than one window edits settings, each commits only the fields it changed, onto the store as it is at save time. A window that copies the settings when it opens and writes the whole copy back when it closes silently reverts anything another window saved meanwhile, and that is reachable: Options opens non-modally from the tray, and a modal Options opened from Startup does not disable the Recorder. A field both windows edit, such as the capture mode, is written by Options only when its own control changed from the value it loaded. The writers are still unordered with respect to each other and share one `.tmp` path, so do not add a save that can run concurrently with another without serialising them.

Apply settings that affect process construction before Avalonia starts. In particular, acquire the single-instance guard and select X11 software rendering before `StartWithClassicDesktopLifetime`; applying either from the Options window is too late for the current process. Map the configured FFmpeg executable through `SCREENTOGIF_FFMPEG` and derive `SCREENTOGIF_FFPROBE` from an absolute FFmpeg path so existing media services remain the single resolution seam. Because `LinuxSettings.ApplyEnvironment()` writes both variables from the saved `FfmpegPath` at startup, setting `SCREENTOGIF_FFMPEG` in the launching environment has no effect on the running application; a missing or wrong FFmpeg is reproduced by writing `FfmpegPath` into the scratch `settings.json` before launch.

Theme changes use `Application.RequestedThemeVariant`. The current Linux palette maps both Light and Medium to Avalonia Light, Dark to Dark, and Follow system to Default; do not imply a distinct Medium theme until one exists.

## XDG autostart ownership

Use `$XDG_CONFIG_HOME/autostart/screentogif.desktop` and mark app-owned entries with `X-ScreenToGif-Managed=true`. Refuse to overwrite an entry at that path without the marker, and delete only a marker-owned entry.

Quote each desktop `Exec` argument by escaping backslash, double quote, backtick, and dollar. When `Environment.ProcessPath` is the `dotnet` host, include the absolute managed assembly path as the next quoted argument; persisting only `dotnet` creates a non-starting entry.

## Verification

Unit tests should round-trip every supported setting, recover from corrupt JSON, enable and remove an owned autostart entry, preserve an unmanaged entry, and cover `Exec` quoting. Then launch the actual application and verify startup settings, every enabled Options page, tray/lifetime behavior, and an editor setting through its real workflow. Compilation alone misses XAML initialization-order failures.
