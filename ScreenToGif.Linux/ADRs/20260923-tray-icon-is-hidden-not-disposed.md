# ADR: The tray icon is created once and hidden, never disposed while the application runs

## Status
Accepted

## Area
Application lifetime

## Context
The notification icon is optional and off by default. Options rebuilt it on every Ok by disposing the current `TrayIcon` and creating a new one. On Linux, Avalonia's D-Bus tray implementation (`DBusTrayIconImpl`) responds to disposal by cancelling a watch task nobody observes, and the resulting `TaskCanceledException` reaches the dispatcher unhandled and aborts the process. With the icon shown, every Ok in Options killed the application and every open window, including a Recorder in the middle of a recording.

## Decision
`App.RefreshTrayIcon` creates the icon the first time it is shown and afterwards only toggles `IsVisible` to match the setting. The icon is never disposed while the application is expected to keep running.

## Alternatives Considered
- **Catch the exception**: — It is raised on the dispatcher from inside Avalonia, not from a call the application makes, so there is no call site to wrap.
- **Rebuild the icon only when its content changes**: — Nothing about the icon changes with settings; its click handler reads the current settings when clicked.

## Consequences

### Positive
- Saving Options cannot end the process, whatever the tray setting.

### Negative
- Tray visibility now depends on the platform implementation honoring `IsVisible`; hiding the icon from Options was not exercised on the real desktop.
- The explicit Exit command still disposes the icon; that path ends the process anyway, but it is the same call.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: none
