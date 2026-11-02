# Linux Wayland compatibility

## Destination

ScreenToGif's existing recording, drawing, editing, and export workflows remain usable on Ubuntu 26.04 Wayland, with clear display/layout behavior and native desktop capture through the screen-sharing portal.

## Context

Display/layout compatibility and consistent return from Editor to StartUp come before Wayland-specific feature work. That is the user's preferred execution order, rather than a structural dependency.

The existing Recorder behavior and platform limits are described in [the Recorder guide](../../ScreenToGif.Linux/docs/recorder.md) and [Recorder follow-ups](../../ScreenToGif.Linux/docs/recorder-followups.md). The original capture choice is recorded in [the X11 capture decision](../../ScreenToGif.Linux/ADRs/20260921-in-process-x11-screen-capture.md).
